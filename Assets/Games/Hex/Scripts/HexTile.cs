using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class HexTile : MonoBehaviour
{
    public float size = 0.8f; // center to vertex
    public Color color = new Color(0.2f, 0.7f, 0.8f);
    [SerializeField] private bool _drawBorder = true;
    [SerializeField] private Color _borderColor = new Color(0f, 0f, 0f, 0.9f);
    [SerializeField] private float _borderWidthPixels = 2.0f; // constant on screen
    [SerializeField] private float _highlightWidthPixels = 3.0f; // constant on screen
    [SerializeField] public Color highlightColor = new Color(1f, 0.92f, 0.25f);

    private Material _material;
    private Color _baseColor;
    private LineRenderer _border;
    private float _currentBorderWidthPixels;

    private void Awake()
    {
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        mf.sharedMesh = BuildHexMesh(size);
        _material = CreateUnlit(color);
        mr.sharedMaterial = _material;
        _baseColor = color;

        var mc = GetComponent<MeshCollider>();
        mc.sharedMesh = mf.sharedMesh;

        if (_drawBorder)
        {
            CreateBorder();
        }
    }

    private static Mesh BuildHexMesh(float size)
    {
        // Build a filled hexagon mesh centered at origin
        var m = new Mesh();

        Vector3[] verts = new Vector3[7]; // center + 6 corners
        verts[0] = Vector3.zero;
        for (int i = 0; i < 6; i++)
        {
            // Flat-top hex corners
            float angleDeg = 60f * i;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            verts[i + 1] = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * size;
        }

        int[] tris = new int[6 * 3];
        for (int i = 0; i < 6; i++)
        {
            int a = 0;
            int b = i + 1;
            int c = i == 5 ? 1 : i + 2;
            int t = i * 3;
            // Use clockwise winding (a, c, b) so faces render toward the camera
            tris[t + 0] = a;
            tris[t + 1] = c;
            tris[t + 2] = b;
        }

        m.vertices = verts;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private static Material CreateUnlit(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        return mat;
    }

    private void CreateBorder()
    {
        var border = new GameObject("Border");
        border.transform.SetParent(transform, false);
        var lr = border.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.alignment = LineAlignment.View;
#if UNITY_2022_1_OR_NEWER
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
#endif
        lr.startWidth = lr.endWidth = 0.01f; // will be set per-camera every frame
        lr.positionCount = 6;
        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), -0.001f) * size);
        }
        // Simple unlit material for the line
        lr.material = CreateUnlit(_borderColor);
        // For URP Unlit, width is in world units; already scaled by size above
        _border = lr;
        _currentBorderWidthPixels = _borderWidthPixels;
    }

    public void SetHighlighted(bool on)
    {
        if (_material != null)
        {
            var target = on ? highlightColor : _baseColor;
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", target);
            else if (_material.HasProperty("_Color")) _material.SetColor("_Color", target);
        }
        if (_border != null)
        {
            var mat = _border.material;
            var target = on ? Color.white : _borderColor;
            if (mat != null)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", target);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", target);
            }
            _currentBorderWidthPixels = on ? _highlightWidthPixels : _borderWidthPixels;
        }
    }

    public void ConfigureBorderPixels(float normalPx, float highlightPx)
    {
        _borderWidthPixels = normalPx;
        _highlightWidthPixels = highlightPx;
        // Do not change current highlight state; next UpdateBorderForCamera will apply
    }

    public void UpdateBorderForCamera(Camera cam)
    {
        if (_border == null || cam == null) return;
        // Keep a roughly constant pixel width when zooming with an orthographic camera
        float pixelsPerWorldUnit = Screen.height / (cam.orthographicSize * 2f);
        float worldWidth = Mathf.Max(_currentBorderWidthPixels / Mathf.Max(1f, pixelsPerWorldUnit), 0.01f);
        _border.startWidth = _border.endWidth = worldWidth;
    }
}
