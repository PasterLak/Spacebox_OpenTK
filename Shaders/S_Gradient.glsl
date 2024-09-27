--Vert

#version 330 core

layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;

out vec2 TexCoord;

void main()
{
  
    TexCoord = aTexCoord;
 
    gl_Position = vec4(aPosition, 0.0, 1.0);
}


--Frag

#version 330 core

in vec2 TexCoord; 
out vec4 FragColor;

uniform sampler2D spriteTexture;

uniform float time = 0;
uniform vec2 screen;


void main()
{
  
   
    //vec4 texColor = texture(spriteTexture, TexCoord);

  
   
    vec2 fragCoord2 = TexCoord * screen;

   
    vec2 uv = fragCoord2 / screen;

  
    vec3 col = 0.5 + 0.5 * cos(time + uv.xyx + vec3(0.0, 2.0, 4.0));

  
    FragColor = vec4(col, 1.0);
}

