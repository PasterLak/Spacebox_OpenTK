--Vert
#version 330 core

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoord;

uniform mat4 projection;

out vec2 TexCoord;

void main()
{
    gl_Position = projection * vec4(aPosition, 0.0, 1.0);
    TexCoord = aTexCoord;
}

--Frag
#version 330 core

in vec2 TexCoord;

uniform sampler2D textTexture;
uniform vec3 textColor;

out vec4 FragColor;

void main()
{
    vec4 sampled = texture(textTexture, TexCoord);
    FragColor = vec4(textColor, sampled.a);
}
