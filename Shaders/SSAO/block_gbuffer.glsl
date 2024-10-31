--Vert

#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec2 aTexCoord;

out vec2 TexCoord;
out vec3 FragPos;
out vec3 Normal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
   
    FragPos = vec3(model * vec4(aPosition, 1.0));
  
    Normal = mat3(transpose(inverse(model))) * vec3(0.0, 1.0, 0.0);
    TexCoord = aTexCoord;
    gl_Position = projection * view * vec4(FragPos, 1.0);
}


--Frag

#version 330 core

in vec2 TexCoord;
in vec3 FragPos;
in vec3 Normal;

layout (location = 0) out vec3 gPosition;
layout (location = 1) out vec3 gNormal;
layout (location = 2) out vec4 gAlbedoSpec;

uniform sampler2D texture0;     
uniform sampler2D textureAtlas; 

void main()
{
    vec4 baseTexColor = texture(texture0, TexCoord);
    vec4 atlasTexColor = texture(textureAtlas, TexCoord);
    vec4 mixedColor = mix(baseTexColor, atlasTexColor, atlasTexColor.a);

    if (mixedColor.a < 0.1)
        discard;

    gPosition = FragPos;
    gNormal = normalize(Normal);
    gAlbedoSpec.rgb = mixedColor.rgb;
    gAlbedoSpec.a = 1.0; 
}
