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
uniform float uIntensity;
uniform float uTime;
uniform int uGrainSeed;
uniform vec2 uResolution;

const float BARREL = 0.05;
const float ABERRATION = 0.003;
const float SCANLINE_PERIOD = 3.0;
const vec3 TINT = vec3(0.9, 1.0, 0.96);
const float EXPOSURE = 1.15;
const float DESATURATION = 0.3;
const float GRAIN = 0.07;

float hash(ivec2 pixel, int seed)
{
    uint h = uint(pixel.x) * 1973u + uint(pixel.y) * 9277u + uint(seed) * 26699u;
    h = (h << 13u) ^ h;
    h = h * (h * h * 15731u + 789221u) + 1376312589u;
    return float(h & 0x7fffffffu) / 2147483647.0;
}

vec2 barrel(vec2 uv, float amount)
{
    vec2 centered = uv * 2.0 - 1.0;
    centered *= 1.0 + amount * dot(centered, centered);
    return centered * 0.5 + 0.5;
}

void main()
{
    vec4 original = texture(scene, vUV);
    vec2 uv = barrel(vUV, BARREL * uIntensity);
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
    {
        FragColor = vec4(mix(original.rgb, vec3(0.0), uIntensity), 1.0);
        return;
    }

    float band = fract(uTime * 0.07);
    float bandDistance = abs(uv.y - band);
    float jitter = (1.0 - smoothstep(0.0, 0.02, bandDistance)) * (hash(ivec2(0, int(gl_FragCoord.y)), uGrainSeed) - 0.5) * 0.006;
    uv.x += jitter * uIntensity;

    vec2 fromCenter = uv - 0.5;
    vec2 shift = fromCenter * ABERRATION * uIntensity * 2.0;
    vec3 color = vec3(
        texture(scene, uv + shift).r,
        texture(scene, uv).g,
        texture(scene, uv - shift).b);

    float luma = dot(color, vec3(0.299, 0.587, 0.114));
    color = mix(color, vec3(luma), DESATURATION) * TINT * EXPOSURE;

    float scanline = 0.94 + 0.06 * sin((gl_FragCoord.y + uTime * 12.0) * 6.2831853 / SCANLINE_PERIOD);
    color *= scanline;

    color += (1.0 - smoothstep(0.0, 0.06, bandDistance)) * 0.04;

    color += (hash(ivec2(gl_FragCoord.xy) / 2, uGrainSeed) - 0.5) * GRAIN;

    float vignette = smoothstep(1.0, 0.45, length(fromCenter * vec2(uResolution.x / uResolution.y, 1.0)) * 0.9);
    color *= mix(0.7, 1.0, vignette);

    FragColor = vec4(mix(original.rgb, clamp(color, 0.0, 1.0), uIntensity), 1.0);
}
