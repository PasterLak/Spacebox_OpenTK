--Vert
#version 330 core
layout(location = 0) in vec3 vertexPosition;
layout(location = 1) in vec3 vertexNormal;
layout(location = 2) in vec2 vertexUV;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

#define SEA_OCTAVES   5
#define SEA_FREQ      0.14
#define SEA_HEIGHT    0.0015
#define SEA_CHOPPY    7.5
#define SEA_SPEED     0.65
#define SEA_UV_TILE   512.0
#define GRAD_EPS      0.05
#define FOG_LN2       1.44269504

const mat2 O_M = mat2(1.6,1.2,-1.2,1.6);

float hash(vec2 p){
    return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453123);
}
float noise(vec2 p){
    vec2 i=floor(p),f=fract(p),u=f*f*(3.0-2.0*f);
    return mix(mix(hash(i),hash(i+vec2(1,0)),u.x),
               mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),u.x),u.y)*2.0-1.0;
}
float sea_octave(vec2 uv,float choppy){
    uv+=noise(uv);
    vec2 wv=1.0-abs(sin(uv)),swv=abs(cos(uv));
    wv=mix(wv,swv,wv);
    return pow(1.0-pow(wv.x*wv.y,0.65),choppy);
}
float sea_height(vec2 uvFlow){
    float freq=SEA_FREQ,amp=SEA_HEIGHT,choppy=SEA_CHOPPY,h=0.0;
    for(int i=0;i<SEA_OCTAVES;++i){
        float d=sea_octave((uvFlow+TIME*SEA_SPEED)*freq,choppy);
        d+=sea_octave((uvFlow-TIME*SEA_SPEED)*freq,choppy);
        h+=d*amp;
        uvFlow*=O_M;freq*=1.8;amp*=0.22;
        choppy=mix(choppy,1.0,0.35);
    }
    return h;
}
float fog_fac(vec3 pos,float k){
    float d2=dot(pos-CAMERA_POS,pos-CAMERA_POS);
    return exp2(-FOG_DENSITY*k*k*d2*FOG_LN2);
}

out vec3 vWorldPos;
out vec2 vUV;
out float vFog;
out float vHeightN;
out vec3 vWorldNormal;

void main(){
    vec3 local=vertexPosition;
    vec2 uvSea=vertexPosition.xz*SEA_UV_TILE;
    float h=sea_height(uvSea);
    local.y+=h;
    float hDx=sea_height(uvSea+vec2(GRAD_EPS,0)) - sea_height(uvSea-vec2(GRAD_EPS,0));
    float hDz=sea_height(uvSea+vec2(0,GRAD_EPS)) - sea_height(uvSea-vec2(0,GRAD_EPS));
    vec3 n=normalize(vec3(-hDx, 2.0*GRAD_EPS, -hDz));
    vec4 wp=model*vec4(local,1.0);
    gl_Position=wp*view*projection;
    vWorldPos=wp.xyz;
    vWorldNormal=mat3(transpose(inverse(model))) * n;
    vUV=vertexUV;
    vFog=fog_fac(wp.xyz,1.0);
    vHeightN=clamp(h/(SEA_HEIGHT*3.0),-1.0,1.0);
}

--Frag
#version 330 core
#include "includes/lighting.fs"

#define BASE_COLOR               vec3(0.36,0.51,0.54)
#define FOAM_LOW                 0.22
#define FOAM_HIGH                0.98
#define SPEC_POW                 180.0
#define SPEC_COLOR               vec3(1.0)
#define DEEP_COLOR               vec3(0.36,0.51,0.54)*0.9
#define DEEP_EDGE                0.005
#define DEEP_SOFT                0.2

#define SUN_DIR                  normalize(vec3(-0.3,0.2,0))
#define SUN_COLOR_DAY            vec3(1.0,1.0,0.8) 
#define SUN_COLOR_EVE            vec3(1.0,0.2,0.1) * 1.5

#define FRONT_TINT_COLOR         vec3(1.0,0.3,0)
#define BACK_TINT_COLOR          vec3(0,1,0)
#define FRONT_TINT_INTENSITY     1
#define BACK_TINT_INTENSITY      0.1

#define BAND_POWER               45.0
#define GATHER_NEAR              12
#define GATHER_FAR               80.0

uniform sampler2D noiseMap;

in vec3 vWorldPos;
in vec2 vUV;
in float vFog;
in float vHeightN;
in vec3 vWorldNormal;

out vec4 fragColor;

void main(){
    vec3 N=normalize(vWorldNormal);
    vec3 V=normalize(CAMERA_POS - vWorldPos);

    float n=texture(noiseMap, vUV * 64.0 + TIME * vec2(-0.04,0.03)).r;
    vec3 base=BASE_COLOR*(0.8 + 0.4*n);

    float deepFac=smoothstep(DEEP_EDGE,DEEP_EDGE+DEEP_SOFT,vHeightN);
    base=mix(DEEP_COLOR,base,deepFac);

    float foam=smoothstep(FOAM_LOW,FOAM_HIGH,vHeightN);

    vec3 sunColor=mix(SUN_COLOR_EVE,SUN_COLOR_DAY,clamp(SUN_DIR.y*0.5+0.5,0.0,1.0));

    float facing=dot(N,SUN_DIR);
    float frontFactor=max(facing,0.0);
    float backFactor=max(-facing,0.0);

    base=mix(base, FRONT_TINT_COLOR * sunColor, foam * frontFactor * FRONT_TINT_INTENSITY);
    base=mix(base, BACK_TINT_COLOR * base, foam * backFactor * BACK_TINT_INTENSITY);

    base=mix(base, vec3(1.0), foam);

    vec3 color=base*(
        AMBIENT +
        accumulateDirLights(N,V,base) +
        accumulatePointLights(N,V,vWorldPos,base) +
        accumulateSpotLights(N,V,vWorldPos,base)
    );

    if(lightsCount.dir>0){
        vec3 L=normalize(-dirLights[0].direction);
        float spec=pow(max(dot(reflect(-V,N),L),0.0),SPEC_POW);
        color+=SPEC_COLOR*spec;
    }

    float dist=length(vWorldPos - CAMERA_POS);
    float gather=clamp((dist - GATHER_NEAR)/(GATHER_FAR - GATHER_NEAR),0.0,1.0);
    vec2 sunProj=normalize(SUN_DIR.xz);
    vec2 fragProj=normalize(vWorldPos.xz - CAMERA_POS.xz);
    float band=pow(max(dot(fragProj, sunProj),0.0),BAND_POWER)*gather;
    color+=sunColor * band;

    fragColor=fogMix(vec4(color,1.0),vFog,FOG);
}
