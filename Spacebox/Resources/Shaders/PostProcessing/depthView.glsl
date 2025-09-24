--Vert
#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUv;
out vec2 uv;

void main() {
    uv = aUv;
    gl_Position = vec4(aPos, 0.0, 1.0);
}

--Frag
#version 330 core
in vec2 uv;
out vec4 FragColor;

uniform sampler2D uDepthMap;
uniform float uNear;
uniform float uFar;

float LinearizeDepth(float depth)
{
    float z = depth * 2.0 - 1.0;
    return (2.0 * uNear * uFar) / (uFar + uNear - z * (uFar - uNear));
}

void main()
{
    float zNdc = texture(uDepthMap, uv).r;
    float depth = LinearizeDepth(zNdc);
    float gray = clamp(1.0 - depth / uFar, 0.0, 1.0);

    FragColor = vec4(vec3(gray), 1.0);

    //FragColor = vec4(vec3(texture(uDepthMap, uv).r), 1.0);
}

