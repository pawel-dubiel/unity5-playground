using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Simple runtime-only Pong. Drop-in: auto-creates camera, paddles, ball, walls, and UI.
// Controls: Left = W/S, Right = Up/Down. Press R to reset round.
public class PongGame : MonoBehaviour
{
    // Configurable sizes/speeds
    [Header("Dimensions")]
    public float cameraOrthoSize = 5f;
    public Vector2 paddleSize = new Vector2(0.3f, 2.0f);
    public float paddleSpeed = 7f;
    public float ballRadius = 0.3f;
    public float ballSpeed = 7f;
    public float ballSpeedIncrementOnHit = 0.5f;

    [Header("Gameplay")]
    public int maxScore = 11;
    public float serveDelay = 1.0f;

    // Runtime objects
    private Camera _cam;
    private Transform _leftPaddle;
    private Transform _rightPaddle;
    private Transform _ball;
    private Transform _topWall;
    private Transform _bottomWall;
    private GameObject _topWall2D;
    private GameObject _bottomWall2D;

    // State
    private Vector2 _ballVelocity;
    private int _leftScore;
    private int _rightScore;
    private float _halfWidth;
    private float _halfHeight;
    private float _serveTimer;
    private bool _serving;
    private bool _startedAfterSelection;
    private bool _usePhysics2D;
    private Ball2D _ball2d;
    private bool _serveToLeft;

    // Created via PongBootstrap when the game is selected

    private void Awake()
    {
        SetupCamera();
        CacheExtents();
        CreatePaddles();
        CreateBall();
        _ball2d = _ball != null ? _ball.GetComponent<Ball2D>() : null;
        _usePhysics2D = _ball2d != null || (_ball != null && _ball.GetComponent<Rigidbody2D>() != null);
        CreateWalls();
        CenterEverything();
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
        _cam.backgroundColor = new Color(0.07f, 0.07f, 0.09f);
    }

    private void CacheExtents()
    {
        _halfHeight = _cam.orthographicSize;
        _halfWidth = _halfHeight * _cam.aspect;
    }

    private Transform CreateBlock(string name, Vector2 size, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var rend = go.GetComponent<Renderer>();
        ApplyUnlitColor(rend, color);
        // Remove colliders; we do manual collision.
        var col = go.GetComponent<Collider>();
        if (col) Destroy(col);
        return go.transform;
    }

    private Transform CreateBallSphere(string name, float radius, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.localScale = Vector3.one * (radius * 2f);
        var rend = go.GetComponent<Renderer>();
        ApplyUnlitColor(rend, color);
        var col = go.GetComponent<Collider>();
        if (col) Destroy(col);
        return go.transform;
    }

    private void ApplyUnlitColor(Renderer rend, Color color)
    {
        // Prefer URP Unlit, fallback to built-in Unlit, then Sprites/Default
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        rend.sharedMaterial = mat;
    }

    private void CreateWalls()
    {
        float thickness = 0.5f;
        if (_usePhysics2D)
        {
            _topWall2D = new GameObject("TopWall2D");
            var topCol = _topWall2D.AddComponent<BoxCollider2D>();
            _topWall2D.transform.position = new Vector3(0, _halfHeight + thickness * 0.5f - 0.25f, 0);
            topCol.size = new Vector2(_halfWidth * 2f + 5f, thickness);

            _bottomWall2D = new GameObject("BottomWall2D");
            var botCol = _bottomWall2D.AddComponent<BoxCollider2D>();
            _bottomWall2D.transform.position = new Vector3(0, -_halfHeight - thickness * 0.5f + 0.25f, 0);
            botCol.size = new Vector2(_halfWidth * 2f + 5f, thickness);
        }
        else
        {
            _topWall = CreateBlock("TopWall", new Vector2(_halfWidth * 2f, thickness), new Color(0.2f, 0.2f, 0.26f));
            _bottomWall = CreateBlock("BottomWall", new Vector2(_halfWidth * 2f, thickness), new Color(0.2f, 0.2f, 0.26f));
            _topWall.position = new Vector3(0, _halfHeight + thickness * 0.5f - 0.25f, 0);
            _bottomWall.position = new Vector3(0, -_halfHeight - thickness * 0.5f + 0.25f, 0);
        }
    }

    private void CreatePaddles()
    {
        // Try loading prefab from Resources first
        var paddlePrefab = Resources.Load<GameObject>("Pong/Paddle");
        if (paddlePrefab != null)
        {
            var left = Instantiate(paddlePrefab);
            left.name = "LeftPaddle";
            left.transform.localScale = new Vector3(paddleSize.x, paddleSize.y, 1f);
            _leftPaddle = left.transform;

            var right = Instantiate(paddlePrefab);
            right.name = "RightPaddle";
            right.transform.localScale = new Vector3(paddleSize.x, paddleSize.y, 1f);
            _rightPaddle = right.transform;
        }
        else
        {
            _leftPaddle = CreateBlock("LeftPaddle", paddleSize, new Color(0.9f, 0.9f, 0.95f));
            _rightPaddle = CreateBlock("RightPaddle", paddleSize, new Color(0.9f, 0.9f, 0.95f));
        }
    }

