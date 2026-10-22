--Vert

#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aUV;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 uRight;
uniform vec3 uUp;
uniform vec3 uDir;
uniform float uSlope;

out vec3 vRenderPos;
out vec3 vNormal;
out float vAxial;

void main()
{
    vec4 world = vec4(aPos, 1.0) * model;
    vRenderPos = world.xyz;
    vec3 radial = normalize(aNormal.x * uRight + aNormal.y * uUp);
    vNormal = normalize(radial - uDir * uSlope);
    vAxial = aUV.y;
    gl_Position = world * view * projection;
}


--Frag

#version 330 core
in vec3 vRenderPos;
in vec3 vNormal;
in float vAxial;
out vec4 FragColor;

uniform vec3 uCameraPos;
uniform vec3 uNoiseOffset;
uniform vec3 uColor;
uniform float uIntensity;
uniform float uTime;
uniform float uInside;

float hash3(vec3 p)
{
    p = fract(p * 0.3183099 + 0.1);
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float valueNoise3(vec3 p)
{
    vec3 i = floor(p);
    vec3 f = fract(p);
    vec3 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(mix(hash3(i), hash3(i + vec3(1, 0, 0)), u.x),
                   mix(hash3(i + vec3(0, 1, 0)), hash3(i + vec3(1, 1, 0)), u.x), u.y),
               mix(mix(hash3(i + vec3(0, 0, 1)), hash3(i + vec3(1, 0, 1)), u.x),
                   mix(hash3(i + vec3(0, 1, 1)), hash3(i + vec3(1, 1, 1)), u.x), u.y), u.z);
}

void main()
{
    vec3 cameraOffset = uCameraPos - vRenderPos;
    float cameraDistance = length(cameraOffset);
    vec3 toCamera = cameraOffset / max(cameraDistance, 0.0001);
    float facing = abs(dot(normalize(vNormal), toCamera));
    float nearFade = smoothstep(1.5, 6.0, cameraDistance);
    float edge = smoothstep(0.0, 0.6, facing);

    float along = smoothstep(0.0, 0.06, vAxial) * pow(1.0 - vAxial, 1.7);

    vec3 q = vRenderPos + uNoiseOffset;
    vec3 drift = vec3(uTime * 0.04, uTime * 0.025, -uTime * 0.03);
    float haze = valueNoise3(q * 0.8 + drift) * 0.6 + valueNoise3(q * 2.1 - drift * 1.7) * 0.4;
    float density = mix(0.35, 1.3, haze);

    float sparkle = smoothstep(0.93, 0.99, valueNoise3(q * 22.0 + drift * 3.0)) * smoothstep(4.0, 10.0, cameraDistance) * (1.0 - uInside);

    float insideDim = 1.0 - uInside * 0.7;
    float glow = uIntensity * along * nearFade * insideDim * (edge * density * 0.16 + sparkle * edge * 0.3);
    FragColor = vec4(uColor * glow, 1.0);
}
