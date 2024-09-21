#version 300 es

precision mediump float;

in vec3 fs_normal;
in vec3 fs_posW;
in vec4 fs_color;

out vec4 FragColor;

void main()
{

    vec3 lightPos=vec3(0,0,-1000); 
    vec3 lightColor=vec3(1,1,1);
        // ambient
    float ambientStrength = 0.3;
    vec3 ambient = ambientStrength * lightColor;
  	
    // diffuse 
    vec3 lightDir = normalize(lightPos - fs_posW);
    float diff = max(dot(fs_normal, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;
            
    vec3 result = (ambient + diffuse) * fs_color.xyz;
    FragColor = vec4(result, fs_color.w);
}
