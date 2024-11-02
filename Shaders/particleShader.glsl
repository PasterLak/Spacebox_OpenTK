﻿--Vert

#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in mat4 instanceModel;
layout (location = 6) in vec4 instanceColor;

out vec2 TexCoords;
out vec4 ParticleColor;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec3 translation = vec3(instanceModel[3]);
    vec3 camRight = vec3(view[0][0], view[1][0], view[2][0]);
    vec3 camUp = vec3(view[0][1], view[1][1], view[2][1]);
    float scale = length(vec3(instanceModel[0]));
    vec3 worldPos = translation + (aPos.x * camRight + aPos.y * camUp) * scale;
    gl_Position = projection * view * vec4(worldPos, 1.0);
    TexCoords = aTexCoords;
    ParticleColor = instanceColor;
}
    
--Frag

#version 330 core

in vec2 TexCoords;
in vec4 ParticleColor;

out vec4 FragColor;

uniform sampler2D particleTexture;
uniform vec3 color = vec3(1,1,1);


void main()
{
    vec4 texColor = texture(particleTexture, TexCoords);

   
    vec3 clampedColor = clamp(color, vec3(0.5), vec3(0.9)) ;


    FragColor = texColor * ParticleColor * vec4(clampedColor,1);

}


