--Vert

#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

uniform mat4 model;

out vec3 normal;
out vec3 worldPosition;

void main()
{
    gl_Position = vec4(aPos, 1.0) * model * VIEW * PROJECTION;
    normal = aNormal * mat3(model);
    worldPosition = (vec4(aPos, 1.0 ) * model).xyz;
}

--Frag

#version 330 core

out vec4 FragColor;
in vec3 normal;
in vec3 worldPosition;

uniform vec4 color = vec4(1,1,1,1);
uniform vec3 cameraPos; 

vec3 rawL = vec3(-1, -1, 0); 

void main()
{
    vec3 N = normalize(normal);
    vec3 V = normalize(cameraPos - worldPosition);
    
    vec3 L = normalize(rawL); 

    vec3 lightDir = -L; 

    float diffFactor = max(dot(N, lightDir), 0.0);
    vec3 diffColor = color.rgb * diffFactor;

    vec3 specColor = vec3(0.0);
    
 
        vec3 R = reflect(L, N); 

         vec3 halfVec = normalize(lightDir + V);
        float spec = pow(max(dot(N, halfVec), 0.0), 32.0);
        specColor = vec3(66.0) * spec * 10.5;
    

  
    vec3 result = AMBIENT + diffColor + specColor;
    FragColor = vec4(result, 1.0) * color;
}