
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StarChart;

public sealed class StarRenderer : IDisposable
{
    private readonly int _program;
    private readonly int _vao;
    private readonly int _allBuffer;
    private readonly int _namedBuffer;

    private readonly List<Star> _namedStars;
    private readonly int _allCount;

    private bool _showNamedOnly;

    public bool ShowNamedOnly => _showNamedOnly;

    public IReadOnlyList<Star> NamedStars => _namedStars;

    private static readonly string[] PriorityNames =
    {
        "Rojani", "Sirius", "Vega", "Altair",
        "Procyon", "Arcturus", "Rigel",
        "Betelgeuse", "Capella", "Deneb", "Polaris"
    };

    public StarRenderer(StarCollection collection)
    {
        var allStars = collection.Stars.ToList();

        _allCount = allStars.Count;

        var named = collection.GetNamedStars().ToList();

        _namedStars = named
            .OrderBy(star =>
            {
                int index = Array.FindIndex(
                    PriorityNames,
                    name => string.Equals(
                        name,
                        star.Name,
                        StringComparison.OrdinalIgnoreCase));

                return index < 0
                    ? PriorityNames.Length
                    : index;
            })
            .ThenBy(
                star => star.Name,
                StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .ToList();

        const string vertexSource = """
            #version 330 core

            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec3 aColour;

            uniform mat4 uViewProjection;

            out vec3 vColour;

            void main()
            {
                gl_Position =
                    uViewProjection * vec4(aPosition, 1.0);

                gl_PointSize = 3.5;
                vColour = aColour;
            }
            """;

        const string fragmentSource = """
            #version 330 core

            in vec3 vColour;
            out vec4 FragColor;

            void main()
            {
                vec2 point = gl_PointCoord * 2.0 - 1.0;
                float radius = length(point);

                if (radius > 1.0)
                    discard;

                float brightness = 1.0 - radius * 0.25;

                FragColor = vec4(
                    vColour * brightness,
                    1.0);
            }
            """;

        int vertexShader = CompileShader(
            ShaderType.VertexShader, vertexSource);

        int fragmentShader = CompileShader(
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

        GL.DetachShader(_program, vertexShader);
        GL.DetachShader(_program, fragmentShader);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        _vao = GL.GenVertexArray();
        _allBuffer = GL.GenBuffer();
        _namedBuffer = GL.GenBuffer();

        Upload(_allBuffer, CreateVertices(allStars));
        Upload(_namedBuffer, CreateVertices(_namedStars));

        GL.BindVertexArray(_vao);
        ConfigureBuffer(_allBuffer);
        GL.BindVertexArray(0);

        Console.WriteLine(
            $"Loaded {_allCount} stars for rendering.");

        Console.WriteLine(
            $"Prepared {_namedStars.Count} named stars for F mode.");
    }

    private static void Upload(int buffer, float[] vertices)
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            vertices.Length * sizeof(float),
            vertices,
            BufferUsageHint.StaticDraw);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    private static float[] CreateVertices(
        IEnumerable<Star> stars)
    {
        var selected = stars.ToList();
        float[] result = new float[selected.Count * 6];

        for (int i = 0; i < selected.Count; i++)
        {
            Star star = selected[i];
            int index = i * 6;

            result[index] = (float)star.X;
            result[index + 1] = (float)star.Y;
            result[index + 2] = (float)star.Z;

            Vector3 colour =
                GetStarColour(star.SpectralType);

            result[index + 3] = colour.X;
            result[index + 4] = colour.Y;
            result[index + 5] = colour.Z;
        }

        return result;
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

    private static void ConfigureBuffer(int buffer)
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);

        int stride = 6 * sizeof(float);

        GL.VertexAttribPointer(
            0, 3,
            VertexAttribPointerType.Float,
            false, stride, 0);

        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(
            1, 3,
            VertexAttribPointerType.Float,
            false,
            stride,
            3 * sizeof(float));

        GL.EnableVertexAttribArray(1);
    }

    public void ToggleNamedStars()
    {
        _showNamedOnly = !_showNamedOnly;

        Console.WriteLine(
            _showNamedOnly
                ? $"Showing {_namedStars.Count} named stars."
                : $"Showing {_allCount} stars.");
    }

    public void Draw(Matrix4 viewProjection)
    {
        GL.Enable(EnableCap.ProgramPointSize);

        GL.UseProgram(_program);

        int location = GL.GetUniformLocation(
            _program, "uViewProjection");

        GL.UniformMatrix4(
            location,
            false,
            ref viewProjection);

        GL.BindVertexArray(_vao);

        ConfigureBuffer(
            _showNamedOnly ? _namedBuffer : _allBuffer);

        GL.DrawArrays(
            PrimitiveType.Points,
            0,
            _showNamedOnly
                ? _namedStars.Count
                : _allCount);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_allBuffer);
        GL.DeleteBuffer(_namedBuffer);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_program);
    }
}
