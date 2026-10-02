using System.Numerics;

namespace Ocelot.Extensions;

public static class Vector3Extensions
{
    public static Vector2 Truncate(this Vector3 vector)
    {
        return new Vector2(vector.X, vector.Z);
    }

    public static float Distance(this Vector3 vector, Vector3 other)
    {
        return Vector3.Distance(vector, other);
    }

    public static float Distance2D(this Vector3 vector, Vector2 other)
    {
        return Vector2.Distance(vector.Truncate(), other);
    }

    public static float Distance2D(this Vector3 vector, Vector3 other)
    {
        return Vector2.Distance(vector.Truncate(), other.Truncate());
    }

    public static Vector3 GetApproachPosition(this Vector3 to, Vector3 from, float range = 3f, float angularJitter = 0f)
    {
        // "Already there" stays a true 3D test — being directly under a tower is not arriving.
        if (from.Distance(to) <= range)
        {
            return from;
        }

        var direction = new Vector3(to.X - from.X, 0f, to.Z - from.Z);
        var horizontal = direction.Length();
        if (horizontal < 0.0001f)
        {
            return to;
        }

        direction /= horizontal;

        if (angularJitter > 0f)
        {
            var angleDeg = Random.Shared.NextSingle() * 2f * angularJitter - angularJitter;
            var angleRad = MathF.PI / 180f * angleDeg;

            var cos = MathF.Cos(angleRad);
            var sin = MathF.Sin(angleRad);

            direction = new Vector3(
                direction.X * cos - direction.Z * sin,
                0f,
                direction.X * sin + direction.Z * cos
            );
        }

        return to - direction * range;
    }
}
