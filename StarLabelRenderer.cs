
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StarChart;

public sealed class StarLabelRenderer : IDisposable
{
    private readonly int _program;
    private readonly int _vao;
    private readonly int _vbo;

    private const float PixelSize = 2f;
    private const int MaxLabels = 50;
    private const float LabelPadding = 5f;

    private readonly List<LabelBox> _labels = new();

    private sealed class LabelBox
    {
        public Star Star { get; init; } = null!;
        public float Left { get; init; }
        public float Top { get; init; }
        public float Right { get; init; }
        public float Bottom { get; init; }
    }

    public Star? PickLabel(Vector2 pointer)
    {
        foreach (LabelBox label in _labels)
        {
            if (pointer.X >= label.Left &&
                pointer.X <= label.Right &&
                pointer.Y >= label.Top &&
                pointer.Y <= label.Bottom)
            {
                return label.Star;
            }
        }

        return null;
    }

    private static readonly Dictionary<char, string[]> Font = new()
    {
        ['A'] = new[] { "01110","10001","10001","11111","10001","10001","10001" },
        ['B'] = new[] { "11110","10001","10001","11110","10001","10001","11110" },
        ['C'] = new[] { "01111","10000","10000","10000","10000","10000","01111" },
        ['D'] = new[] { "11110","10001","10001","10001","10001","10001","11110" },
        ['E'] = new[] { "11111","10000","10000","11110","10000","10000","11111" },
        ['F'] = new[] { "11111","10000","10000","11110","10000","10000","10000" },
        ['G'] = new[] { "01111","10000","10000","10111","10001","10001","01111" },
        ['H'] = new[] { "10001","10001","10001","11111","10001","10001","10001" },
        ['I'] = new[] { "11111","00100","00100","00100","00100","00100","11111" },
        ['J'] = new[] { "00111","00010","00010","00010","10010","10010","01100" },
        ['K'] = new[] { "10001","10010","10100","11000","10100","10010","10001" },
        ['L'] = new[] { "10000","10000","10000","10000","10000","10000","11111" },
        ['M'] = new[] { "10001","11011","10101","10101","10001","10001","10001" },
        ['N'] = new[] { "10001","11001","10101","10011","10001","10001","10001" },
        ['O'] = new[] { "01110","10001","10001","10001","10001","10001","01110" },
        ['P'] = new[] { "11110","10001","10001","11110","10000","10000","10000" },
        ['Q'] = new[] { "01110","10001","10001","10001","10101","10010","01101" },
        ['R'] = new[] { "11110","10001","10001","11110","10100","10010","10001" },
        ['S'] = new[] { "01111","10000","10000","01110","00001","00001","11110" },
        ['T'] = new[] { "11111","00100","00100","00100","00100","00100","00100" },
        ['U'] = new[] { "10001","10001","10001","10001","10001","10001","01110" },
        ['V'] = new[] { "10001","10001","10001","10001","10001","01010","00100" },
        ['W'] = new[] { "10001","10001","10001","10101","10101","10101","01010" },
        ['X'] = new[] { "10001","10001","01010","00100","01010","10001","10001" },
        ['Y'] = new[] { "10001","10001","01010","00100","00100","00100","00100" },
        ['Z'] = new[] { "11111","00001","00010","00100","01000","10000","11111" },
        ['0'] = new[] { "01110","10001","10011","10101","11001","10001","01110" },
        ['1'] = new[] { "00100","01100","00100","00100","00100","00100","01110" },
        ['2'] = new[] { "01110","10001","00001","00010","00100","01000","11111" },
        ['3'] = new[] { "11110","00001","00001","01110","00001","00001","11110" },
        ['4'] = new[] { "00010","00110","01010","10010","11111","00010","00010" },
        ['5'] = new[] { "11111","10000","10000","11110","00001","00001","11110" },
        ['6'] = new[] { "01111","10000","10000","11110","10001","10001","01110" },
        ['7'] = new[] { "11111","00001","00010","00100","01000","01000","01000" },
        ['8'] = new[] { "01110","10001","10001","01110","10001","10001","01110" },
        ['9'] = new[] { "01110","10001","10001","01111","00001","00001","11110" },
        ['-'] = new[] { "00000","00000","00000","11111","00000","00000","00000" },
        ['.'] = new[] { "00000","00000","00000","00000","00000","01100","01100" },
        ['\''] = new[] { "00100","00100","00010","00000","00000","00000","00000" },
        [' '] = new[] { "00000","00000","00000","00000","00000","00000","00000" }
    };

