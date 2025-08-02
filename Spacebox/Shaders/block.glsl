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
out vec3 vNormalVS;
out vec3 vPosVS;
out vec3 vBase; 
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
    vec4 wp_model = vec4(aPosition, 1.0) * model;
    vec4 wp_view  = wp_model * view;

    gl_Position = wp_model * view * projection;

    vUV         = aTexCoord;
    vColor      = aColor;
    vPosVS      = wp_view.xyz;
    vNormalVS   = normalize(aNormal * mat3(transpose(inverse(model))) * mat3(view));
    vBase       = (AMBIENT + aColor) * aAO.x;
    vFog1       = fogFac(wp_model.xyz, 1.0);
    vFog2       = fogFac(wp_model.xyz, 0.5);
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
in vec3  vNormalVS;
in vec3  vPosVS;
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
    vec4 baseTex = texture(texture0, vUV);
    if(baseTex.a < 0.1) discard;

    vec3 N = normalize(vNormalVS);
    vec3 V = normalize(-vPosVS);
    vec3 base = baseTex.rgb;

    vec3 light =
    //accumulateDirLights(N, V, base) +
                 accumulatePointLights(N, V, vPosVS, base) +
                 accumulateSpotLights(N, V, vPosVS, base);

    vec3 col = (base * vBase) + light;

    vec4 fogged1 = fogMix(vec4(col, baseTex.a), vFog1, FOG);
    vec4 fogged2 = fogMix(vec4(base, 1.0), vFog2, FOG);

    vec4 atlas = texture(textureAtlas, vUV);
    float aMix = (vAtlasFlag == 0.0) ? 0.0 : atlas.a;
    vec3 mixed = mix(fogged1.rgb, fogged2.rgb, aMix);

    gColor  = vec4(mixed, fogged1.a);
    gNormal = vec4(N * 0.5 + 0.5, 1.0); 
}

