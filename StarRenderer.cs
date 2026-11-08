
using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StarChart;

public class StarRenderer : IDisposable
{
    private readonly int _vertexArray;
    private readonly int _vertexBuffer;
    private readonly int _shaderProgram;
    private readonly int _starCount;

    public StarRenderer(StarCollection collection)
    {
        int count = Math.Min(collection.Stars.Count, 10000);
        _starCount = count;

        float[] vertices = new float[count * 3];

        for (int i = 0; i < count; i++)
        {
            Star star = collection.Stars[i];

            vertices[i * 3] = (float)star.X;
            vertices[i * 3 + 1] = (float)star.Y;
            vertices[i * 3 + 2] = (float)star.Z;
        }

        const string vertexShaderSource = """
            #version 330 core

            layout (location = 0) in vec3 aPosition;

            uniform mat4 uViewProjection;

            void main()
            {
                gl_Position = uViewProjection * vec4(aPosition, 1.0);
                gl_PointSize = 3.0;
            }
            """;

        const string fragmentShaderSource = """
            #version 330 core

            out vec4 FragColor;

            void main()
            {
                FragColor = vec4(1.0, 1.0, 1.0, 1.0);
            }
            """;

        int vertexShader = CompileShader(
            ShaderType.VertexShader,
            vertexShaderSource
        );

        int fragmentShader = CompileShader(
            ShaderType.FragmentShader,
            fragmentShaderSource
        );

        _shaderProgram = GL.CreateProgram();

        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);

        GL.GetProgram(
            _shaderProgram,
            GetProgramParameterName.LinkStatus,
            out int linked
        );

        if (linked == 0)
        {
            throw new Exception(
                GL.GetProgramInfoLog(_shaderProgram)
            );
        }

        GL.DetachShader(_shaderProgram, vertexShader);
        GL.DetachShader(_shaderProgram, fragmentShader);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();

        GL.BindVertexArray(_vertexArray);

        GL.BindBuffer(
            BufferTarget.ArrayBuffer,
            _vertexBuffer
        );

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.StaticDraw
        );

        GL.VertexAttribPointer(
            0,
            3,
            VertexAttribPointerType.Float,
            false,
            3 * sizeof(float),
            0
        );

        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    private static int CompileShader(
        ShaderType type,
        string source)
    {
        int shader = GL.CreateShader(type);

        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(
            shader,
            ShaderParameter.CompileStatus,
            out int success
        );

        if (success == 0)
        {
            throw new Exception(
                GL.GetShaderInfoLog(shader)
            );
        }

        return shader;
    }

    public void Draw(Matrix4 viewProjection)
    {
        GL.UseProgram(_shaderProgram);

        int location = GL.GetUniformLocation(
            _shaderProgram,
            "uViewProjection"
        );

        GL.UniformMatrix4(
            location,
            false,
            ref viewProjection
        );

        GL.BindVertexArray(_vertexArray);

        GL.DrawArrays(
            PrimitiveType.Points,
            0,
            _starCount
        );

        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteProgram(_shaderProgram);
    }
}
