--Vert
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoords;

out vec2 vUV;

void main()
{
    vUV = aTexCoords;
    gl_Position = vec4(aPos, 0.0, 1.0);
}

--Frag
#version 330 core
#define DEBUG_MODE 0
#define SSAO_SAMPLE_COUNT 32
#define SSAO_RADIUS 0.6
#define SSAO_BIAS 0.02
#define SSAO_POWER 1.0
#define SSAO_STRENGTH 1.0
#define SSAO_USE_NOISE 1
#define SSAO_NOISE_STRENGTH 1.0

in vec2 vUV;
out vec4 FragColor;

uniform sampler2D uColor;
uniform sampler2D uNormalMap;
uniform sampler2D uDepthMap;
uniform sampler2D uNoiseMap;
uniform mat4 uProjection;
uniform mat4 uInvProjection;
uniform vec2 uNoiseScale;
uniform vec3 uSamples[64];

vec3 ReconstructViewPos(vec2 uv, float depthWin){
    vec2 ndc = uv * 2.0 - 1.0;
    float zndc = depthWin * 2.0 - 1.0;
    vec4 clip = vec4(ndc, zndc, 1.0);
    vec4 view = uInvProjection * clip;
    return view.xyz / view.w;
}

vec2 ProjectToUV(vec3 viewPos){
    vec4 clip = uProjection * vec4(viewPos, 1.0);
    vec3 ndc = clip.xyz / clip.w;
    return ndc.xy * 0.5 + 0.5;
}

void main(){
#if DEBUG_MODE==1
    FragColor = vec4(texture(uNormalMap, vUV).xyz,1);
    return;
#elif DEBUG_MODE==2
    float d = texture(uDepthMap, vUV).r;
    FragColor = vec4(vec3(d),1);
    return;
#endif

    vec3 n = normalize(texture(uNormalMap, vUV).xyz * 2.0 - 1.0);
    float depthCenter = texture(uDepthMap, vUV).r;
    if (depthCenter >= 1.0){
        FragColor = texture(uColor, vUV);
        return;
    }

    vec3 posVS = ReconstructViewPos(vUV, depthCenter);

    vec3 randVec = vec3(1,0,0);
#if SSAO_USE_NOISE
    randVec = texture(uNoiseMap, vUV * uNoiseScale).xyz * 2.0 - 1.0;
#endif
    randVec = mix(vec3(1,0,0), randVec, clamp(SSAO_NOISE_STRENGTH,0.0,1.0));

    vec3 tangent = normalize(randVec - n * dot(randVec, n));
    if (length(tangent) < 1e-4) tangent = normalize(vec3(-n.y, n.x, 0.0));
    vec3 bitangent = normalize(cross(n, tangent));
    mat3 TBN = mat3(tangent, bitangent, n);

    float occSum = 0.0;
    for (int i = 0; i < SSAO_SAMPLE_COUNT; ++i){
        vec3 offsetVS = TBN * uSamples[i];
        vec3 samplePosVS = posVS + offsetVS * SSAO_RADIUS;
        vec2 sampleUV = ProjectToUV(samplePosVS);
        if (sampleUV.x < 0.0 || sampleUV.x > 1.0 || sampleUV.y < 0.0 || sampleUV.y > 1.0) continue;
        float sampleDepthWin = texture(uDepthMap, sampleUV).r;
        if (sampleDepthWin >= 1.0) continue;
        float sampleZ = ReconstructViewPos(sampleUV, sampleDepthWin).z;
        float hit = (sampleZ > samplePosVS.z + SSAO_BIAS) ? 1.0 : 0.0;
        float range = SSAO_RADIUS / max(abs(posVS.z - sampleZ), 1e-3);
        occSum += hit * clamp(range, 0.0, 1.0);
    }

    float ao = 1.0 - (occSum / float(SSAO_SAMPLE_COUNT));
    ao = pow(clamp(ao,0.0,1.0), SSAO_POWER);
#if DEBUG_MODE==3
    FragColor = vec4(vec3(ao),1);
    return;
#endif
    vec3 base = texture(uColor, vUV).rgb;
    vec3 outCol = mix(base, base * ao, clamp(SSAO_STRENGTH,0.0,1.0));
    FragColor = vec4(outCol,1);
}
