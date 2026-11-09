
using System;
using System.Linq;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace StarChart;

public class StarChartWindow : GameWindow
{
    private readonly StarCollection _starCollection;

    private StarRenderer? _renderer;
    private StarLabelRenderer? _labelRenderer;

    // Camera settings
    private Vector3 _cameraPosition = new Vector3(0f, 0f, 100f);
    private float _cameraSpeed = 30f;

    // Camera rotation
    private float _yaw = -90f;
    private float _pitch = 0f;
    private float _rotationSpeed = 90f;

    // Mouse settings
    private float _mouseSensitivity = 0.15f;
    private Vector2 _lastMousePosition;
    private bool _isRotating = false;

    // Maximum number of named stars in F mode
    private const int MaxNamedStars = 50;

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
        _labelRenderer = new StarLabelRenderer();

        Console.WriteLine("3D star renderer ready.");
        Console.WriteLine("Use WASD to move.");
        Console.WriteLine("Use arrow keys to rotate.");
        Console.WriteLine("Use Space and Shift to move vertically.");
        Console.WriteLine("Use the trackpad to zoom.");
        Console.WriteLine("Right-click and drag to rotate.");
        Console.WriteLine("Press F to show up to 50 named stars.");
        Console.WriteLine("Press ESC to exit.");
    }

    private Vector3 GetCameraForward()
    {
        float yawRadians =
            MathHelper.DegreesToRadians(_yaw);

        float pitchRadians =
            MathHelper.DegreesToRadians(_pitch);

        Vector3 direction = new Vector3(
            MathF.Cos(yawRadians) * MathF.Cos(pitchRadians),
            MathF.Sin(pitchRadians),
            MathF.Sin(yawRadians) * MathF.Cos(pitchRadians)
        );

        return Vector3.Normalize(direction);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // Exit
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        // Toggle named-star mode
        if (KeyboardState.IsKeyPressed(Keys.F))
        {
            _renderer?.ToggleNamedStars();
        }

        float deltaTime = (float)args.Time;
        float movement = _cameraSpeed * deltaTime;
        float rotation = _rotationSpeed * deltaTime;

        // Camera rotation
        if (KeyboardState.IsKeyDown(Keys.Left))
            _yaw -= rotation;

        if (KeyboardState.IsKeyDown(Keys.Right))
            _yaw += rotation;

        if (KeyboardState.IsKeyDown(Keys.Up))
            _pitch += rotation;

        if (KeyboardState.IsKeyDown(Keys.Down))
            _pitch -= rotation;

        _pitch = MathHelper.Clamp(_pitch, -89f, 89f);

        Vector3 forward = GetCameraForward();

        Vector3 right = Vector3.Normalize(
            Vector3.Cross(forward, Vector3.UnitY)
        );

        // Camera movement
        if (KeyboardState.IsKeyDown(Keys.W))
            _cameraPosition += forward * movement;

        if (KeyboardState.IsKeyDown(Keys.S))
            _cameraPosition -= forward * movement;

        if (KeyboardState.IsKeyDown(Keys.A))
            _cameraPosition -= right * movement;

        if (KeyboardState.IsKeyDown(Keys.D))
            _cameraPosition += right * movement;

        if (KeyboardState.IsKeyDown(Keys.Space))
            _cameraPosition.Y += movement;

        if (KeyboardState.IsKeyDown(Keys.LeftShift))
            _cameraPosition.Y -= movement;
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (!MouseState.IsButtonDown(MouseButton.Right))
        {
            _isRotating = false;
            return;
        }

        if (!_isRotating)
        {
            _lastMousePosition = e.Position;
            _isRotating = true;
            return;
        }

        Vector2 delta = e.Position - _lastMousePosition;
        _lastMousePosition = e.Position;

        _yaw += delta.X * _mouseSensitivity;
        _pitch -= delta.Y * _mouseSensitivity;

        _pitch = MathHelper.Clamp(_pitch, -89f, 89f);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        Vector3 forward = GetCameraForward();

        _cameraPosition += forward * e.OffsetY * 5f;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        float aspect =
            Size.X / (float)Math.Max(1, Size.Y);

        Matrix4 projection =
            Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f),
                aspect,
                0.1f,
                10000f
            );

        Matrix4 view = Matrix4.LookAt(
            _cameraPosition,
            _cameraPosition + GetCameraForward(),
            Vector3.UnitY
        );

        Matrix4 viewProjection = view * projection;

        // Draw stars
        _renderer?.Draw(viewProjection);

        // Draw labels for the same 50 stars selected in F mode
        if (_renderer?.ShowNamedOnly == true)
        {
            var namedStars = _starCollection.GetNamedStars()
                .OrderByDescending(star =>
                    string.Equals(
                        star.Name,
                        "Rojani",
                        StringComparison.OrdinalIgnoreCase))
                .Take(MaxNamedStars);

            _labelRenderer?.Draw(
                namedStars,
                viewProjection,
                _cameraPosition,
                Size.X,
                Size.Y
            );
        }

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        _labelRenderer?.Dispose();
        _renderer?.Dispose();

        base.OnUnload();
    }
}
