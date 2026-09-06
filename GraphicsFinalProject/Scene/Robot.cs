using OpenTK.Mathematics;

namespace GraphicsFinalProject.Scene;

/// <summary>
/// A simple low-poly humanoid robot built entirely from cubes, using
/// hierarchical (parent-child) transformations. Every body part is
/// positioned relative to its parent joint, so rotating a joint (e.g. the
/// shoulder) carries the whole limb with it - exactly like a real skeleton.
///
/// There is no generic "skeleton" or animation framework here: each part's
/// matrix is written out explicitly so it is easy to point at during a
/// defense.
/// </summary>
public class Robot
{
    /// <summary>Signature matching Renderer's DrawCube(model, color) helper.</summary>
    public delegate void CubeDrawer(Matrix4 model, Vector3 color);

    // --- Root transform: where the whole robot is in the world ---
    public Vector3 Position { get; set; } = Vector3.Zero;
    public float RotationY { get; set; } // degrees, facing direction
    public float Scale { get; set; } = 1.0f;

    // --- Joint angles (degrees), one per animated body part ---
    public float HeadAngle { get; set; }
    public float LeftArmAngle { get; set; }
    public float RightArmAngle { get; set; }
    public float LeftLegAngle { get; set; }
    public float RightLegAngle { get; set; }

    // --- Body part sizes (kept as fields so the joint math below can
    //     reference them directly - this is a display robot, not a
    //     configurable rig) ---
    private const float TorsoWidth = 0.9f;
    private const float TorsoHeight = 1.1f;
    private const float TorsoDepth = 0.5f;

    private const float HeadSize = 0.5f;

    private const float UpperArmLength = 0.55f;
    private const float LowerArmLength = 0.5f;
    private const float ArmThickness = 0.25f;

    private const float LegLength = 0.7f;
    private const float LegThickness = 0.3f;

    private static readonly Vector3 TorsoColor = new(0.75f, 0.15f, 0.15f);
    private static readonly Vector3 HeadColor = new(0.85f, 0.85f, 0.85f);
    private static readonly Vector3 ArmColor = new(0.75f, 0.75f, 0.15f);
    private static readonly Vector3 LegColor = new(0.20f, 0.35f, 0.75f);
    private static readonly Vector3 EyeColor = new(0.10f, 0.90f, 0.95f);
    private static readonly Vector3 AntennaColor = new(0.90f, 0.30f, 0.10f);
    private static readonly Vector3 ShoulderColor = new(0.45f, 0.45f, 0.50f);

    /// <summary>
    /// Height of the ground the robot's feet stand on, measured from the
    /// robot's own local origin. Used by Renderer/animation code that
    /// needs to know how tall the robot is.
    /// </summary>
    public float FeetToHipHeight => LegLength;

    /// <summary>
    /// Draws the whole robot. Every body part multiplies the root world
    /// matrix by a chain of local (parent-relative) transforms before
    /// being handed to <paramref name="drawCube"/>.
    /// </summary>
    public void Draw(CubeDrawer drawCube)
    {
        // Root/world transform shared by every part: where the robot
        // stands, which way it faces, and how big it is overall.
        Matrix4 root =
            Matrix4.CreateScale(Scale) *
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(RotationY)) *
            Matrix4.CreateTranslation(Position);

        // Hip height: legs plant the whole robot on the floor. Everything
        // above (torso, head, arms) is built upward from hip height.
        float hipY = LegLength;

