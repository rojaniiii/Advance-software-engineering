
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

    // Camera
    private Vector3 _cameraPosition = Vector3.Zero;
    private float _yaw = -90f;
    private float _pitch = 0f;

    private float _cameraSpeed = 15f;
    private const float MinCameraSpeed = 1f;
    private const float MaxCameraSpeed = 150f;
    private const float RotationSpeed = 110f;
    private const float MouseSensitivity = 2f;
    private const float ZoomSensitivity = 2f;

    private Vector2 _lastMousePosition;
    private bool _isRotating;

    // Travel animation
    private bool _isTravelling;
    private Vector3 _travelStart;
    private Vector3 _travelEnd;
    private float _travelElapsed;
    private const float TravelDuration = 6f;
    private int _lastProgress = -1;
    private string _destinationName = "";

    // Smooth camera turning
    private bool _isTurning;
    private float _turnStartYaw;
    private float _turnStartPitch;
    private float _turnTargetYaw;
    private float _turnTargetPitch;
    private float _turnElapsed;
    private const float TurnDuration = 1.2f;

    // Star search
    private bool _isSearching;
    private string _searchText = "";
    private string _searchMessage = "";

    public StarChartWindow(StarCollection starCollection)
        : base(
            GameWindowSettings.Default,
            new NativeWindowSettings
            {
                ClientSize = new Vector2i(1000, 700),
                Title = "3D Star Chart - Camera Navigation",
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
        Console.WriteLine("N / P: Select destination");
        Console.WriteLine("/: Search for a star");
        Console.WriteLine("ENTER: Travel to selected star");
        Console.WriteLine("R: Return home");
        Console.WriteLine("F: Toggle named-star mode");
        Console.WriteLine("WASD: Move camera");
        Console.WriteLine("Arrow keys: Rotate camera");
        Console.WriteLine("Space / Shift: Move vertically");
        Console.WriteLine("Trackpad: Controlled zoom");
        Console.WriteLine("Right-click + drag: Rotate");
        Console.WriteLine("+ / -: Increase / decrease speed");
        Console.WriteLine("C: Show camera position");
        Console.WriteLine("ESC: Exit (or cancel search)");
        Console.WriteLine();
        Console.WriteLine(
            $"Initial camera speed: {_cameraSpeed:F1} pc/s");

        UpdateNavigationDisplay();
    }

    private Vector3 GetCameraForward()
    {
        float yaw = MathHelper.DegreesToRadians(_yaw);
        float pitch = MathHelper.DegreesToRadians(_pitch);

        return Vector3.Normalize(new Vector3(
            MathF.Cos(yaw) * MathF.Cos(pitch),
            MathF.Sin(pitch),
            MathF.Sin(yaw) * MathF.Cos(pitch)
        ));
    }

    private static float ShortestAngleDifference(
        float start, float target)
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
        Vector3 direction = destination - _cameraPosition;

        if (direction.LengthSquared < 0.000001f)
            return;

        direction = Vector3.Normalize(direction);

        _turnStartYaw = _yaw;
        _turnStartPitch = _pitch;

        _turnTargetYaw = MathHelper.RadiansToDegrees(
            MathF.Atan2(direction.Z, direction.X));

        _turnTargetPitch = MathHelper.RadiansToDegrees(
            MathF.Asin(MathHelper.Clamp(
                direction.Y, -1f, 1f)));

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

        _navigation?.ShowSelection(
            _cameraPosition.X,
            _cameraPosition.Y,
            _cameraPosition.Z);

        Console.WriteLine(
            $"Turning toward {star.Name}...");

        UpdateNavigationDisplay();
    }

    private void UpdateTurning(float deltaTime)
    {
        if (!_isTurning)
            return;

        _turnElapsed += deltaTime;

        float t = MathHelper.Clamp(
            _turnElapsed / TurnDuration, 0f, 1f);

        float smoothT = t * t * (3f - 2f * t);

        float yawDifference = ShortestAngleDifference(
            _turnStartYaw, _turnTargetYaw);

        _yaw = _turnStartYaw + yawDifference * smoothT;

        _pitch = MathHelper.Lerp(
            _turnStartPitch, _turnTargetPitch, smoothT);

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
            Console.WriteLine("No destination selected.");
            return;
        }

        Vector3 destination = new Vector3(
            (float)star.X,
            (float)star.Y,
            (float)star.Z);

        _travelStart = _cameraPosition;

        Vector3 direction = destination - _travelStart;
        float distance = direction.Length;

        if (distance < 0.001f)
        {
            Console.WriteLine("Already at destination.");
            return;
        }

        float stoppingDistance = MathF.Min(
            2f, distance * 0.1f);

        _travelEnd = destination -
            Vector3.Normalize(direction) * stoppingDistance;

        _travelElapsed = 0f;
        _lastProgress = -1;
        _destinationName = star.Name;
        _isTravelling = true;

        StartTurningToward(destination);

        Console.WriteLine();
        Console.WriteLine($"Travelling to {star.Name}...");
        Console.WriteLine(
            $"Starting position: X={_travelStart.X:F2}, " +
            $"Y={_travelStart.Y:F2}, Z={_travelStart.Z:F2}");

        Console.WriteLine(
            $"Distance: {distance:F2} parsecs " +
            $"({distance * 3.26156:F2} light-years)");

        Console.WriteLine(
            $"Travel duration: {TravelDuration} seconds");
    }

    private void UpdateTravel(float deltaTime)
    {
        if (!_isTravelling)
            return;

        _travelElapsed += deltaTime;

        float t = MathHelper.Clamp(
            _travelElapsed / TravelDuration, 0f, 1f);

        float smoothT = t * t * (3f - 2f * t);

        _cameraPosition = Vector3.Lerp(
            _travelStart, _travelEnd, smoothT);

        int milestone = (int)(t * 100) / 25;

        if (milestone > _lastProgress)
        {
            _lastProgress = milestone;

            Console.WriteLine(
                $"Travel progress: {Math.Min(milestone * 25, 100)}%");
        }

        if (t >= 1f)
        {
            _isTravelling = false;
            Console.WriteLine(
                $"Arrived near {_destinationName}!");
            PrintCameraPosition();
        }
    }

    private void PrintCameraPosition()
    {
        Console.WriteLine(
            $"Camera: X={_cameraPosition.X:F2}, " +
            $"Y={_cameraPosition.Y:F2}, " +
            $"Z={_cameraPosition.Z:F2}");
    }

    private void BeginSearch()
    {
        if (_isTravelling)
            return;

        _isSearching = true;
        _searchText = "";
        _searchMessage = "";

        Console.WriteLine();
        Console.WriteLine("=== STAR SEARCH ===");
        Console.WriteLine("Type a star name.");
        Console.WriteLine("ENTER: Search");
        Console.WriteLine("BACKSPACE: Delete character");
        Console.WriteLine("ESC: Cancel search");

        UpdateNavigationDisplay();
    }

    private void FinishSearch()
    {
        string query = _searchText.Trim();

        if (query.Length == 0)
        {
            _searchMessage = "Please type a star name";
            UpdateNavigationDisplay();
            return;
        }

        bool found = _navigation?.SearchStar(query) ?? false;

        if (found)
        {
            _isSearching = false;
            _searchMessage = "";

            FaceSelectedStar();

            Console.WriteLine(
                "Press ENTER again to travel to this star.");
        }
        else
        {
            _searchMessage = $"No match: {query}";
        }

        UpdateNavigationDisplay();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (!_isSearching)
            return;

        string input = e.AsString;

        foreach (char character in input)
        {
            if (char.IsControl(character))
                continue;

            if (_searchText.Length >= 60)
                break;

            _searchText += character;
        }

        _searchMessage = "";
        UpdateNavigationDisplay();
    }

    private void UpdateNavigationDisplay()
    {
        if (_isSearching)
        {
            Title = $"Star Search | {_searchText}_";

            if (!string.IsNullOrWhiteSpace(_searchMessage))
                Title += $" | {_searchMessage}";

            return;
        }

        Star? star = _navigation?.SelectedStar;

        if (star == null)
        {
            Title = "3D Star Chart";
            return;
        }

        double dx = star.X - _cameraPosition.X;
        double dy = star.Y - _cameraPosition.Y;
        double dz = star.Z - _cameraPosition.Z;

        double distancePc = Math.Sqrt(
            dx * dx + dy * dy + dz * dz);

        double distanceLy = distancePc * 3.26156;

        double bearing = Math.Atan2(dx, dy)
            * 180.0 / Math.PI;

        bearing = (bearing + 360.0) % 360.0;

        double horizontal = Math.Sqrt(
            dx * dx + dy * dy);

        double path = Math.Atan2(
            dz, horizontal) * 180.0 / Math.PI;

        Title = $"Star Chart | {star.Name} | " +
                $"{distanceLy:F2} ly | " +
                $"Bearing: {bearing:F1}° | " +
                $"Path: {path:F1}°";
    }
