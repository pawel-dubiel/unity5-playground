using System.Collections.Generic;
using UnityEngine;

// High-performance grid: combines many hexes into chunk meshes for fast rendering.
// No per-tile GameObjects or colliders; picking is math-based via HexMath.
public class HexGridChunked : MonoBehaviour
{
    [Header("Layout")] 
    public float hexSize = 0.8f;

    [Header("Generation")] public int width = 50; // rectangle, cols
    public int height = 30; // rectangle, rows
    public int chunkCols = 32;
    public int chunkRows = 32;

    [Header("Appearance")] public Color baseColor = new Color(0.18f, 0.6f, 0.75f);
    public Color altColor = new Color(0.15f, 0.5f, 0.65f);

    private readonly List<GameObject> _chunks = new List<GameObject>();
    private Material _material;

    public void Build()
    {
        Clear();
        _material = CreateUnlitWhite(); // vertex colors drive tint

        for (int row0 = 0; row0 < height; row0 += chunkRows)
        {
            for (int col0 = 0; col0 < width; col0 += chunkCols)
            {
                int rows = Mathf.Min(chunkRows, height - row0);
                int cols = Mathf.Min(chunkCols, width - col0);
                var go = new GameObject($"Chunk_{col0}_{row0}");
                go.transform.SetParent(transform, false);
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = _material;
                mf.sharedMesh = BuildChunkMesh(col0, row0, cols, rows);
                _chunks.Add(go);
            }
        }
    }

    public Vector3 AxialToWorld(int q, int r)
    {
        return HexMath.AxialToWorld(q, r, hexSize);
    }

    public Vector3 OffsetToWorld(int col, int row)
    {
        int q = col - (row >> 1);
        int r = row;
        return AxialToWorld(q, r);
    }

    private Mesh BuildChunkMesh(int col0, int row0, int cols, int rows)
    {
        var verts = new List<Vector3>(cols * rows * 7);
        var colors = new List<Color>(cols * rows * 7);
        var tris = new List<int>(cols * rows * 18);

        for (int r = 0; r < rows; r++)
        {
            int row = row0 + r;
            for (int c = 0; c < cols; c++)
            {
                int col = col0 + c;
                int q = col - (row >> 1);
                int a = q;
                int b = row; // axial r
                Vector3 center = HexMath.AxialToWorld(a, b, hexSize);

                int vi = verts.Count;
                verts.Add(center); // center
                colors.Add(((q + b) & 1) == 0 ? baseColor : altColor);
                for (int i = 0; i < 6; i++)
                {
                    float ang = (60f * i) * Mathf.Deg2Rad; // flat-top
                    verts.Add(center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * hexSize);
                    colors.Add(((q + b) & 1) == 0 ? baseColor : altColor);
                }
                for (int i = 0; i < 6; i++)
                {
                    int a0 = vi + 0;
                    int b0 = vi + (i == 5 ? 1 : i + 2);
                    int c0 = vi + (i + 1);
                    tris.Add(a0); tris.Add(b0); tris.Add(c0); // clockwise
                }
            }
        }

        var m = new Mesh();
        m.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        m.SetVertices(verts);
        m.SetColors(colors);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        m.RecalculateNormals();
        return m;
    }

    public void Clear()
    {
        for (int i = 0; i < _chunks.Count; i++)
        {
            if (_chunks[i] != null) DestroyImmediate(_chunks[i]);
        }
        _chunks.Clear();
    }

    private static Material CreateUnlitWhite()
    {
        // Use Sprites/Default to honor vertex colors in URP and built-in pipelines.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        return mat;
    }
}
