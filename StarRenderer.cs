
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

        // Position (XYZ) + colour (RGB)
        float[] vertices = new float[count * 6];

        int colouredStars = 0;

        for (int i = 0; i < count; i++)
        {
            Star star = collection.Stars[i];
            int index = i * 6;

            vertices[index] = (float)star.X;
            vertices[index + 1] = (float)star.Y;
            vertices[index + 2] = (float)star.Z;

            Vector3 colour = GetStarColour(star.SpectralType);

            vertices[index + 3] = colour.X;
            vertices[index + 4] = colour.Y;
            vertices[index + 5] = colour.Z;

            if (!string.IsNullOrWhiteSpace(star.SpectralType)
                && "OBAFGKM".Contains(
                    char.ToUpperInvariant(star.SpectralType.Trim()[0])))
            {
                colouredStars++;
            }
        }

        Console.WriteLine(
            $"Rendering {_starCount} stars; " +
            $"{colouredStars} have recognised spectral types.");

        const string vertexShaderSource = """
            #version 330 core

            layout (location = 0) in vec3 aPosition;
            layout (location = 1) in vec3 aColour;

            uniform mat4 uViewProjection;

            out vec3 starColour;

            void main()
            {
                gl_Position =
                    uViewProjection * vec4(aPosition, 1.0);

                gl_PointSize = 3.0;
                starColour = aColour;
            }
            """;

        const string fragmentShaderSource = """
            #version 330 core

            in vec3 starColour;
            out vec4 FragColor;

            void main()
            {
                FragColor = vec4(starColour, 1.0);
            }
            """;

        int vertexShader = CompileShader(
            ShaderType.VertexShader, vertexShaderSource);

        int fragmentShader = CompileShader(
            ShaderType.FragmentShader, fragmentShaderSource);

        _shaderProgram = GL.CreateProgram();

        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);

        GL.GetProgram(
            _shaderProgram,
            GetProgramParameterName.LinkStatus,
            out int linked);

        if (linked == 0)
        {
            throw new Exception(
                GL.GetProgramInfoLog(_shaderProgram));
        }

        GL.DetachShader(_shaderProgram, vertexShader);
        GL.DetachShader(_shaderProgram, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.StaticDraw);

        int stride = 6 * sizeof(float);

        // XYZ coordinates
        GL.VertexAttribPointer(
            0, 3, VertexAttribPointerType.Float,
            false, stride, 0);

        GL.EnableVertexAttribArray(0);

        // RGB colour
        GL.VertexAttribPointer(
            1, 3, VertexAttribPointerType.Float,
            false, stride, 3 * sizeof(float));

        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
    }

    private static Vector3 GetStarColour(string spectralType)
    {
        if (string.IsNullOrWhiteSpace(spectralType))
            return new Vector3(1f, 1f, 1f);

        char type = char.ToUpperInvariant(
            spectralType.Trim()[0]);

        return type switch
        {
            'O' => new Vector3(0.60f, 0.70f, 1.00f),
            'B' => new Vector3(0.70f, 0.80f, 1.00f),
            'A' => new Vector3(0.90f, 0.93f, 1.00f),
            'F' => new Vector3(1.00f, 1.00f, 0.90f),
            'G' => new Vector3(1.00f, 0.90f, 0.60f),
            'K' => new Vector3(1.00f, 0.70f, 0.40f),
            'M' => new Vector3(1.00f, 0.45f, 0.35f),
            _ => new Vector3(1f, 1f, 1f)
        };
    }

    private static int CompileShader(
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

    public void Draw(Matrix4 viewProjection)
    {
        GL.UseProgram(_shaderProgram);

        int location = GL.GetUniformLocation(
            _shaderProgram, "uViewProjection");

        GL.UniformMatrix4(
            location, false, ref viewProjection);

        GL.BindVertexArray(_vertexArray);
        GL.DrawArrays(PrimitiveType.Points, 0, _starCount);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteProgram(_shaderProgram);
    }
}
