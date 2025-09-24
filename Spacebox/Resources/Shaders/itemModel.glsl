--Vert
#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;
uniform mat4 projection;
uniform mat4 view;
uniform mat4 model;
out vec3 Normal;
out vec3 FragPos;
out vec2 TexCoords;
out vec3 vPos;
out vec4 screenPos;
void main()
{
   vec4 wp = vec4(aPosition, 1.0) * model;
   gl_Position = wp * view * projection;
   FragPos = vec3(vec4(aPosition, 1.0) * model);
   Normal = aNormal;
   TexCoords = aTexCoord;
   vPos = wp.xyz;
   screenPos = gl_Position;
}

--Frag
#version 330 core
#include "includes/lighting.fs"

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoords;
in vec3 vPos;
in vec4 screenPos;

layout(location = 0) out vec4 gColor;   
layout(location = 1) out vec4 gNormal; 

uniform sampler2D texture0;
uniform sampler2D texture1;
uniform float pointLightStrength = 0.5;
uniform vec3 flashlightColor = vec3(0.96, 0.87, 0.67);

const vec3 lightDir = vec3(1, 1, 1);
const vec3 lightColor = vec3(1, 1, 1);
const float shadows = 0.2;
const float flashlightRadius = 0.6;
const float flashlightIntensity = 1.7;
const float flashlightFalloff = 0.9;
const vec2 flashlightCenter = vec2(0.0, -0.4);

void main()
{
   vec4 texColor = texture(texture0, TexCoords);
   if(texColor.a < 0.1)
       discard;

   vec3 norm = normalize(Normal);
   vec3 V = normalize(CAMERA_POS - vPos);
   vec3 base = texColor.rgb;
   
   vec3 lightDirection = normalize(lightDir);
   float diff = max(dot(norm, lightDirection), shadows);
   vec3 shading = diff * lightColor;
   
   vec3 ambientBase = base * clamp(0.3 + AMBIENT * 0.6, 0.5, 1);
   vec3 directionalLight = shading * ambientBase;
   
   vec3 pointLight = accumulatePointLightsWithAmbient(norm, V, vPos, base, vec3(0.1)) * pointLightStrength;
   float shadowFactor = smoothstep(shadows, 1.0, diff);
   pointLight *= shadowFactor;
   
   vec2 ndc = screenPos.xy / screenPos.w;
   float distanceFromCenter = length(ndc - flashlightCenter);
   
   float flashlightFactor = 1.0 - smoothstep(0.0, flashlightRadius, distanceFromCenter);
   flashlightFactor = pow(flashlightFactor, flashlightFalloff);
   flashlightFactor = clamp(flashlightFactor, 0.0, 1.0);
   
   float surfaceAngle = max(dot(norm, V), 0.0);
   flashlightFactor *= surfaceAngle;
   
   vec3 flashlightContribution = base * flashlightColor * flashlightIntensity * flashlightFactor;
   
   vec3 combined = directionalLight + pointLight + flashlightContribution;
   vec4 emission = texture(texture1, TexCoords);
   vec3 final = mix(combined, base, emission.a);
   
   gColor = vec4(final, texColor.a);
   gNormal = vec4(norm * 0.5 + 0.5, 1.0);
}