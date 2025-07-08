

vec4 applyFog(vec4 texColor)
{
    return mix(vec4(fog, texColor.a), texColor, FogFactor);
}