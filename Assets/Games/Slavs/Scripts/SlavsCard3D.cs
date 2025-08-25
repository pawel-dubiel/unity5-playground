using System.Collections;
using UnityEngine;

// Renders a 3D "card" (a simple quad) to a RenderTexture that UI can display.
public class SlavsCard3D : MonoBehaviour
{
    public RenderTexture Texture { get; private set; }

    private Camera _cam;
    private GameObject _card;
    private Material _mat;
    private bool _flipping;

    private void Awake()
    {
        Texture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
        {
            name = "SlavsCardRT"
        };

        var camGo = new GameObject("SlavsCardCamera");
        camGo.transform.SetParent(transform, false);
        _cam = camGo.AddComponent<Camera>();
        _cam.orthographic = true;
        _cam.orthographicSize = 1.2f;
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
        _cam.targetTexture = Texture;

        _card = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _card.name = "CardQuad";
        _card.transform.SetParent(transform, false);
        _card.transform.localPosition = new Vector3(0f, 0f, 2f);
        _card.transform.localScale = new Vector3(1.6f, 1.0f, 1f);
        var mr = _card.GetComponent<MeshRenderer>();
        _mat = new Material(Shader.Find("Unlit/Color"));
        _mat.color = new Color(0.5f, 0.4f, 0.3f, 1f);
        mr.sharedMaterial = _mat;
    }

    public void SetTint(Color c)
    {
        if (_mat != null) _mat.color = c;
    }

    public void Flip()
    {
        if (!_flipping) StartCoroutine(FlipRoutine());
    }

    private IEnumerator FlipRoutine()
    {
        _flipping = true;
        var t = 0f;
        var dur = 0.35f;
        var start = _card.transform.localEulerAngles;
        var end = start + new Vector3(0f, 180f, 0f);
        while (t < dur)
        {
            t += Time.deltaTime;
            var k = Mathf.SmoothStep(0f, 1f, t / dur);
            _card.transform.localEulerAngles = Vector3.Lerp(start, end, k);
            yield return null;
        }
        _card.transform.localEulerAngles = end;
        _flipping = false;
    }
}

