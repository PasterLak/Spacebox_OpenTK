--Vert
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;
out vec2 TexCoords;

void main()
{
    TexCoords = aTexCoords;
    gl_Position = vec4(aPos, 0.0, 1.0);
}

--Frag
#version 330 core
in vec2 TexCoords;
out vec4 FragColor;

uniform sampler2D uNormalMap;

void main()
{
    vec3 normal = texture(uNormalMap, TexCoords).rgb;
   // FragColor = vec4(normal * 0.5 + 0.25, 1.0);  for world normals

    FragColor = vec4(normal * 0.5 + 0.5, 1.0);
}
