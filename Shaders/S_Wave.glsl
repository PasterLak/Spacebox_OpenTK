
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
    float amplitude = 0.02;
    float frequency = 10.0;
    float speed = 2.0;

    float offsetY = sin((TexCoord.x * frequency) + (time * speed)) * amplitude;
    vec2 distortedTexCoord = vec2(TexCoord.x, TexCoord.y + offsetY);
    vec4 texColor = texture(spriteTexture, distortedTexCoord);

    float colorShift = sin(time) * 0.1;
    texColor.r += colorShift;
    texColor.g += colorShift;
    texColor.b += colorShift;

    texColor = clamp(texColor, 0.0, 1.0);
    FragColor = texColor;
}
