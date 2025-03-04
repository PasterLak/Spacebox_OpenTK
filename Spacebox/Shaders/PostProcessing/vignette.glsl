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
uniform float vignetteStrength;
uniform float vignetteRadius;
uniform float vignetteSoftness;

void main()
{
    vec4 color = texture(scene, TexCoords);
    float dist = distance(TexCoords, vec2(0.5, 0.5));
    float vig = smoothstep(vignetteRadius  - vignetteSoftness, vignetteRadius , dist );
    FragColor = vec4(color.rgb * (1.0 - vignetteStrength * vig), color.a);
}