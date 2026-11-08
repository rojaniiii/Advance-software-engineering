
using System;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace StarChart;

public class StarChartWindow : GameWindow
{
    private readonly StarCollection _starCollection;
    private StarRenderer? _renderer;

    public StarChartWindow(StarCollection starCollection)
        : base(
            GameWindowSettings.Default,
            new NativeWindowSettings()
            {
                ClientSize = new Vector2i(1000, 700),
                Title = "3D Star Chart",
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core
            })
    {
        _starCollection = starCollection;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0f, 0f, 0.05f, 1f);
        GL.Enable(EnableCap.ProgramPointSize);

        _renderer = new StarRenderer(_starCollection);

        Console.WriteLine("3D star renderer ready.");
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        float aspect = Size.X / (float)Math.Max(1, Size.Y);

        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(60f),
            aspect,
            0.1f,
            10000f
        );

        Matrix4 view = Matrix4.LookAt(
            new Vector3(0f, 0f, 100f),
            Vector3.Zero,
            Vector3.UnitY
        );

        Matrix4 viewProjection = view * projection;

        _renderer?.Draw(viewProjection);

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        _renderer?.Dispose();
        base.OnUnload();
    }
}
