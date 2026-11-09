
using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StarChart;

public sealed class DustRenderer : IDisposable
{
    private readonly int _program;
    private readonly int _vao;
    private readonly int _vbo;

    private readonly Vector3[] _particles;
    private readonly Random _random = new(42);

    private const int ParticleCount = 350;
    private const float Spread = 35f;

    public DustRenderer()
    {
        _particles = new Vector3[ParticleCount];

        for (int i = 0; i < ParticleCount; i++)
            _particles[i] = RandomPosition();

        const string vertexSource = """
            #version 330 core

            layout(location = 0) in vec3 aPosition;

            uniform mat4 uViewProjection;

            void main()
            {
                gl_Position =
                    uViewProjection * vec4(aPosition, 1.0);

                gl_PointSize = 6.0;
            }
            """;

        const string fragmentSource = """
            #version 330 core

            out vec4 FragColor;

            void main()
            {
                vec2 point = gl_PointCoord * 2.0 - 1.0;

                if (dot(point, point) > 1.0)
                    discard;

                FragColor = vec4(0.3, 0.8, 1.0, 1.0);
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
            throw new Exception(
                GL.GetProgramInfoLog(_program));

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(
            BufferTarget.ArrayBuffer, _vbo);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            ParticleCount * 3 * sizeof(float),
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

    private Vector3 RandomPosition()
    {
        return new Vector3(
            ((float)_random.NextDouble() - 0.5f) * Spread,
            ((float)_random.NextDouble() - 0.5f) * Spread,
            ((float)_random.NextDouble() - 0.5f) * Spread);
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
            throw new Exception(
                GL.GetShaderInfoLog(shader));

        return shader;
    }

    public void Update(float deltaTime)
    {
        for (int i = 0; i < ParticleCount; i++)
        {
            Vector3 p = _particles[i];

            p.Z += deltaTime * 15f;

            if (p.Z > Spread / 2f)
                p.Z = -Spread / 2f;

            _particles[i] = p;
        }
    }

    public void Draw(
        Matrix4 viewProjection,
        Vector3 cameraPosition)
    {
        float[] vertices = new float[ParticleCount * 3];

        for (int i = 0; i < ParticleCount; i++)
        {
            Vector3 position =
                _particles[i] + cameraPosition;

            int index = i * 3;

            vertices[index] = position.X;
            vertices[index + 1] = position.Y;
            vertices[index + 2] = position.Z;
        }

        GL.UseProgram(_program);

        int location = GL.GetUniformLocation(
            _program,
            "uViewProjection");

        GL.UniformMatrix4(
            location,
            false,
            ref viewProjection);

        GL.BindVertexArray(_vao);
        GL.BindBuffer(
            BufferTarget.ArrayBuffer, _vbo);

        GL.BufferSubData(
            BufferTarget.ArrayBuffer,
            IntPtr.Zero,
            vertices.Length * sizeof(float),
            vertices);

        GL.Enable(EnableCap.ProgramPointSize);

        GL.DrawArrays(
            PrimitiveType.Points,
            0,
            ParticleCount);

        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_program);
    }
}
