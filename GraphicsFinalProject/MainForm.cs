using System.Diagnostics;
using OpenTK.Windowing.Common;
using OpenTK.GLControl;
using OpenTK.Mathematics;
using GraphicsFinalProject.Graphics;
using GraphicsFinalProject.Scene;

namespace GraphicsFinalProject;

/// <summary>
/// Main application window. Hosts an OpenTK GLControl for 3D rendering
/// on the left and a Windows Forms control panel on the right.
///
/// MainForm is responsible for translating Windows Forms input (keyboard,
/// mouse) into calls on the Camera. The Camera does the math; the
/// Renderer only consumes the resulting matrices.
/// </summary>
public class MainForm : Form
{
    private readonly GLControl _glControl;
    private readonly Panel _controlPanel;
    private readonly Label _titleLabel;
    private readonly Renderer _renderer = new();
    private readonly Camera _camera = new(new Vector3(0.0f, 1.5f, 5.0f));
    private readonly Robot _robot = new()
    {
        // Standing near the corridor entrance, facing down the corridor
        // (which runs along -Z).
        Position = new Vector3(0.0f, 0.0f, -2.0f),
        RotationY = 180.0f,
    };
    private readonly Stopwatch _clock = new();

    private readonly HashSet<Keys> _pressedKeys = new();
    private bool _isLooking;
    private Point _lastMousePosition;
    private bool _isAnimationPaused;
    private bool _spaceWasDown;
    private bool _spotlightEnabled = true;
    private bool _lWasDown;
    private bool _tWasDown;
    private bool _pWasDown;
    private bool _walkingAnimationEnabled = true;
    private RadioButton _perspectiveRadio = null!;
    private RadioButton _orthographicRadio = null!;
    private CheckBox _spotlightCheckBox = null!;
    private CheckBox _walkingCheckBox = null!;
    private CheckBox _shadowsCheckBox = null!;
    private CheckBox _reflectionsCheckBox = null!;
    private TrackBar _fovTrackBar = null!;

    public MainForm()
    {
        Text = "Graphics Final Project";
        Width = 1200;
        Height = 800;
        KeyPreview = true;

        // GLControl settings: request an OpenGL 3.3 Core profile context.
        var glSettings = new GLControlSettings
        {
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
            API = ContextAPI.OpenGL,
        };

        _glControl = new GLControl(glSettings)
        {
            Dock = DockStyle.Fill,
            TabStop = true,
        };
        _glControl.Load += GlControl_Load;
        _glControl.Resize += GlControl_Resize;
        _glControl.Paint += GlControl_Paint;

        // Camera input: keyboard for movement, mouse for look/zoom.
        // Hooked on the Form (via KeyPreview) rather than the GLControl so
        // input still works regardless of which child control has focus.
        KeyDown += GlControl_KeyDown;
        KeyUp += GlControl_KeyUp;
        PreviewKeyDown += GlControl_PreviewKeyDown;
        _glControl.MouseDown += GlControl_MouseDown;
        _glControl.MouseUp += GlControl_MouseUp;
        _glControl.MouseMove += GlControl_MouseMove;
        _glControl.MouseWheel += GlControl_MouseWheel;

        _controlPanel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 220,
            BackColor = SystemColors.Control,
        };

        _titleLabel = new Label
        {
            Text = "Graphics Final Project",
            AutoSize = true,
            Location = new Point(10, 10),
        };
        _controlPanel.Controls.Add(_titleLabel);
        CreateControlPanel();

        // Add GLControl first, then the panel, so the panel keeps its
        // fixed width and the GLControl fills the remaining space.
        Controls.Add(_glControl);
        Controls.Add(_controlPanel);

