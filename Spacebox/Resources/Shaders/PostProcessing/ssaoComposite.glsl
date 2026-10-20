--Vert
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;
out vec2 vUV;

void main()
{
    vUV = aTexCoords;
    gl_Position = vec4(aPos, 0.0, 1.0);
}

--Frag
#version 330 core
in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uScene;
uniform sampler2D uAo;
uniform sampler2D uAoDepth;
uniform sampler2D uSceneDepth;
uniform sampler2D uSceneNormal;
uniform vec4 uProjMain;
uniform vec4 uProjView;
uniform vec2 uSceneSize;
uniform float uStrength;
uniform float uViewModelStrength;
uniform float uGrain;
uniform int uGrainSize;
uniform int uActive;
uniform int uDebugView;

float linearDepth(vec4 proj, float depthWindow)
{
    return proj.w / ((depthWindow * 2.0 - 1.0) + proj.z);
}

float grainNoise(ivec2 pixel)
{
    uint h = uint(pixel.x) * 1973u + uint(pixel.y) * 9277u;
    h = (h << 13u) ^ h;
    h = h * (h * h * 15731u + 789221u) + 1376312589u;
    return float(h & 0x7fffffffu) / 2147483647.0;
}

float applyGrain(float occlusion)
{
    const float dotOcclusion = 0.6;
    float noise = grainNoise(ivec2(gl_FragCoord.xy) / max(uGrainSize, 1));
    float dots = noise < occlusion / dotOcclusion ? max(dotOcclusion, occlusion) : 0.0;
    return mix(occlusion, dots, uGrain);
}

float upsampleAo(bool viewModel, float depth)
{
    ivec2 aoSize = textureSize(uAo, 0);
    vec2 coord = vUV * vec2(aoSize) - 0.5;
    ivec2 base = ivec2(floor(coord));
    vec2 f = fract(coord);
    float tolerance = depth * 0.02 + 0.001;

    float sum = 0.0;
    float weightSum = 0.0;
    for (int y = 0; y < 2; ++y)
    for (int x = 0; x < 2; ++x)
    {
        ivec2 tap = clamp(base + ivec2(x, y), ivec2(0), aoSize - 1);
        float stored = texelFetch(uAoDepth, tap, 0).r;
        if (stored == 0.0 || (stored < 0.0) != viewModel) continue;

        float bilinear = (x == 0 ? 1.0 - f.x : f.x) * (y == 0 ? 1.0 - f.y : f.y) + 1e-3;
        float weight = bilinear / (1.0 + abs(abs(stored) - depth) / tolerance);
        sum += texelFetch(uAo, tap, 0).r * weight;
        weightSum += weight;
    }

    return weightSum > 0.0 ? sum / weightSum : 1.0;
}

void main()
{
    vec4 color = texture(uScene, vUV);
    if (uActive == 0)
    {
        FragColor = color;
        return;
    }

    ivec2 pixel = min(ivec2(vUV * uSceneSize), ivec2(uSceneSize) - 1);
    vec4 gBuffer = texelFetch(uSceneNormal, pixel, 0);
    float mask = gBuffer.a;
    float ao = 1.0;
    if (mask >= 0.25)
    {
        bool viewModel = mask < 0.75;
        float depth = linearDepth(viewModel ? uProjView : uProjMain, texelFetch(uSceneDepth, pixel, 0).r);
        float strength = uStrength;
        if (viewModel)
        {
            float facing = gBuffer.z * 2.0 - 1.0;
            strength *= uViewModelStrength * (1.0 - smoothstep(0.2, 0.7, facing));
        }
        float occlusion = 1.0 - upsampleAo(viewModel, depth);
        if (uGrain > 0.0) occlusion = applyGrain(occlusion);
        ao = clamp(1.0 - occlusion * strength, 0.0, 1.0);
    }

    FragColor = uDebugView == 1 ? vec4(vec3(ao), 1.0) : vec4(color.rgb * ao, color.a);
}
