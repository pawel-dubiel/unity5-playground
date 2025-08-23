using UnityEngine;

// Simple AI controller for the right paddle in PongGame.
// Auto-creates itself at runtime and follows the ball with configurable speed.
[DefaultExecutionOrder(50)]
public class PaddleAI : MonoBehaviour
{
    public static bool Active { get; private set; }

    [Header("Targets")]
    public string ballName = "Ball";
    public string paddleName = "RightPaddle";

    [Header("Behavior")]
    public float moveSpeed = 5.0f; // units per second
    public float anticipate = 0.1f; // seconds to look ahead along ball velocity
    public float deadZone = 0.1f;  // do not move if within this distance
    public float reactionThreshold = 0.3f; // only react if ball is moving towards paddle
    public float smoothingFactor = 0.1f; // smooth out AI movements
    public bool onlyTrackWhenBallApproaching = true; // smarter AI behavior

    private Transform _ball;
    private Transform _paddle;
    private Camera _cam;

    // Created by PongBootstrap

    private void OnEnable() { Active = true; }
    private void OnDisable() { Active = false; }
    private void OnDestroy() { Active = false; }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        if (_paddle == null)
        {
            var p = GameObject.Find(paddleName);
            if (p != null) _paddle = p.transform;
        }
        if (_ball == null)
        {
            var b = GameObject.Find(ballName);
            if (b != null) _ball = b.transform;
        }
    }

    private float _targetY;
    private bool _hasTargets;
    private Vector2 _lastBallPos;
    private float _smoothedTargetY; // for smoother AI movement

    private void LateUpdate()
    {
        if (_paddle == null || _ball == null || _cam == null) { _hasTargets = false; return; }

        // Compute clamped target Y using anticipation each frame
        Vector2 ballPos = _ball.position;
        Vector2 ballVel = _ball.GetComponent<Rigidbody2D>() != null ? 
            _ball.GetComponent<Rigidbody2D>().linearVelocity : 
            (ballPos - _lastBallPos) / Mathf.Max(Time.deltaTime, 1e-5f);
        _lastBallPos = ballPos;

        // Check if ball is moving towards the AI paddle (right side)
        bool ballApproaching = ballVel.x > 0 || !onlyTrackWhenBallApproaching;
        bool shouldReact = ballApproaching || Mathf.Abs(ballPos.x - _paddle.position.x) < reactionThreshold;

        float predictedY = ballPos.y;
        
        if (shouldReact)
        {
            // Only predict when ball is approaching or close enough
            predictedY = ballPos.y + ballVel.y * anticipate;
        }

        float halfHeight = _cam.orthographicSize;
        float clampY = halfHeight - _paddle.localScale.y * 0.5f;
        _targetY = Mathf.Clamp(predictedY, -clampY, clampY);
        
        // Apply smoothing to avoid jittery movements
        if (_smoothedTargetY == 0) // initialize if not set
        {
            _smoothedTargetY = _targetY;
        }
        else
        {
            _smoothedTargetY = Mathf.Lerp(_smoothedTargetY, _targetY, smoothingFactor);
        }
        
        _hasTargets = true;
    }

    private void FixedUpdate()
    {
        if (!_hasTargets || _paddle == null) return;
        float currentY = _paddle.position.y;
        float delta = _smoothedTargetY - currentY; // Use smoothed target
        if (Mathf.Abs(delta) < deadZone) return;

        float step = moveSpeed * Time.fixedDeltaTime;
        float newY = Mathf.MoveTowards(currentY, _smoothedTargetY, step); // Use smoothed target
        var rb2d = _paddle.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            float vy = (newY - rb2d.position.y) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
            rb2d.linearVelocity = new Vector2(0f, vy); // expose paddle velocity to collisions
            rb2d.MovePosition(new Vector2(rb2d.position.x, newY));
        }
        else
        {
            _paddle.position = new Vector3(_paddle.position.x, newY, _paddle.position.z);
        }
    }
}
