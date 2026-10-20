layout(location = 1) out vec4 gNormal;

const float GBUFFER_MASK_NONE = 0.0;
const float GBUFFER_MASK_VIEW_MODEL = 0.5;
const float GBUFFER_MASK_WORLD = 1.0;

void writeGBuffer(vec3 viewNormal, float mask)
{
    gNormal = vec4(normalize(viewNormal) * 0.5 + 0.5, mask);
}

void writeGBufferNone()
{
    gNormal = vec4(0.0);
}
