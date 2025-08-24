using UnityEngine;

// A single camera-scaled outline for the selected hex. Very cheap even on huge maps.
[RequireComponent(typeof(LineRenderer))]
public class HexSelectionOverlay : MonoBehaviour
{
    public float hexSize = 0.8f;
    public Color color = Color.yellow;
    public float widthPixels = 3f;

    private LineRenderer _lr;
    private Camera _cam;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = false;
        _lr.loop = true;
#if UNITY_2022_1_OR_NEWER
        _lr.numCornerVertices = 0;
        _lr.numCapVertices = 0;
#endif
        _lr.positionCount = 6;
        var mat = CreateUnlit(color);
        _lr.material = mat;
        _lr.enabled = false;
        BuildGeometry();
    }

    public void Rebuild()
    {
        BuildGeometry();
    }

    private void BuildGeometry()
    {
        if (_lr == null) return;
        for (int i = 0; i < 6; i++)
        {
            float ang = (60f * i) * Mathf.Deg2Rad; // flat-top
            _lr.SetPosition(i, new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * hexSize);
        }
    }

    public void Bind(Camera cam)
    {
        _cam = cam;
    }

    public void SetVisible(bool on)
    {
        _lr.enabled = on;
    }

    public void SetPosition(Vector3 worldPosition)
    {
        transform.position = new Vector3(worldPosition.x, worldPosition.y, -0.002f);
    }

    private void LateUpdate()
    {
        if (_cam == null || !_lr.enabled) return;
        float pixelsPerWorld = Screen.height / (_cam.orthographicSize * 2f);
        float worldWidth = Mathf.Max(widthPixels / Mathf.Max(1f, pixelsPerWorld), 0.01f);
        _lr.startWidth = _lr.endWidth = worldWidth;
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
}
