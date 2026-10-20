--Vert
#version 330 core

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec3 vertexNormal;
layout(location = 2) in vec2 vertexTexCoords;

uniform mat4 model;

out vec3 vPos;
out vec3 vNormal;
out vec2 vUV;
out float vFog;

#define FOG_LN2 1.44269504

float fogFac(vec3 p, float k)
{
    float d2  = dot(p - CAMERA_POS, p - CAMERA_POS);
    float kd2 = k * k * d2;
    return exp2(-(FOG_DENSITY) * kd2 * FOG_LN2);
}

void main()
{
    vec4 wp = vec4(vertexPosition, 1.0) * model;
    gl_Position = wp * VIEW * PROJECTION;

    vPos    = wp.xyz;
    vNormal = vertexNormal * mat3(transpose(inverse(model)));
    vUV     = vertexTexCoords;
    vFog    = fogFac(wp.xyz, 1.0);
}

--Frag
#version 330 core
#include "includes/lighting.fs"
#include "includes/gbuffer.glsl"

in vec3 vPos;
in vec3 vNormal;
in vec2 vUV;
in float vFog;

layout(location = 0) out vec4 gColor;

uniform sampler2D texture0;
uniform vec3 extraLight = vec3(0.0);

void main()
{
    vec4 baseTex = texture(texture0, vUV);
    if (baseTex.a < 0.1) discard;

    vec3 N = normalize(vNormal);
    vec3 V = normalize(CAMERA_POS - vPos);
    vec3 base = baseTex.rgb;

    vec3 light = accumulatePointLights(N, V, vPos, base) +
                 accumulateSpotLights(N, V, vPos, base);

    vec3 col = base * (AMBIENT + extraLight) + light;

    gColor  = fogMix(vec4(col, baseTex.a), vFog, FOG);
    writeGBuffer(N * mat3(VIEW), GBUFFER_MASK_WORLD);
}
