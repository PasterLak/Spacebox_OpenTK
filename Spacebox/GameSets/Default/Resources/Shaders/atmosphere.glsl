--Vert
#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec3 Normal;
out vec3 FragPos;
out vec2 TexCoord;

void main()
{
    gl_Position = vec4(aPos, 1.0) * model * view * projection;
    FragPos = vec3(vec4(aPos, 1.0) * model);
    Normal =   aNormal * mat3(transpose(inverse(model)));

    TexCoord = aTexCoords;
}

--Frag

#version 330 core
in vec2 TexCoord;
in vec3 Normal;
in vec3 FragPos;
out vec4 FragColor;

uniform sampler2D texture0;
uniform vec4 color = vec4(1,1,1,1);
uniform vec3 cameraPos;
uniform vec3 glowColor;
uniform float glowIntensity;
uniform vec3 sunPos;

vec3 CalcSunLighting(vec3 norm, vec3 fragPos, vec3 viewDir) {
    vec3 lightDir = normalize(sunPos - fragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
    vec3 ambient = vec3(0.2);
    vec3 diffuse = vec3(1.0) * diff;
    vec3 specular = vec3(1.0) * spec;
    return ambient + diffuse + specular;
}

void main()
{
    vec4 texColor = texture(texture0, TexCoord);

    vec3 norm = normalize(Normal);
    vec3 viewDir = normalize(cameraPos - FragPos);
    vec3 lighting = CalcSunLighting(norm, FragPos, viewDir);
    vec4 litColor = vec4(texColor.rgb * lighting, texColor.a);
    float fresnel = pow(1.0 - dot(norm, viewDir), 3.0);
    vec4 glow = vec4(glowColor, 1.0) * glowIntensity * fresnel;
    FragColor = (litColor * color) ;
}
