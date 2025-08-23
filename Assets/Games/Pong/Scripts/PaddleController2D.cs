using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class PaddleController2D : MonoBehaviour
{
    public enum ControlSide { Left, Right }

    [Header("Control")]
    public ControlSide side = ControlSide.Left;
    public float moveSpeed = 7f;

    private Rigidbody2D _rb;
    private Camera _cam;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;
    }

    private void Update()
    {
        // Read input each frame; apply in FixedUpdate via velocity/MovePosition
        _input = ReadInput();
    }

    private float _input;

    private float ReadInput()
    {
        float dir = 0f;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (side == ControlSide.Left)
            {
                if (kb.wKey.isPressed) dir += 1f;
                if (kb.sKey.isPressed) dir -= 1f;
            }
            else
            {
                if (kb.upArrowKey.isPressed) dir += 1f;
                if (kb.downArrowKey.isPressed) dir -= 1f;
            }
        }
#else
        if (side == ControlSide.Left)
        {
            if (Input.GetKey(KeyCode.W)) dir += 1f;
            if (Input.GetKey(KeyCode.S)) dir -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow)) dir += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) dir -= 1f;
        }
#endif
        return dir;
    }

    private void FixedUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        float halfHeight = _cam != null ? _cam.orthographicSize : 5f;
        float clampY = halfHeight - (transform.localScale.y * 0.5f);

        Vector2 current = _rb.position;
        float targetY = Mathf.Clamp(current.y + _input * moveSpeed * Time.fixedDeltaTime, -clampY, clampY);
        float vy = (targetY - current.y) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
        _rb.linearVelocity = new Vector2(0f, vy); // expose paddle motion to collisions
        _rb.MovePosition(new Vector2(current.x, targetY));
    }
}
