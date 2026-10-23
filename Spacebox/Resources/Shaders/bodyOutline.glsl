--Vert
#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;

uniform mat4 model;
uniform mat3 normalMatrix;
uniform mat4 view;
uniform mat4 projection;
uniform float uThickness;

void main()
{
    vec3 radial = dot(aPos, aPos) > 0.000001 ? normalize(aPos) : aNormal;
    vec3 local = normalize(radial + aNormal * 0.35);
    vec4 world = vec4(aPos, 1.0) * model;
    vec3 direction = normalize(local * normalMatrix);
    world.xyz += direction * uThickness;
    gl_Position = world * view * projection;
}

--Frag
#version 330 core
out vec4 FragColor;

uniform vec4 uColor;

void main()
{
    FragColor = uColor;
}
