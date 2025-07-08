--Vert
#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aColor;
layout(location = 3) in vec3 aNormal;
layout(location = 4) in vec2 aAO;

out vec2 vUV;
out vec3 vColor;
out float vFog1;
out float vFog2;
out vec3 vNormal;
out vec3 vPos;
out vec3  vBase; 
out float vAO;
out float vAtlasFlag;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

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
    gl_Position=wp*view*projection;

    vUV         = aTexCoord;
    vColor      = aColor;
    vPos        = wp.xyz;
    vNormal     = aNormal*mat3(transpose(inverse(model)));
     vBase     = (AMBIENT + aColor) * aAO.x;
    vFog1       = fogFac(wp.xyz,1.0);
    vFog2       = fogFac(wp.xyz,0.5);
    vAO         = aAO.x;
    vAtlasFlag  = aAO.y;
}

--Frag
#version 330 core
#include "includes/lighting.fs"

in vec2  vUV;
in vec3  vColor;
in float vFog1;
in float vFog2;
in vec3  vNormal;
in vec3  vPos;
in float vAO;
in vec3  vBase;
in float vAtlasFlag;

layout(location = 0) out vec4 gColor;
layout(location = 1) out vec4 gNormal;

uniform vec4  color      = vec4(1,1,1,1);
uniform sampler2D texture0;
uniform sampler2D textureAtlas;


void main()
{
    vec4 baseTex = texture(texture0,vUV);
    if(baseTex.a < 0.1) discard;

    vec3 N = normalize(vNormal);
    vec3 V = normalize(CAMERA_POS - vPos);
    vec3 base = baseTex.rgb;

    vec3 light = accumulateDirLights(N,V,base) +
                 accumulatePointLights(N,V,vPos,base) +
                 accumulateSpotLights(N,V,vPos,base);

    vec3 col  = (base*vBase) + light;

    vec4 fogged1 = fogMix(vec4(col ,baseTex.a),vFog1, FOG);
    vec4 fogged2 = fogMix(vec4(base,1.0),vFog2, FOG);

    vec4 atlas   = texture(textureAtlas,vUV);
    float aMix = (vAtlasFlag==0.0)?0.0:atlas.a;
    vec3 mixed   = mix(fogged1.rgb,fogged2.rgb,aMix);

    gColor  = vec4(mixed,fogged1.a);
    gNormal = vec4(N*0.5+0.5,1.0);
}
