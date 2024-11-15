--Vert
#version 330 core


layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aColor;
layout (location = 3) in vec3 aNormal;



uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;

out vec3 Normal;
out vec3 FragPos;
out vec2 TexCoords;


void main()
{
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
    FragPos = vec3(vec4(aPosition, 1.0) * model);
    //Normal = aNormal * mat3(transpose(inverse(model)));
    Normal = aNormal;
    TexCoords = aTexCoord;
    //Color = aColor;
}

--Frag


#version 330 core

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoords;

out vec4 FragColor;

uniform sampler2D texture0;

// Diffuse
uniform vec3 lightDir = vec3(-1,-1,-1);
uniform vec3 lightColor = vec3(1,1,1); 
uniform vec3 objectColor = vec3(1,1,1);


// SSAO
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
    float diff = max(dot(norm, lightDirection), 0.1);
    vec3 diffuse = diff * lightColor;
    vec3 ambient = 0.1 * lightColor;
     result = (ambient + diffuse) * objectColor * vec3(texColor);
    }
    
  


    FragColor = vec4(result, texColor.a);
}