        DrawLegs(drawCube, root, hipY);
        DrawTorso(drawCube, root, hipY);
        DrawHead(drawCube, root, hipY);
        DrawArms(drawCube, root, hipY);
    }

    private void DrawLegs(CubeDrawer drawCube, Matrix4 root, float hipY)
    {
        const float hipSpacing = TorsoWidth / 2.0f - LegThickness / 2.0f;

        DrawLeg(drawCube, root, hipY, hipSpacing: -hipSpacing, angle: LeftLegAngle);
        DrawLeg(drawCube, root, hipY, hipSpacing: hipSpacing, angle: RightLegAngle);
    }

    private void DrawLeg(CubeDrawer drawCube, Matrix4 root, float hipY, float hipSpacing, float angle)
    {
        // Hip joint: fixed position relative to the robot root.
        Matrix4 hipJoint = Matrix4.CreateTranslation(hipSpacing, hipY, 0.0f) * root;

        // The leg swings forward/back around the hip (rotation happens
        // before the leg is pushed downward, so it pivots at the hip
        // rather than at the leg's own center).
        Matrix4 legRotation = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(angle));

        // Move the leg's cube down by half its length so its TOP edge
        // (not its center) sits at the hip joint.
        Matrix4 legLocal =
            Matrix4.CreateScale(LegThickness, LegLength, LegThickness) *
            Matrix4.CreateTranslation(0.0f, -LegLength / 2.0f, 0.0f);

        Matrix4 legModel = legLocal * legRotation * hipJoint;
        drawCube(legModel, LegColor);
    }

    private void DrawTorso(CubeDrawer drawCube, Matrix4 root, float hipY)
    {
        Matrix4 torsoLocal =
            Matrix4.CreateScale(TorsoWidth, TorsoHeight, TorsoDepth) *
            Matrix4.CreateTranslation(0.0f, hipY + TorsoHeight / 2.0f, 0.0f);

        Matrix4 torsoModel = torsoLocal * root;
        drawCube(torsoModel, TorsoColor);
    }

    private void DrawHead(CubeDrawer drawCube, Matrix4 root, float hipY)
    {
        float neckY = hipY + TorsoHeight;

        // Neck joint: fixed position on top of the torso, relative to root.
        Matrix4 neckJoint = Matrix4.CreateTranslation(0.0f, neckY, 0.0f) * root;

        // Head nods/turns around the neck joint.
        Matrix4 headRotation = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(HeadAngle));

        Matrix4 headLocal =
            Matrix4.CreateScale(HeadSize, HeadSize, HeadSize) *
            Matrix4.CreateTranslation(0.0f, HeadSize / 2.0f, 0.0f);

        Matrix4 headModel = headLocal * headRotation * neckJoint;
        drawCube(headModel, HeadColor);

        // Two small eyes on the front of the head, parented to the same
        // neck joint + head rotation so they move together with the head.
        const float eyeSize = 0.08f;
        const float eyeSpacing = 0.15f;
        float eyeY = HeadSize * 0.6f;
        float eyeZ = HeadSize / 2.0f + eyeSize / 2.0f - 0.02f;

        Matrix4 leftEyeLocal =
            Matrix4.CreateScale(eyeSize, eyeSize, eyeSize) *
            Matrix4.CreateTranslation(-eyeSpacing, eyeY, eyeZ);
        drawCube(leftEyeLocal * headRotation * neckJoint, EyeColor);

        Matrix4 rightEyeLocal =
            Matrix4.CreateScale(eyeSize, eyeSize, eyeSize) *
            Matrix4.CreateTranslation(eyeSpacing, eyeY, eyeZ);
        drawCube(rightEyeLocal * headRotation * neckJoint, EyeColor);

        // Small antenna on top of the head.
        const float antennaThickness = 0.05f;
        const float antennaLength = 0.3f;
        Matrix4 antennaLocal =
            Matrix4.CreateScale(antennaThickness, antennaLength, antennaThickness) *
            Matrix4.CreateTranslation(0.0f, HeadSize + antennaLength / 2.0f, 0.0f);
        drawCube(antennaLocal * headRotation * neckJoint, AntennaColor);
    }

    private void DrawArms(CubeDrawer drawCube, Matrix4 root, float hipY)
    {
        float shoulderY = hipY + TorsoHeight * 0.85f;
        float shoulderX = TorsoWidth / 2.0f + ArmThickness / 2.0f;

        DrawArm(drawCube, root, shoulderY, shoulderX: -shoulderX, angle: LeftArmAngle);
        DrawArm(drawCube, root, shoulderY, shoulderX: shoulderX, angle: RightArmAngle);
    }

    /// <summary>
    /// Draws one arm (shoulder block + upper arm + lower arm), all
    /// hanging off a single shoulder joint so rotating the shoulder swings
    /// the entire arm as one rigid chain.
    /// </summary>
    private void DrawArm(CubeDrawer drawCube, Matrix4 root, float shoulderY, float shoulderX, float angle)
    {
        // 1) Shoulder joint: a fixed attachment point on the torso,
        //    expressed relative to the robot root.
        Matrix4 shoulderJoint = Matrix4.CreateTranslation(shoulderX, shoulderY, 0.0f) * root;

        // Small shoulder pad cube, does not rotate with the arm.
        const float shoulderPadSize = 0.3f;
        Matrix4 shoulderPadLocal = Matrix4.CreateScale(shoulderPadSize, shoulderPadSize, shoulderPadSize);
        drawCube(shoulderPadLocal * shoulderJoint, ShoulderColor);

        // 2) Shoulder rotation: swings the whole arm forward/back. This
        //    is applied AFTER the shoulder joint's translation but BEFORE
        //    any arm geometry, so it rotates around the shoulder point,
        //    not around the arm's own center.
        Matrix4 shoulderRotation = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(angle));

        // 3) Upper arm: a box whose local origin is at its TOP (shoulder
        //    end). We push it down by half its length so it hangs below
        //    the shoulder joint instead of being centered on it.
        Matrix4 upperArmLocal =
            Matrix4.CreateScale(ArmThickness, UpperArmLength, ArmThickness) *
            Matrix4.CreateTranslation(0.0f, -UpperArmLength / 2.0f, 0.0f);
        Matrix4 upperArmModel = upperArmLocal * shoulderRotation * shoulderJoint;
        drawCube(upperArmModel, ArmColor);

        // 4) Elbow joint: sits at the bottom of the upper arm. It inherits
        //    the shoulder's rotation (elbow moves with the shoulder swing)
        //    by being expressed relative to shoulderRotation * shoulderJoint,
        //    exactly the same chain the upper arm used.
        Matrix4 elbowJoint = Matrix4.CreateTranslation(0.0f, -UpperArmLength, 0.0f) * shoulderRotation * shoulderJoint;

        // 5) Lower arm hangs from the elbow the same way the upper arm
        //    hangs from the shoulder. (No separate elbow bend angle yet -
        //    it stays straight, but the joint already exists for later.)
        Matrix4 lowerArmLocal =
            Matrix4.CreateScale(ArmThickness * 0.85f, LowerArmLength, ArmThickness * 0.85f) *
            Matrix4.CreateTranslation(0.0f, -LowerArmLength / 2.0f, 0.0f);
        Matrix4 lowerArmModel = lowerArmLocal * elbowJoint;
        drawCube(lowerArmModel, ArmColor);
    }
}
