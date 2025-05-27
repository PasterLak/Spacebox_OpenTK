﻿--Vert
#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUv;
out vec2 uv;
void main() { uv = aUv; gl_Position = vec4(aPos,0,1); }

--Frag
#version 330 core
in  vec2 uv;
out vec4 FragColor;

uniform sampler2D uDepthMap;
uniform float     uNear;
uniform float     uFar;

float LinDepth(float z)               
{
    float clip = z * 2.0 - 1.0;
    return (2.0 * uNear * uFar) /
           (uFar + uNear - clip * (uFar - uNear));
}

void main()
{
    ivec2 ts   = textureSize(uDepthMap, 0);
    float zNdc = texelFetch(uDepthMap, ivec2(uv * ts), 0).r;  
    float d    = LinDepth(zNdc) / uFar;                     
           
    d          = 1.0 - d;                                   
    FragColor  = vec4(vec3(d), 1.0);
}
