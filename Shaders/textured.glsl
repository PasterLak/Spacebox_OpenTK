--Vert
#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec3 aColor;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;

//out vec3 Normal;
out vec3 FragPos;
out vec2 TexCoords;
out vec3 Color;

void main()
{
    gl_Position = vec4(aPos, 1.0) * model * view * projection;
    FragPos = vec3(vec4(aPos, 1.0) * model);
    //Normal = aNormal * mat3(transpose(inverse(model)));
    TexCoords = aTexCoords;
    Color = aColor;
}

--Frag


#version 330 core

in vec3 Color;
in vec2 TexCoords;
out vec4 FragColor;

uniform sampler2D texture0;

void main()
{
    vec4 texColor = texture(texture0, TexCoords);
    if(texColor.a < 0.1)
        discard;
    FragColor = texColor;
}
