using OpenTK.Mathematics;

namespace GraphicsFinalProject.Scene;

/// <summary>
/// Builds a simple futuristic corridor entirely out of scaled/translated
/// unit cubes. Every piece below is expressed as Translation * Rotation *
/// Scale applied to the same shared cube mesh (see Renderer.DrawCube) -
/// there is no separate geometry per piece, only different matrices.
/// </summary>
public class Corridor
{
    /// <summary>Signature matching Renderer's DrawCube(model, color) helper.</summary>
    public delegate void CubeDrawer(Matrix4 model, Vector3 color);

    private const float Width = 8.0f;
    private const float Height = 5.0f;
    private const float Length = 30.0f;

    private static readonly Vector3 FloorColor = new(0.25f, 0.25f, 0.28f);
    private static readonly Vector3 CeilingColor = new(0.18f, 0.18f, 0.22f);
    private static readonly Vector3 WallColor = new(0.30f, 0.35f, 0.45f);
    private static readonly Vector3 EndWallColor = new(0.22f, 0.24f, 0.30f);
    private static readonly Vector3 PanelColor = new(0.10f, 0.65f, 0.85f);
    private static readonly Vector3 BeamColor = new(0.55f, 0.55f, 0.60f);
    private static readonly Vector3 DoorFrameColor = new(0.85f, 0.65f, 0.10f);

    /// <summary>
    /// Draws every corridor piece by calling <paramref name="drawCube"/>
    /// once per piece with its own model matrix and color.
    /// </summary>
    public void Draw(CubeDrawer drawCube)
    {
        const float wallThickness = 0.3f;

        // The corridor runs along -Z, starting at Z = 0 (entrance) and
        // ending at Z = -Length (end wall). It is centered on X = 0.

        // --- Floor ---
        // A single flat cube: wide and long, but very thin (the "scale").
        Matrix4 floorModel =
            Matrix4.CreateScale(Width, wallThickness, Length) *
            Matrix4.CreateTranslation(0.0f, -wallThickness / 2.0f, -Length / 2.0f);
        drawCube(floorModel, FloorColor);

        // --- Ceiling ---
        Matrix4 ceilingModel =
            Matrix4.CreateScale(Width, wallThickness, Length) *
            Matrix4.CreateTranslation(0.0f, Height + wallThickness / 2.0f, -Length / 2.0f);
        drawCube(ceilingModel, CeilingColor);

        // --- Left wall ---
        Matrix4 leftWallModel =
            Matrix4.CreateScale(wallThickness, Height, Length) *
            Matrix4.CreateTranslation(-Width / 2.0f - wallThickness / 2.0f, Height / 2.0f, -Length / 2.0f);
        drawCube(leftWallModel, WallColor);

        // --- Right wall ---
        Matrix4 rightWallModel =
            Matrix4.CreateScale(wallThickness, Height, Length) *
            Matrix4.CreateTranslation(Width / 2.0f + wallThickness / 2.0f, Height / 2.0f, -Length / 2.0f);
        drawCube(rightWallModel, WallColor);

        // --- End wall (closes off the far end of the corridor) ---
        Matrix4 endWallModel =
            Matrix4.CreateScale(Width, Height, wallThickness) *
            Matrix4.CreateTranslation(0.0f, Height / 2.0f, -Length - wallThickness / 2.0f);
        drawCube(endWallModel, EndWallColor);

        DrawWallPanels(drawCube);
        DrawCeilingBeams(drawCube);
        DrawDoorFrame(drawCube);
    }

    /// <summary>
    /// A row of glowing accent panels running along both walls, evenly
    /// spaced down the corridor. Purely decorative.
    /// </summary>
    private void DrawWallPanels(CubeDrawer drawCube)
    {
        const float panelWidth = 0.1f;
        const float panelHeight = 1.0f;
        const float panelDepth = 2.0f;
        const int panelCount = 6;
        const float spacing = Length / panelCount;

        for (int i = 0; i < panelCount; i++)
        {
            float z = -spacing * (i + 0.5f);

            Matrix4 leftPanel =
                Matrix4.CreateScale(panelWidth, panelHeight, panelDepth) *
                Matrix4.CreateTranslation(-Width / 2.0f, Height / 2.0f, z);
            drawCube(leftPanel, PanelColor);

            Matrix4 rightPanel =
                Matrix4.CreateScale(panelWidth, panelHeight, panelDepth) *
                Matrix4.CreateTranslation(Width / 2.0f, Height / 2.0f, z);
            drawCube(rightPanel, PanelColor);
        }
    }

    /// <summary>
    /// Structural-looking beams across the ceiling, evenly spaced.
    /// </summary>
    private void DrawCeilingBeams(CubeDrawer drawCube)
    {
        const float beamWidth = 0.4f;
        const float beamHeight = 0.4f;
        const int beamCount = 7;
        const float spacing = Length / beamCount;

        for (int i = 0; i < beamCount; i++)
        {
            float z = -spacing * (i + 0.5f);

            Matrix4 beamModel =
                Matrix4.CreateScale(Width, beamHeight, beamWidth) *
                Matrix4.CreateTranslation(0.0f, Height - beamHeight / 2.0f, z);
            drawCube(beamModel, BeamColor);
        }
    }

    /// <summary>
    /// A simple door frame (three cubes) set just in front of the end wall.
    /// </summary>
    private void DrawDoorFrame(CubeDrawer drawCube)
    {
        const float frameThickness = 0.3f;
        const float doorWidth = 3.0f;
        const float doorHeight = 3.5f;
        float frameZ = -Length + 1.0f;

        // Left post
        Matrix4 leftPost =
            Matrix4.CreateScale(frameThickness, doorHeight, frameThickness) *
            Matrix4.CreateTranslation(-doorWidth / 2.0f, doorHeight / 2.0f, frameZ);
        drawCube(leftPost, DoorFrameColor);

        // Right post
        Matrix4 rightPost =
            Matrix4.CreateScale(frameThickness, doorHeight, frameThickness) *
            Matrix4.CreateTranslation(doorWidth / 2.0f, doorHeight / 2.0f, frameZ);
        drawCube(rightPost, DoorFrameColor);

        // Top lintel spanning between the posts
        Matrix4 lintel =
            Matrix4.CreateScale(doorWidth + frameThickness, frameThickness, frameThickness) *
            Matrix4.CreateTranslation(0.0f, doorHeight, frameZ);
        drawCube(lintel, DoorFrameColor);
    }
}
