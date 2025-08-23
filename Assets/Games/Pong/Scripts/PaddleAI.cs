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
            if (enableSpinCompensation)
            {
                try
                {
                    // Use spin-aware prediction
                    predictedY = PredictPositionWithSpin(ballPos, ballVel, _ball.GetComponent<Rigidbody2D>());
                    
                    // Fallback to linear prediction if spin prediction gives extreme values
                    if (float.IsNaN(predictedY) || float.IsInfinity(predictedY))
                    {
                        predictedY = ballPos.y + ballVel.y * anticipate;
                    }
                }
                catch (System.Exception)
                {
                    // Fallback to simple linear prediction if anything goes wrong
                    predictedY = ballPos.y + ballVel.y * anticipate;
                }
            }
            else
            {
                // Simple linear prediction (original behavior)
                predictedY = ballPos.y + ballVel.y * anticipate;
            }
        }

        float halfHeight = _cam.orthographicSize;
        float clampY = halfHeight - _paddle.localScale.y * 0.5f;
        _targetY = Mathf.Clamp(predictedY, -clampY, clampY);
        
        // Calculate horizontal target position based on ball speed and position
        if (enableHorizontalMovement)
        {
            float ballSpeed = ballVel.magnitude;
            float distanceFromCenter = Mathf.Abs(_paddle.position.x);
            
            // Aggressive stance when ball is slow and approaching
            if (ballApproaching && ballSpeed < aggressiveThreshold)
            {
                _targetX = Mathf.Sign(_paddle.position.x) * minDistanceFromNet;
            }
            // Defensive stance when ball is fast
            else if (ballSpeed > defensiveThreshold)
            {
                _targetX = Mathf.Sign(_paddle.position.x) * maxDistanceFromNet;
            }
            // Normal positioning based on ball Y position (move closer when ball is near paddle)
            else if (ballApproaching)
            {
                float ballDistanceFromPaddle = Mathf.Abs(ballPos.y - _paddle.position.y);
                float normalizedDistance = Mathf.Clamp01(ballDistanceFromPaddle / halfHeight);
                float desiredDistance = Mathf.Lerp(minDistanceFromNet, maxDistanceFromNet, normalizedDistance * 0.5f + 0.5f);
                _targetX = Mathf.Sign(_paddle.position.x) * desiredDistance;
            }
            // Default position when ball is moving away
            else
            {
                float defaultDistance = (minDistanceFromNet + maxDistanceFromNet) * 0.5f;
                _targetX = Mathf.Sign(_paddle.position.x) * defaultDistance;
            }
        }
        else
        {
            _targetX = _paddle.position.x; // No horizontal movement
        }
        
        // Apply smoothing to avoid jittery movements
        if (!_initialized) // initialize if not set
        {
            _smoothedTargetY = _targetY;
            _smoothedTargetX = _targetX;
            _initialized = true;
        }
        else
        {
            _smoothedTargetY = Mathf.Lerp(_smoothedTargetY, _targetY, smoothingFactor);
            _smoothedTargetX = Mathf.Lerp(_smoothedTargetX, _targetX, smoothingFactor);
        }
        
        // Store prediction for debug visualization
        if (showDebugPrediction && _paddle != null)
        {
            _debugPredictionY = predictedY;
            _debugBallPos = ballPos;
            _debugTargetX = _targetX;
        }
        
        // Debug logging (disable in production)
        if (enableSpinCompensation && shouldReact && Time.frameCount % 60 == 0)
        {
            Debug.Log($"AI: Ball pos: {ballPos.y:F2}, Predicted: {predictedY:F2}, Ball vel: {ballVel.y:F2}, Spin: {_ball.GetComponent<Rigidbody2D>()?.angularVelocity ?? 0:F1}");
        }
        
        _hasTargets = true;
    }

    private float _debugPredictionY;
    private Vector2 _debugBallPos;
    private float _debugTargetX;

    private void OnDrawGizmos()
    {
        if (showDebugPrediction && _paddle != null && _ball != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 predictionPoint = new Vector3(_paddle.position.x, _debugPredictionY, 0);
            Gizmos.DrawLine(_debugBallPos, predictionPoint);
            Gizmos.DrawSphere(predictionPoint, 0.1f);
            
            // Draw current ball position
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_debugBallPos, 0.05f);
            
            // Draw horizontal target position
            if (enableHorizontalMovement)
            {
                Gizmos.color = Color.blue;
                Vector3 horizontalTarget = new Vector3(_debugTargetX, _paddle.position.y, 0);
                Gizmos.DrawLine(_paddle.position, horizontalTarget);
                Gizmos.DrawSphere(horizontalTarget, 0.08f);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_hasTargets || _paddle == null) return;
        float currentY = _paddle.position.y;
        float currentX = _paddle.position.x;
        
        // Vertical movement
        float deltaY = _smoothedTargetY - currentY;
        float verticalStep = moveSpeed * Time.fixedDeltaTime;
        float newY = Mathf.MoveTowards(currentY, _smoothedTargetY, verticalStep);
        
        // Horizontal movement
        float newX = currentX;
        if (enableHorizontalMovement)
        {
            float deltaX = _smoothedTargetX - currentX;
            float horizontalStep = horizontalMoveSpeed * Time.fixedDeltaTime;
            newX = Mathf.MoveTowards(currentX, _smoothedTargetX, horizontalStep);
        }
        
        // Skip if both movements are within dead zones
        if (Mathf.Abs(deltaY) < deadZone && Mathf.Abs(newX - currentX) < 0.01f) return;

        var rb2d = _paddle.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            float vy = (newY - rb2d.position.y) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
            float vx = (newX - rb2d.position.x) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
            rb2d.linearVelocity = new Vector2(vx, vy); // expose paddle velocity to collisions
            rb2d.MovePosition(new Vector2(newX, newY));
        }
        else
        {
            _paddle.position = new Vector3(newX, newY, _paddle.position.z);
        }
    }
}
