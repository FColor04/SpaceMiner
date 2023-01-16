using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SpaceMiner.Utils;

public static class CommonExtensions
{
    public static Vector2 GetNormalized(this Vector2 vector)
    {
        var copy = vector;
        if(copy.X != 0 && copy.Y != 0)
            copy.Normalize();
        return copy;
    }

    public static bool TryGetFirst<T>(this IEnumerable<T> enumeration, Func<T, bool> func, out T first)
    {
        first = default;
        if (enumeration.Any(func))
        {
            first = enumeration.First(func);
            return true;
        }
        return false;
    }

    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}