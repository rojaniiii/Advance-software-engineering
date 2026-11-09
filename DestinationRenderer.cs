
using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StarChart;

public class DestinationRenderer : IDisposable
{
    private readonly int _vao;
    private readonly int _vbo;
    private readonly int _program;

    public DestinationRenderer()
    {
        const string vertexSource = """
            #version 330 core
            layout(location = 0) in vec3 aPosition;
            uniform mat4 uViewProjection;

            void main()
            {
                gl_Position = uViewProjection *
                    vec4(aPosition, 1.0);
                gl_PointSize = 18.0;
            }
            """;

        const string fragmentSource = """
            #version 330 core
            out vec4 FragColor;

            void main()
            {
                vec2 p = gl_PointCoord * 2.0 - 1.0;
                float radius = length(p);

                if (radius > 1.0)
                    discard;

                float brightness = 1.0 - radius * 0.45;

                FragColor = vec4(
                    1.0 * brightness,
                    0.85 * brightness,
                    0.25 * brightness,
                    1.0
                );
            }
            """;

        int vertexShader = Compile(
            ShaderType.VertexShader, vertexSource);

        int fragmentShader = Compile(
            ShaderType.FragmentShader, fragmentSource);

        _program = GL.CreateProgram();

        GL.AttachShader(_program, vertexShader);
        GL.AttachShader(_program, fragmentShader);
        GL.LinkProgram(_program);

        GL.GetProgram(
            _program,
            GetProgramParameterName.LinkStatus,
            out int linked);

        if (linked == 0)
            throw new Exception(GL.GetProgramInfoLog(_program));

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            3 * sizeof(float),
            IntPtr.Zero,
            BufferUsageHint.DynamicDraw);

        GL.VertexAttribPointer(
            0, 3,
            VertexAttribPointerType.Float,
            false,
            3 * sizeof(float),
            0);

        GL.EnableVertexAttribArray(0);
        GL.BindVertexArray(0);
    }

    private static int Compile(
        ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(
            shader,
            ShaderParameter.CompileStatus,
            out int success);

        if (success == 0)
            throw new Exception(GL.GetShaderInfoLog(shader));

        return shader;
    }

    public void Draw(Star? star, Matrix4 viewProjection)
    {
        if (star == null)
            return;

        float[] position =
        {
            (float)star.X,
            (float)star.Y,
            (float)star.Z
        };

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        GL.BufferSubData(
            BufferTarget.ArrayBuffer,
            IntPtr.Zero,
            position.Length * sizeof(float),
            position);

        GL.UseProgram(_program);

        int location = GL.GetUniformLocation(
            _program, "uViewProjection");

        GL.UniformMatrix4(
            location,
            false,
            ref viewProjection);

        GL.BindVertexArray(_vao);

        GL.DrawArrays(
            PrimitiveType.Points, 0, 1);

        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_program);
    }
}
