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
    public float horizontalMoveSpeed = 5f; // speed for movement toward/away from net
    public float minXPosition = -8f; // minimum X position (how far back paddle can go)
    public float maxXPosition = -2f; // maximum X position (how far forward paddle can go)
    public bool enableHorizontalMovement = true; // toggle horizontal movement on/off

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
        _horizontalInput = ReadHorizontalInput();
    }

    private float _input;
    private float _horizontalInput;

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

    private float ReadHorizontalInput()
    {
        if (!enableHorizontalMovement) return 0f;
        
        float dir = 0f;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (side == ControlSide.Left)
            {
                if (kb.dKey.isPressed) dir += 1f; // D moves toward net (right)
                if (kb.aKey.isPressed) dir -= 1f; // A moves away from net (left)
            }
            else
            {
                if (kb.leftArrowKey.isPressed) dir -= 1f; // Left arrow moves toward net (left)
                if (kb.rightArrowKey.isPressed) dir += 1f; // Right arrow moves away from net (right)
            }
        }
#else
        if (side == ControlSide.Left)
        {
            if (Input.GetKey(KeyCode.D)) dir += 1f;
            if (Input.GetKey(KeyCode.A)) dir -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow)) dir -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) dir += 1f;
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
        
        // Vertical movement
        float targetY = Mathf.Clamp(current.y + _input * moveSpeed * Time.fixedDeltaTime, -clampY, clampY);
        
        // Horizontal movement (toward/away from net)
        float targetX = current.x;
        if (enableHorizontalMovement)
        {
            // Adjust limits based on which side the paddle is on
            float minX = side == ControlSide.Left ? minXPosition : -maxXPosition;
            float maxX = side == ControlSide.Left ? maxXPosition : -minXPosition;
            
            targetX = Mathf.Clamp(current.x + _horizontalInput * horizontalMoveSpeed * Time.fixedDeltaTime, minX, maxX);
        }
        
        // Calculate velocities and apply
        float vy = (targetY - current.y) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
        float vx = (targetX - current.x) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
        _rb.linearVelocity = new Vector2(vx, vy); // expose paddle motion to collisions
        _rb.MovePosition(new Vector2(targetX, targetY));
    }
}
