using OpenTK.Mathematics;

namespace GraphicsFinalProject.Scene;

/// <summary>
/// A single point light with a position, color, intensity and simple
/// distance-attenuation coefficients.
/// </summary>
public class PointLight
{
    public Vector3 Position { get; set; }
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 1.0f;
    public float Constant { get; set; } = 1.0f;
    public float Linear { get; set; } = 0.09f;
    public float Quadratic { get; set; } = 0.032f;

    public PointLight(Vector3 position, Vector3 color, float intensity)
    {
        Position = position;
        Color = color;
        Intensity = intensity;
    }
}
