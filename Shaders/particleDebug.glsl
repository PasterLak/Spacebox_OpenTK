--Vert

#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 2) in mat4 instanceModel;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec3 translation = vec3(instanceModel[3]);
    vec3 camRight = vec3(view[0][0], view[1][0], view[2][0]);
    vec3 camUp = vec3(view[0][1], view[1][1], view[2][1]);
    float scale = length(vec3(instanceModel[0]));
    vec3 worldPos = translation + (aPos.x * camRight + aPos.y * camUp) * scale;
    gl_Position = projection * view * vec4(worldPos, 1.0);
}

--Frag

#version 330 core

out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0, 0.0, 1.0, 1.0);
}
