--Vert
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;

void main()
{
    gl_Position = vec4(aPos, 0.0, 1.0);
}

--Frag
#version 330 core
out float outAo;

uniform sampler2D uAo;
uniform sampler2D uDepth;
uniform vec2 uDirection;
uniform int uRadius;

void main()
{
    ivec2 pixel = ivec2(gl_FragCoord.xy);
    ivec2 size = textureSize(uAo, 0);
    float centerAo = texelFetch(uAo, pixel, 0).r;
    float centerStored = texelFetch(uDepth, pixel, 0).r;
    if (centerStored == 0.0)
    {
        outAo = centerAo;
        return;
    }

    bool viewModel = centerStored < 0.0;
    float centerDepth = abs(centerStored);
    float sharpness = 1.0 / max(centerDepth * 0.05, 0.001);
    float sigma = float(uRadius + 1) * 0.5;
    float falloff = 1.0 / (2.0 * sigma * sigma);
    ivec2 stride = ivec2(uDirection);

    float sum = centerAo;
    float weightSum = 1.0;
    for (int i = -uRadius; i <= uRadius; ++i)
    {
        if (i == 0) continue;
        ivec2 tap = clamp(pixel + stride * i, ivec2(0), size - 1);
        float stored = texelFetch(uDepth, tap, 0).r;
        if (stored == 0.0 || (stored < 0.0) != viewModel) continue;

        float weight = exp(-float(i * i) * falloff) * max(0.0, 1.0 - abs(abs(stored) - centerDepth) * sharpness);
        sum += texelFetch(uAo, tap, 0).r * weight;
        weightSum += weight;
    }

    outAo = sum / weightSum;
}
