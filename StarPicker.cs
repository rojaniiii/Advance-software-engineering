
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace StarChart;

// Converts star positions to screen coordinates.
public sealed class StarPicker
{
    private const float ClickRadius = 24f;

    public Star? PickPoint(
        IEnumerable<Star> stars,
        Vector2 pointer,
        Matrix4 viewProjection,
        int width,
        int height)
    {
        Star? closest = null;
        float bestDistance = ClickRadius;

        foreach (Star star in stars)
        {
            if (!TryProject(
                star, viewProjection, width, height,
                out Vector2 point))
                continue;

            float distance = (point - pointer).Length;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = star;
            }
        }

        return closest;
    }

    private static bool TryProject(
        Star star,
        Matrix4 matrix,
        int width,
        int height,
        out Vector2 point)
    {
        point = Vector2.Zero;

        if (width <= 0 || height <= 0)
            return false;

        Vector4 clip = Vector4.TransformRow(
            new Vector4(
                (float)star.X,
                (float)star.Y,
                (float)star.Z,
                1f),
            matrix);

        if (clip.W <= 0.0001f)
            return false;

        float x = clip.X / clip.W;
        float y = clip.Y / clip.W;
        float z = clip.Z / clip.W;

        if (x < -1f || x > 1f ||
            y < -1f || y > 1f ||
            z < -1f || z > 1f)
            return false;

        point = new Vector2(
            (x + 1f) * width * 0.5f,
            (1f - y) * height * 0.5f);

        return true;
    }
}