    public StarLabelRenderer()
    {
        const string vertexSource = """
            #version 330 core

            layout(location = 0) in vec2 aPosition;

            void main()
            {
                gl_Position = vec4(aPosition, 0.0, 1.0);
            }
            """;

        const string fragmentSource = """
            #version 330 core

            out vec4 FragColor;

            void main()
            {
                FragColor = vec4(1.0, 0.95, 0.70, 1.0);
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

        GL.DetachShader(_program, vertexShader);
        GL.DetachShader(_program, fragmentShader);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        GL.VertexAttribPointer(
            0, 2,
            VertexAttribPointerType.Float,
            false,
            2 * sizeof(float),
            0);

        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
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

    private static bool Overlaps(LabelBox a, LabelBox b)
    {
        return a.Left < b.Right + LabelPadding &&
               a.Right + LabelPadding > b.Left &&
               a.Top < b.Bottom + LabelPadding &&
               a.Bottom + LabelPadding > b.Top;
    }

    private bool TryPlaceLabel(
        Star star,
        float pointX,
        float pointY,
        int width,
        int height,
        out float labelX,
        out float labelY)
    {
        labelX = 0f;
        labelY = 0f;

        float textWidth =
            star.Name.Length * 6f * PixelSize;

        float textHeight = 7f * PixelSize;

        float gap = 12f;

        // Try different positions around the star.
        Vector2[] offsets =
        {
            new Vector2(gap, -textHeight / 2f),
            new Vector2(-textWidth - gap, -textHeight / 2f),
            new Vector2(-textWidth / 2f, -textHeight - gap),
            new Vector2(-textWidth / 2f, gap),
            new Vector2(gap, -textHeight - gap),
            new Vector2(-textWidth - gap, -textHeight - gap),
            new Vector2(gap, gap),
            new Vector2(-textWidth - gap, gap)
        };

        foreach (Vector2 offset in offsets)
        {
            float x = pointX + offset.X;
            float y = pointY + offset.Y;

            LabelBox candidate = new()
            {
                Star = star,
                Left = x,
                Top = y,
                Right = x + textWidth,
                Bottom = y + textHeight
            };

            // Do not place text outside the window.
            if (candidate.Left < 5f ||
                candidate.Top < 5f ||
                candidate.Right > width - 5f ||
                candidate.Bottom > height - 5f)
                continue;

            bool occupied = _labels.Any(existing =>
                Overlaps(candidate, existing));

            if (occupied)
                continue;

            _labels.Add(candidate);

            labelX = x;
            labelY = y;
            return true;
        }

        // No free position: hide this label instead
        // of drawing it over another label.
        return false;
    }

    public void Draw(
        IEnumerable<Star> stars,
        Matrix4 viewProjection,
        Vector3 cameraPosition,
        int width,
        int height)
    {
        _labels.Clear();

        if (width <= 0 || height <= 0)
            return;

        var visible = new List<
            (Star Star, float X, float Y, float Distance)>();

        foreach (Star star in stars)
        {
            Vector4 clip = Vector4.TransformRow(
                new Vector4(
                    (float)star.X,
                    (float)star.Y,
                    (float)star.Z,
                    1f),
                viewProjection);

            if (clip.W <= 0.0001f)
                continue;

            float x = clip.X / clip.W;
            float y = clip.Y / clip.W;
            float z = clip.Z / clip.W;

            if (x < -1f || x > 1f ||
                y < -1f || y > 1f ||
                z < -1f || z > 1f)
                continue;

            float distance = Vector3.Distance(
                cameraPosition,
                new Vector3(
                    (float)star.X,
                    (float)star.Y,
                    (float)star.Z));

            visible.Add((star, x, y, distance));
        }

        // Prioritise recognisable stars.
        string[] priorityNames =
        {
            "Rojani", "Sirius", "Vega", "Altair",
            "Procyon", "Arcturus", "Rigel",
            "Betelgeuse", "Capella", "Deneb", "Polaris"
        };

        var selected = visible
            .OrderBy(item =>
            {
                int index = Array.FindIndex(
                    priorityNames,
                    name => string.Equals(
                        name,
                        item.Star.Name,
                        StringComparison.OrdinalIgnoreCase));

                return index < 0
                    ? priorityNames.Length
                    : index;
            })
            .ThenBy(item => item.Distance)
            .Take(MaxLabels);

        List<float> vertices = new();

        foreach (var item in selected)
        {
            float pointX =
                (item.X + 1f) * width / 2f;

            float pointY =
                (1f - item.Y) * height / 2f;

            if (!TryPlaceLabel(
                item.Star,
                pointX,
                pointY,
                width,
                height,
                out float labelX,
                out float labelY))
                continue;

            AddText(
                vertices,
                item.Star.Name,
                labelX,
                labelY,
                width,
                height);
        }

        if (vertices.Count == 0)
            return;

        float[] data = vertices.ToArray();

        GL.UseProgram(_program);
        GL.BindVertexArray(_vao);

        GL.BindBuffer(
            BufferTarget.ArrayBuffer, _vbo);

        GL.BufferData(
            BufferTarget.ArrayBuffer,
            data.Length * sizeof(float),
            data,
            BufferUsageHint.DynamicDraw);

        GL.Disable(EnableCap.DepthTest);

        GL.DrawArrays(
            PrimitiveType.Triangles,
            0,
            data.Length / 2);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    private static void AddText(
        List<float> vertices,
        string text,
        float x,
        float y,
        int width,
        int height)
    {
        foreach (char character in text.ToUpperInvariant())
        {
            if (!Font.TryGetValue(
                character, out string[]? glyph))
            {
                x += 6 * PixelSize;
                continue;
            }

            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    if (glyph[row][col] != '1')
                        continue;

                    float left = x + col * PixelSize;
                    float top = y + row * PixelSize;

                    AddRectangle(
                        vertices,
                        left,
                        top,
                        PixelSize,
                        PixelSize,
                        width,
                        height);
                }
            }

            x += 6 * PixelSize;
        }
    }

    private static void AddRectangle(
        List<float> vertices,
        float x,
        float y,
        float w,
        float h,
        int screenWidth,
        int screenHeight)
    {
        float left =
            2f * x / screenWidth - 1f;

        float right =
            2f * (x + w) / screenWidth - 1f;

        float top =
            1f - 2f * y / screenHeight;

        float bottom =
            1f - 2f * (y + h) / screenHeight;

        vertices.AddRange(new[]
        {
            left, top,
            right, top,
            right, bottom,

            left, top,
            right, bottom,
            left, bottom
        });
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        GL.DeleteProgram(_program);
    }
}
