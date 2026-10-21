--Vert
#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec4 aColor;
layout(location = 3) in vec4 aNormal;

out vec2 vUV;
out vec3 vColor;
out float vFog1;
out float vFog2;
out vec3 vNormal;
out vec3 vPos;
out vec3  vBase; 
out float vAO;
flat out float vAtlasFlag;
flat out int vDamage;
out vec3 vLocal;
out vec3 vLocalNormal;

uniform mat4 model;
uniform mat3 normalMatrix;

#define FOG_LN2 1.44269504

float fogFac(vec3 p, float k)
{
    float d2   = dot(p - CAMERA_POS, p - CAMERA_POS);   
    float kd2  = k * k * d2;                            
    return exp2(-(FOG_DENSITY) * kd2 * FOG_LN2);       
}

void main()
{
    vec4 wp=vec4(aPosition,1.0)*model;
    gl_Position=wp*VIEW*PROJECTION;

    vUV         = aTexCoord;
    vColor      = aColor.rgb;
    vPos        = wp.xyz;
    vNormal     = (aNormal.xyz * 2.0 - 1.0)*normalMatrix;
     vBase     = (AMBIENT + aColor.rgb) * aNormal.w;
    vFog1       = fogFac(wp.xyz,1.0);
    vFog2       = fogFac(wp.xyz,0.5);
    vAO         = aNormal.w;
    int flags   = int(round(aColor.a * 255.0));
    vAtlasFlag  = flags >= 128 ? 1.0 : 0.0;
    vDamage     = flags & 7;
    vLocal      = aPosition;
    vLocalNormal = aNormal.xyz * 2.0 - 1.0;
}

--Frag
#version 330 core
#include "includes/lighting.fs"
#include "includes/gbuffer.glsl"

in vec2  vUV;
in vec3  vColor;
in float vFog1;
in float vFog2;
in vec3  vNormal;
in vec3  vPos;
in float vAO;
in vec3  vBase;
flat in float vAtlasFlag;
flat in int vDamage;
in vec3  vLocal;
in vec3  vLocalNormal;

layout(location = 0) out vec4 gColor;

uniform vec4  color      = vec4(1,1,1,1);
uniform sampler2D texture0;
uniform sampler2D textureAtlas;
uniform sampler2D crackTexture;

const float CRACK_STAGES = 6.0;
const float CRACK_VARIANTS = 8.0;

uint crackHash(ivec3 c)
{
    uint h = uint(c.x) * 73856093u ^ uint(c.y) * 19349663u ^ uint(c.z) * 83492791u;
    h ^= h >> 13u;
    h *= 0x5bd1e995u;
    h ^= h >> 15u;
    return h;
}

vec3 applyCracks(vec3 base)
{
    if (vDamage == 0) return base;

    vec3 n = abs(vLocalNormal);
    int axis = n.x > n.y && n.x > n.z ? 0 : (n.y > n.z ? 1 : 2);
    float side = axis == 0 ? vLocalNormal.x : (axis == 1 ? vLocalNormal.y : vLocalNormal.z);
    uint face = uint(axis * 2 + (side > 0.0 ? 1 : 0));

    vec2 coords = axis == 0 ? vLocal.zy : (axis == 1 ? vLocal.xz : vLocal.xy);
    vec2 c = fract(coords);

    uint h = crackHash(ivec3(floor(vLocal - vLocalNormal * 0.5)));
    float variant = float(h & 7u);
    uint orient = ((h >> 3u) ^ face) & 7u;
    if ((orient & 1u) != 0u) c.x = 1.0 - c.x;
    if ((orient & 2u) != 0u) c.y = 1.0 - c.y;
    if ((orient & 4u) != 0u) c = c.yx;

    vec2 uv = vec2((float(vDamage - 1) + c.x) / CRACK_STAGES, (variant + c.y) / CRACK_VARIANTS);
    float a = texture(crackTexture, uv).a;
    float luminance = dot(base, vec3(0.299, 0.587, 0.114));
    float shade = mix(1.0, luminance > 0.35 ? 0.6 : 1.35, a);
    return base * shade + vec3(luminance > 0.35 ? 0.0 : 0.04 * a);
}


float near = 0.1; 
float far  = 100.0; 
  
float LinearizeDepth(float depth) 
{
    float z = depth * 2.0 - 1.0; // back to NDC 
    return (2.0 * near * far) / (far + near - z * (far - near));	
}


void main()
{
    vec4 baseTex = texture(texture0,vUV);
    if(baseTex.a < 0.1) discard;

    vec3 N = normalize(vNormal);
    vec3 V = normalize(CAMERA_POS - vPos);
    vec3 base = applyCracks(baseTex.rgb);

    vec3 light = 
    //accumulateDirLights(N,V,base) +
                 accumulatePointLights(N,V,vPos,base) +
                 accumulateSpotLights(N,V,vPos,base);

    vec3 col  = (base*vBase) + light;

    vec4 fogged1 = fogMix(vec4(col ,baseTex.a),vFog1, FOG);
    vec4 fogged2 = fogMix(vec4(base,1.0),vFog2, FOG);

    vec4 atlas   = texture(textureAtlas,vUV);
    float aMix = (vAtlasFlag==0.0)?0.0:atlas.a;
    vec3 mixed   = mix(fogged1.rgb,fogged2.rgb,aMix);

    gColor  = vec4(mixed,fogged1.a);
   // gColor  =  vec4(vec3(LinearizeDepth(gl_FragCoord.z)/100), 1.0);
    writeGBuffer(N * mat3(VIEW), aMix > 0.5 ? GBUFFER_MASK_NONE : GBUFFER_MASK_WORLD);
}
