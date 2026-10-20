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
uniform vec2 uLightPos;
uniform vec3 uColor;
uniform float uAspect;
uniform float uTime;

float hash(vec2 p)
{
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float valueNoise(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
}

float fbm(vec2 p)
{
    float sum = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 3; i++)
    {
        sum += valueNoise(p) * amplitude;
        p = p * 2.03 + vec2(1.7, 9.2);
        amplitude *= 0.5;
    }
    return sum;
}

float glassDirt(vec2 p)
{
    float smudges = smoothstep(0.45, 0.85, fbm(p * 2.2 + vec2(3.1, 7.4)));
    float stains = smoothstep(0.55, 0.9, fbm(p * 5.5 + vec2(11.3, 2.8))) * 0.5;
    vec2 cell = floor(p * 90.0);
    float dust = step(0.993, hash(cell)) * smoothstep(0.5, 0.0, length(fract(p * 90.0) - 0.5));
    return smudges * 0.8 + stains + dust * 0.8;
}

float ring(vec2 p, vec2 center, float size, float width)
{
    float d = length((p - center) * vec2(uAspect, 1.0));
    return exp(-abs(d - size) / width);
}

float disc(vec2 p, vec2 center, float size)
{
    float d = length((p - center) * vec2(uAspect, 1.0));
    return smoothstep(size, size * 0.4, d);
}

void main()
{
    vec3 original = texture(scene, vUV).rgb;
    vec2 p = vUV * 2.0 - 1.0;
    vec2 light = uLightPos;

    vec2 d = (p - light) * vec2(uAspect, 1.0);
    float r = length(d);

    float onScreen = 1.0 - smoothstep(0.9, 1.6, max(abs(light.x), abs(light.y)));
    float near = exp(-r * 2.6);

    float glow = exp(-r * 4.5) * 0.6 * onScreen;
    float hotspot = exp(-r * 14.0) * 0.9 * onScreen;
    float halo = ring(p, light, 0.32, 0.04) * 0.14 * onScreen;
    float dirt = 0.0;
    if (near * uIntensity > 0.004) dirt = glassDirt(p * vec2(uAspect, 1.0)) * near * 1.1;

    vec3 ghosts = vec3(0.0);
    ghosts += vec3(0.65, 0.85, 1.0) * (disc(p, light * 0.45, 0.07) * 0.10 + ring(p, light * 0.45, 0.07, 0.01) * 0.06);
    ghosts += vec3(1.0, 0.8, 0.55) * (disc(p, light * 0.2, 0.12) * 0.06 + ring(p, light * 0.2, 0.12, 0.012) * 0.05);
    ghosts += vec3(0.75, 1.0, 0.75) * (disc(p, light * -0.15, 0.05) * 0.08);
    ghosts += vec3(0.85, 0.7, 1.0) * (disc(p, light * -0.4, 0.16) * 0.04 + ring(p, light * -0.4, 0.16, 0.015) * 0.04);
    ghosts *= onScreen * 2.5;

    vec3 tint = mix(vec3(1.0), uColor, 0.6);
    vec3 glare = tint * (glow + hotspot + halo + dirt) + ghosts;

    vec3 color = original * (1.0 + near * uIntensity * 0.25) + glare * uIntensity;

    FragColor = vec4(color, 1.0);
}
