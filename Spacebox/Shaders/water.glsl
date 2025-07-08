--Vert
#version 330 core

layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec3 vertexNormal;
layout(location = 2) in vec2 vertexUV;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

#define AMP1  0.01
#define AMP2  0.001
#define AMP3  0.005
#define F1    1.5
#define F2    2.1
#define F3    8.2
#define S1    1.1
#define S2    1.3
#define S3    1.7
#define DIR1  vec2( -1.0, 0.0)
#define DIR2  vec2( 0.6, -0.8)
#define DIR3  vec2(-0.7, 0.4)
#define TILE_UV 16.0
#define GRAD_EPS 0.06

out vec3 vWorldPos;
out vec3 vWorldNormal;
out vec2 vUV;
out float vFog;
out float vHeightN;

float wave(vec2 p)
{
    float a = sin(dot(DIR1,p)*F1 + TIME*S1)*AMP1;
    float b = sin(dot(DIR2,p)*F2 + TIME*S2)*AMP2;
    float c = sin(dot(DIR3,p)*F3 + TIME*S3)*AMP3;
    return (a+b+c);
}

#define FOG_LN2 1.44269504

float fogFac(vec3 p, float k)
{
    float d2   = dot(p - CAMERA_POS, p - CAMERA_POS);   
    float kd2  = k * k * d2;                            
    return exp2(-(FOG_DENSITY) * kd2 * FOG_LN2);       
}

void main()
{
    vec3 local = vertexPosition;
    vec2 p     = vertexPosition.xz * TILE_UV;
    float h    = wave(p);
    local.y   += h;

    float dx = wave(p + vec2(GRAD_EPS,0)) - wave(p - vec2(GRAD_EPS,0));
    float dz = wave(p + vec2(0,GRAD_EPS)) - wave(p - vec2(0,GRAD_EPS));
    vec3 n   = normalize(vec3(-dx, 2.0*GRAD_EPS, -dz));

    vec4 wp  = model * vec4(local,1.0);
    gl_Position = wp * view * projection;

    vWorldPos    = wp.xyz;
    vWorldNormal = mat3(transpose(inverse(model))) * n;
    vUV          = vertexUV;
    vFog         = fogFac(wp.xyz, 1.0);
    vHeightN     = clamp(h / (AMP1+AMP2+AMP3), -1.0, 1.0);
}
--Frag
#version 330 core
#include "includes/lighting.fs"

in vec3 vWorldPos;
in vec3 vWorldNormal;
in vec2 vUV;
in float vFog;
in float vHeightN;

out vec4 fragColor;

#define BASE_COLOR   vec3(0.03,0.22,0.45)
#define NOISE_TILE   24.0
#define NOISE_SPEED  0.07
#define FOAM_LOW     0.15
#define FOAM_HIGH    0.95
#define ALPHA_LO     0.92
#define ALPHA_HI     0.94
#define SPEC_POW     56.0
#define SPEC_COLOR   vec3(1.0)

uniform sampler2D noiseMap;

vec4 fogMix(vec4 c){ return mix(vec4(FOG,c.a),c,vFog); }

void main()
{
    vec3 N  = normalize(vWorldNormal);
    vec3 V  = normalize(CAMERA_POS - vWorldPos);

    float n1 = texture(noiseMap, vUV*NOISE_TILE + vec2(TIME*NOISE_SPEED,0)).r;
    float n2 = texture(noiseMap, vUV*NOISE_TILE + vec2(0,TIME*NOISE_SPEED)).r;
    float rip= mix(n1,n2,0.5);

    float crest = smoothstep(FOAM_LOW,FOAM_HIGH,vHeightN);
    float trough= 1.0 - crest;

    vec3 base = BASE_COLOR * (0.55 + 0.45*rip) * (0.65 + 0.35*trough);

    vec3 lightSum =
          AMBIENT +
          accumulateDirLights(N,V,base) +
          accumulatePointLights(N,V,vWorldPos,base) +
          accumulateSpotLights(N,V,vWorldPos,base);

    vec3 color = base * lightSum;

    vec3 R = reflect(-V,N);
    vec3 L = normalize(-dirLights[0].direction);
    float spec = pow(max(dot(R,L),0.0), SPEC_POW);
    color += SPEC_COLOR * spec * 0.6;

    color = mix(color, vec3(1.0), crest);

    float alpha = mix(ALPHA_LO,ALPHA_HI,crest);

    fragColor = fogMix(vec4(color,alpha), vFog, FOG);

}
