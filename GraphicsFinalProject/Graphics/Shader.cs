using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace GraphicsFinalProject.Graphics;

/// <summary>
/// Loads, compiles and links a vertex/fragment shader pair into an
/// OpenGL program, and exposes small helpers for setting uniforms.
/// </summary>
public class Shader : IDisposable
{
    public int Handle { get; }

    public Shader(string vertexPath, string fragmentPath)
    {
        string vertexSource = File.ReadAllText(vertexPath);
        string fragmentSource = File.ReadAllText(fragmentPath);

        int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            string infoLog = GL.GetProgramInfoLog(Handle);
            throw new Exception($"Shader program failed to link:\n{infoLog}");
        }

        // Shaders are linked into the program now; the intermediate objects
        // are no longer needed.
        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int compileStatus);
        if (compileStatus == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"Error compiling {type}:\n{infoLog}");
        }

        return shader;
    }

    public void Use()
    {
        GL.UseProgram(Handle);
    }

    public void SetMatrix4(string name, Matrix4 matrix)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.UniformMatrix4(location, false, ref matrix);
    }

    public void SetVector3(string name, Vector3 value)
    {
        int location = GL.GetUniformLocation(Handle, name);
        GL.Uniform3(location, value.X, value.Y, value.Z);
    }

    public void Dispose()
    {
        GL.DeleteProgram(Handle);
        GC.SuppressFinalize(this);
    }
}
