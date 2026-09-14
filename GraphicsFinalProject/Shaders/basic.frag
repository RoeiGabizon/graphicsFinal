#version 330 core

in vec3 vFragPosition;
in vec3 vNormal;

#define MAX_POINT_LIGHTS 4

uniform vec3 uColor;
uniform float uSpecularStrength;
uniform float uShininess;

uniform int uPointLightCount;
uniform vec3 uPointLightPositions[MAX_POINT_LIGHTS];
uniform vec3 uPointLightColors[MAX_POINT_LIGHTS];
uniform float uPointLightIntensities[MAX_POINT_LIGHTS];

uniform vec3 uViewPosition;
uniform float uAmbientStrength;

uniform bool uSpotlightEnabled;
uniform vec3 uSpotlightPosition;
uniform vec3 uSpotlightDirection;
uniform vec3 uSpotlightColor;
uniform float uSpotlightInnerCutoff;
uniform float uSpotlightOuterCutoff;
uniform float uSpotlightIntensity;

out vec4 FragColor;

void main()
{
    vec3 normal = normalize(vNormal);

    // Ambient: a small constant amount of light so nothing is ever fully
    // black, even facing away from the light. Kept fairly strong so the
    // corridor stays easy to see during the demo.
    vec3 lighting = vec3(uAmbientStrength);
    vec3 viewDirection = normalize(uViewPosition - vFragPosition);

    for (int i = 0; i < MAX_POINT_LIGHTS; i++)
    {
        if (i >= uPointLightCount)
        {
            break;
        }

        vec3 lightDirection = normalize(uPointLightPositions[i] - vFragPosition);
        float diffuseFactor = max(dot(normal, lightDirection), 0.0);
        vec3 halfwayDirection = normalize(lightDirection + viewDirection);
        float specularFactor = pow(max(dot(normal, halfwayDirection), 0.0), uShininess);
        vec3 contribution = diffuseFactor * uPointLightColors[i]
            + uSpecularStrength * specularFactor * uPointLightColors[i];
        lighting += contribution * uPointLightIntensities[i];
    }

    if (uSpotlightEnabled)
    {
        vec3 toFragment = normalize(vFragPosition - uSpotlightPosition);
        float spotCosine = dot(toFragment, normalize(uSpotlightDirection));
        float spotFactor = smoothstep(uSpotlightOuterCutoff,
                                      uSpotlightInnerCutoff,
                                      spotCosine);
        vec3 lightDirection = normalize(uSpotlightPosition - vFragPosition);
        float diffuseFactor = max(dot(normal, lightDirection), 0.0);
        vec3 halfwayDirection = normalize(lightDirection + viewDirection);
        float specularFactor = pow(max(dot(normal, halfwayDirection), 0.0), uShininess);
        vec3 contribution = diffuseFactor * uSpotlightColor
            + uSpecularStrength * specularFactor * uSpotlightColor;
        lighting += contribution * spotFactor * uSpotlightIntensity;
    }

    vec3 result = lighting * uColor;

    FragColor = vec4(result, 1.0);
}
