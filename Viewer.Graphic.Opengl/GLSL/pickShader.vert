#version 330 core
layout (location = 0) in vec4 vIn;

uniform mat4 g_World; 
uniform mat4 g_View;  
uniform mat4 g_Proj;  
uniform mat4 g_Translation;
uniform mat4 g_Origin;
uniform vec4 baseId;

out vec4 vout;

void main()
{
    vec3 posL=vIn.xyz;
    vec4 orig=g_Origin*vec4(posL,1.0);
    vec4 pos=g_World*orig;
    uint value=floatBitsToUint(vIn.w)+floatBitsToUint(baseId.x);
    float a = float((value / 16777216u) % 256u) / 255.0;
    float b = float((value / 65536u) % 256u) / 255.0;
    float g = float((value / 256u) % 256u) / 255.0;
    float r = float(value % 256u) / 255.0;
    vout=vec4(r,g,b,a);
    gl_Position=g_Proj*g_View*pos*g_Translation;
}

