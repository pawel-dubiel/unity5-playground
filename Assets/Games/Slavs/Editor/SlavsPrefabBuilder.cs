using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class SlavsPrefabBuilder
{
    [MenuItem("Slavs/Build UI Prefab (Card Panel)")]
    public static void BuildUIPrefab()
    {
        // Create a temporary root
        var root = new GameObject("SlavsUI_Root");
        // Attach the runtime UI controller so prefab is self-contained
        root.AddComponent<SlavsVillageUI>();

        // Canvas
        var canvasGo = new GameObject("UI Canvas");
        canvasGo.transform.SetParent(root.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        // EventSystem
        var es = new GameObject("EventSystem");
        es.transform.SetParent(root.transform);
        es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif

        // Simple main panel placeholder
        var panel = new GameObject("CardPanel", typeof(Image));
        panel.transform.SetParent(canvasGo.transform);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(920f, 560f);
        rt.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        // Save prefab
        const string folder = "Assets/Resources/Slavs";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Slavs");
        }
        var path = folder + "/SlavsUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log("Slavs UI Prefab saved to: " + path);
    }
}
