--Vert
#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoords;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out vec2 vTex;

void main()
{

    gl_Position = vec4(aPos, 1.0) * model * view * projection;
    vTex = aTexCoords;
}



--Frag
#version 330 core
in  vec2 vTex;
out vec4 FragColor;

uniform sampler2D texture0;
uniform vec4      color = vec4(1.0);

void main()
{
    vec4 tex = texture(texture0, vTex);
    FragColor = tex * color;     
}
