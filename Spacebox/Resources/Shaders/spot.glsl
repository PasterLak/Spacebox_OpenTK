
--Vert
// vert.glsl
#version 330 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec3 LocalPos;
out float LocalZ;

void main()
{
    gl_Position = vec4(aPos,1.0) * model * view * projection;
    LocalPos = aPos;
    LocalZ   = aPos.z;
}




--Frag
// frag.glsl
#version 330 core

in  vec3 LocalPos;
in  float LocalZ;
out vec4 FragColor;

const float lightLength = 1;
const float cosInner = 15;
const float cosOuter = 20;
const float fadeStart = 0.5;
const float fadeEnd = 0.4;
const vec3  lightColor = vec3(0.4,0.8,0.7);
const float maxAlpha = 1;

void main()
{
    if(LocalZ < 0.0 || LocalZ > lightLength) discard;
    vec3 dir = normalize(LocalPos);
    float spot = smoothstep(cosOuter, cosInner, dir.z);
    if(spot <= 0.0) discard;
    float t = LocalZ / lightLength;
    float fadeIn  = clamp(t / fadeStart,       0.0, 1.0);
    float fadeOut = clamp((1.0 - t) / (1.0 - fadeEnd), 0.0, 1.0);
    float fade    = min(fadeIn, fadeOut);
    float intensity = spot * fade;
    float alpha     = intensity * maxAlpha;

    FragColor = vec4(lightColor * intensity, alpha);


}
