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

uniform sampler2D scene;
uniform float uGamma;
uniform float uContrast;
uniform float uSaturation;
uniform float uGrain;
uniform int uGrainSeed;

float grainNoise(ivec2 pixel, int seed)
{
    uint h = uint(pixel.x) * 1973u + uint(pixel.y) * 9277u + uint(seed) * 26699u;
    h = (h << 13u) ^ h;
    h = h * (h * h * 15731u + 789221u) + 1376312589u;
    return float(h & 0x7fffffffu) / 2147483647.0;
}

void main()
{
    vec4 source = texture(scene, vUV);
    vec3 color = pow(max(source.rgb, vec3(0.0)), vec3(1.0 / uGamma));
    color = (color - 0.5) * uContrast + 0.5;

    float luma = dot(color, vec3(0.299, 0.587, 0.114));
    color = mix(vec3(luma), color, uSaturation);

    if (uGrain > 0.0)
    {
        float midtones = 1.0 - abs(clamp(luma, 0.0, 1.0) * 2.0 - 1.0);
        float noise = grainNoise(ivec2(gl_FragCoord.xy), uGrainSeed) - 0.5;
        color += noise * uGrain * (0.08 + 0.17 * midtones);
    }

    FragColor = vec4(clamp(color, 0.0, 1.0), source.a);
}