private void CancelJourney()
{
    if (!_isTravelling && !_isTurning)
    {
        Console.WriteLine("No active journey to cancel.");
        return;
    }

    _isTravelling = false;
    _isTurning = false;

    Console.WriteLine("Journey cancelled.");
    Console.WriteLine("Camera stopped at current position.");

    PrintCameraPosition();
    UpdateNavigationDisplay();
}
    private void ReturnHome()
    {
        _isTravelling = false;
        _isTurning = false;
        _isRotating = false;

        _cameraPosition = Vector3.Zero;
        _yaw = -90f;
        _pitch = 0f;

        Console.WriteLine("Returned to starting position.");
        PrintCameraPosition();

        UpdateNavigationDisplay();
    }

    private void ChangeCameraSpeed(float multiplier)
    {
        _cameraSpeed = MathHelper.Clamp(
            _cameraSpeed * multiplier,
            MinCameraSpeed,
            MaxCameraSpeed);

        Console.WriteLine(
            $"Camera speed: {_cameraSpeed:F2} pc/s");
    }

    protected override void OnUpdateFrame(
        FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // Search mode takes priority over all other controls.
        if (_isSearching)
        {
            if (KeyboardState.IsKeyPressed(Keys.Escape))
            {
                _isSearching = false;
                _searchText = "";
                _searchMessage = "";
                UpdateNavigationDisplay();
            }
            else if (KeyboardState.IsKeyPressed(Keys.Backspace))
            {
                if (_searchText.Length > 0)
                {
                    _searchText =
                        _searchText[..^1];
                }

                _searchMessage = "";
                UpdateNavigationDisplay();
            }
            else if (KeyboardState.IsKeyPressed(Keys.Enter)
                || KeyboardState.IsKeyPressed(Keys.KeyPadEnter))
            {
                FinishSearch();
            }

            return;
        }

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        float deltaTime = (float)args.Time;
// Press X to cancel a journey at any time.
if (KeyboardState.IsKeyPressed(Keys.X))
{
    CancelJourney();
    return;
}
        if (!_isTravelling &&
            KeyboardState.IsKeyPressed(Keys.Slash))
        {
            BeginSearch();
            return;
        }

        if (KeyboardState.IsKeyPressed(Keys.F))
            _renderer?.ToggleNamedStars();

        if (KeyboardState.IsKeyPressed(Keys.R))
            ReturnHome();

        if (KeyboardState.IsKeyPressed(Keys.C))
            PrintCameraPosition();

        if (KeyboardState.IsKeyPressed(Keys.Equal)
            || KeyboardState.IsKeyPressed(Keys.KeyPadAdd))
        {
            ChangeCameraSpeed(1.5f);
        }

        if (KeyboardState.IsKeyPressed(Keys.Minus)
            || KeyboardState.IsKeyPressed(Keys.KeyPadSubtract))
        {
            ChangeCameraSpeed(1f / 1.5f);
        }

        if (!_isTravelling &&
            KeyboardState.IsKeyPressed(Keys.N))
        {
            _navigation?.SelectNext();
            FaceSelectedStar();
        }

        if (!_isTravelling &&
            KeyboardState.IsKeyPressed(Keys.P))
        {
            _navigation?.SelectPrevious();
            FaceSelectedStar();
        }

        if (!_isTravelling &&
            KeyboardState.IsKeyPressed(Keys.Enter))
        {
            StartTravel();
        }

        UpdateTurning(deltaTime);
        UpdateTravel(deltaTime);

        if (_isTravelling || _isTurning)
            return;

        float movement = _cameraSpeed * deltaTime;
        float rotation = RotationSpeed * deltaTime;

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
            Vector3.Cross(forward, Vector3.UnitY));

        if (KeyboardState.IsKeyDown(Keys.W))
            _cameraPosition += forward * movement;

        if (KeyboardState.IsKeyDown(Keys.S))
            _cameraPosition -= forward * movement;

        if (KeyboardState.IsKeyDown(Keys.A))
            _cameraPosition -= right * movement;

        if (KeyboardState.IsKeyDown(Keys.D))
            _cameraPosition += right * movement;

        // SHIFT = Move upward
