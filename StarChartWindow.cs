
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
    private StarNavigation? _navigation;
    private DestinationRenderer? _destinationRenderer;

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

    private bool _isTravelling;
    private Vector3 _travelStart;
    private Vector3 _travelEnd;
    private float _travelElapsed;

    private const float TravelDuration = 6f;
    private int _lastProgress = -1;
    private string _destinationName = "";

    private bool _isTurning;
    private float _turnStartYaw;
    private float _turnStartPitch;
    private float _turnTargetYaw;
    private float _turnTargetPitch;
    private float _turnElapsed;

    private const float TurnDuration = 1.2f;

    private bool _isSearching;
    private string _searchText = "";
    private string _searchMessage = "";

    public StarChartWindow(StarCollection starCollection)
        : base(
            GameWindowSettings.Default,
            new NativeWindowSettings
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
        _navigation = new StarNavigation(_starCollection);
        _destinationRenderer = new DestinationRenderer();

        Console.WriteLine();
        Console.WriteLine("=== STAR CHART CONTROLS ===");
        Console.WriteLine("LEFT CLICK: Select named star and travel");
        Console.WriteLine("N / P: Next / previous destination");
        Console.WriteLine("/: Search for star");
        Console.WriteLine("ENTER: Travel to selected star");
        Console.WriteLine("X: Cancel journey");
        Console.WriteLine("R: Return home");
        Console.WriteLine("F: Toggle named stars");
        Console.WriteLine("WASD: Move camera");
        Console.WriteLine("SHIFT: Move up");
        Console.WriteLine("SPACE: Move down");
        Console.WriteLine("Arrow keys: Rotate");
        Console.WriteLine("Right drag: Rotate");
        Console.WriteLine("Trackpad: Zoom");
        Console.WriteLine("+ / -: Change speed");
        Console.WriteLine("C: Camera coordinates");
        Console.WriteLine("ESC: Exit or cancel search");

        UpdateNavigationDisplay();
    }

    private Vector3 GetCameraForward()
    {
        float yaw = MathHelper.DegreesToRadians(_yaw);
        float pitch = MathHelper.DegreesToRadians(_pitch);

        return Vector3.Normalize(new Vector3(
            MathF.Cos(yaw) * MathF.Cos(pitch),
            MathF.Sin(pitch),
            MathF.Sin(yaw) * MathF.Cos(pitch)));
    }

    private Matrix4 GetViewProjection()
    {
        float aspect = Size.X / (float)Math.Max(1, Size.Y);

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

        return view * projection;
    }

    private static float ShortestAngleDifference(
        float start, float target)
    {
        float difference = (target - start) % 360f;

        if (difference > 180f) difference -= 360f;
        if (difference < -180f) difference += 360f;

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
        if (star == null) return;

        Vector3 destination = new Vector3(
            (float)star.X,
            (float)star.Y,
            (float)star.Z);

        StartTurningToward(destination);

        _navigation?.ShowSelection(
            _cameraPosition.X,
            _cameraPosition.Y,
            _cameraPosition.Z);

        Console.WriteLine($"Turning toward {star.Name}...");
        UpdateNavigationDisplay();
    }

    private void UpdateTurning(float deltaTime)
    {
        if (!_isTurning) return;

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

        float stoppingDistance =
            MathF.Min(2f, distance * 0.1f);

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
            $"Distance: {distance:F2} pc " +
            $"({distance * 3.26156:F2} light-years)");
    }

    private void UpdateTravel(float deltaTime)
    {
        if (!_isTravelling) return;

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

        Console.WriteLine("Returned home.");
        UpdateNavigationDisplay();
    }

    private void PrintCameraPosition()
    {
        Console.WriteLine(
            $"Camera: X={_cameraPosition.X:F2}, " +
            $"Y={_cameraPosition.Y:F2}, " +
            $"Z={_cameraPosition.Z:F2}");
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

    private void BeginSearch()
    {
        if (_isTravelling) return;

        _isSearching = true;
        _searchText = "";
        _searchMessage = "";

        Console.WriteLine(
            "Type a star name; ENTER to search; ESC to cancel.");

        UpdateNavigationDisplay();
    }

    private void FinishSearch()
    {
        string query = _searchText.Trim();

        if (query.Length == 0)
        {
            _searchMessage = "Please type a star name";
            return;
        }

        bool found = _navigation?.SearchStar(query) ?? false;

        if (found)
        {
            _isSearching = false;
            _searchMessage = "";

            FaceSelectedStar();

            Console.WriteLine(
                "Press ENTER again to travel.");
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

        if (!_isSearching) return;

        foreach (char character in e.AsString)
        {
            if (char.IsControl(character)) continue;
            if (_searchText.Length >= 60) break;

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

        double distance = Math.Sqrt(
            dx * dx + dy * dy + dz * dz);

        double bearing =
            (Math.Atan2(dx, dy) * 180 / Math.PI + 360) % 360;

        double path = Math.Atan2(
            dz, Math.Sqrt(dx * dx + dy * dy))
            * 180 / Math.PI;

        Title = $"Star Chart | {star.Name} | " +
                $"{distance * 3.26156:F2} ly | " +
                $"Bearing: {bearing:F1}° | " +
                $"Path: {path:F1}°";
    }

    // Convert a star's 3D position to its 2D window position.
    private bool TryProjectStar(
        Star star,
        Matrix4 viewProjection,
        out Vector2 screenPosition)
    {
        screenPosition = Vector2.Zero;

        Vector4 world = new Vector4(
            (float)star.X,
            (float)star.Y,
            (float)star.Z,
            1f);

        Vector4 clip = new Vector4(
    world.X * viewProjection.M11 +
    world.Y * viewProjection.M21 +
    world.Z * viewProjection.M31 +
    world.W * viewProjection.M41,

    world.X * viewProjection.M12 +
    world.Y * viewProjection.M22 +
    world.Z * viewProjection.M32 +
    world.W * viewProjection.M42,

    world.X * viewProjection.M13 +
    world.Y * viewProjection.M23 +
    world.Z * viewProjection.M33 +
    world.W * viewProjection.M43,

    world.X * viewProjection.M14 +
    world.Y * viewProjection.M24 +
    world.Z * viewProjection.M34 +
    world.W * viewProjection.M44
);

        if (clip.W <= 0.0001f)
            return false;

        float x = clip.X / clip.W;
        float y = clip.Y / clip.W;
        float z = clip.Z / clip.W;

        if (x < -1f || x > 1f ||
            y < -1f || y > 1f ||
            z < -1f || z > 1f)
            return false;

        screenPosition = new Vector2(
            (x + 1f) * 0.5f * Size.X,
            (1f - y) * 0.5f * Size.Y);

        return true;
    }

    // Detect which named star was clicked.
    private void SelectStarAt(Vector2 mousePosition)
    {
        if (_navigation == null || _renderer == null)
            return;

        Matrix4 matrix = GetViewProjection();

        Star? closest = null;
        float closestDistance = 18f;

        // In named-only mode, use the displayed 50 stars.
        // Otherwise, use navigation stars and Rojani.
        var candidates = _renderer.ShowNamedOnly
            ? _renderer.NamedStars
            : _navigation.Destinations
                .Concat(_starCollection.GetNamedStars()
                    .Where(s => s.Name == "Rojani"))
                .ToList();

        foreach (Star star in candidates)
        {
            if (!TryProjectStar(
                star, matrix, out Vector2 position))
                continue;

            float distance =
                (position - mousePosition).Length;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = star;
            }
        }

        if (closest == null)
        {
            Console.WriteLine(
                "No selectable named star at this position.");
            return;
        }

        _navigation.SelectStar(closest);

        Console.WriteLine(
            $"Mouse selected: {closest.Name}");

        StartTravel();
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButton.Left)
            return;

        if (_isSearching || _isTravelling || _isTurning)
            return;

        SelectStarAt(MouseState.Position);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (_isSearching)
        {
            if (KeyboardState.IsKeyPressed(Keys.Escape))
            {
                _isSearching = false;
                _searchText = "";
                _searchMessage = "";
            }
            else if (KeyboardState.IsKeyPressed(Keys.Backspace))
            {
                if (_searchText.Length > 0)
                    _searchText = _searchText[..^1];

                _searchMessage = "";
            }
            else if (
                KeyboardState.IsKeyPressed(Keys.Enter) ||
                KeyboardState.IsKeyPressed(Keys.KeyPadEnter))
            {
                FinishSearch();
            }

            UpdateNavigationDisplay();
            return;
        }

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        float dt = (float)args.Time;

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
        {
            ReturnHome();
            return;
        }

        if (KeyboardState.IsKeyPressed(Keys.C))
            PrintCameraPosition();

        if (KeyboardState.IsKeyPressed(Keys.Equal) ||
            KeyboardState.IsKeyPressed(Keys.KeyPadAdd))
            ChangeCameraSpeed(1.5f);

        if (KeyboardState.IsKeyPressed(Keys.Minus) ||
            KeyboardState.IsKeyPressed(Keys.KeyPadSubtract))
            ChangeCameraSpeed(1f / 1.5f);

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
            (KeyboardState.IsKeyPressed(Keys.Enter) ||
             KeyboardState.IsKeyPressed(Keys.KeyPadEnter)))
        {
            StartTravel();
        }

        UpdateTurning(dt);
        UpdateTravel(dt);

        if (_isTravelling || _isTurning)
            return;

        float movement = _cameraSpeed * dt;
        float rotation = RotationSpeed * dt;

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

        // SHIFT moves upward.
        if (KeyboardState.IsKeyDown(Keys.LeftShift) ||
            KeyboardState.IsKeyDown(Keys.RightShift))
            _cameraPosition.Y += movement;

        // SPACE moves downward.
        if (KeyboardState.IsKeyDown(Keys.Space))
            _cameraPosition.Y -= movement;
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

        float scroll = MathHelper.Clamp(
            e.OffsetY, -1f, 1f);

        _cameraPosition +=
            GetCameraForward() * scroll * ZoomSensitivity;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        Matrix4 matrix = GetViewProjection();

        _renderer?.Draw(matrix);

        _destinationRenderer?.Draw(
            _navigation?.SelectedStar, matrix);

        if (_navigation != null && _renderer != null)
        {
            var starsToLabel = _renderer.ShowNamedOnly
                ? _renderer.NamedStars
                : _navigation.Destinations;

            _labelRenderer?.Draw(
                starsToLabel,
                matrix,
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