    private void CreateBall()
    {
        var ballPrefab = Resources.Load<GameObject>("Pong/Ball");
        if (ballPrefab != null)
        {
            var b = Instantiate(ballPrefab);
            b.name = "Ball";
            b.transform.localScale = Vector3.one * (ballRadius * 2f);
            // Ensure Ball2D is present for physics serve behavior
            var comp = b.GetComponent<Ball2D>();
            if (comp == null) comp = b.AddComponent<Ball2D>();
            comp.baseSpeed = ballSpeed;
            comp.speedIncrementOnPaddle = ballSpeedIncrementOnHit;
            _ball = b.transform;
        }
        else
        {
            _ball = CreateBallSphere("Ball", ballRadius, new Color(0.9f, 0.9f, 0.95f));
        }
    }

    private void CenterEverything()
    {
        float margin = 0.7f;
        _leftPaddle.position = new Vector3(-_halfWidth + margin, 0, 0);
        _rightPaddle.position = new Vector3(_halfWidth - margin, 0, 0);
        _ball.position = Vector3.zero;
        _ballVelocity = Vector2.zero;
        if (_usePhysics2D && _ball2d != null)
        {
            _ball2d.ResetToCenter();
        }
    }

    private void Update()
    {
        CacheExtents(); // update if aspect changes (resize window)

        // Wait for mode selection before starting gameplay
        if (!ModeSelect.Selected)
        {
            _ball.position = Vector3.zero;
            _ballVelocity = Vector2.zero;
            return;
        }

        // On first frame after selection, serve the ball
        if (!_startedAfterSelection)
        {
            _startedAfterSelection = true;
            CenterEverything();
            StartServe(toLeft: Random.value > 0.5f);
        }

        // Reset round
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
#else
        if (Input.GetKeyDown(KeyCode.R))
#endif
        {
            CenterEverything();
            StartServe(toLeft: Random.value > 0.5f);
        }

        // Serve countdown
        if (_serving)
        {
            _serveTimer -= Time.deltaTime;
            if (_serveTimer <= 0f)
            {
                _serving = false;
                if (_usePhysics2D && _ball2d != null)
                {
                    _ball2d.Serve(_serveToLeft);
                }
                else if (_usePhysics2D) // fallback if prefab lacks Ball2D
                {
                    var rb2d = _ball.GetComponent<Rigidbody2D>();
                    if (rb2d != null)
                    {
                        Vector2 dir = (_serveToLeft ? Vector2.left : Vector2.right);
                        float angle = Random.Range(-0.35f, 0.35f);
                        var rot = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                        rb2d.linearVelocity = ((Vector2)(rot * dir)) * ballSpeed;
                    }
                }
            }
        }

        if (_usePhysics2D)
        {
            Physics2DUpdate();
        }
        else
        {
            HandlePaddleInput();
            MoveBall();
        }
    }

    private void Physics2DUpdate()
    {
        var pos = (Vector2)_ball.position;
        float goalX = _halfWidth + 0.5f;
        if (pos.x < -goalX)
        {
            ScoreRight();
            return;
        }
        if (pos.x > goalX)
        {
            ScoreLeft();
            return;
        }
    }

    private void HandlePaddleInput()
    {
        // Left paddle: W/S | Right paddle: Up/Down (disabled if AI active)
        float leftDir = 0f;
        float rightDir = 0f;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) leftDir += 1f;
            if (kb.sKey.isPressed) leftDir -= 1f;
            if (kb.upArrowKey.isPressed) rightDir += 1f;
            if (kb.downArrowKey.isPressed) rightDir -= 1f;
        }
#else
        if (Input.GetKey(KeyCode.W)) leftDir += 1f;
        if (Input.GetKey(KeyCode.S)) leftDir -= 1f;
        if (Input.GetKey(KeyCode.UpArrow)) rightDir += 1f;
        if (Input.GetKey(KeyCode.DownArrow)) rightDir -= 1f;
