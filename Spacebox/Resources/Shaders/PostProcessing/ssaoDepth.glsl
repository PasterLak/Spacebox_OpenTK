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
out float outDepth;

uniform sampler2D uSceneDepth;
uniform sampler2D uSceneNormal;
uniform vec4 uProjMain;
uniform vec4 uProjView;
uniform vec2 uSceneSize;
uniform int uDivisor;

float linearDepth(vec4 proj, float depthWindow)
{
    return proj.w / ((depthWindow * 2.0 - 1.0) + proj.z);
}

void main()
{
    ivec2 pixel = min(ivec2(gl_FragCoord.xy) * uDivisor, ivec2(uSceneSize) - 1);
    float mask = texelFetch(uSceneNormal, pixel, 0).a;
    if (mask < 0.25)
    {
        outDepth = 0.0;
        return;
    }

    float depthWindow = texelFetch(uSceneDepth, pixel, 0).r;
    outDepth = mask < 0.75 ? -linearDepth(uProjView, depthWindow) : linearDepth(uProjMain, depthWindow);
}
