using OpenTK.Windowing.GraphicsLibraryFramework;
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
    private Vector3 _cameraPosition = new Vector3(0f, 0f, 100f);
private float _cameraSpeed = 30f;

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

protected override void OnUpdateFrame(FrameEventArgs args)
{
    base.OnUpdateFrame(args);

    if (KeyboardState.IsKeyDown(Keys.Escape))
    {
        Close();
        return;
    }

    float movement = _cameraSpeed * (float)args.Time;

    if (KeyboardState.IsKeyDown(Keys.W))
        _cameraPosition.Z -= movement;

    if (KeyboardState.IsKeyDown(Keys.S))
        _cameraPosition.Z += movement;

    if (KeyboardState.IsKeyDown(Keys.A))
        _cameraPosition.X -= movement;

    if (KeyboardState.IsKeyDown(Keys.D))
        _cameraPosition.X += movement;

    if (KeyboardState.IsKeyDown(Keys.Space))
        _cameraPosition.Y += movement;

    if (KeyboardState.IsKeyDown(Keys.LeftShift))
        _cameraPosition.Y -= movement;
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
         _cameraPosition,
        _cameraPosition - Vector3.UnitZ,
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

