--Vert
#version 330 core
layout(location = 0) in vec3 aPos;
uniform mat4 view;
uniform mat4 projection;
out vec3 vDir;
out mat3 vInvRot;
void main()
{
    mat3 rot = mat3(view);
    vDir = rot * aPos;
    vInvRot = transpose(rot);
    vec4 clip = vec4(aPos,1.0) * mat4(rot) * projection;
    gl_Position = clip.xyww;
}

--Frag
#version 330 core
in vec3 vDir;
in mat3 vInvRot;
out vec4 FragColor;

#define TIME_SCALE            0.0
#define CLOUD_OCTAVES         7
#define CLOUD_SCALE           7.0
#define CLOUD_SPEED           TIME_SCALE
#define CLOUD_LOW             0.3
#define CLOUD_HIGH            0.8
#define CLOUD_BLEND           0.9

#define CLOUD_DEEP_THRESHOLD  0.4
#define CLOUD_MID_THRESHOLD   0.1
#define CLOUD_DEEP_COLOR      vec3(1.0)
#define CLOUD_MID_COLOR       vec3(0.2, 0.12, 0.015)
#define CLOUD_EDGE_COLOR      vec3(0.4, 0.25, 0.1)

#define CLOUD_SUN_COLOR       vec3(1.0, 0.8, 0.6) * 1.2
#define CLOUD_BACK_COLOR      vec3(0.5, 0.4, 0.35) 

#define SKY_BOTTOM            vec3(0.78, 0.8, 0.851)
#define SKY_ZENITH            vec3(0.56, 0.68, 0.812)
#define HORIZON_COLOR         vec3(0.851, 0.847, 0.635)
#define HORIZON_POWER         5.0

#define SUN_DIR               vec3(-0.3, 0.0, 0.0)
#define SUN_COLOR1            vec3(1.0, 0.7, 0.2)
#define SUN_INTENSITY1        1.0
#define SUN_POWER1            8.0
#define SUN_COLOR2            vec3(1.0, 0.8, 0.7)
#define SUN_INTENSITY2        0.3
#define SUN_POWER2            9.0

#define SUN_RADIUS            0.04
#define HALO_INTENSITY        0.5
#define HALO_WIDTH            0.1

float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f*=f*(3.-2.*f);float a=hash(i),b=hash(i+vec2(1,0)),c=hash(i+vec2(0,1)),d=hash(i+vec2(1,1));return mix(mix(a,b,f.x),mix(c,d,f.x),f.y);}
float fbm(vec2 p){float v=0.,a=0.5;for(int i=0;i<CLOUD_OCTAVES;i++){v+=noise(p)*a;p*=2.;a*=0.5;}return v;}

vec3 sky(vec3 rd)
{
    float t = clamp(rd.y,0.0,1.0);
    vec3 col = mix(SKY_BOTTOM, SKY_ZENITH, t);
    float hp = pow(max(1.0 - max(rd.y,0.0),0.0), HORIZON_POWER);
    col = mix(col, HORIZON_COLOR, hp);

    vec3 ld = normalize(SUN_DIR);
    float cosSun = dot(rd, ld);
    col += SUN_COLOR1 * SUN_INTENSITY1 * pow(max(cosSun,0.0), SUN_POWER1);
    col += SUN_COLOR2 * SUN_INTENSITY2 * pow(max(cosSun,0.0), SUN_POWER2);

    float angle = acos(clamp(cosSun, -1.0, 1.0));
    float disc = smoothstep(SUN_RADIUS, 0.0, angle);
    col += SUN_COLOR1 * SUN_INTENSITY1 * disc;
    float halo = smoothstep(SUN_RADIUS+HALO_WIDTH, SUN_RADIUS, angle) * HALO_INTENSITY;
    col += SUN_COLOR2 * halo;

    vec2 cp = rd.xz / (1.0 + rd.y) * CLOUD_SCALE;
    float c = fbm(cp + vec2(TIME * CLOUD_SPEED));
    float baseCloud = smoothstep(CLOUD_LOW, CLOUD_HIGH, c);
    float edgeFade = smoothstep(0.0, 0.3, rd.y);
    float cloudMask = baseCloud * edgeFade;

    float deepMix = clamp((c - CLOUD_DEEP_THRESHOLD) / (CLOUD_MID_THRESHOLD - CLOUD_DEEP_THRESHOLD), 0.0, 1.0);
    float midMix = clamp((c - CLOUD_MID_THRESHOLD) / (1.0 - CLOUD_MID_THRESHOLD), 0.0, 1.0);
    vec3 cloudColor = mix(CLOUD_DEEP_COLOR, CLOUD_MID_COLOR, deepMix);
    cloudColor = mix(cloudColor, CLOUD_EDGE_COLOR, midMix);

    float fwd = max(dot(rd, ld), 0.0);
    float bwd = max(-dot(rd, ld), 0.0);
    cloudColor = mix(cloudColor, CLOUD_SUN_COLOR, fwd * CLOUD_BLEND);
    cloudColor = mix(cloudColor, CLOUD_BACK_COLOR, bwd * CLOUD_BLEND);

    col = mix(col, cloudColor, cloudMask);

    return col;
}

void main()
{
    vec3 worldDir = normalize(vInvRot * vDir);
    FragColor = vec4(sky(worldDir), 1.0);
}
