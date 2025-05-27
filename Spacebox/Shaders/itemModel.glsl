--Vert
#version 330 core


layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;



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

    Normal =  aNormal;
    TexCoords = aTexCoord;
   
}

--Frag


#version 330 core

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoords;

layout(location = 0) out vec4 gColor;   
layout(location = 1) out vec4 gNormal; 

uniform sampler2D texture0;

const vec3 lightDir = vec3(1,1,1);
const vec3 lightColor = vec3(1,1,1); 
const vec3 objectColor = vec3(1,1,1);

const float shadows = 0.1;

void main()
{
    vec4 texColor = texture(texture0, TexCoords);
    if(texColor.a < 0.1)
        discard;

    vec3 norm = normalize(Normal);

    vec3 lightDirection = normalize(lightDir);
        float diff = max(dot(norm, lightDirection), shadows);
        vec3 diffuse = diff * lightColor;
        vec3 ambient = 0.1 * lightColor;
        vec3  result = (ambient + diffuse) * objectColor * vec3(texColor);

    gColor = vec4(result, texColor.a);

             
    gNormal = vec4(norm * 0.5 + 0.5, 1.0); 
}
