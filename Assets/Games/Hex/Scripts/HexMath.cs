using UnityEngine;

public static class HexMath
{
    // Flat-top axial -> world
    public static Vector3 AxialToWorld(int q, int r, float size)
    {
        float x = size * (1.5f * q);
        float y = size * (Mathf.Sqrt(3f) * (r + q * 0.5f));
        return new Vector3(x, y, 0f);
    }

    // Flat-top world -> axial (fractional)
    public static Vector2 WorldToAxial(Vector3 world, float size)
    {
        float q = (2f / 3f) * (world.x / size);
        float r = (-1f / 3f) * (world.x / size) + (1f / Mathf.Sqrt(3f)) * (world.y / size);
        return new Vector2(q, r);
    }

    public static Vector2Int AxialRound(float q, float r)
    {
        // Convert to cube coords, round, then convert back
        float x = q;
        float z = r;
        float y = -x - z;

        int rx = Mathf.RoundToInt(x);
        int ry = Mathf.RoundToInt(y);
        int rz = Mathf.RoundToInt(z);

        float x_diff = Mathf.Abs(rx - x);
        float y_diff = Mathf.Abs(ry - y);
        float z_diff = Mathf.Abs(rz - z);

        if (x_diff > y_diff && x_diff > z_diff)
        {
            rx = -ry - rz;
        }
        else if (y_diff > z_diff)
        {
            ry = -rx - rz;
        }
        else
        {
            rz = -rx - ry;
        }

        return new Vector2Int(rx, rz);
    }

    public static Vector2Int WorldToAxialRounded(Vector3 world, float size)
    {
        var fr = WorldToAxial(world, size);
        return AxialRound(fr.x, fr.y);
    }
}
