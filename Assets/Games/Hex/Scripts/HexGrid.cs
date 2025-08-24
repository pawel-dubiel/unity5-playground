using UnityEngine;

// Generates a simple hex grid using axial coordinates and flat-top layout.
public class HexGrid : MonoBehaviour
{
    [Tooltip("Distance from center to any vertex (world units)")]
    public float hexSize = 0.8f;

    public void GenerateHexagon(int radius)
    {
        // Hexagon-shaped map using axial coordinates within a given radius
        for (int q = -radius; q <= radius; q++)
        {
            int r1 = Mathf.Max(-radius, -q - radius);
            int r2 = Mathf.Min(radius, -q + radius);
            for (int r = r1; r <= r2; r++)
            {
                SpawnHex(q, r);
            }
        }
    }

    public void GenerateRectangle(int width, int height)
    {
        // Flat-top even-r offset to axial
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                int q = col - (row >> 1); // even-r offset to axial q
                int r = row;               // axial r
                SpawnHex(q, r);
            }
        }
    }

    private void SpawnHex(int q, int r)
    {
        var go = new GameObject($"Hex_{q}_{r}");
        go.transform.SetParent(transform, false);
        var tile = go.AddComponent<HexTile>();
        tile.size = hexSize;

        // Simple alternating color for readability
        var baseColor = new Color(0.18f, 0.6f, 0.75f);
        var altColor = new Color(0.15f, 0.5f, 0.65f);
        tile.color = ((q + r) & 1) == 0 ? baseColor : altColor;

        go.transform.position = HexMath.AxialToWorld(q, r, hexSize);
    }
}
