--Vert
#version 330 core


layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec3 vertexNormal;
layout(location = 2) in vec2 vertexTexCoords;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec3 worldNormal;
out vec3 worldPosition;
out vec2 texCoords;
out float fogFactor;
// купупкыурукырыр
#define FOG_LN2 1.44269504

float fogFac(vec3 p, float k, float density)
{
    float d2   = dot(p - CAMERA_POS, p - CAMERA_POS);   
    float kd2  = k * k * d2;                            
    return exp2(-(density) * kd2 * FOG_LN2);       
}

void main()
{
    vec4 posWorld = vec4(vertexPosition, 1.0) * model;
    gl_Position   = posWorld * view * projection;
    worldPosition = posWorld.xyz;
    worldNormal   = mat3(transpose(inverse(model))) * vertexNormal;
    texCoords     = vertexTexCoords;
    fogFactor     = fogFac(posWorld.xyz, 1.0, FOG_DENSITY);
}

--Frag
#version 330 core
#include "includes/lighting.fs"


uniform vec4 color = vec4(1,1,1,1);

in vec3 worldPosition;
in vec3 worldNormal;
in vec2 texCoords;
in float fogFactor;

out vec4 finalColor;

uniform sampler2D texture0;


void main()
{
    vec4 textureColor = texture(texture0, texCoords);
    if(textureColor.a < 0.1) discard;

    vec3 N = normalize(worldNormal);
    vec3 V = normalize(CAMERA_POS - worldPosition);
    vec3 baseColor = textureColor.rgb;
   
    vec3 lighting =
          AMBIENT +
          accumulateDirLights (N, V, baseColor) +
          accumulatePointLights(N, V, worldPosition, baseColor) +
          accumulateSpotLights(N, V, worldPosition, baseColor);

    vec4 shaded = vec4(baseColor * color.rgb * lighting, textureColor.a);

    shaded = fogMix(shaded, fogFactor, FOG);
    
    finalColor  = shaded;
}
