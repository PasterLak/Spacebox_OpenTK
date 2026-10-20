--Vert
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;

void main()
{
    gl_Position = vec4(aPos, 0.0, 1.0);
}

--Frag
#version 330 core
#define MAX_SAMPLES 32
#define DEPTH_STEPS 16777215.0

out float outAo;

uniform sampler2D uDepth;
uniform sampler2D uSceneNormal;
uniform sampler2D uNoise;
uniform vec3 uKernel[MAX_SAMPLES];
uniform int uSampleCount;
uniform vec4 uProjMain;
uniform vec4 uProjView;
uniform vec2 uSceneSize;
uniform int uDivisor;
uniform float uRadius;
uniform float uViewModelRadius;
uniform float uBias;
uniform float uFadeStart;
uniform float uFadeEnd;

vec3 viewPosition(vec2 uv, float depth, vec4 proj)
{
    vec2 ndc = uv * 2.0 - 1.0;
    return vec3(ndc.x * depth / proj.x, ndc.y * depth / proj.y, -depth);
}

void main()
{
    ivec2 pixel = ivec2(gl_FragCoord.xy);
    float stored = texelFetch(uDepth, pixel, 0).r;
    if (stored == 0.0)
    {
        outAo = 1.0;
        return;
    }

    bool viewModel = stored < 0.0;
    float depth = abs(stored);
    float fade = viewModel ? 1.0 : 1.0 - smoothstep(uFadeStart, uFadeEnd, depth);
    if (fade <= 0.0)
    {
        outAo = 1.0;
        return;
    }

    vec4 proj = viewModel ? uProjView : uProjMain;
    float radius = viewModel ? uViewModelRadius : uRadius;
    float near = proj.w / (proj.z - 1.0);
    float bias = uBias * (radius / uRadius) + depth * depth / (near * DEPTH_STEPS) * 4.0;

    ivec2 fullPixel = min(pixel * uDivisor, ivec2(uSceneSize) - 1);
    vec2 uv = (vec2(fullPixel) + 0.5) / uSceneSize;
    vec3 position = viewPosition(uv, depth, proj);
    vec3 normal = normalize(texelFetch(uSceneNormal, fullPixel, 0).xyz * 2.0 - 1.0);

    vec3 random = vec3(texelFetch(uNoise, pixel % textureSize(uNoise, 0), 0).xy * 2.0 - 1.0, 0.0);
    vec3 tangent = random - normal * dot(random, normal);
    if (dot(tangent, tangent) < 1e-4)
        tangent = cross(normal, abs(normal.x) < 0.9 ? vec3(1.0, 0.0, 0.0) : vec3(0.0, 1.0, 0.0));
    tangent = normalize(tangent);
    mat3 tbn = mat3(tangent, cross(normal, tangent), normal);

    float occlusion = 0.0;
    for (int i = 0; i < uSampleCount; ++i)
    {
        vec3 samplePos = position + tbn * uKernel[i] * radius;
        if (samplePos.z >= -1e-4) continue;

        vec2 sampleUv = vec2(samplePos.x * proj.x, samplePos.y * proj.y) / -samplePos.z * 0.5 + 0.5;
        if (sampleUv.x < 0.0 || sampleUv.x > 1.0 || sampleUv.y < 0.0 || sampleUv.y > 1.0) continue;

        float sampleStored = texture(uDepth, sampleUv).r;
        if (sampleStored == 0.0 || (sampleStored < 0.0) != viewModel) continue;

        float sampleDepth = abs(sampleStored);
        float range = smoothstep(0.0, 1.0, radius / abs(depth - sampleDepth));
        occlusion += (sampleDepth <= -samplePos.z - bias ? 1.0 : 0.0) * range;
    }

    outAo = 1.0 - occlusion / float(max(uSampleCount, 1)) * fade;
}
