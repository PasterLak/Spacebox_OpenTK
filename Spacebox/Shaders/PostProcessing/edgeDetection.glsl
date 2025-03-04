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
uniform float edgeThreshold;
uniform vec2 texelSize;

void main()
{
    
    float tl = texture(scene, TexCoords + vec2(-texelSize.x, texelSize.y)).r;
    float t  = texture(scene, TexCoords + vec2(0.0, texelSize.y)).r;
    float tr = texture(scene, TexCoords + vec2(texelSize.x, texelSize.y)).r;
    float l  = texture(scene, TexCoords + vec2(-texelSize.x, 0.0)).r;
    float c  = texture(scene, TexCoords).r;
    float r  = texture(scene, TexCoords + vec2(texelSize.x, 0.0)).r;
    float bl = texture(scene, TexCoords + vec2(-texelSize.x, -texelSize.y)).r;
    float b  = texture(scene, TexCoords + vec2(0.0, -texelSize.y)).r;
    float br = texture(scene, TexCoords + vec2(texelSize.x, -texelSize.y)).r;

  
    float gx = -tl - 2.0 * l - bl + tr + 2.0 * r + br;
    float gy = -tl - 2.0 * t - tr + bl + 2.0 * b + br;
    float g = sqrt(gx * gx + gy * gy);

   
    if(g > edgeThreshold)
        FragColor = vec4(1.0);
    else
        FragColor = texture(scene, TexCoords);
}