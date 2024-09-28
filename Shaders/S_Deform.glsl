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
    vec2 center = vec2(0.5, 0.5);
    vec2 coord = TexCoord - center;
    float dist = length(coord);
    float angle = atan(coord.y, coord.x) + dist * sin(time) * 3.0;
    float swirlStrength = 0.2;
    coord = vec2(cos(angle), sin(angle)) * dist * (1.0 + swirlStrength * sin(time));
    vec2 swirlTexCoord = coord + center;
    vec4 color = texture(spriteTexture, swirlTexCoord);
    FragColor = color;
}
