--Vert

#version 330 core

layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;

out vec2 TexCoords;

void main()
{
    TexCoords = aTexCoords;
    gl_Position = vec4(aPos, 0.0, 1.0);
}




--Frag

#version 330 core

in vec2 TexCoords;
out float FragColor;

uniform sampler2D ssaoInput;
uniform float screenWidth;
uniform float screenHeight;

void main()
{
    float result = 0.0;
    vec2 texelSize = 1.0 / vec2(screenWidth, screenHeight);
    for(int x = -2; x <= 2; ++x)
    {
        for(int y = -2; y <= 2; ++y)
        {
            vec2 offset = vec2(float(x), float(y)) * texelSize;
            result += texture(ssaoInput, TexCoords + offset).r;
        }
    }
    FragColor = result / 25.0;
}
