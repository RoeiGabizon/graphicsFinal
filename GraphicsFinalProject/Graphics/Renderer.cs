using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using GraphicsFinalProject.Scene;

namespace GraphicsFinalProject.Graphics;

/// <summary>
/// Holds the core OpenGL setup and per-frame rendering logic.
/// Kept intentionally simple: no scene graph, no engine abstractions.
///
/// Owns exactly one unit-cube Mesh. Every cube-shaped object in the scene
/// (corridor walls, floor, decorations, later the robot) reuses this same
/// mesh through DrawCube(model, color) - only the model matrix and color
/// uniform change between draws.
/// </summary>
public class Renderer
{
    private Shader? _shader;
    private Mesh? _cubeMesh;
    private Corridor? _corridor;
    private float _aspectRatio = 1.0f;

    public void Initialize()
    {
        GL.ClearColor(0.05f, 0.05f, 0.08f, 1.0f);
        GL.Enable(EnableCap.DepthTest);

        string shaderDir = Path.Combine(AppContext.BaseDirectory, "Shaders");
        _shader = new Shader(
            Path.Combine(shaderDir, "basic.vert"),
            Path.Combine(shaderDir, "basic.frag"));

        _cubeMesh = new Mesh(CubeVertices, CubeIndices);
        _corridor = new Corridor();
    }

    public void Resize(int width, int height)
    {
        GL.Viewport(0, 0, width, height);
        if (height > 0)
        {
            _aspectRatio = width / (float)height;
        }
    }

    public void Render(float deltaTime, Camera camera, Robot robot)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (_shader is null || _cubeMesh is null || _corridor is null)
        {
            return;
        }

        Matrix4 view = camera.GetViewMatrix();
        Matrix4 projection = camera.GetProjectionMatrix(_aspectRatio);

        _shader.Use();
        _shader.SetMatrix4("uView", view);
        _shader.SetMatrix4("uProjection", projection);

        _corridor.Draw(DrawCube);
        robot.Draw(DrawCube);
    }

    /// <summary>
    /// Draws the shared unit cube mesh with the given model matrix and
    /// flat color. This is how one mesh becomes many different-looking
    /// objects: only the uniforms change between calls, not the geometry.
    /// </summary>
    private void DrawCube(Matrix4 model, Vector3 color)
    {
        _shader!.SetMatrix4("uModel", model);
        _shader.SetVector3("uColor", color);
        _cubeMesh!.Draw();
    }

    public void Dispose()
    {
        _cubeMesh?.Dispose();
        _shader?.Dispose();
    }

    // Unit cube centered at the origin (side length 1), position-only.
    // Scale/translate/rotate this via the model matrix to build any
    // box-shaped object (walls, beams, panels, robot parts, ...).
    private static readonly float[] CubeVertices =
    {
        // Front face
        -0.5f, -0.5f,  0.5f,
         0.5f, -0.5f,  0.5f,
         0.5f,  0.5f,  0.5f,
        -0.5f,  0.5f,  0.5f,

        // Back face
         0.5f, -0.5f, -0.5f,
        -0.5f, -0.5f, -0.5f,
        -0.5f,  0.5f, -0.5f,
         0.5f,  0.5f, -0.5f,

        // Left face
        -0.5f, -0.5f, -0.5f,
        -0.5f, -0.5f,  0.5f,
        -0.5f,  0.5f,  0.5f,
        -0.5f,  0.5f, -0.5f,

        // Right face
         0.5f, -0.5f,  0.5f,
         0.5f, -0.5f, -0.5f,
         0.5f,  0.5f, -0.5f,
         0.5f,  0.5f,  0.5f,

        // Top face
        -0.5f,  0.5f,  0.5f,
         0.5f,  0.5f,  0.5f,
         0.5f,  0.5f, -0.5f,
        -0.5f,  0.5f, -0.5f,

        // Bottom face
        -0.5f, -0.5f, -0.5f,
         0.5f, -0.5f, -0.5f,
         0.5f, -0.5f,  0.5f,
        -0.5f, -0.5f,  0.5f,
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
