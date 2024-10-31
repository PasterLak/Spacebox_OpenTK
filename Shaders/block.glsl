--Vert

#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aColor;

out vec2 TexCoord;
out vec3 Color;
out float FogFactor; 

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

uniform vec3 cameraPosition = vec3(0,0,0);
uniform float fogDensity = 0.08; // standart 0.05

uniform vec3 globalOffset;

void fog(vec4 worldPosition)
{
    float distance = length(worldPosition.xyz - cameraPosition);
    FogFactor = exp(-pow(fogDensity * distance, 2.0)); 
}

void main()
{


    vec4 worldPosition = vec4(aPosition, 1.0) * model;
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
    TexCoord = aTexCoord;
    Color = aColor;
    fog(worldPosition);
}



--Frag

#version 330 core

in vec2 TexCoord;
in vec3 Color;
in float FogFactor;

out vec4 FragColor;

uniform sampler2D texture0;     
uniform sampler2D textureAtlas; 

uniform vec3 fogColor = vec3(1,0,0);
uniform vec3 ambientColor = vec3(0.2, 0.2, 0.2); 

vec4 applyFog(vec4 texColor)
{
    return mix(vec4(fogColor, texColor.a), texColor, FogFactor);
}

void main()
{
    vec4 baseTexColor = texture(texture0, TexCoord);
    vec4 atlasTexColor = texture(textureAtlas, TexCoord);

    if (baseTexColor.a < 0.1)
        discard;

    
    vec3 finalColor = baseTexColor.rgb * (Color + ambientColor);

   
    vec4 foggedColor = applyFog(vec4(finalColor, baseTexColor.a));

    
    vec3 combinedColor = mix(foggedColor.rgb, atlasTexColor.rgb, atlasTexColor.a);

    FragColor = vec4(combinedColor, foggedColor.a);
}




