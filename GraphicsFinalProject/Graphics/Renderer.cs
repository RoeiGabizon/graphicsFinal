using OpenTK.Graphics.OpenGL;

namespace GraphicsFinalProject.Graphics;

/// <summary>
/// Holds the core OpenGL setup and per-frame rendering logic.
/// Kept intentionally simple: no scene graph, no engine abstractions.
/// </summary>
public class Renderer
{
    public void Initialize()
    {
        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
    }

    public void Resize(int width, int height)
    {
        GL.Viewport(0, 0, width, height);
    }

    public void Render(float deltaTime)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Future phases will draw the robot and corridor geometry here.
    }

    public void Dispose()
    {
        // No GPU resources allocated yet in this phase.
    }
}
