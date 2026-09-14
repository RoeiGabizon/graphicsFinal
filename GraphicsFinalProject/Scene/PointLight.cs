using OpenTK.Mathematics;

namespace GraphicsFinalProject.Scene;

/// <summary>
/// A single point light with a position, color and intensity.
/// Intentionally simple - one light is enough to demonstrate Phong lighting.
/// </summary>
public class PointLight
{
    public Vector3 Position { get; set; }
    public Vector3 Color { get; set; } = Vector3.One;
    public float Intensity { get; set; } = 1.0f;

    public PointLight(Vector3 position, Vector3 color, float intensity)
    {
        Position = position;
        Color = color;
        Intensity = intensity;
    }
}
