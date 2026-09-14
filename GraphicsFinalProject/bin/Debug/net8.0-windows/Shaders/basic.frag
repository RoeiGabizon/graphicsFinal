#version 330 core

in vec3 vFragPosition;
in vec3 vNormal;
in vec2 vTexCoord;

#define MAX_POINT_LIGHTS 4

uniform vec3 uColor;
uniform bool uUseTexture;
uniform sampler2D uTexture;
uniform bool uShadowPass;
uniform bool uReflectionPass;
uniform float uAlpha;
uniform float uSpecularStrength;
uniform float uShininess;

uniform int uPointLightCount;
uniform vec3 uPointLightPositions[MAX_POINT_LIGHTS];
uniform vec3 uPointLightColors[MAX_POINT_LIGHTS];
uniform float uPointLightIntensities[MAX_POINT_LIGHTS];
uniform float uPointLightConstants[MAX_POINT_LIGHTS];
uniform float uPointLightLinears[MAX_POINT_LIGHTS];
uniform float uPointLightQuadratics[MAX_POINT_LIGHTS];

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
    vec3 baseColor = uUseTexture ? texture(uTexture, vTexCoord).rgb * uColor : uColor;

    if (uShadowPass)
    {
        FragColor = vec4(0.01, 0.01, 0.015, uAlpha);
        return;
    }

    if (uReflectionPass)
    {
        FragColor = vec4(baseColor * vec3(0.35, 0.45, 0.60), uAlpha);
        return;
    }

    vec3 normal = normalize(vNormal);

    // Ambient: a small constant amount of light so nothing is ever fully
    // black, even facing away from the light. Kept fairly strong so the
    // corridor stays easy to see during the demo.
    vec3 ambientResult = vec3(uAmbientStrength) * baseColor;
    vec3 diffuseResult = vec3(0.0);
    vec3 specularResult = vec3(0.0);
    vec3 viewDirection = normalize(uViewPosition - vFragPosition);

    for (int i = 0; i < MAX_POINT_LIGHTS; i++)
    {
        if (i >= uPointLightCount)
        {
            break;
        }

        vec3 toLight = uPointLightPositions[i] - vFragPosition;
        float distanceToLight = length(toLight);
        vec3 lightDirection = normalize(toLight);
        float attenuation = 1.0 / (uPointLightConstants[i]
            + uPointLightLinears[i] * distanceToLight
            + uPointLightQuadratics[i] * distanceToLight * distanceToLight);
        float diffuseFactor = max(dot(normal, lightDirection), 0.0);
        diffuseResult += diffuseFactor * uPointLightColors[i]
            * uPointLightIntensities[i] * attenuation * baseColor;

        // Do not add a highlight when the surface receives no direct light.
        if (diffuseFactor > 0.0)
        {
            vec3 halfwayDirection = normalize(lightDirection + viewDirection);
            float specularFactor = pow(max(dot(normal, halfwayDirection), 0.0), uShininess);
            specularResult += uSpecularStrength * specularFactor * uPointLightColors[i]
                * uPointLightIntensities[i] * attenuation;
        }
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
        diffuseResult += diffuseFactor * uSpotlightColor
            * spotFactor * uSpotlightIntensity * baseColor;
        if (diffuseFactor > 0.0)
        {
            vec3 halfwayDirection = normalize(lightDirection + viewDirection);
            float specularFactor = pow(max(dot(normal, halfwayDirection), 0.0), uShininess);
            specularResult += uSpecularStrength * specularFactor * uSpotlightColor
                * spotFactor * uSpotlightIntensity;
        }
    }

    vec3 result = ambientResult + diffuseResult + specularResult;

    FragColor = vec4(result, 1.0);
}
