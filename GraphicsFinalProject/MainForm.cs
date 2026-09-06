using System.Diagnostics;
using OpenTK.Windowing.Common;
using OpenTK.GLControl;
using GraphicsFinalProject.Graphics;

namespace GraphicsFinalProject;

/// <summary>
/// Main application window. Hosts an OpenTK GLControl for 3D rendering
/// on the left and a Windows Forms control panel on the right.
/// </summary>
public class MainForm : Form
{
    private readonly GLControl _glControl;
    private readonly Panel _controlPanel;
    private readonly Label _titleLabel;
    private readonly Renderer _renderer = new();
    private readonly Stopwatch _clock = new();

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
        };
        _glControl.Load += GlControl_Load;
        _glControl.Resize += GlControl_Resize;
        _glControl.Paint += GlControl_Paint;

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

        // Drive continuous redraws for the animation loop (added later phases).
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

        _renderer.Render(deltaTime);
        _glControl.SwapBuffers();
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
