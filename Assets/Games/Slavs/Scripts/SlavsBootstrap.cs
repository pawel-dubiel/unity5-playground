using UnityEngine;

public static class SlavsBootstrap
{
    public static bool Started { get; private set; }

    public static void Start()
    {
        if (Started) return;
        Started = true;

        var go = new GameObject("SlavsVillageGame");
        go.AddComponent<SlavsVillageGame>();

        // Try to use a prefabbed UI if present, otherwise build at runtime
        var prefab = Resources.Load<GameObject>("Slavs/SlavsUI");
        if (prefab != null)
        {
            Object.Instantiate(prefab);
        }

        // Ensure a UI controller exists even if prefab lacks it
#if UNITY_2022_2_OR_NEWER
        var ui = Object.FindFirstObjectByType<SlavsVillageUI>();
#else
        var ui = Object.FindObjectOfType<SlavsVillageUI>();
#endif
        if (ui == null)
        {
            go.AddComponent<SlavsVillageUI>();
        }
    }
}
