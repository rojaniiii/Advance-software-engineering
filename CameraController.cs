
using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace StarChart;

public sealed class CameraController
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public float Yaw { get; set; } = -90f;
    public float Pitch { get; set; } = 0f;

    public float Speed { get; private set; } = 35f;

    private const float RotationSpeed = 160f;
    private const float MouseSensitivity = 2.5f;
    private const float ZoomSensitivity = 12f;

    private Vector2 _lastMousePosition;
    private bool _rotating;

    public Vector3 Forward
    {
        get
        {
            float yaw = MathHelper.DegreesToRadians(Yaw);
            float pitch = MathHelper.DegreesToRadians(Pitch);

            return Vector3.Normalize(new Vector3(
                MathF.Cos(yaw) * MathF.Cos(pitch),
                MathF.Sin(pitch),
                MathF.Sin(yaw) * MathF.Cos(pitch)));
        }
    }

    public Matrix4 GetViewProjection(int width, int height)
    {
        float aspect = width / (float)Math.Max(1, height);

        Matrix4 view = Matrix4.LookAt(
            Position,
            Position + Forward,
            Vector3.UnitY);

        Matrix4 projection =
            Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(75f),
                aspect,
                0.01f,
                100000f);

        return view * projection;
    }

    public void UpdateKeyboard(
        KeyboardState keyboard,
        float deltaTime)
    {
        float rotation = RotationSpeed * deltaTime;

        if (keyboard.IsKeyDown(Keys.Left))
            Yaw -= rotation;

        if (keyboard.IsKeyDown(Keys.Right))
            Yaw += rotation;

        if (keyboard.IsKeyDown(Keys.Up))
            Pitch += rotation;

        if (keyboard.IsKeyDown(Keys.Down))
            Pitch -= rotation;

        Pitch = MathHelper.Clamp(Pitch, -89f, 89f);

        float movement = Speed * deltaTime;

        Vector3 forward = Forward;

        Vector3 right = Vector3.Normalize(
            Vector3.Cross(forward, Vector3.UnitY));

        if (keyboard.IsKeyDown(Keys.W))
            Position += forward * movement;

        if (keyboard.IsKeyDown(Keys.S))
            Position -= forward * movement;

        if (keyboard.IsKeyDown(Keys.A))
            Position -= right * movement;

        if (keyboard.IsKeyDown(Keys.D))
            Position += right * movement;

        if (keyboard.IsKeyDown(Keys.LeftShift) ||
            keyboard.IsKeyDown(Keys.RightShift))
            Position += Vector3.UnitY * movement;

        if (keyboard.IsKeyDown(Keys.Space))
            Position -= Vector3.UnitY * movement;
    }

    public void UpdateMouse(
        Vector2 position,
        bool rightButtonDown)
    {
        if (!rightButtonDown)
        {
            StopRotating();
            return;
        }

        if (!_rotating)
        {
            _lastMousePosition = position;
            _rotating = true;
            return;
        }

        Vector2 delta = position - _lastMousePosition;
        _lastMousePosition = position;

        Yaw += delta.X * MouseSensitivity;

        Pitch = MathHelper.Clamp(
            Pitch - delta.Y * MouseSensitivity,
            -89f,
            89f);
    }

    public void Zoom(float offsetY)
    {
        // Stronger trackpad zoom, without huge jumps.
        float scroll = MathHelper.Clamp(
            offsetY, -3f, 3f);

        Position += Forward * scroll * ZoomSensitivity;
    }

    public void ChangeSpeed(float multiplier)
    {
        Speed = MathHelper.Clamp(
            Speed * multiplier,
            5f,
            300f);
    }

    public void StopRotating()
    {
        _rotating = false;
    }

    public void Reset()
    {
        Position = Vector3.Zero;
        Yaw = -90f;
        Pitch = 0f;
        StopRotating();
    }
}
