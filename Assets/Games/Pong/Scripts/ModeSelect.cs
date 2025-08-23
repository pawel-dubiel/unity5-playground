using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Simple mode selector at startup: choose Single Player (AI) or Multiplayer.
// Keys: 1 = Single Player, 2 = Multiplayer. Also clickable buttons.
public class ModeSelect : MonoBehaviour
{
    public static bool Selected { get; private set; }
    public static bool SinglePlayer { get; private set; }

    // Created by PongBootstrap

    [SerializeField] private PaddleAI _ai;
    private bool _applied;

    private void Update()
    {
        if (!Selected)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Choose(singlePlayer: true);
                if (kb.digit2Key.wasPressedThisFrame) Choose(singlePlayer: false);
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) Choose(singlePlayer: true);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Choose(singlePlayer: false);
#endif
        }
        else if (!_applied)
        {
            ApplySelection();
        }
    }

    private void ApplySelection()
    {
        if (_ai == null) _ai = FindObjectOfType<PaddleAI>() ?? CreateAI();
        if (_ai != null) _ai.enabled = SinglePlayer; // enable AI for single player

        // Add/remove human controllers only in 2D physics setup (when Rigidbody2D exists)
        var left = GameObject.Find("LeftPaddle");
        var right = GameObject.Find("RightPaddle");
        bool use2D = (GameObject.Find("Ball")?.GetComponent<Rigidbody2D>() != null) ||
                     (left?.GetComponent<Rigidbody2D>() != null) ||
                     (right?.GetComponent<Rigidbody2D>() != null);

        if (use2D)
        {
            if (left != null)
            {
                var pc = left.GetComponent<PaddleController2D>() ?? left.AddComponent<PaddleController2D>();
                pc.side = PaddleController2D.ControlSide.Left;
            }
            if (right != null)
            {
                var pc = right.GetComponent<PaddleController2D>();
                if (SinglePlayer)
                {
                    if (pc != null) Object.Destroy(pc);
                }
                else
                {
                    pc = pc ?? right.AddComponent<PaddleController2D>();
                    pc.side = PaddleController2D.ControlSide.Right;
                }
            }
        }
        _applied = true;
    }

    private PaddleAI CreateAI()
    {
        var go = new GameObject("PaddleAI");
        var ai = go.AddComponent<PaddleAI>();
        DontDestroyOnLoad(go);
        return ai;
    }

    private void Choose(bool singlePlayer)
    {
        SinglePlayer = singlePlayer;
        Selected = true;
    }

    private void OnGUI()
    {
        if (Selected) return;
        var w = 360f; var h = 200f;
        var rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(rect, "");

        var title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(rect.x, rect.y + 12, rect.width, 30), "Pong — Select Mode", title);

        GUILayout.BeginArea(new Rect(rect.x + 20, rect.y + 56, rect.width - 40, rect.height - 76));
        GUILayout.Space(8);
        if (GUILayout.Button("1) Single Player (vs AI)", GUILayout.Height(36)))
            Choose(true);
        GUILayout.Space(10);
        if (GUILayout.Button("2) Multiplayer (2 players)", GUILayout.Height(36)))
            Choose(false);
        GUILayout.Space(8);
        GUILayout.Label("Press 1 or 2 to choose", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        GUILayout.EndArea();
    }
}
