--Vert

#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;


uniform vec2 offset;  
uniform vec2 tiling = vec2(1,1); 

out vec3 Normal;
out vec3 FragPos;
out vec2 TexCoords;



void main()
{
    gl_Position = vec4(aPos, 1.0) * model * view * projection;
    FragPos = vec3(vec4(aPos, 1.0) * model);
    Normal = aNormal * mat3(transpose(inverse(model)));
    TexCoords = aTexCoords  ;
}

--Frag


#version 330 core

in vec2 TexCoords;

uniform vec3 ambient = vec3(1, 0.8, 0.8); 
out vec4 FragColor;

uniform sampler2D mainTexture;

void main()
{
    vec4 texColor = texture(mainTexture, TexCoords);
    
    vec3 color = texColor.rgb * ambient;
  
    //color = clamp(color, 0, 1);
    FragColor = vec4(color, texColor.a);
}


 