if (KeyboardState.IsKeyDown(Keys.LeftShift)
    || KeyboardState.IsKeyDown(Keys.RightShift))
{
    _cameraPosition.Y += movement;
}

// SPACE = Move downward
if (KeyboardState.IsKeyDown(Keys.Space))
{
    _cameraPosition.Y -= movement;
}
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isTravelling || _isTurning || _isSearching)
            return;

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

        _yaw += delta.X * MouseSensitivity;
        _pitch -= delta.Y * MouseSensitivity;

        _pitch = MathHelper.Clamp(_pitch, -89f, 89f);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (_isTravelling || _isTurning || _isSearching)
            return;

        float scrollAmount = MathHelper.Clamp(
            e.OffsetY, -1f, 1f);

        _cameraPosition += GetCameraForward()
            * scrollAmount * ZoomSensitivity;
    }

    protected override void OnRenderFrame(
        FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        float aspect = Size.X /
            (float)Math.Max(1, Size.Y);

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

        Matrix4 viewProjection = view * projection;

        _renderer?.Draw(viewProjection);

        _destinationRenderer?.Draw(
            _navigation?.SelectedStar,
            viewProjection);

        if (_navigation != null && _renderer != null)
        {
            var starsToLabel = _renderer.ShowNamedOnly
                ? _renderer.NamedStars
                : _navigation.Destinations;

            _labelRenderer?.Draw(
                starsToLabel,
                viewProjection,
                _cameraPosition,
                Size.X,
                Size.Y);
        }

        UpdateNavigationDisplay();

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
