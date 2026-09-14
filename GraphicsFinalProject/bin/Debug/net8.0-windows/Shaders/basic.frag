#version 330 core

in vec3 vFragPosition;
in vec3 vNormal;

uniform vec3 uColor;
uniform float uSpecularStrength;
uniform float uShininess;

uniform vec3 uLightPosition;
uniform vec3 uLightColor;
uniform float uLightIntensity;

uniform vec3 uViewPosition;
uniform float uAmbientStrength;

out vec4 FragColor;

void main()
{
    vec3 normal = normalize(vNormal);

    // Ambient: a small constant amount of light so nothing is ever fully
    // black, even facing away from the light. Kept fairly strong so the
    // corridor stays easy to see during the demo.
    vec3 ambient = uAmbientStrength * uLightColor;

    // Diffuse: Lambertian shading - brighter where the surface faces
    // directly toward the light, zero where it faces away.
    vec3 lightDirection = normalize(uLightPosition - vFragPosition);
    float diffuseFactor = max(dot(normal, lightDirection), 0.0);
    vec3 diffuse = diffuseFactor * uLightColor;

    // Specular (Blinn-Phong): a bright highlight when the surface reflects
    // light toward the camera. Using the halfway vector between the light
    // and view directions is the standard, cheaper alternative to computing
    // a true reflection vector.
    vec3 viewDirection = normalize(uViewPosition - vFragPosition);
    vec3 halfwayDirection = normalize(lightDirection + viewDirection);
    float specularFactor = pow(max(dot(normal, halfwayDirection), 0.0), uShininess);
    vec3 specular = uSpecularStrength * specularFactor * uLightColor;

    vec3 lighting = (ambient + (diffuse + specular) * uLightIntensity);
    vec3 result = lighting * uColor;

    FragColor = vec4(result, 1.0);
}
