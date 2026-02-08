--Vert

#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

uniform mat4 model;


uniform vec2 offset;  
uniform vec2 tiling = vec2(1,1); 

out vec2 TexCoords;

void main()
{
    gl_Position = vec4(aPos, 1.0) * model * VIEW * PROJECTION;
   
    TexCoords = aTexCoords;
}

--Frag

#version 330 core

in vec2 TexCoords;
out vec4 FragColor;
uniform sampler2D mainTexture;

void main()
{
    vec4 texColor = texture(mainTexture, TexCoords);
    
    vec3 color = texColor.rgb;

    FragColor = vec4(color, texColor.a);
}


 