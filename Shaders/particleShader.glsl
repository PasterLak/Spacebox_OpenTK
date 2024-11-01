--Vert

#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;


layout (location = 2) in mat4 instanceModel;
layout (location = 6) in vec4 instanceColor;

out vec2 TexCoord;
out vec4 ParticleColor;

uniform mat4 view;
uniform mat4 projection;

void main()
{
   
    vec4 worldPos = instanceModel * vec4(aPos, 1.0);
    
  
    gl_Position = projection * view * worldPos;
    
    TexCoord = aTexCoord;
    ParticleColor = instanceColor;
}


--Frag

#version 330 core

out vec4 FragColor;

in vec2 TexCoord;
in vec4 ParticleColor;

uniform sampler2D particleTexture;

void main()
{
    vec4 texColor = texture(particleTexture, TexCoord);
    FragColor = texColor * ParticleColor;
    
   
    if (FragColor.a < 0.1)
        discard;
}
