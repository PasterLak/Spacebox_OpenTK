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
out vec4 FragColor;
in vec2 TexCoords;
uniform sampler2D scene;
void main()
{
    FragColor = texture(scene, TexCoords);
    //FragColor = vec4(1,0.3,0,1);
}
