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
uniform float time;    

void main()
{
    vec4 br = vec4(1,1,1,1);
    //vec4 br = vec4(1,sin(time),cos(time),1);
    FragColor = texture(spriteTexture, TexCoord) * br;
}