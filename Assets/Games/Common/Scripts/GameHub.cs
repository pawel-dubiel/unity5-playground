using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Simple hub to choose a game demo to launch.
public class GameHub : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var go = new GameObject("GameHub");
        go.AddComponent<GameHub>();
        DontDestroyOnLoad(go);
    }

    private bool _selected;

    private void Update()
    {
        if (_selected) return;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) SelectPong();
            if (kb.digit2Key.wasPressedThisFrame) SelectHex();
            if (kb.digit3Key.wasPressedThisFrame) SelectSlavs();
        }
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectPong();
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectHex();
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlavs();
#endif
    }

    private void SelectPong()
    {
        _selected = true;
        PongBootstrap.Start();
    }

    private void SelectHex()
    {
        _selected = true;
        HexBootstrap.Start();
    }

    private void SelectSlavs()
    {
        _selected = true;
        SlavsBootstrap.Start();
    }

    private void OnGUI()
    {
        if (_selected) return;
        var w = 420f; var h = 260f;
        var rect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.Box(rect, "");

        var title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(rect.x, rect.y + 12, rect.width, 30), "Game Hub — Select a Demo", title);

        GUILayout.BeginArea(new Rect(rect.x + 20, rect.y + 56, rect.width - 40, rect.height - 76));
        GUILayout.Space(8);
        if (GUILayout.Button("1) Pong", GUILayout.Height(36)))
            SelectPong();
        GUILayout.Space(10);
        if (GUILayout.Button("2) Hex TBS", GUILayout.Height(36)))
            SelectHex();
        GUILayout.Space(10);
        if (GUILayout.Button("3) Slavic Village (Poland)", GUILayout.Height(36)))
            SelectSlavs();
        GUILayout.Space(8);
        GUILayout.Label("Press 1 Pong • 2 Hex • 3 Slavs Village", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        GUILayout.EndArea();
    }
}