#endif

        float clampY = _halfHeight - paddleSize.y * 0.5f;

        var lp = _leftPaddle.position;
        lp.y = Mathf.Clamp(lp.y + leftDir * paddleSpeed * Time.deltaTime, -clampY, clampY);
        _leftPaddle.position = lp;

        // If AI is active, it will move the right paddle in LateUpdate.
        bool aiActive = false;
        try { aiActive = PaddleAI.Active; } catch { aiActive = false; }
        if (!aiActive)
        {
            var rp = _rightPaddle.position;
            rp.y = Mathf.Clamp(rp.y + rightDir * paddleSpeed * Time.deltaTime, -clampY, clampY);
            _rightPaddle.position = rp;
        }
    }

    private void MoveBall()
    {
        if (_serving)
        {
            // Ball follows the serving side for a moment
            _ball.position = Vector3.Lerp(_ball.position, Vector3.zero, 0.1f);
            return;
        }

        var pos = (Vector2)_ball.position;
        pos += _ballVelocity * Time.deltaTime;

        // Collide with top/bottom
        float top = _halfHeight - ballRadius;
        float bottom = -_halfHeight + ballRadius;
        if (pos.y > top)
        {
            pos.y = top;
            _ballVelocity.y = -Mathf.Abs(_ballVelocity.y);
        }
        else if (pos.y < bottom)
        {
            pos.y = bottom;
            _ballVelocity.y = Mathf.Abs(_ballVelocity.y);
        }

        // Collide with paddles
        // Left paddle AABB
        if (pos.x - ballRadius < _leftPaddle.position.x + paddleSize.x * 0.5f &&
            pos.x > _leftPaddle.position.x &&
            Mathf.Abs(pos.y - _leftPaddle.position.y) < (paddleSize.y * 0.5f + ballRadius))
        {
            pos.x = _leftPaddle.position.x + paddleSize.x * 0.5f + ballRadius;
            _ballVelocity.x = Mathf.Abs(_ballVelocity.x) + ballSpeedIncrementOnHit;
            // Add some angle based on hit position
            float offset = (pos.y - _leftPaddle.position.y) / (paddleSize.y * 0.5f);
            _ballVelocity.y = Mathf.Lerp(_ballVelocity.y, offset * ballSpeed, 0.5f);
        }
        // Right paddle AABB
        if (pos.x + ballRadius > _rightPaddle.position.x - paddleSize.x * 0.5f &&
            pos.x < _rightPaddle.position.x &&
            Mathf.Abs(pos.y - _rightPaddle.position.y) < (paddleSize.y * 0.5f + ballRadius))
        {
            pos.x = _rightPaddle.position.x - paddleSize.x * 0.5f - ballRadius;
            _ballVelocity.x = -Mathf.Abs(_ballVelocity.x) - ballSpeedIncrementOnHit;
            float offset = (pos.y - _rightPaddle.position.y) / (paddleSize.y * 0.5f);
            _ballVelocity.y = Mathf.Lerp(_ballVelocity.y, offset * ballSpeed, 0.5f);
        }

        // Score if passes beyond paddles
        float goalX = _halfWidth + 0.5f;
        if (pos.x < -goalX)
        {
            ScoreRight();
            return;
        }
        if (pos.x > goalX)
        {
            ScoreLeft();
            return;
        }

        _ball.position = new Vector3(pos.x, pos.y, 0f);
    }

    private void StartServe(bool toLeft)
    {
        _serving = true;
        _serveTimer = serveDelay;
        _serveToLeft = toLeft;
        if (_usePhysics2D && _ball2d != null)
        {
            _ball2d.baseSpeed = ballSpeed;
            _ball2d.speedIncrementOnPaddle = ballSpeedIncrementOnHit;
            _ball2d.ResetToCenter();
        }
        else
        {
            Vector2 dir = (toLeft ? Vector2.left : Vector2.right);
            float angle = Random.Range(-0.35f, 0.35f);
            var rot = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            _ballVelocity = (Vector2)(rot * dir) * ballSpeed;
            _ball.position = Vector3.zero;
        }
    }

    private void ScoreLeft()
    {
        _leftScore++;
        CenterEverything();
        if (_leftScore >= maxScore)
        {
            _leftScore = 0; _rightScore = 0;
        }
        StartServe(toLeft: false);
    }

    private void ScoreRight()
    {
        _rightScore++;
        CenterEverything();
        if (_rightScore >= maxScore)
        {
            _leftScore = 0; _rightScore = 0;
        }
        StartServe(toLeft: true);
    }

    private void OnGUI()
    {
        // Simple UI for score and hints
        var prev = GUI.color;
        GUI.color = Color.white;

        var style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };

        GUILayout.BeginArea(new Rect(0, 8, Screen.width, 40));
        GUILayout.Label($"{_leftScore}        PONG        {_rightScore}", style);
        GUILayout.EndArea();

        var hint = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerCenter,
            fontSize = 12
        };
        GUILayout.BeginArea(new Rect(0, Screen.height - 28, Screen.width, 24));
        GUILayout.Label("Left: W/S • Right: ↑/↓ • R: Reset", hint);
        GUILayout.EndArea();

        // Center dashed line
        DrawCenterLine();

        GUI.color = prev;
    }

    private void DrawCenterLine()
    {
        float dashH = 10f;
        float gap = 8f;
        float x = Screen.width * 0.5f - 1f;
        for (float y = 60; y < Screen.height - 60; y += dashH + gap)
        {
            GUI.Box(new Rect(x, y, 2f, dashH), GUIContent.none);
        }
    }
}

