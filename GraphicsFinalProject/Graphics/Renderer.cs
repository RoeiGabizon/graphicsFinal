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
    private Texture? _corridorTexture;
    private Texture? _floorTexture;
    private readonly Texture?[] _robotTextures = new Texture?[3];
    private int _robotAppearance;
    public bool TexturesEnabled { get; set; } = true;
    public float AmbientIntensity { get; set; } = 0.65f;
    public float MainLightIntensity
    {
        get => _ceilingLights[0].Intensity;
        set
        {
            foreach (PointLight light in _ceilingLights)
            {
                light.Intensity = value;
            }
        }
    }
    // A small, fixed set of point lights matching the visible ceiling
    // fixtures. Keeping the count fixed keeps the shader easy to explain.
    private readonly PointLight[] _ceilingLights =
    {
        new(new Vector3(0f, 4.75f, -5.0f), new Vector3(1.0f, 0.85f, 0.55f), 1.8f),
        new(new Vector3(0f, 4.75f, -14.0f), new Vector3(1.0f, 0.95f, 0.75f), 1.8f),
        new(new Vector3(0f, 4.75f, -23.0f), new Vector3(0.75f, 0.85f, 1.0f), 1.8f),
    };

    private readonly Material _material = Material.Default;

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
        string textureDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Textures");
        _corridorTexture = Texture.Load(Path.Combine(textureDir, "corridor_wall.png"));
        _floorTexture = Texture.Load(Path.Combine(textureDir, "floor.png"));
        _robotTextures[0] = Texture.Load(Path.Combine(textureDir, "robot_metal.png"));
        _robotTextures[1] = Texture.Load(Path.Combine(textureDir, "robot_red.png"));
        _robotTextures[2] = Texture.Load(Path.Combine(textureDir, "robot_blue.png"));
    }

    public void CycleRobotAppearance()
    {
        _robotAppearance = (_robotAppearance + 1) % _robotTextures.Length;
    }

    public void SetRobotAppearance(int appearance)
    {
        _robotAppearance = Math.Clamp(appearance, 0, _robotTextures.Length - 1);
    }

    public void Resize(int width, int height)
    {
        GL.Viewport(0, 0, width, height);
        if (height > 0)
        {
            _aspectRatio = width / (float)height;
        }
    }

    public void Render(float deltaTime, Camera camera, Robot robot, bool spotlightEnabled, bool shadowsEnabled, bool reflectionsEnabled)
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

        _shader.SetInt("uPointLightCount", _ceilingLights.Length);
        for (int i = 0; i < _ceilingLights.Length; i++)
        {
            PointLight light = _ceilingLights[i];
            _shader.SetVector3($"uPointLightPositions[{i}]", light.Position);
            _shader.SetVector3($"uPointLightColors[{i}]", light.Color);
            _shader.SetFloat($"uPointLightIntensities[{i}]", light.Intensity);
            _shader.SetFloat($"uPointLightConstants[{i}]", light.Constant);
            _shader.SetFloat($"uPointLightLinears[{i}]", light.Linear);
            _shader.SetFloat($"uPointLightQuadratics[{i}]", light.Quadratic);
        }

        _shader.SetInt("uSpotlightEnabled", spotlightEnabled ? 1 : 0);
        _shader.SetVector3("uSpotlightPosition", robot.SpotlightPosition);
        _shader.SetVector3("uSpotlightDirection", robot.SpotlightDirection);
        _shader.SetVector3("uSpotlightColor", new Vector3(0.75f, 0.9f, 1.0f));
        _shader.SetFloat("uSpotlightInnerCutoff", MathF.Cos(MathHelper.DegreesToRadians(12.0f)));
        _shader.SetFloat("uSpotlightOuterCutoff", MathF.Cos(MathHelper.DegreesToRadians(25.0f)));
        _shader.SetFloat("uSpotlightIntensity", 2.0f);
        _shader.SetVector3("uViewPosition", camera.Position);
        _shader.SetFloat("uAmbientStrength", AmbientIntensity);
        _shader.SetFloat("uSpecularStrength", _material.SpecularStrength);
        _shader.SetFloat("uShininess", _material.Shininess);
        _shader.SetInt("uShadowPass", 0);
        _shader.SetInt("uReflectionPass", 0);
        _shader.SetFloat("uAlpha", 1.0f);

        _corridor.Draw(DrawCube);
        if (shadowsEnabled)
        {
            DrawRobotShadow(robot);
        }
        robot.Draw(DrawCube);
        if (reflectionsEnabled)
        {
            DrawRobotReflection(robot);
        }
    }

    private void DrawRobotShadow(Robot robot)
    {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        // The opaque floor has already populated the depth buffer. Disable
        // depth testing for this simple floor-only overlay so the reflection
        // remains visible through the floor without a second floor pass.
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);

        Matrix4 shadowMatrix = CreateFloorShadowMatrix(_ceilingLights[0].Position);
        robot.Draw((model, _, _) => DrawShadowCube(model * shadowMatrix));

        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Blend);
        _shader!.SetInt("uShadowPass", 0);
    }

    private void DrawShadowCube(Matrix4 model)
    {
        _shader!.SetMatrix4("uModel", model);
        _shader.SetInt("uUseTexture", 0);
        _shader.SetInt("uShadowPass", 1);
        _shader.SetFloat("uAlpha", 0.45f);
        // The projected shadow matrix is intentionally singular, so the
        // normal matrix is irrelevant during this unlit shadow pass.
        _shader.SetMatrix3("uNormalMatrix", Matrix3.Identity);
        _cubeMesh!.Draw();
    }

    private void DrawRobotReflection(Robot robot)
    {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(false);

        Matrix4 reflectionMatrix =
            Matrix4.CreateScale(1.0f, -1.0f, 1.0f) *
            Matrix4.CreateTranslation(0.0f, 0.02f, 0.0f);
        robot.Draw((model, color, textureKind) =>
            DrawReflectionCube(model * reflectionMatrix, color, textureKind));

        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
        _shader!.SetInt("uReflectionPass", 0);
    }

    private void DrawReflectionCube(Matrix4 model, Vector3 color, TextureKind textureKind)
    {
        _shader!.SetMatrix4("uModel", model);
        if (textureKind == TextureKind.Robot && TexturesEnabled)
        {
            // The selected robot texture supplies the appearance color
            // consistently across the body parts.
            color = Vector3.One;
        }
        _shader.SetVector3("uColor", color);
        Texture? texture = TexturesEnabled && textureKind == TextureKind.Robot
            ? _robotTextures[_robotAppearance]
            : null;
        _shader.SetInt("uUseTexture", texture is null ? 0 : 1);
        _shader.SetInt("uTexture", 0);
        texture?.Bind(TextureUnit.Texture0);
        _shader.SetInt("uShadowPass", 0);
        _shader.SetInt("uReflectionPass", 1);
        _shader.SetFloat("uAlpha", 0.28f);
        _shader.SetMatrix3("uNormalMatrix", Matrix3.Identity);
        _cubeMesh!.Draw();
    }

    /// <summary>
    /// Creates a planar projection onto y = 0 using the first ceiling light.
    /// The matrix is transposed because the rest of this project composes
    /// transforms in OpenTK's established row-style order. This is only a
    /// floor shadow and is not a general-purpose shadow solution.
    /// </summary>
    private static Matrix4 CreateFloorShadowMatrix(Vector3 lightPosition)
    {
        Vector4 light = new(lightPosition.X, lightPosition.Y, lightPosition.Z, 1.0f);
        Vector4 plane = new(0.0f, 1.0f, 0.0f, 0.0f);
        float dot = Vector4.Dot(plane, light);

        Matrix4 columnStyle = new Matrix4(
            dot - light.X * plane.X, -light.X * plane.Y, -light.X * plane.Z, -light.X * plane.W,
            -light.Y * plane.X, dot - light.Y * plane.Y, -light.Y * plane.Z, -light.Y * plane.W,
            -light.Z * plane.X, -light.Z * plane.Y, dot - light.Z * plane.Z, -light.Z * plane.W,
            -light.W * plane.X, -light.W * plane.Y, -light.W * plane.Z, dot - light.W * plane.W);

        return Matrix4.Transpose(columnStyle)
            * Matrix4.CreateTranslation(0.0f, 0.01f, 0.0f);
    }

    /// <summary>
    /// Draws the shared unit cube mesh with the given model matrix and
    /// flat color. This is how one mesh becomes many different-looking
    /// objects: only the uniforms change between calls, not the geometry.
    /// </summary>
    private void DrawCube(Matrix4 model, Vector3 color, TextureKind textureKind)
    {
        _shader!.SetMatrix4("uModel", model);
        if (textureKind == TextureKind.Robot && TexturesEnabled)
        {
            color = Vector3.One;
        }
        _shader.SetVector3("uColor", color);

        Texture? texture = TexturesEnabled ? textureKind switch
        {
            TextureKind.CorridorWall => _corridorTexture,
            TextureKind.Floor => _floorTexture,
            TextureKind.Robot => _robotTextures[_robotAppearance],
            _ => null,
        } : null;
        _shader.SetInt("uUseTexture", texture is null ? 0 : 1);
        _shader.SetInt("uTexture", 0);
        texture?.Bind(TextureUnit.Texture0);

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
        _corridorTexture?.Dispose();
        _floorTexture?.Dispose();
        foreach (Texture? texture in _robotTextures)
        {
            texture?.Dispose();
        }
    }

    // Unit cube centered at the origin (side length 1). Each vertex has a
    // position (3 floats), face normal (3 floats), and face-local UV (2 floats). Every
    // face uses its own 4 vertices so each can have a flat, correct normal
    // (a shared-vertex cube can't do this without smoothing edges).
    private static readonly float[] CubeVertices =
    {
        // Front face (+Z)
        -0.5f, -0.5f,  0.5f,   0f, 0f, 1f,   0f, 0f,
         0.5f, -0.5f,  0.5f,   0f, 0f, 1f,   1f, 0f,
         0.5f,  0.5f,  0.5f,   0f, 0f, 1f,   1f, 1f,
        -0.5f,  0.5f,  0.5f,   0f, 0f, 1f,   0f, 1f,

        // Back face (-Z)
         0.5f, -0.5f, -0.5f,   0f, 0f, -1f,   0f, 0f,
        -0.5f, -0.5f, -0.5f,   0f, 0f, -1f,   1f, 0f,
        -0.5f,  0.5f, -0.5f,   0f, 0f, -1f,   1f, 1f,
         0.5f,  0.5f, -0.5f,   0f, 0f, -1f,   0f, 1f,

        // Left face (-X)
        -0.5f, -0.5f, -0.5f,  -1f, 0f, 0f,   0f, 0f,
        -0.5f, -0.5f,  0.5f,  -1f, 0f, 0f,   1f, 0f,
        -0.5f,  0.5f,  0.5f,  -1f, 0f, 0f,   1f, 1f,
        -0.5f,  0.5f, -0.5f,  -1f, 0f, 0f,   0f, 1f,

        // Right face (+X)
         0.5f, -0.5f,  0.5f,   1f, 0f, 0f,   0f, 0f,
         0.5f, -0.5f, -0.5f,   1f, 0f, 0f,   1f, 0f,
         0.5f,  0.5f, -0.5f,   1f, 0f, 0f,   1f, 1f,
         0.5f,  0.5f,  0.5f,   1f, 0f, 0f,   0f, 1f,

        // Top face (+Y)
        -0.5f,  0.5f,  0.5f,   0f, 1f, 0f,   0f, 0f,
         0.5f,  0.5f,  0.5f,   0f, 1f, 0f,   1f, 0f,
         0.5f,  0.5f, -0.5f,   0f, 1f, 0f,   1f, 1f,
        -0.5f,  0.5f, -0.5f,   0f, 1f, 0f,   0f, 1f,

        // Bottom face (-Y)
        -0.5f, -0.5f, -0.5f,   0f, -1f, 0f,   0f, 0f,
         0.5f, -0.5f, -0.5f,   0f, -1f, 0f,   1f, 0f,
         0.5f, -0.5f,  0.5f,   0f, -1f, 0f,   1f, 1f,
        -0.5f, -0.5f,  0.5f,   0f, -1f, 0f,   0f, 1f,
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
