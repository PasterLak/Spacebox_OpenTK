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
uniform float fogDensity = 0.2; // standart 0.05

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
uniform vec3 ambientColor = vec3(1,1,1);

vec4 applyFog(vec4 texColor)
{
    return mix(vec4(fogColor, 1.0), texColor, FogFactor);
}

void main()
{
    vec4 baseTexColor = texture(texture0, TexCoord) * vec4(Color, 1.0) * vec4(ambientColor, 1.0);

    if (baseTexColor.a < 0.1)
        discard;

    vec4 foggedBaseColor = applyFog(baseTexColor);
    vec4 atlasTexColor = texture(textureAtlas, TexCoord);
    vec4 mixedColor = mix(foggedBaseColor, atlasTexColor, atlasTexColor.a);
    mixedColor.a = max(foggedBaseColor.a, atlasTexColor.a);

    

    FragColor = mixedColor;
}


