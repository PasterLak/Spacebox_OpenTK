--Vert
#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

uniform mat4 model;

void main()
{
    gl_Position = vec4(aPos, 1.0) * model * VIEW * PROJECTION;
}

--Frag

#version 330 core

out vec4 FragColor;


uniform vec4 color = vec4(1,1,1,1);  


void main()
{
    FragColor = color;
   
}