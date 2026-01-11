--Vert


#version 330 core

layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;

out vec2 TexCoord;



void main()
{
    
    TexCoord = aTexCoord;

    gl_Position = vec4(aPosition, 0.0, 1.0);
}


--Frag

#version 330 core

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D spriteTexture;
uniform vec2 offset;  
uniform vec2 tiling;

uniform float time;
uniform vec2 screen;
uniform vec2 mouse;

void main()
{
   // vec2 adjustedTexCoord = TexCoord * tiling + offset;
    FragColor = texture(spriteTexture, TexCoord);
}