--Vert


#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 cameraPosition;

out vec3 Normal;
out vec3 FragPos;
out vec2 TexCoord;
out float FogFactor;
float calcFog(vec4 worldPosition, float distanceMultiplicator)
{
    float distance = length(worldPosition.xyz - cameraPosition) * distanceMultiplicator;
    return exp(-pow(0.015 * distance, 2.0));
}
void main()
{
    vec4 worldPosition = vec4(aPos, 1.0) * model;
    gl_Position = worldPosition * view * projection;
    FragPos = vec3(worldPosition);
    Normal = mat3(transpose(inverse(model))) * aNormal;
    TexCoord = aTexCoords;
    FogFactor = calcFog(worldPosition, 1.0);
}



--Frag


#version 330 core
in vec2 TexCoord;
in float FogFactor;
out vec4 FragColor;
uniform sampler2D texture0;
uniform vec4 color = vec4(1.0);
uniform vec3 fogColor = vec3(0.05, 0.05, 0.05);
void main()
{
    vec4 pixel = texture(texture0, TexCoord);
    if(pixel.a < 0.1)
        discard;
    vec4 finalColor = pixel * color;
    FragColor = mix(vec4(fogColor, finalColor.a), finalColor, color);
}
