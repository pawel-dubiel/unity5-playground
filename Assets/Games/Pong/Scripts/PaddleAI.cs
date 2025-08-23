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
    public float deadZone = 0.2f;  // do not move if within this distance
    public float reactionThreshold = 0.3f; // only react if ball is moving towards paddle
    public float smoothingFactor = 0.05f; // smooth out AI movements
    public bool onlyTrackWhenBallApproaching = true; // smarter AI behavior
    
    [Header("Horizontal Movement")]
    public bool enableHorizontalMovement = true; // enable AI horizontal movement
    public float horizontalMoveSpeed = 3.0f; // speed for horizontal movement
    public float minDistanceFromNet = 2.0f; // minimum distance from net (center)
    public float maxDistanceFromNet = 8.0f; // maximum distance from net (center)
    public float aggressiveThreshold = 0.5f; // when ball speed is below this, move closer
    public float defensiveThreshold = 8.0f; // when ball speed is above this, move farther back
    
    [Header("Spin Compensation")]
    public bool enableSpinCompensation = true; // enable spin-aware prediction
    public float spinLookaheadSteps = 5; // number of physics steps to simulate for spin
    public float spinSimulationTimeStep = 0.02f; // time step for spin simulation
    public bool showDebugPrediction = false; // show prediction gizmos in editor

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
    private float _targetX; // for horizontal movement
    private bool _hasTargets;
    private Vector2 _lastBallPos;
    private float _smoothedTargetY; // for smoother AI movement
    private float _smoothedTargetX; // for smoother horizontal movement

    private bool _initialized;

    private float PredictPositionWithSpin(Vector2 currentPos, Vector2 currentVel, Rigidbody2D ballRigidbody)
    {
        Vector2 simulatedPos = currentPos;
        Vector2 simulatedVel = currentVel;
        float angularVelocity = ballRigidbody != null ? ballRigidbody.angularVelocity : 0f;
        
        // Simulate forward in time to predict where ball will be
        float totalSimulationTime = anticipate;
        int steps = Mathf.Max(1, Mathf.FloorToInt(totalSimulationTime / spinSimulationTimeStep));
        
        for (int i = 0; i < steps; i++)
        {
            // Apply Magnus effect if there's significant spin
            float speed = simulatedVel.magnitude;
            float omega = angularVelocity * Mathf.Deg2Rad; // rad/s
            
            if (speed > 0.01f && Mathf.Abs(omega) > 0.01f)
            {
                // Magnus force: perpendicular to velocity, proportional to spin and speed
                Vector2 perp = new Vector2(-simulatedVel.y, simulatedVel.x).normalized;
                float sign = Mathf.Sign(omega);
                float magnusCoefficient = 0.15f; // Match Ball2D.magnusCoefficient
                Vector2 magnusForce = perp * sign * magnusCoefficient * Mathf.Abs(omega) * speed;
                
                // Apply force to change velocity
                simulatedVel += magnusForce * spinSimulationTimeStep;
            }
            
            // Update position
            simulatedPos += simulatedVel * spinSimulationTimeStep;
            
            // Stop if we've reached the paddle's x position
            if (_paddle != null && simulatedPos.x >= _paddle.position.x)
            {
                break;
            }
        }
        
        return simulatedPos.y;
    }

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
