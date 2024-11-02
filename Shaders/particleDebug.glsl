--Vert

#version 330 core

layout (location = 0) in vec3 aPos;


layout (location = 2) in mat4 instanceModel;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec4 worldPos = instanceModel * vec4(aPos, 1.0);
    gl_Position = worldPos * view * projection ;
}

--Frag

#version 330 core

out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0, 0.0, 1.0, 1.0);
}