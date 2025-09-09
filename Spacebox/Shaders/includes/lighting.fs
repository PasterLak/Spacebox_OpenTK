

// lighting.fs
// use:   #include "includes/lighting.fs"
#define MAX_DIR    4
#define MAX_POINT 64
#define MAX_SPOT  32

struct LightsCount
{ int dir; int point;
  int spot; int pad; 
 };

struct DirLight
{ vec3 direction; float intensity;
 vec3 diffuse; float pad2; 
 vec3 specular; float pad3; };

struct PointLight
{ vec3 position; float constant;
 vec3 diffuse; float quadratic; 
 vec3 specular; float linear;
 float intensity;float pad0;float pad1;float pad2; };

struct SpotLight
{ vec3 position; float constant; 
vec3 direction; float linear; 
float innerCut; float outerCut; 
float quadratic; float intensity; 
 vec3 diffuse; float pad2;
  vec3 specular; float pad3; };

layout(std140) uniform LightsCountBlock   { LightsCount   lightsCount; };
layout(std140) uniform DirBlock   { DirLight   dirLights  [MAX_DIR  ]; };
layout(std140) uniform PointBlock { PointLight pointLights[MAX_POINT]; };
layout(std140) uniform SpotBlock  { SpotLight  spotLights [MAX_SPOT ]; };


vec4 fogMix(vec4 color,float factor, vec3 fogColor)
{return mix(vec4(fogColor,color.a),color,factor);}

vec3 accumulateDirLights(vec3 N, vec3 V, vec3 baseCol)
{
    vec3 sum = vec3(0.0);

    int count = lightsCount.dir;
    if(count==0) return vec3(0);

    vec3 ambientFinal = AMBIENT * baseCol;

    for(int i = 0; i < count; ++i)
    {
        float I = dirLights[i].intensity;

        vec3 dir = dirLights[i].direction;

        vec3 L = normalize(-dir);
        float diff = max(dot(N, L), 0.0);
        vec3 halfVec = normalize(L + V);
        float spec = pow(max(dot(N, halfVec), 0.0), 32.0);

        sum += ambientFinal * I +
               dirLights[i].diffuse  * diff    * baseCol * I +
               dirLights[i].specular * spec               * I;
    }
    return sum;
}

vec3 calcPointLight(PointLight L, vec3 N, vec3 V, vec3 P, 
vec3 baseCol, vec3 ambientFinal)
{

    float I = L.intensity;
    vec3 Ldir = L.position - P;
    float dist = length(Ldir);
    Ldir = normalize(Ldir);

    float atten = 1.0 / (L.constant + L.linear * dist + L.quadratic * dist * dist);
    float diff  = max(dot(N, Ldir), 0.0);
    vec3 halfV  = normalize(Ldir + V);
    float spec  = pow(max(dot(N, halfV), 0.0), 32.0);

    return (ambientFinal * I+
            L.diffuse * diff * baseCol * I +
            L.specular * spec) * atten * I;
}

vec3 accumulatePointLightsWithAmbient(vec3 N, vec3 V, vec3 P, vec3 baseCol, vec3 ambient)
{
    vec3 sum = vec3(0.0);
    int count = lightsCount.point;
    if(count==0) return vec3(0);

    vec3 ambientFinal = ambient * baseCol ;

    for(int i = 0; i < count; ++i)
        sum += calcPointLight(pointLights[i], N, V, P, baseCol,ambientFinal);
    return sum;
}

vec3 accumulatePointLights(vec3 N, vec3 V, vec3 P, vec3 baseCol)
{
  
    return accumulatePointLightsWithAmbient(N,V,P,baseCol, AMBIENT);
}

vec3 calcSpotLight(SpotLight L, vec3 N, vec3 V, vec3 P, vec3 baseCol, vec3 ambientFinal)
{

    float span = L.innerCut - L.outerCut;
    if(span <= 0.0) return vec3(0.0);

    float I = L.intensity;

    vec3 Ldir = L.position - P;
    float dist = length(Ldir);
    Ldir = normalize(Ldir);

    float theta = dot(Ldir, normalize(-L.direction));
    float spot  = clamp((theta - L.outerCut) / span, 0.0, 1.0);

    float atten = 1.0 / (L.constant + L.linear * dist + L.quadratic * dist * dist);
    float diff  = max(dot(N, Ldir), 0.0);
    vec3 halfV  = normalize(Ldir + V);
    float spec  = pow(max(dot(N, halfV), 0.0), 32.0);

    return (ambientFinal * I +
            L.diffuse * diff * baseCol * I +
            L.specular * spec) * atten * spot * I;
}

vec3 accumulateSpotLightsWithAmbient(vec3 N, vec3 V, vec3 P, vec3 baseCol, vec3 ambient)
{
    vec3 sum = vec3(0.0);
     int count = lightsCount.spot;
     if(count==0) return vec3(0);

     vec3 ambientFinal = ambient * baseCol;
    for(int i = 0; i < count; ++i){

        if(spotLights[i].intensity == 0.0) continue;

        sum += calcSpotLight(spotLights[i], N, V, P, baseCol,ambientFinal);
    }
        
    return sum;
}

vec3 accumulateSpotLights(vec3 N, vec3 V, vec3 P, vec3 baseCol)
{
    return accumulateSpotLightsWithAmbient(N,V,P,baseCol, AMBIENT);
}