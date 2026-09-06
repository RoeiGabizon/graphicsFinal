using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace GraphicsFinalProject.Graphics;

/// <summary>
/// Holds the core OpenGL setup and per-frame rendering logic.
/// Kept intentionally simple: no scene graph, no engine abstractions.
/// </summary>
public class Renderer
{
    private Shader? _shader;
    private Mesh? _cube;
    private float _aspectRatio = 1.0f;
    private float _rotationAngle;

    public void Initialize()
    {
        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
        GL.Enable(EnableCap.DepthTest);

        string shaderDir = Path.Combine(AppContext.BaseDirectory, "Shaders");
        _shader = new Shader(
            Path.Combine(shaderDir, "basic.vert"),
            Path.Combine(shaderDir, "basic.frag"));

        _cube = new Mesh(CubeVertices, CubeIndices);
    }

    public void Resize(int width, int height)
    {
        GL.Viewport(0, 0, width, height);
        if (height > 0)
        {
            _aspectRatio = width / (float)height;
        }
    }

    public void Render(float deltaTime)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (_shader is null || _cube is null)
        {
            return;
        }

        // Slowly rotate the cube over time.
        _rotationAngle += deltaTime * MathHelper.DegreesToRadians(30.0f);

        Matrix4 model = Matrix4.CreateRotationY(_rotationAngle) * Matrix4.CreateRotationX(_rotationAngle * 0.5f);

        Matrix4 view = Matrix4.LookAt(
            eye: new Vector3(0.0f, 1.5f, 5.0f),
            target: Vector3.Zero,
            up: Vector3.UnitY);

        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(60.0f),
            _aspectRatio,
            0.1f,
            100.0f);

        _shader.Use();
        _shader.SetMatrix4("uModel", model);
        _shader.SetMatrix4("uView", view);
        _shader.SetMatrix4("uProjection", projection);

        _cube.Draw();
    }

    public void Dispose()
    {
        _cube?.Dispose();
        _shader?.Dispose();
    }

    // Cube centered at the origin, side length 1. Each face has its own
    // 4 vertices (rather than sharing corners) so each face can have a
    // distinct flat color.
    private static readonly float[] CubeVertices =
    {
        // Position           // Color (per face)
        // Front face (red)
        -0.5f, -0.5f,  0.5f,  1f, 0f, 0f,
         0.5f, -0.5f,  0.5f,  1f, 0f, 0f,
         0.5f,  0.5f,  0.5f,  1f, 0f, 0f,
        -0.5f,  0.5f,  0.5f,  1f, 0f, 0f,

        // Back face (green)
         0.5f, -0.5f, -0.5f,  0f, 1f, 0f,
        -0.5f, -0.5f, -0.5f,  0f, 1f, 0f,
        -0.5f,  0.5f, -0.5f,  0f, 1f, 0f,
         0.5f,  0.5f, -0.5f,  0f, 1f, 0f,

        // Left face (blue)
        -0.5f, -0.5f, -0.5f,  0f, 0f, 1f,
        -0.5f, -0.5f,  0.5f,  0f, 0f, 1f,
        -0.5f,  0.5f,  0.5f,  0f, 0f, 1f,
        -0.5f,  0.5f, -0.5f,  0f, 0f, 1f,

        // Right face (yellow)
         0.5f, -0.5f,  0.5f,  1f, 1f, 0f,
         0.5f, -0.5f, -0.5f,  1f, 1f, 0f,
         0.5f,  0.5f, -0.5f,  1f, 1f, 0f,
         0.5f,  0.5f,  0.5f,  1f, 1f, 0f,

        // Top face (cyan)
        -0.5f,  0.5f,  0.5f,  0f, 1f, 1f,
         0.5f,  0.5f,  0.5f,  0f, 1f, 1f,
         0.5f,  0.5f, -0.5f,  0f, 1f, 1f,
        -0.5f,  0.5f, -0.5f,  0f, 1f, 1f,

        // Bottom face (magenta)
        -0.5f, -0.5f, -0.5f,  1f, 0f, 1f,
         0.5f, -0.5f, -0.5f,  1f, 0f, 1f,
         0.5f, -0.5f,  0.5f,  1f, 0f, 1f,
        -0.5f, -0.5f,  0.5f,  1f, 0f, 1f,
    };

    private static readonly uint[] CubeIndices =
    {
        0, 1, 2,  2, 3, 0,       // front
        4, 5, 6,  6, 7, 4,       // back
        8, 9, 10, 10, 11, 8,     // left
        12, 13, 14, 14, 15, 12,  // right
        16, 17, 18, 18, 19, 16,  // top
        20, 21, 22, 22, 23, 20,  // bottom
    };
}
