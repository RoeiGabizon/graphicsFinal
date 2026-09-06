using OpenTK.Mathematics;

namespace GraphicsFinalProject.Scene;

/// <summary>
/// A simple free-look/fly camera. Owns its own position/orientation state
/// and knows how to turn that into View/Projection matrices. All input
/// (which keys are down, mouse deltas, wheel deltas) is fed in from
/// MainForm - this class only does the camera math.
/// </summary>
public class Camera
{
    private const float MinPitchDegrees = -89.0f;
    private const float MaxPitchDegrees = 89.0f;
    private const float MinFovDegrees = 20.0f;
    private const float MaxFovDegrees = 90.0f;

    public Vector3 Position { get; set; }

    /// <summary>Direction the camera is looking, always kept unit length.</summary>
    public Vector3 Front { get; private set; } = -Vector3.UnitZ;

    /// <summary>World-space up, used for the view matrix.</summary>
    public Vector3 Up { get; private set; } = Vector3.UnitY;

    /// <summary>Points to the camera's right, derived from Front and world up.</summary>
    public Vector3 Right { get; private set; } = Vector3.UnitX;

    /// <summary>Rotation around the world Y axis, in degrees.</summary>
    public float Yaw { get; set; } = -90.0f;

    /// <summary>Rotation up/down, in degrees. Clamped to avoid flipping.</summary>
    public float Pitch { get; set; }

    /// <summary>Vertical field of view, in degrees. Controls zoom.</summary>
    public float Fov { get; set; } = 60.0f;

    /// <summary>Movement speed in world units per second.</summary>
    public float MovementSpeed { get; set; } = 3.0f;

    /// <summary>Mouse look sensitivity in degrees per pixel dragged.</summary>
    public float MouseSensitivity { get; set; } = 0.15f;

    public Camera(Vector3 startPosition)
    {
        Position = startPosition;
        UpdateVectors();
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + Front, Up);
    }

    public Matrix4 GetProjectionMatrix(float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(Fov),
            aspectRatio,
            0.1f,
            100.0f);
    }

    /// <summary>
    /// Moves the camera in its own local space. Each direction flag is
    /// independent so e.g. W+D together move diagonally.
    /// </summary>
    public void Move(bool forward, bool backward, bool left, bool right, bool up, bool down, float deltaTime)
    {
        float distance = MovementSpeed * deltaTime;

        if (forward)
        {
            Position += Front * distance;
        }
        if (backward)
        {
            Position -= Front * distance;
        }
        if (left)
        {
            Position -= Right * distance;
        }
        if (right)
        {
            Position += Right * distance;
        }
        if (up)
        {
            Position += Vector3.UnitY * distance;
        }
        if (down)
        {
            Position -= Vector3.UnitY * distance;
        }
    }

    /// <summary>
    /// Applies a mouse drag (in pixels) to yaw/pitch. Call only while the
    /// look button (right mouse button) is held down.
    /// </summary>
    public void ProcessMouseLook(float deltaX, float deltaY)
    {
        Yaw += deltaX * MouseSensitivity;
        Pitch -= deltaY * MouseSensitivity;
        Pitch = MathHelper.Clamp(Pitch, MinPitchDegrees, MaxPitchDegrees);

        UpdateVectors();
    }

    /// <summary>
    /// Applies mouse wheel scroll to zoom (by narrowing/widening the FOV).
    /// </summary>
    public void ProcessMouseWheel(float wheelDelta)
    {
        Fov -= wheelDelta;
        Fov = MathHelper.Clamp(Fov, MinFovDegrees, MaxFovDegrees);
    }

    private void UpdateVectors()
    {
        float yawRad = MathHelper.DegreesToRadians(Yaw);
        float pitchRad = MathHelper.DegreesToRadians(Pitch);

        Vector3 front;
        front.X = MathF.Cos(yawRad) * MathF.Cos(pitchRad);
        front.Y = MathF.Sin(pitchRad);
        front.Z = MathF.Sin(yawRad) * MathF.Cos(pitchRad);
        Front = Vector3.Normalize(front);

        Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
        Up = Vector3.Normalize(Vector3.Cross(Right, Front));
    }
}
