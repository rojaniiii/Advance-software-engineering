
using System;
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

    // Camera settings
    private Vector3 _cameraPosition = new Vector3(0f, 0f, 100f);
    private float _cameraSpeed = 30f;

    // Camera rotation
    private float _yaw = -90f;
    private float _pitch = 0f;
    private float _rotationSpeed = 90f;

    // Mouse and trackpad settings
    private float _mouseSensitivity = 0.15f;
    private Vector2 _lastMousePosition;
    private bool _isRotating = false;

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

    // Initialise the 3D window
    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0f, 0f, 0.05f, 1f);
        GL.Enable(EnableCap.ProgramPointSize);

        _renderer = new StarRenderer(_starCollection);

        Console.WriteLine("3D star renderer ready.");
        Console.WriteLine("Use WASD to move.");
        Console.WriteLine("Use arrow keys to rotate.");
        Console.WriteLine("Use Space and Shift to move vertically.");
        Console.WriteLine("Use the trackpad to zoom.");
        Console.WriteLine("Right-click and drag to rotate.");
        Console.WriteLine("Press F to filter stars by proper name.");
        Console.WriteLine("Press ESC to exit.");
    }

    // Calculate the direction the camera is facing
    private Vector3 GetCameraForward()
    {
        float yawRadians = MathHelper.DegreesToRadians(_yaw);
        float pitchRadians = MathHelper.DegreesToRadians(_pitch);

        Vector3 direction = new Vector3(
            MathF.Cos(yawRadians) * MathF.Cos(pitchRadians),
            MathF.Sin(pitchRadians),
            MathF.Sin(yawRadians) * MathF.Cos(pitchRadians)
        );

        return Vector3.Normalize(direction);
    }

    // Handle keyboard navigation and filtering
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // Close the application
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        // Toggle between all stars and properly named stars
        if (KeyboardState.IsKeyPressed(Keys.F))
        {
            _renderer?.ToggleNamedStars();
        }

        float deltaTime = (float)args.Time;
        float movement = _cameraSpeed * deltaTime;
        float rotation = _rotationSpeed * deltaTime;

        // Rotate camera using arrow keys
        if (KeyboardState.IsKeyDown(Keys.Left))
            _yaw -= rotation;

        if (KeyboardState.IsKeyDown(Keys.Right))
            _yaw += rotation;

        if (KeyboardState.IsKeyDown(Keys.Up))
            _pitch += rotation;

        if (KeyboardState.IsKeyDown(Keys.Down))
            _pitch -= rotation;

        // Prevent the camera from rotating too far vertically
        _pitch = MathHelper.Clamp(_pitch, -89f, 89f);

        // Calculate the camera directions
        Vector3 forward = GetCameraForward();

        Vector3 right = Vector3.Normalize(
            Vector3.Cross(forward, Vector3.UnitY)
        );

        // Move forward and backward
        if (KeyboardState.IsKeyDown(Keys.W))
            _cameraPosition += forward * movement;

        if (KeyboardState.IsKeyDown(Keys.S))
            _cameraPosition -= forward * movement;

        // Move left and right
        if (KeyboardState.IsKeyDown(Keys.A))
            _cameraPosition -= right * movement;

        if (KeyboardState.IsKeyDown(Keys.D))
            _cameraPosition += right * movement;

        // Move vertically
        if (KeyboardState.IsKeyDown(Keys.Space))
            _cameraPosition.Y += movement;

        if (KeyboardState.IsKeyDown(Keys.LeftShift))
            _cameraPosition.Y -= movement;
    }

    // Rotate camera with right-click and drag
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

    // Zoom using mouse wheel or MacBook trackpad
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        Vector3 forward = GetCameraForward();

        _cameraPosition += forward * e.OffsetY * 5f;
    }

    // Render the 3D star chart
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
            _cameraPosition + GetCameraForward(),
            Vector3.UnitY
        );

        Matrix4 viewProjection = view * projection;

        _renderer?.Draw(viewProjection);

        SwapBuffers();
    }

    // Release graphics resources
    protected override void OnUnload()
    {
        _renderer?.Dispose();

        base.OnUnload();
    }
}
