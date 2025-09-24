--Vert

#version 330 core

layout(location = 0) in vec3 aPos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec2 size;

out vec2 TexCoord;

void main()
{
    vec3 worldOrigin = (model * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    vec3 camRight   = normalize(vec3(view[0][0], view[1][0], view[2][0]));
    vec3 camUp      = normalize(vec3(view[0][1], view[1][1], view[2][1]));
    vec3 worldPos   = worldOrigin
                     + camRight * aPos.x * size.x
                     + camUp    * aPos.y * size.y;
    gl_Position     = projection * view * vec4(worldPos, 1.0);
    TexCoord        = aPos.xy * 1 + 0.5;
}



--Frag

#version 330 core

in  vec2 TexCoord;
out vec4 FragColor;
uniform sampler2D tex;
uniform vec4 color = vec4(1,1,1,1);

void main()
{
     vec4 textureColor = texture(tex, TexCoord);
    if(textureColor.a < 0.5) discard;

    FragColor = textureColor * color;
}
