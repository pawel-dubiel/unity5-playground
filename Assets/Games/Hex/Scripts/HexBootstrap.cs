using UnityEngine;

public static class HexBootstrap
{
    public static bool Started { get; private set; }

    public static void Start()
    {
        if (Started) return;
        Started = true;

        var hex = new GameObject("HexGame");
        hex.AddComponent<HexGame>();
    }
}

