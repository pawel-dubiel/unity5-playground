using UnityEngine;

// Sets up camera and spawns a very basic flat-top axial hex grid map.
public class HexGame : MonoBehaviour
{
    [Header("Camera")] public float cameraOrthoSize = 60f;
    [Header("Grid Shape")] public int hexRadius = 180; // hexagon radius in tiles
    public bool useRectangle = true;
    public int rectWidth = 360;
    public int rectHeight = 360;
    [Header("Hex Appearance")] public float hexSize = 0.8f; // distance from center to a vertex

    private Camera _cam;
    private Vector3 _camTarget;
    private float _zoomMin = 2f;
    private float _zoomMax = 60f;
    private float _panSpeed = 8f;
    private float _zoomSpeed = 20f;
    private float _rotationDeg = 0f;
    private float _rotationSpeed = 90f; // degrees per second
    private float _appliedRotationDeg = 0f; // what is currently applied to _gridRoot
    private HexTile _selected;
    private Transform _gridRoot;
    private HexTile[] _tiles;
    [Header("Performance")]
    public bool fastMode = true; // use chunked renderer and math picking
    private HexGridChunked _chunked;
    private HexSelectionOverlay _selectionOverlay;
    private bool _hasSelectedPivot;
    private Vector3 _selectedPivotWorld;
    [Header("Borders")]
    public float borderWidthPixels = 2f;
    public float borderHighlightPixels = 3f;

    private void Awake()
    {
        SetupCamera();

        if (fastMode)
        {
            var gridGo = new GameObject("HexGridChunked");
            _gridRoot = gridGo.transform;
            _chunked = gridGo.AddComponent<HexGridChunked>();
            _chunked.hexSize = hexSize;
            if (useRectangle)
            {
                _chunked.width = rectWidth;
                _chunked.height = rectHeight;
                _chunked.chunkCols = 64;
                _chunked.chunkRows = 64;
            }
            else
            {
                // Build a rectangle that bounds the radius hexagon for simplicity
                int d = hexRadius * 2 + 1;
                _chunked.width = d;
                _chunked.height = d;
                _chunked.chunkCols = 64;
                _chunked.chunkRows = 64;
            }
            _chunked.Build();

            // Selection overlay
            var selGo = new GameObject("SelectionOverlay");
            selGo.transform.SetParent(_gridRoot, false);
            _selectionOverlay = selGo.AddComponent<HexSelectionOverlay>();
            _selectionOverlay.hexSize = hexSize;
            _selectionOverlay.widthPixels = borderHighlightPixels;
            _selectionOverlay.Bind(_cam);
            _selectionOverlay.Rebuild();
        }
        else
        {
            var gridGo = new GameObject("HexGrid");
            _gridRoot = gridGo.transform;
            var grid = gridGo.AddComponent<HexGrid>();
            grid.hexSize = hexSize;
            if (useRectangle)
                grid.GenerateRectangle(rectWidth, rectHeight);
            else
                grid.GenerateHexagon(hexRadius);
        }

        _camTarget = _cam.transform.position;

        if (!fastMode)
        {
            _tiles = _gridRoot.GetComponentsInChildren<HexTile>();
            ApplyBorderPixelSettings();
        }
    }

    private void SetupCamera()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            var camGo = new GameObject("Main Camera");
            _cam = camGo.AddComponent<Camera>();
            _cam.tag = "MainCamera";
        }
        _cam.orthographic = true;
        _cam.orthographicSize = cameraOrthoSize;
        _cam.transform.position = new Vector3(0, 0, -10);
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
    }

    private void Update()
    {
        HandleCameraInput();
        HandleClickSelection();
    }

    private void HandleCameraInput()
    {
        // WASD / Arrows to pan, Q/E or -/+ to zoom
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        float dx = 0f, dy = 0f, zoomDelta = 0f; float rot = 0f;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dx -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dx += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dy -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dy += 1f;
            if (kb.qKey.isPressed || kb.minusKey.isPressed) zoomDelta += 1f; // zoom out
            if (kb.eKey.isPressed || kb.equalsKey.isPressed) zoomDelta -= 1f; // zoom in (Shift+= for '+')
            if (kb.zKey.isPressed) rot += 1f; // rotate CCW
            if (kb.cKey.isPressed) rot -= 1f; // rotate CW
            if (kb.rKey.wasPressedThisFrame) _rotationDeg = 0f;
            bool fast = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            float speed = _panSpeed * (fast ? 2f : 1f) * Mathf.Max(1f, _cam.orthographicSize * 0.15f);
            _camTarget += new Vector3(dx, dy, 0f) * speed * Time.deltaTime;
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + zoomDelta * _zoomSpeed * Time.deltaTime, _zoomMin, _zoomMax);
            _rotationDeg += rot * _rotationSpeed * Time.deltaTime;
        }
