--Vert

#version 330 core

layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;

out vec2 TexCoord;

void main()
{
    // Pass the texture coordinates to the fragment shader
    TexCoord = aTexCoord;
    // Set the position of the vertex
    gl_Position = vec4(aPosition, 0.0, 1.0);
}


--Frag

#version 330 core

in vec2 TexCoord; // Incoming texture coordinates
out vec4 FragColor; // Output fragment color

uniform sampler2D spriteTexture; // 2D texture sampler

void main()
{
    // Sample the texture color, including the alpha channel
    //vec4 texColor = texture(spriteTexture, TexCoord);
    
    // Set the fragment color to the sampled texture color
    // The alpha channel is automatically included

    FragColor = vec4(1,1,1,1);
}

