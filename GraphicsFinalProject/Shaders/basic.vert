#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform mat3 uNormalMatrix;

out vec3 vFragPosition;
out vec3 vNormal;

void main()
{
    vec4 worldPosition = uModel * vec4(aPosition, 1.0);
    vFragPosition = worldPosition.xyz;

    // The normal matrix (inverse-transpose of the model matrix, upper 3x3)
    // is required so normals stay perpendicular to surfaces even when the
    // model matrix contains non-uniform scale (very common here: walls,
    // beams and limbs are all stretched unit cubes).
    vNormal = normalize(uNormalMatrix * aNormal);

    gl_Position = uProjection * uView * worldPosition;
}
