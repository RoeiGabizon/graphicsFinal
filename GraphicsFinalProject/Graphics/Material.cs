namespace GraphicsFinalProject.Graphics;

/// <summary>
/// A minimal material: how shiny/reflective a surface looks under the
/// point light. Diffuse color is passed separately (as the existing
/// per-draw "uColor" uniform) so DrawCube keeps its simple signature.
/// </summary>
public readonly struct Material
{
    public float SpecularStrength { get; }
    public float Shininess { get; }

    public Material(float specularStrength, float shininess)
    {
        SpecularStrength = specularStrength;
        Shininess = shininess;
    }

    /// <summary>A reasonable default for most corridor/robot surfaces.</summary>
    public static readonly Material Default = new(specularStrength: 0.3f, shininess: 32f);
}
