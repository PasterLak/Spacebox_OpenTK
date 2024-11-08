--Vert
#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec3 aNormal;

uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;

out vec3 Normal;
out vec3 FragPos;
out vec2 TexCoords;
out vec3 Color;

void main()
{
    gl_Position = vec4(aPos, 1.0) * model * view * projection;
    FragPos = vec3(vec4(aPos, 1.0) * model);
    //Normal = aNormal * mat3(transpose(inverse(model)));
    Normal = aNormal ;
    TexCoords = aTexCoords;
    //Color = aColor;
}

--Frag


#version 330 core

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoords;

out vec4 FragColor;

uniform sampler2D texture0;
uniform vec3 lightDir = vec3(-1,-1,-1);
uniform vec3 lightColor = vec3(1,1,1);
uniform vec3 objectColor = vec3(1,1,1);

uniform bool useSsao = false;


uniform float aoIntensity = 0.5; 
uniform vec3 aoDirection = vec3(0.0, 0.0, 1.0); 


void main()
{
    vec4 texColor = texture(texture0, TexCoords);
    if(texColor.a < 0.1)
        discard;

    vec3 norm = normalize(Normal);

    vec3 result;

    if(useSsao)
    {
     float ao = clamp(dot(norm, aoDirection), 0.0, 1.0);
    ao = 1.0 - aoIntensity * (1.0 - ao);
    vec3 shadedColor = texColor.rgb * ao;
    result = shadedColor;
    }
    else
    {
     vec3 lightDirection = normalize(-lightDir);
    float diff = max(dot(norm, lightDirection), 0.0);
    vec3 diffuse = diff * lightColor;
    vec3 ambient = 0.1 * lightColor;
     result = (ambient + diffuse) * objectColor * vec3(texColor);
    }
    
  


    FragColor = vec4(result, texColor.a);
}
