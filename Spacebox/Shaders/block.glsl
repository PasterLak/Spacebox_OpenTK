--Vert

#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aColor;
layout (location = 3) in vec3 aNormal;
layout (location = 4) in vec2 aAO;


out vec2 TexCoord;
out vec3 Color;
out float FogFactor;
out float FogFactor2;
out vec3 Normal;
out vec3 FragPos;
out float AO;
out float isActive;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 cameraPosition = vec3(0,0,0);
uniform float fogDensity = 0.08;

float calcFog(vec4 worldPosition, float distanceMultiplicator)
{
    float distance = length(worldPosition.xyz - cameraPosition) * distanceMultiplicator;
    return exp(-pow(fogDensity*distance,2.0));
}

void main()
{
    vec4 worldPosition = vec4(aPosition,1.0)*model;
    gl_Position = worldPosition*view*projection;
    TexCoord = aTexCoord;
    Color = aColor;
    FragPos = vec3(worldPosition);
    Normal = aNormal * mat3(transpose(inverse(model)));
    FogFactor = calcFog(worldPosition,1);
    FogFactor2 = calcFog(worldPosition,0.5f);
    AO = aAO.x;
    isActive = aAO.y;
}



--Frag

#version 330 core
struct SpotLight {
    vec3 position;
    vec3 direction;
    float cutOff;
    float outerCutOff;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float constant;
    float linear;
    float quadratic;
};
uniform SpotLight spotLight;
uniform sampler2D texture0;
uniform sampler2D textureAtlas;
uniform vec3 viewPos;
uniform float material_shininess;
uniform vec3 fogColor=vec3(1,0,0);
uniform vec3 ambientColor=vec3(0.2,0.2,0.2);

in vec2 TexCoord;
in vec3 Color;
in float FogFactor;
in float FogFactor2;
in vec3 Normal;
in vec3 FragPos;
in float AO;
in float isActive;
out vec4 FragColor;

vec3 calcSpotLight(SpotLight light,vec3 normal,vec3 fragPos,vec3 viewDir,vec3 diffC)
{
    vec3 lightDir=normalize(light.position-fragPos);
    float diff=max(dot(normal,lightDir),0.0);
    vec3 reflectDir=reflect(-lightDir,normal);
    float spec=pow(max(dot(viewDir,reflectDir),0.0),material_shininess);
    float distance=length(light.position-fragPos);
    float attenuation=1.0/(light.constant+light.linear*distance+light.quadratic*(distance*distance));
    float theta=dot(lightDir,normalize(-light.direction));
    float epsilon=light.cutOff-light.outerCutOff;
    float intensity=clamp((theta-light.outerCutOff)/epsilon,0.0,1.0);
    vec3 ambient=light.ambient*diffC;
    vec3 diffuse=light.diffuse*diff*diffC;
    vec3 specular=light.specular*spec*vec3(1.0);
    ambient*=attenuation;
    diffuse*=attenuation*intensity;
    specular*=attenuation*intensity;
    return (ambient+diffuse+specular);
}

vec4 applyFog(vec4 texColor)
{
    return mix(vec4(fogColor,texColor.a),texColor,FogFactor);
}

vec4 applyFog2(vec4 texColor)
{
    return mix(vec4(fogColor,texColor.a),texColor,FogFactor2);
}

void main()
{
    vec4 baseTexColor=texture(texture0,TexCoord);
    vec4 atlasTexColor=texture(textureAtlas,TexCoord);

    if(baseTexColor.a<0.1) discard;

    vec3 norm=normalize(Normal);
    vec3 viewDir=normalize(viewPos-FragPos);
    vec3 ambient=baseTexColor.rgb*(ambientColor+Color);
    vec3 lighting=calcSpotLight(spotLight,norm,FragPos,viewDir,baseTexColor.rgb);
    vec3 finalColor=(ambient  *AO+lighting);
    vec4 foggedColor=applyFog(vec4(finalColor,baseTexColor.a));
    vec3 combinedColor=mix(foggedColor.rgb,applyFog2(vec4(baseTexColor.rgb,1)).rgb,isActive == 0 ? 0 : atlasTexColor.a); // atlasTexColor.a
    FragColor=vec4(combinedColor,foggedColor.a);

   
}
