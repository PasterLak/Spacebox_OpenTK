--Vert
#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoords;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec3 Normal;
out vec3 FragPos;
out vec2 TexCoord;

void main()
{
    gl_Position = vec4(aPos, 1.0) * model * view * projection;
    FragPos = vec3(vec4(aPos, 1.0) * model);
    Normal = aNormal * mat3(transpose(inverse(model)));

    TexCoord = aTexCoords;
}

--Frag

#version 330 core

in vec2 TexCoord;
in vec3 Normal;
in vec3 FragPos;
out vec4 FragColor;

uniform sampler2D texture0;
uniform vec4 color = vec4(1,1,1,1);

uniform vec3 cameraPos;
uniform vec3 glowColor;
uniform float glowIntensity;

void main()
{
    vec4 pixel = texture(texture0, TexCoord);

    if (pixel.a < 0.1)
        discard;

    vec3 viewDir = normalize(cameraPos - FragPos);

    float fresnel = pow(1.0 - dot(normalize(Normal), viewDir), 3.0);


    vec4 glow = vec4(glowColor, 1.0) * glowIntensity * fresnel;


    FragColor = pixel * color * glow;
}