        // Drive continuous redraws for the animation loop.
        Application.Idle += (_, _) => _glControl.Invalidate();
    }

    private void GlControl_Load(object? sender, EventArgs e)
    {
        _renderer.Initialize();
        _renderer.Resize(_glControl.ClientSize.Width, _glControl.ClientSize.Height);
        _clock.Start();
    }

    private void GlControl_Resize(object? sender, EventArgs e)
    {
        if (!_glControl.IsHandleCreated)
        {
            return;
        }

        _renderer.Resize(_glControl.ClientSize.Width, _glControl.ClientSize.Height);
        _glControl.Invalidate();
    }

    private void GlControl_Paint(object? sender, PaintEventArgs e)
    {
        float deltaTime = (float)_clock.Elapsed.TotalSeconds;
        _clock.Restart();
        deltaTime = MathF.Min(deltaTime, 0.1f);

        _camera.Move(
            forward: _pressedKeys.Contains(Keys.W),
            backward: _pressedKeys.Contains(Keys.S),
            left: _pressedKeys.Contains(Keys.A),
            right: _pressedKeys.Contains(Keys.D),
            up: _pressedKeys.Contains(Keys.E),
            down: _pressedKeys.Contains(Keys.Q),
            deltaTime: deltaTime);

        // Space toggles pause on press (not held), so tapping it doesn't
        // rapidly flip the pause state every frame.
        bool spaceIsDown = _pressedKeys.Contains(Keys.Space);
        if (spaceIsDown && !_spaceWasDown)
        {
            _isAnimationPaused = !_isAnimationPaused;
        }
        _spaceWasDown = spaceIsDown;

        bool lIsDown = _pressedKeys.Contains(Keys.L);
        if (lIsDown && !_lWasDown)
        {
            _spotlightEnabled = !_spotlightEnabled;
            _spotlightCheckBox.Checked = _spotlightEnabled;
        }
        _lWasDown = lIsDown;

        bool tIsDown = _pressedKeys.Contains(Keys.T);
        if (tIsDown && !_tWasDown)
        {
            _renderer.CycleRobotAppearance();
        }
        _tWasDown = tIsDown;

        bool pIsDown = _pressedKeys.Contains(Keys.P);
        if (pIsDown && !_pWasDown)
        {
            _camera.ProjectionMode = _camera.ProjectionMode == ProjectionMode.Perspective
                ? ProjectionMode.Orthographic
                : ProjectionMode.Perspective;
            _perspectiveRadio.Checked = _camera.ProjectionMode == ProjectionMode.Perspective;
            _orthographicRadio.Checked = _camera.ProjectionMode == ProjectionMode.Orthographic;
        }
        _pWasDown = pIsDown;

        _robot.Update(
            deltaTime: deltaTime,
            moveForward: _pressedKeys.Contains(Keys.Up),
            moveBackward: _pressedKeys.Contains(Keys.Down),
            rotateLeft: _pressedKeys.Contains(Keys.Left),
            rotateRight: _pressedKeys.Contains(Keys.Right),
            animate: _walkingAnimationEnabled && !_isAnimationPaused);

        _renderer.Render(deltaTime, _camera, _robot, _spotlightEnabled, _shadowsCheckBox.Checked, _reflectionsCheckBox.Checked);
        _glControl.SwapBuffers();
    }

    private void CreateControlPanel()
    {
        _controlPanel.AutoScroll = true;
        int y = 40;

        GroupBox projectionGroup = new()
        {
            Text = "Projection",
            Location = new Point(8, y),
            Size = new Size(198, 78),
        };
        _perspectiveRadio = new RadioButton
        {
            Text = "Perspective",
            Location = new Point(10, 22),
            AutoSize = true,
            Checked = true,
        };
        _orthographicRadio = new RadioButton
        {
            Text = "Orthographic",
            Location = new Point(10, 46),
            AutoSize = true,
        };
        _perspectiveRadio.CheckedChanged += (_, _) => SetProjectionFromControls();
        _orthographicRadio.CheckedChanged += (_, _) => SetProjectionFromControls();
        projectionGroup.Controls.AddRange(new Control[] { _perspectiveRadio, _orthographicRadio });
        _controlPanel.Controls.Add(projectionGroup);
        y += 86;

        GroupBox lightingGroup = new()
        {
            Text = "Lighting",
            Location = new Point(8, y),
            Size = new Size(198, 190),
        };
        TrackBar lightTrackBar = CreateTrackBar(0, 30, 12, new Point(10, 28));
        TrackBar ambientTrackBar = CreateTrackBar(0, 100, 25, new Point(10, 84));
        lightingGroup.Controls.Add(new Label { Text = "Main Light Intensity", Location = new Point(10, 10), AutoSize = true });
        lightingGroup.Controls.Add(lightTrackBar);
        lightingGroup.Controls.Add(new Label { Text = "Ambient Intensity", Location = new Point(10, 66), AutoSize = true });
        lightingGroup.Controls.Add(ambientTrackBar);
        _spotlightCheckBox = new CheckBox { Text = "Robot Spotlight", Location = new Point(10, 138), AutoSize = true, Checked = true };
        lightingGroup.Controls.Add(_spotlightCheckBox);
        lightTrackBar.Scroll += (_, _) => _renderer.MainLightIntensity = lightTrackBar.Value / 10.0f;
        ambientTrackBar.Scroll += (_, _) => _renderer.AmbientIntensity = ambientTrackBar.Value / 100.0f;
        _spotlightCheckBox.CheckedChanged += (_, _) => _spotlightEnabled = _spotlightCheckBox.Checked;
        _controlPanel.Controls.Add(lightingGroup);
        y += 198;

        GroupBox robotGroup = new()
        {
            Text = "Robot",
            Location = new Point(8, y),
            Size = new Size(198, 175),
        };
        ComboBox appearanceCombo = new()
        {
            Location = new Point(10, 28),
            Width = 170,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        appearanceCombo.Items.AddRange(new object[] { "Metallic / Default", "Red", "Blue" });
        appearanceCombo.SelectedIndex = 0;
        appearanceCombo.SelectedIndexChanged += (_, _) => _renderer.SetRobotAppearance(appearanceCombo.SelectedIndex);
        _walkingCheckBox = new CheckBox { Text = "Walking Animation", Location = new Point(10, 62), AutoSize = true, Checked = true };
        _walkingCheckBox.CheckedChanged += (_, _) => _walkingAnimationEnabled = _walkingCheckBox.Checked;
        TrackBar scaleTrackBar = CreateTrackBar(5, 20, 10, new Point(10, 110));
        scaleTrackBar.Scroll += (_, _) => _robot.Scale = scaleTrackBar.Value / 10.0f;
        robotGroup.Controls.Add(new Label { Text = "Robot Appearance", Location = new Point(10, 10), AutoSize = true });
        robotGroup.Controls.Add(appearanceCombo);
        robotGroup.Controls.Add(_walkingCheckBox);
        robotGroup.Controls.Add(new Label { Text = "Robot Scale", Location = new Point(10, 94), AutoSize = true });
        robotGroup.Controls.Add(scaleTrackBar);
        _controlPanel.Controls.Add(robotGroup);
        y += 183;

        GroupBox renderingGroup = new()
        {
            Text = "Rendering",
            Location = new Point(8, y),
            Size = new Size(198, 105),
        };
        CheckBox texturesCheckBox = new() { Text = "Textures", Location = new Point(10, 22), AutoSize = true, Checked = true };
        _shadowsCheckBox = new() { Text = "Planar Shadows", Location = new Point(10, 46), AutoSize = true, Checked = true };
        _reflectionsCheckBox = new() { Text = "Planar Reflections", Location = new Point(10, 70), AutoSize = true, Checked = true };
        texturesCheckBox.CheckedChanged += (_, _) => _renderer.TexturesEnabled = texturesCheckBox.Checked;
        renderingGroup.Controls.AddRange(new Control[] { texturesCheckBox, _shadowsCheckBox, _reflectionsCheckBox });
        _controlPanel.Controls.Add(renderingGroup);
        y += 113;

        GroupBox cameraGroup = new()
        {
            Text = "Camera",
            Location = new Point(8, y),
            Size = new Size(198, 92),
        };
        _fovTrackBar = CreateTrackBar(20, 90, 60, new Point(10, 28));
        _fovTrackBar.Scroll += (_, _) => _camera.Fov = _fovTrackBar.Value;
        cameraGroup.Controls.Add(new Label { Text = "Field of View", Location = new Point(10, 10), AutoSize = true });
        cameraGroup.Controls.Add(_fovTrackBar);
        _controlPanel.Controls.Add(cameraGroup);
    }

    private static TrackBar CreateTrackBar(int minimum, int maximum, int value, Point location)
    {
        return new TrackBar
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            TickFrequency = Math.Max(1, (maximum - minimum) / 5),
            Location = location,
            Width = 170,
        };
    }

    private void SetProjectionFromControls()
    {
        if (_perspectiveRadio.Checked)
        {
            _camera.ProjectionMode = ProjectionMode.Perspective;
        }
        else if (_orthographicRadio.Checked)
        {
            _camera.ProjectionMode = ProjectionMode.Orthographic;
        }

        _fovTrackBar.Enabled = _camera.ProjectionMode == ProjectionMode.Perspective;
        _glControl.Invalidate();
    }

    private void GlControl_KeyDown(object? sender, KeyEventArgs e)
    {
        _pressedKeys.Add(e.KeyCode);
    }

    private void GlControl_KeyUp(object? sender, KeyEventArgs e)
    {
        _pressedKeys.Remove(e.KeyCode);
    }

    private void GlControl_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
    {
        // Arrow keys and Space are normally reserved by WinForms for
        // dialog/control navigation and never reach KeyDown unless we
        // explicitly ask for them here.
        switch (e.KeyCode)
        {
            case Keys.Up:
            case Keys.Down:
            case Keys.Left:
            case Keys.Right:
            case Keys.Space:
                e.IsInputKey = true;
                break;
        }
    }

    private void GlControl_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        // Right mouse button held down = look mode. Give the control focus
        // so WASD keeps working while looking around.
        _isLooking = true;
        _lastMousePosition = e.Location;
        _glControl.Focus();
    }

    private void GlControl_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            _isLooking = false;
        }
    }

    private void GlControl_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isLooking)
        {
            return;
        }

        float deltaX = e.Location.X - _lastMousePosition.X;
        float deltaY = e.Location.Y - _lastMousePosition.Y;
        _lastMousePosition = e.Location;

        _camera.ProcessMouseLook(deltaX, deltaY);
    }

    private void GlControl_MouseWheel(object? sender, MouseEventArgs e)
    {
        // e.Delta is +/-120 per notch; scale down to a gentle FOV step.
        _camera.ProcessMouseWheel(e.Delta / 12.0f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderer.Dispose();
        }
        base.Dispose(disposing);
    }
}
