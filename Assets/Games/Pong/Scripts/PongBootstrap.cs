using UnityEngine;

public static class PongBootstrap
{
    public static bool Started { get; private set; }

    public static void Start()
    {
        if (Started) return;
        Started = true;

        var pong = new GameObject("PongGame");
        pong.AddComponent<PongGame>();

        var mode = new GameObject("ModeSelect");
        mode.AddComponent<ModeSelect>();

        var ai = new GameObject("PaddleAI");
        var aiComp = ai.AddComponent<PaddleAI>();
        aiComp.enabled = false; // enabled by ModeSelect when Single Player chosen
    }
}

