using System.Collections.Generic;
using UnityEngine;

public class VoronoiHelpers
{
    public static void DrawVoronoiCell(List<Vector2> points, int centerIndex)
    {
        var center = points[centerIndex];

        // Large starting polygon.
        List<Vector2> polygon =
        new List<Vector2>() {
            center + new Vector2(-10000, -10000),
            center + new Vector2(-10000, 10000),
            center + new Vector2(10000, 10000),
            center + new Vector2(10000, -10000),
        };

        for (int i = 0; i < points.Count; i++)
        {
            if (i == centerIndex)
                continue;

            Vector2 other = points[i];

            Vector2 midpoint = (center + other) * 0.5f;

            // Keep the half-plane closer to center.
            Vector2 normal = (center - other).normalized;

            polygon = ClipPolygon(polygon, midpoint, normal);

            if (polygon.Count == 0)
                return;
        }

        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];

            Gizmos.DrawLine(
                new Vector3(a.x, a.y, 0),
                new Vector3(b.x, b.y, 0));
        }
    }

    static List<Vector2> ClipPolygon(
        List<Vector2> poly,
        Vector2 planePoint,
        Vector2 planeNormal)
    {
        List<Vector2> result = new();

        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[(i + 1) % poly.Count];

            float da = Vector2.Dot(a - planePoint, planeNormal);
            float db = Vector2.Dot(b - planePoint, planeNormal);

            bool ina = da >= 0;
            bool inb = db >= 0;

            if (ina && inb)
            {
                result.Add(b);
            }
            else if (ina && !inb)
            {
                float t = da / (da - db);
                result.Add(Vector2.Lerp(a, b, t));
            }
            else if (!ina && inb)
            {
                float t = da / (da - db);
                result.Add(Vector2.Lerp(a, b, t));
                result.Add(b);
            }
        }

        return result;
    }
}
