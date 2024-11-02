--Vert

#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aColor;
layout (location = 3) in float aType;

out vec2 TexCoord;
out vec3 Color;
out float FogFactor;
out float Type;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

uniform vec3 cameraPosition;
uniform float fogDensity;

void fog(vec4 worldPosition)
{
    float distance = length(worldPosition.xyz - cameraPosition);
    FogFactor = exp(-pow(fogDensity * distance, 2.0));
}

void main()
{
    vec4 worldPosition = vec4(aPosition, 1.0) * model * view * projection;
    gl_Position = worldPosition;
    TexCoord = aTexCoord;
    Color = aColor;
    Type = aType;
    fog(worldPosition);
}



--Frag


#version 330 core

in vec2 TexCoord;
in vec3 Color;
in float FogFactor;
in float Type;

out vec4 FragColor;

uniform sampler2D textureAtlas;

uniform vec3 fogColor;
uniform vec3 ambientColor;

vec4 applyFog(vec4 texColor)
{
    return mix(vec4(fogColor, 1.0), texColor, FogFactor);
}

void main()
{
    if (Type < 0.5)
    {
        FragColor = vec4(Color, 1.0);
    }
    else
    {
        vec4 texColor = texture(textureAtlas, TexCoord) * vec4(Color, 1.0) * vec4(ambientColor, 1.0);
        FragColor = applyFog(texColor);
        if (FragColor.a < 0.1)
            discard;
    }
}

