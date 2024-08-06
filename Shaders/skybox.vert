// skybox.vert
#version 330 core

layout(location = 0) in vec3 aPosition;

out vec3 TexCoords;

uniform mat4 world;
uniform mat4 projection;
uniform mat4 view;

void main()
{
    TexCoords = aPosition;
    vec4 pos = world * projection * view * vec4(aPosition, 1.0);
    gl_Position = pos.xyww; // Trick to ensure the depth is correct
}
