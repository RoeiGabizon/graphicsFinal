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
    private readonly Stopwatch _clock = new();

    private readonly HashSet<Keys> _pressedKeys = new();
    private bool _isLooking;
    private Point _lastMousePosition;

    public MainForm()
    {
        Text = "Graphics Final Project";
        Width = 1200;
        Height = 800;

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
        _glControl.KeyDown += GlControl_KeyDown;
        _glControl.KeyUp += GlControl_KeyUp;
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

        _camera.Move(
            forward: _pressedKeys.Contains(Keys.W),
            backward: _pressedKeys.Contains(Keys.S),
            left: _pressedKeys.Contains(Keys.A),
            right: _pressedKeys.Contains(Keys.D),
            up: _pressedKeys.Contains(Keys.E),
            down: _pressedKeys.Contains(Keys.Q),
            deltaTime: deltaTime);

        _renderer.Render(deltaTime, _camera);
        _glControl.SwapBuffers();
    }

    private void GlControl_KeyDown(object? sender, KeyEventArgs e)
    {
        _pressedKeys.Add(e.KeyCode);
    }

    private void GlControl_KeyUp(object? sender, KeyEventArgs e)
    {
        _pressedKeys.Remove(e.KeyCode);
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
