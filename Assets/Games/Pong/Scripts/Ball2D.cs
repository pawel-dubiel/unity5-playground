using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Ball2D : MonoBehaviour
{
    public float baseSpeed = 7f;
    public float speedIncrementOnPaddle = 0.5f;
    [Header("Spin / Magnus")]
    public float magnusCoefficient = 0.15f; // stronger sideways force from spin
    public float spinOnPaddleHit = 300f;    // deg/sec added based on hit offset
    public float maxSpin = 720f;           // clamp deg/sec
    [Tooltip("How much paddle Y velocity influences outgoing ball Y")] public float paddleYVelocityInfluence = 0.4f;
    [Tooltip("How much paddle Y velocity is converted into spin (deg/sec per unit)")] public float paddleSpinTransfer = 60f;

    [Header("Visuals")] public Transform visual; // child visual that rotates with the ball

    private Rigidbody2D _rb;
    private Camera _cam;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;
        _rb.gravityScale = 0f;
        _rb.freezeRotation = false; // allow visual rotation to reflect spin
    }

    public void ResetToCenter()
    {
        _rb.linearVelocity = Vector2.zero;
        _rb.rotation = 0f;
        _rb.position = Vector2.zero;
    }

    public void Serve(bool toLeft)
    {
        Vector2 dir = (toLeft ? Vector2.left : Vector2.right);
        float angle = Random.Range(-0.35f, 0.35f);
        var rot = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
        _rb.linearVelocity = ((Vector2)(rot * dir)) * baseSpeed;
        _rb.angularVelocity = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If hitting paddles, tweak angle and increase speed a bit
        if (collision.collider.name.Contains("Paddle"))
        {
            float paddleY = collision.collider.bounds.center.y;
            float diffY = transform.position.y - paddleY;
            float norm = Mathf.Clamp(diffY / (collision.collider.bounds.extents.y), -1f, 1f);
            var v = _rb.linearVelocity;
            float paddleVy = collision.rigidbody != null ? collision.rigidbody.linearVelocity.y : 0f;
            v.x = Mathf.Sign(v.x) * (Mathf.Abs(v.x) + speedIncrementOnPaddle);
            v.y = Mathf.Lerp(v.y, norm * baseSpeed, 0.5f) + paddleYVelocityInfluence * paddleVy;
            _rb.linearVelocity = v;

            // Impart spin based on where the ball hits the paddle (top/bottom)
            float spinAdd = spinOnPaddleHit * norm + paddleSpinTransfer * paddleVy;
            _rb.angularVelocity = Mathf.Clamp(_rb.angularVelocity + spinAdd, -maxSpin, maxSpin);
        }
        else if (collision.collider.name.Contains("TopWall") || collision.collider.name.Contains("BottomWall"))
        {
            // Slightly reduce spin on wall bounces to avoid runaway
            _rb.angularVelocity *= 0.9f;
        }
    }

    private void FixedUpdate()
    {
        // Apply Magnus effect: sideways force proportional to spin (ω) and speed
        var v = _rb.linearVelocity;
        float speed = v.magnitude;
        float omega = _rb.angularVelocity * Mathf.Deg2Rad; // rad/s
        if (speed > 0.01f && Mathf.Abs(omega) > 0.01f)
        {
            // Perpendicular to velocity (rotate +90 degrees): (-vy, vx)
            Vector2 perp = new Vector2(-v.y, v.x).normalized;
            float sign = Mathf.Sign(omega);
            Vector2 magnus = perp * sign * magnusCoefficient * Mathf.Abs(omega) * speed;
            _rb.AddForce(magnus);
        }
    }

    // Visual rotation comes from Rigidbody2D rotation (freezeRotation=false)
}