#else
        float dx = 0f, dy = 0f, zoomDelta = 0f; float rot = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dx -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dx += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dy -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dy += 1f;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) zoomDelta += 1f;
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) zoomDelta -= 1f;
        if (Input.GetKey(KeyCode.Z)) rot += 1f;
        if (Input.GetKey(KeyCode.C)) rot -= 1f;
        if (Input.GetKeyDown(KeyCode.R)) _rotationDeg = 0f;
        bool fast = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float speed = _panSpeed * (fast ? 2f : 1f) * Mathf.Max(1f, _cam.orthographicSize * 0.15f);
        _camTarget += new Vector3(dx, dy, 0f) * speed * Time.deltaTime;
        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize + zoomDelta * _zoomSpeed * Time.deltaTime, _zoomMin, _zoomMax);
        _rotationDeg += rot * _rotationSpeed * Time.deltaTime;
#endif
        _cam.transform.position = Vector3.Lerp(_cam.transform.position, new Vector3(_camTarget.x, _camTarget.y, -10f), 0.9f);
        // Apply incremental rotation around a pivot (selected hex or nearest to screen center)
        if (_gridRoot != null)
        {
            float delta = _rotationDeg - _appliedRotationDeg;
            if (Mathf.Abs(delta) > 0.0001f)
            {
                var pivot = GetRotationPivotWorld();
                _gridRoot.RotateAround(pivot, Vector3.forward, delta);
                _appliedRotationDeg = _rotationDeg;
            }
        }
        if (!fastMode)
            UpdateTileBorderWidths();
    }

    private void HandleClickSelection()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = mouse.position.ReadValue();
            TrySelectAtScreen(pos);
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectAtScreen(Input.mousePosition);
        }
#endif
    }

    private void TrySelectAtScreen(Vector2 screenPos)
    {
        var ray = _cam.ScreenPointToRay(screenPos);
        if (fastMode)
        {
            // Math-based axial rounding
            var plane = new Plane(Vector3.forward, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 world = ray.GetPoint(enter);
                // Convert to grid-local before axial rounding (accounts for rotation)
                Vector3 local = _gridRoot != null ? _gridRoot.InverseTransformPoint(world) : world;
                var qr = HexMath.WorldToAxialRounded(local, hexSize);
                Vector3 centerLocal = HexMath.AxialToWorld(qr.x, qr.y, hexSize);
                Vector3 center = _gridRoot != null ? _gridRoot.TransformPoint(centerLocal) : centerLocal;
                if (_selectionOverlay != null)
                {
                    _selectionOverlay.SetVisible(true);
                    _selectionOverlay.SetPosition(center);
                }
                _hasSelectedPivot = true;
                _selectedPivotWorld = center;
            }
        }
        else
        {
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
            {
                var tile = hit.collider.GetComponent<HexTile>();
                if (tile != null)
                {
                    if (_selected != null) _selected.SetHighlighted(false);
                    _selected = tile;
                    _selected.SetHighlighted(true);
                    _hasSelectedPivot = true;
                    _selectedPivotWorld = tile.transform.position;
                }
            }
        }
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 18 };
        GUILayout.BeginArea(new Rect(0, 8, Screen.width, 28));
        string perf = fastMode ? "FAST (chunked)" : "DEMO (per-tile)";
        GUILayout.Label($"Hex TBS — {perf} • Click to highlight • Pan: WASD/Arrows • Zoom: Q/E or -/+ • Rotate: Z/C (R reset) • Shift = faster", style);
        GUILayout.EndArea();
    }

    private Vector3 GetRotationPivotWorld()
    {
        if (_hasSelectedPivot)
            return _selectedPivotWorld;

        // Fallback: hex nearest to screen center
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        var ray = _cam.ScreenPointToRay(center);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 world = ray.GetPoint(enter);
            Vector3 local = _gridRoot != null ? _gridRoot.InverseTransformPoint(world) : world;
            var qr = HexMath.WorldToAxialRounded(local, hexSize);
            Vector3 centerLocal = HexMath.AxialToWorld(qr.x, qr.y, hexSize);
            Vector3 centerWorld = _gridRoot != null ? _gridRoot.TransformPoint(centerLocal) : centerLocal;
            return centerWorld;
        }
        return Vector3.zero;
    }

    private void UpdateTileBorderWidths()
    {
        if (_gridRoot == null) return;
        if (_tiles == null) return;
        for (int i = 0; i < _tiles.Length; i++)
        {
            _tiles[i].UpdateBorderForCamera(_cam);
        }
    }

    private void ApplyBorderPixelSettings()
    {
        if (_tiles == null) return;
        for (int i = 0; i < _tiles.Length; i++)
        {
            _tiles[i].ConfigureBorderPixels(borderWidthPixels, borderHighlightPixels);
            _tiles[i].UpdateBorderForCamera(_cam);
        }
    }
}
