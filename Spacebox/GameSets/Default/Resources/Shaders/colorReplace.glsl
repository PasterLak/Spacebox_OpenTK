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
uniform sampler2D scene;
void main()
{
    vec4 col = texture(scene, TexCoords);
    if(col.r > 0.5)
        FragColor = vec4(0.0, 1.0, 0.0, col.a);
    else
        FragColor = col;
}