#version 300 es

precision highp float;

in vec3 fs_normal;
in vec3 fs_posW;
flat in float fs_color;

out vec4 FragColor;

void main()
{
    vec3 lightPos = vec3(0, 0, -1000);
    vec3 lightColor = vec3(1, 1, 1);
    // ambient
    float ambientStrength = 0.3;
    vec3 ambient = ambientStrength * lightColor;

    // diffuse
    vec3 lightDir = normalize(lightPos - fs_posW);
    float diff = max(dot(fs_normal, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    uint uintColor = floatBitsToUint(fs_color);
    float alpha = float((uintColor >> 24u) & 0xFFu) / 255.0;
    float blue = float((uintColor >> 16u) & 0xFFu) / 255.0;
    float green = float((uintColor >> 8u) & 0xFFu) / 255.0;
    float red = float(uintColor & 0xFFu) / 255.0;

    vec3 color = vec3(red, green, blue);
    vec3 result = (ambient + diffuse) * color;
    FragColor = vec4(result, alpha);
}
