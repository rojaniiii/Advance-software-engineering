
using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StarChart;

public class StarRenderer : IDisposable
{
    private readonly int _vertexArray;
    private readonly int _vertexBuffer;
    private readonly int _shaderProgram;
    private readonly StarCollection _collection;

    private const int MaxNamedStars = 50;

    private int _starCount;
    private bool _showNamedOnly = false;

    public bool ShowNamedOnly => _showNamedOnly;

    public StarRenderer(StarCollection collection)
    {
        _collection = collection;

        // Load ALL available stars, not just the first 20,000
        float[] vertices = CreateVertices(
            collection.Stars);

        _starCount = vertices.Length / 6;

        Console.WriteLine(
            $"Rendering {_starCount} stars with spectral colours.");

        const string vertexShaderSource = """
            #version 330 core

            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec3 aColour;

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
                vec2 p = gl_PointCoord * 2.0 - 1.0;

                float radius = length(p);

                if (radius > 1.0)
                    discard;

                float brightness =
                    1.0 - radius * 0.35;

                FragColor = vec4(
                    starColour * brightness,
                    1.0
                );
            }
            """;

        int vertexShader = CompileShader(
            ShaderType.VertexShader,
            vertexShaderSource);

        int fragmentShader = CompileShader(
            ShaderType.FragmentShader,
            fragmentShaderSource);

        _shaderProgram = GL.CreateProgram();

        GL.AttachShader(
            _shaderProgram,
            vertexShader);

        GL.AttachShader(
            _shaderProgram,
            fragmentShader);

        GL.LinkProgram(_shaderProgram);

        GL.GetProgram(
            _shaderProgram,
            GetProgramParameterName.LinkStatus,
            out int linked);

        if (linked == 0)
            throw new Exception(
                GL.GetProgramInfoLog(_shaderProgram));

        GL.DetachShader(
            _shaderProgram,
            vertexShader);

        GL.DetachShader(
            _shaderProgram,
            fragmentShader);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        _vertexArray = GL.GenVertexArray();
        _vertexBuffer = GL.GenBuffer();

        GL.BindVertexArray(_vertexArray);

        GL.BindBuffer(
            BufferTarget.ArrayBuffer,
            _vertexBuffer);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.DynamicDraw);

        int stride = 6 * sizeof(float);

        GL.VertexAttribPointer(
            0,
            3,
            VertexAttribPointerType.Float,
            false,
            stride,
            0);

        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(
            1,
            3,
            VertexAttribPointerType.Float,
            false,
            stride,
            3 * sizeof(float));

        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);

        GL.BindBuffer(
            BufferTarget.ArrayBuffer,
            0);
    }

    private static float[] CreateVertices(
        IEnumerable<Star> stars)
    {
        Star[] selectedStars = stars.ToArray();

        float[] vertices =
            new float[selectedStars.Length * 6];

        for (int i = 0; i < selectedStars.Length; i++)
        {
            Star star = selectedStars[i];

            int index = i * 6;

            vertices[index] = (float)star.X;
            vertices[index + 1] = (float)star.Y;
            vertices[index + 2] = (float)star.Z;

            Vector3 colour =
                GetStarColour(star.SpectralType);

            vertices[index + 3] = colour.X;
            vertices[index + 4] = colour.Y;
            vertices[index + 5] = colour.Z;
        }

        return vertices;
    }

    private static Vector3 GetStarColour(
        string spectralType)
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
        ShaderType type,
        string source)
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

    public void ToggleNamedStars()
    {
        _showNamedOnly = !_showNamedOnly;

        IEnumerable<Star> selectedStars =
            _showNamedOnly
                ? _collection.GetNamedStars()
                    .OrderByDescending(star =>
                        string.Equals(
                            star.Name,
                            "Rojani",
                            StringComparison.OrdinalIgnoreCase))
                    .Take(MaxNamedStars)
                : _collection.Stars;

        float[] vertices =
            CreateVertices(selectedStars);

        GL.BindBuffer(
            BufferTarget.ArrayBuffer,
            _vertexBuffer);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.DynamicDraw);

        GL.BindBuffer(
            BufferTarget.ArrayBuffer,
            0);

        _starCount = vertices.Length / 6;

        Console.WriteLine(
            _showNamedOnly
                ? $"Showing {_starCount} named stars."
                : $"Showing {_starCount} stars.");
    }

    public void Draw(Matrix4 viewProjection)
    {
        GL.UseProgram(_shaderProgram);

        int location = GL.GetUniformLocation(
            _shaderProgram,
            "uViewProjection");

        GL.UniformMatrix4(
            location,
            false,
            ref viewProjection);

        GL.BindVertexArray(_vertexArray);

        GL.DrawArrays(
            PrimitiveType.Points,
            0,
            _starCount);

        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteProgram(_shaderProgram);
    }
}
