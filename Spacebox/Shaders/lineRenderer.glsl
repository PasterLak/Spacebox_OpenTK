﻿--Vert
#version 330 core

layout(location = 0) in vec3 aPosition; 
layout(location = 1) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vNormal;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    vNormal = aNormal;
}

--Frag
#version 330 core

uniform vec4 uColor;

in vec3 vNormal;
out vec4 fragColor;

void main()
{
    fragColor = uColor;
}
