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

    // One simple point light for the whole scene, placed roughly in the
    // middle of the corridor so both the robot and the walls are lit.
    private readonly PointLight _light = new(
        position: new Vector3(0f, 3.5f, -10f),
        color: new Vector3(1.0f, 0.95f, 0.85f),
        intensity: 1.4f);

    private readonly Material _material = Material.Default;

    // Kept fairly strong (per Phase 6 instructions) so the scene stays easy
    // to see and demonstrate even in areas the point light doesn't reach well.
    private const float AmbientStrength = 0.25f;

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

        _shader.SetVector3("uLightPosition", _light.Position);
        _shader.SetVector3("uLightColor", _light.Color);
        _shader.SetFloat("uLightIntensity", _light.Intensity);
        _shader.SetVector3("uViewPosition", camera.Position);
        _shader.SetFloat("uAmbientStrength", AmbientStrength);
        _shader.SetFloat("uSpecularStrength", _material.SpecularStrength);
        _shader.SetFloat("uShininess", _material.Shininess);

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

        // The normal matrix corrects normals for non-uniform scaling in the
        // model matrix (e.g. a wall stretched thin in one axis): it is the
        // inverse-transpose of the model matrix's upper-left 3x3. Without
        // this, normals on scaled cubes would no longer be perpendicular to
        // the actual (scaled) surface, giving wrong-looking lighting.
        Matrix3 normalMatrix = new Matrix3(Matrix4.Transpose(Matrix4.Invert(model)));
        _shader.SetMatrix3("uNormalMatrix", normalMatrix);

        _cubeMesh!.Draw();
    }

    public void Dispose()
    {
        _cubeMesh?.Dispose();
        _shader?.Dispose();
    }

    // Unit cube centered at the origin (side length 1). Each vertex has a
    // position (3 floats) followed by its face normal (3 floats). Every
    // face uses its own 4 vertices so each can have a flat, correct normal
    // (a shared-vertex cube can't do this without smoothing edges).
    private static readonly float[] CubeVertices =
    {
        // Front face (+Z)
        -0.5f, -0.5f,  0.5f,   0f, 0f, 1f,
         0.5f, -0.5f,  0.5f,   0f, 0f, 1f,
         0.5f,  0.5f,  0.5f,   0f, 0f, 1f,
        -0.5f,  0.5f,  0.5f,   0f, 0f, 1f,

        // Back face (-Z)
         0.5f, -0.5f, -0.5f,   0f, 0f, -1f,
        -0.5f, -0.5f, -0.5f,   0f, 0f, -1f,
        -0.5f,  0.5f, -0.5f,   0f, 0f, -1f,
         0.5f,  0.5f, -0.5f,   0f, 0f, -1f,

        // Left face (-X)
        -0.5f, -0.5f, -0.5f,  -1f, 0f, 0f,
        -0.5f, -0.5f,  0.5f,  -1f, 0f, 0f,
        -0.5f,  0.5f,  0.5f,  -1f, 0f, 0f,
        -0.5f,  0.5f, -0.5f,  -1f, 0f, 0f,

        // Right face (+X)
         0.5f, -0.5f,  0.5f,   1f, 0f, 0f,
         0.5f, -0.5f, -0.5f,   1f, 0f, 0f,
         0.5f,  0.5f, -0.5f,   1f, 0f, 0f,
         0.5f,  0.5f,  0.5f,   1f, 0f, 0f,

        // Top face (+Y)
        -0.5f,  0.5f,  0.5f,   0f, 1f, 0f,
         0.5f,  0.5f,  0.5f,   0f, 1f, 0f,
         0.5f,  0.5f, -0.5f,   0f, 1f, 0f,
        -0.5f,  0.5f, -0.5f,   0f, 1f, 0f,

        // Bottom face (-Y)
        -0.5f, -0.5f, -0.5f,   0f, -1f, 0f,
         0.5f, -0.5f, -0.5f,   0f, -1f, 0f,
         0.5f, -0.5f,  0.5f,   0f, -1f, 0f,
        -0.5f, -0.5f,  0.5f,   0f, -1f, 0f,
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

