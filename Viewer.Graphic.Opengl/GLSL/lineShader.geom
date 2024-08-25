#version 300 es

precision mediump float;

layout(triangles) in;
layout(triangle_strip, max_vertices = 3) out;

uniform mat3 g_WIT;

in VS_Out{
    vec3 origW;
    vec3 posW;
} vout[];


out GS_Out{
    vec3 gs_normal;
    vec3 gs_posW;
} gout;

void main()
{

    vec3 edge1 = vout[1].origW - vout[0].origW;
    vec3 edge2 = vout[2].origW - vout[0].origW;
    vec3 normal = normalize(cross(edge1, edge2));

    for (int i = 0; i < 3; i++)
    {
        gout.gs_normal = normalize(g_WIT*normal);
        gout.gs_posW = vout[i].posW;
        gl_Position = gl_in[i].gl_Position;
        EmitVertex();
    }
    EndPrimitive();
    
}
