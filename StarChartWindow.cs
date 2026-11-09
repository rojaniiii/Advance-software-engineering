
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
    private DustRenderer? _dustRenderer;

    private readonly CameraController _camera = new();
    private readonly StarPicker _starPicker = new();

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
        _dustRenderer = new DustRenderer();

        Console.WriteLine("=== STAR CHART CONTROLS ===");
        Console.WriteLine("LEFT CLICK: Select star or name");
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

    private Matrix4 GetViewProjection() =>
        _camera.GetViewProjection(Size.X, Size.Y);

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
        Vector3 direction = destination - _camera.Position;

        if (direction.LengthSquared < 0.000001f)
            return;

        direction = Vector3.Normalize(direction);

        _turnStartYaw = _camera.Yaw;
        _turnStartPitch = _camera.Pitch;

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
            _camera.Position.X,
            _camera.Position.Y,
            _camera.Position.Z);

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

        _camera.Yaw = _turnStartYaw + yawDifference * smoothT;
        _camera.Pitch = MathHelper.Lerp(
            _turnStartPitch, _turnTargetPitch, smoothT);

        if (t >= 1f)
        {
            _isTurning = false;
            _camera.Yaw = _turnTargetYaw;
            _camera.Pitch = _turnTargetPitch;
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

        _travelStart = _camera.Position;

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

        _camera.Position = Vector3.Lerp(
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
        _camera.Reset();

        Console.WriteLine("Returned home.");
        UpdateNavigationDisplay();
    }

    private void PrintCameraPosition()
    {
        Console.WriteLine(
            $"Camera: X={_camera.Position.X:F2}, " +
            $"Y={_camera.Position.Y:F2}, " +
            $"Z={_camera.Position.Z:F2}");
    }

    private void ChangeCameraSpeed(float multiplier)
    {
        _camera.ChangeSpeed(multiplier);
        Console.WriteLine(
            $"Camera speed: {_camera.Speed:F2} pc/s");
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
            Console.WriteLine("Press ENTER again to travel.");
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

        double dx = star.X - _camera.Position.X;
        double dy = star.Y - _camera.Position.Y;
        double dz = star.Z - _camera.Position.Z;

        double distance = Math.Sqrt(
            dx * dx + dy * dy + dz * dz);

        double bearing =
            (Math.Atan2(dx, -dz) * 180 / Math.PI + 360) % 360;

        double path = Math.Atan2(
            dy, Math.Sqrt(dx * dx + dz * dz))
            * 180 / Math.PI;

        Title = $"Star Chart | {star.Name} | " +
                $"{distance * 3.26156:F2} ly | " +
                $"Bearing: {bearing:F1}° | " +
                $"Path: {path:F1}°";
    }

    private void SelectStarAt(Vector2 mousePosition)
    {
        if (_navigation == null || _renderer == null)
            return;

        var candidates = _renderer.ShowNamedOnly
            ? _renderer.NamedStars
            : _navigation.Destinations;

        Star? selected =
            _labelRenderer?.PickLabel(mousePosition);

        selected ??= _starPicker.PickPoint(
            candidates,
            mousePosition,
            GetViewProjection(),
            Size.X,
            Size.Y);

        if (selected == null)
        {
            Console.WriteLine(
                "No selectable named star at this position.");
            return;
        }

        _navigation.SelectStar(selected);

        Console.WriteLine(
            $"Mouse selected: {selected.Name}");

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

        
        // Animate dust only while travelling.
if (_isTravelling)
{
    _dustRenderer?.Update(dt);
}

        if (_isTravelling || _isTurning)
            return;

        _camera.UpdateKeyboard(KeyboardState, dt);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isTravelling || _isTurning || _isSearching)
            return;

        _camera.UpdateMouse(
            e.Position,
            MouseState.IsButtonDown(MouseButton.Right));
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (_isTravelling || _isTurning || _isSearching)
            return;

        _camera.Zoom(e.OffsetY);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        Matrix4 matrix = GetViewProjection();

        _renderer?.Draw(matrix);

        
       // Show dust only during a journey.
if (_isTravelling)
{
    _dustRenderer?.Draw(matrix, _camera.Position);
}

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
                _camera.Position,
                Size.X,
                Size.Y);
        }

        UpdateNavigationDisplay();
        SwapBuffers();
    }

    protected override void OnUnload()
    {
        _dustRenderer?.Dispose();
        _destinationRenderer?.Dispose();
        _labelRenderer?.Dispose();
        _renderer?.Dispose();

        base.OnUnload();
    }
}
