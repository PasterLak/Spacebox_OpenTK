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
out vec4 FragColor;

uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gAlbedoSpec;
uniform sampler2D ssao;

void main()
{
    vec3 color = texture(gAlbedoSpec, TexCoords).rgb;
    float ambientOcclusion = texture(ssao, TexCoords).r;
    vec3 ambient = vec3(0.3) * color * ambientOcclusion;
   
    FragColor = vec4(ambient, 1.0);
}
