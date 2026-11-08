
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
    private StarLabelRenderer? _labelRenderer;
    private StarNavigation? _navigation;
    private DestinationRenderer? _destinationRenderer;

    // Camera position and movement
    private Vector3 _cameraPosition = Vector3.Zero;
    private readonly Vector3 _homePosition = Vector3.Zero;

    private const float CameraSpeed = 30f;
    private const float RotationSpeed = 90f;
    private const float MouseSensitivity = 0.15f;

    private float _yaw = -90f;
    private float _pitch = 0f;

    private Vector2 _lastMousePosition;
    private bool _isRotating = false;

    // Animated travel
    private bool _isTravelling = false;
    private Vector3 _travelStart;
    private Vector3 _travelEnd;
    private float _travelElapsed = 0f;
    private const float TravelDuration = 6f;
    private int _lastProgress = -1;
    private string _destinationName = "";

    // Smooth camera turning
    private bool _isTurning = false;
    private float _turnStartYaw;
    private float _turnStartPitch;
    private float _turnTargetYaw;
    private float _turnTargetPitch;
    private float _turnElapsed;
    private const float TurnDuration = 1.2f;

    public StarChartWindow(StarCollection starCollection)
        : base(
            GameWindowSettings.Default,
            new NativeWindowSettings()
            {
                ClientSize = new Vector2i(1000, 700),
                Title = "3D Star Chart - Named Star Navigation",
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
        _navigation = new StarNavigation(_starCollection);
        _destinationRenderer = new DestinationRenderer();

        Console.WriteLine();
        Console.WriteLine("=== STAR CHART CONTROLS ===");
        Console.WriteLine("N: Select next destination");
        Console.WriteLine("P: Select previous destination");
        Console.WriteLine("ENTER: Travel to selected star");
        Console.WriteLine("R: Return home");
        Console.WriteLine("F: Toggle named-star mode");
        Console.WriteLine("WASD: Move camera");
        Console.WriteLine("Arrow keys: Rotate");
        Console.WriteLine("Space / Shift: Move vertically");
        Console.WriteLine("Trackpad: Zoom");
        Console.WriteLine("Right-click + drag: Rotate");
        Console.WriteLine("ESC: Exit");
        Console.WriteLine();
        Console.WriteLine(
            "Navigation star names are always enabled.");
    }

    private Vector3 GetCameraForward()
    {
        float yawRadians =
            MathHelper.DegreesToRadians(_yaw);

        float pitchRadians =
            MathHelper.DegreesToRadians(_pitch);

        return Vector3.Normalize(new Vector3(
            MathF.Cos(yawRadians) * MathF.Cos(pitchRadians),
            MathF.Sin(pitchRadians),
            MathF.Sin(yawRadians) * MathF.Cos(pitchRadians)
        ));
    }

    private static float ShortestAngleDifference(
        float start,
        float target)
    {
        float difference = (target - start) % 360f;

        if (difference > 180f)
            difference -= 360f;

        if (difference < -180f)
            difference += 360f;

        return difference;
    }

    private void StartTurningToward(Vector3 destination)
    {
        Vector3 direction =
            destination - _cameraPosition;

        if (direction.LengthSquared < 0.000001f)
            return;

        direction = Vector3.Normalize(direction);

        _turnStartYaw = _yaw;
        _turnStartPitch = _pitch;

        _turnTargetYaw =
            MathHelper.RadiansToDegrees(
                MathF.Atan2(
                    direction.Z,
                    direction.X));

        _turnTargetPitch =
            MathHelper.RadiansToDegrees(
                MathF.Asin(
                    MathHelper.Clamp(
                        direction.Y,
                        -1f,
                        1f)));

        _turnElapsed = 0f;
        _isTurning = true;
    }

    private void FaceSelectedStar()
    {
        Star? star = _navigation?.SelectedStar;

        if (star == null)
            return;

        Vector3 destination = new Vector3(
            (float)star.X,
            (float)star.Y,
            (float)star.Z);

        StartTurningToward(destination);

        Console.WriteLine(
            $"Turning toward {star.Name}...");
    }

    private void UpdateTurning(float deltaTime)
    {
        if (!_isTurning)
            return;

        _turnElapsed += deltaTime;

        float t = MathHelper.Clamp(
            _turnElapsed / TurnDuration,
            0f,
            1f);

        float smoothT =
            t * t * (3f - 2f * t);

        float yawDifference =
            ShortestAngleDifference(
                _turnStartYaw,
                _turnTargetYaw);

        _yaw =
            _turnStartYaw +
            yawDifference * smoothT;

        _pitch = MathHelper.Lerp(
            _turnStartPitch,
            _turnTargetPitch,
            smoothT);

        if (t >= 1f)
        {
            _isTurning = false;
            _yaw = _turnTargetYaw;
            _pitch = _turnTargetPitch;
        }
    }

    private void StartTravel()
    {
        Star? star = _navigation?.SelectedStar;

        if (star == null)
        {
            Console.WriteLine(
                "No destination selected.");
            return;
        }

        Vector3 destination = new Vector3(
            (float)star.X,
            (float)star.Y,
            (float)star.Z);

        _travelStart = _cameraPosition;

        Vector3 direction =
            destination - _travelStart;

        float distance = direction.Length;

        if (distance < 0.001f)
        {
            Console.WriteLine(
                "Already at the destination.");
            return;
        }

        float stoppingDistance =
            MathF.Min(2f, distance * 0.1f);

        _travelEnd =
            destination -
            Vector3.Normalize(direction) *
            stoppingDistance;

        _travelElapsed = 0f;
        _lastProgress = -1;
        _destinationName = star.Name;
        _isTravelling = true;

        StartTurningToward(destination);

        Console.WriteLine();
        Console.WriteLine(
            $"Travelling to {_destinationName}...");

        Console.WriteLine(
            $"Starting position: " +
            $"X={_travelStart.X:F2}, " +
            $"Y={_travelStart.Y:F2}, " +
            $"Z={_travelStart.Z:F2}");

        Console.WriteLine(
            $"Distance: {distance:F2} parsecs");

        Console.WriteLine(
            $"Travel duration: {TravelDuration} seconds");
    }

    private void UpdateTravel(float deltaTime)
    {
        if (!_isTravelling)
            return;

        _travelElapsed += deltaTime;

        float t = MathHelper.Clamp(
            _travelElapsed / TravelDuration,
            0f,
            1f);

        float smoothT =
            t * t * (3f - 2f * t);

        _cameraPosition = Vector3.Lerp(
            _travelStart,
            _travelEnd,
            smoothT);

        int milestone =
            (int)(t * 100) / 25;

        if (milestone > _lastProgress)
        {
            _lastProgress = milestone;

            Console.WriteLine(
                $"Travel progress: " +
                $"{Math.Min(milestone * 25, 100)}%");
        }

        if (t >= 1f)
        {
            _isTravelling = false;

            Console.WriteLine(
                $"Arrived near {_destinationName}!");
        }
    }

    private void ReturnHome()
    {
        _isTravelling = false;
        _isTurning = false;

        _cameraPosition = _homePosition;
        _yaw = -90f;
        _pitch = 0f;

        Console.WriteLine(
            "Returned to starting position.");
    }

    protected override void OnUpdateFrame(
        FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        float deltaTime = (float)args.Time;

        if (KeyboardState.IsKeyPressed(Keys.F))
            _renderer?.ToggleNamedStars();

        if (KeyboardState.IsKeyPressed(Keys.N))
        {
            if (!_isTravelling)
            {
                _navigation?.SelectNext();
                FaceSelectedStar();
            }
        }

        if (KeyboardState.IsKeyPressed(Keys.P))
        {
            if (!_isTravelling)
            {
                _navigation?.SelectPrevious();
                FaceSelectedStar();
            }
        }

        if (KeyboardState.IsKeyPressed(Keys.Enter)
            && !_isTravelling)
        {
            StartTravel();
        }

        if (KeyboardState.IsKeyPressed(Keys.R))
            ReturnHome();

        UpdateTurning(deltaTime);
        UpdateTravel(deltaTime);

        if (_isTravelling || _isTurning)
            return;

        float movement =
            CameraSpeed * deltaTime;

        float rotation =
            RotationSpeed * deltaTime;

        if (KeyboardState.IsKeyDown(Keys.Left))
            _yaw -= rotation;

        if (KeyboardState.IsKeyDown(Keys.Right))
            _yaw += rotation;

        if (KeyboardState.IsKeyDown(Keys.Up))
            _pitch += rotation;

        if (KeyboardState.IsKeyDown(Keys.Down))
            _pitch -= rotation;

        _pitch = MathHelper.Clamp(
            _pitch, -89f, 89f);

        Vector3 forward = GetCameraForward();

        Vector3 right = Vector3.Normalize(
            Vector3.Cross(
                forward,
                Vector3.UnitY));

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

    protected override void OnMouseMove(
        MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isTravelling || _isTurning)
            return;

        if (!MouseState.IsButtonDown(
            MouseButton.Right))
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

        Vector2 delta =
            e.Position - _lastMousePosition;

        _lastMousePosition = e.Position;

        _yaw += delta.X * MouseSensitivity;
        _pitch -= delta.Y * MouseSensitivity;

        _pitch = MathHelper.Clamp(
            _pitch, -89f, 89f);
    }

    protected override void OnMouseWheel(
        MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (_isTravelling || _isTurning)
            return;

        _cameraPosition +=
            GetCameraForward() * e.OffsetY * 5f;
    }

    protected override void OnRenderFrame(
        FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(
            ClearBufferMask.ColorBufferBit);

        float aspect =
            Size.X / (float)Math.Max(1, Size.Y);

        Matrix4 projection =
            Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f),
                aspect,
                0.1f,
                10000f);

        Matrix4 view = Matrix4.LookAt(
            _cameraPosition,
            _cameraPosition + GetCameraForward(),
            Vector3.UnitY);

        Matrix4 viewProjection =
            view * projection;

        // Draw the full star field
        _renderer?.Draw(viewProjection);

        // Highlight the selected star
        _destinationRenderer?.Draw(
            _navigation?.SelectedStar,
            viewProjection);

        // Always draw all 10 navigation star names
        if (_navigation != null)
        {
            _labelRenderer?.Draw(
                _navigation.Destinations,
                viewProjection,
                _cameraPosition,
                Size.X,
                Size.Y);
        }

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        _destinationRenderer?.Dispose();
        _labelRenderer?.Dispose();
        _renderer?.Dispose();

        base.OnUnload();
    }
}
