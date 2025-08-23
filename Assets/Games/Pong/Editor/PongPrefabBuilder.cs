using System.IO;
using UnityEditor;
using UnityEngine;

public static class PongPrefabBuilder
{
    private const string PrefabDir = "Assets/Games/Pong/Resources/Pong";
    private const string SpriteDir = "Assets/Games/Pong/Resources/Pong/Sprites";

    [MenuItem("Pong/Build Prefabs (2D)")] 
    public static void BuildPrefabs()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Games"))
        {
            AssetDatabase.CreateFolder("Assets", "Games");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Games/Pong"))
        {
            AssetDatabase.CreateFolder("Assets/Games", "Pong");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Games/Pong/Resources"))
        {
            AssetDatabase.CreateFolder("Assets/Games/Pong", "Resources");
        }
        if (!AssetDatabase.IsValidFolder(PrefabDir))
        {
            AssetDatabase.CreateFolder("Assets/Games/Pong/Resources", "Pong");
        }
        if (!AssetDatabase.IsValidFolder(SpriteDir))
        {
            AssetDatabase.CreateFolder("Assets/Games/Pong/Resources/Pong", "Sprites");
        }

        var square = EnsureSprite("Square", tex => FillRect(tex, Color.white));
        var circle = EnsureSprite("Circle", DrawCircleSprite);

        CreatePaddlePrefab(square);
        CreateBallPrefab(circle);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Pong prefabs created under " + PrefabDir);
    }

    private static void CreatePaddlePrefab(Sprite sprite)
    {
        var go = new GameObject("Paddle");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        var col = go.AddComponent<BoxCollider2D>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        rb.angularDamping = 0.5f;

        string path = Path.Combine(PrefabDir, "Paddle.prefab");
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void CreateBallPrefab(Sprite sprite)
    {
        var go = new GameObject("Ball");
        var col = go.AddComponent<CircleCollider2D>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        rb.angularDamping = 0.5f;

        // Physics material for bounciness
        var mat = EnsurePhysicsMaterial2D("BallBouncy", 0f, 1f);
        col.sharedMaterial = mat;

        // Attach gameplay script
        var ball2d = go.AddComponent<Ball2D>();
        ball2d.baseSpeed = 7f;
        ball2d.speedIncrementOnPaddle = 0.5f;

        // Visual child for rotation and spin dot
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var vsr = visual.AddComponent<SpriteRenderer>();
        vsr.sprite = sprite;
        ball2d.visual = visual.transform;

        // Spin dot marker
        var square = EnsureSprite("Square", tex => FillRect(tex, Color.white));
        var dot = new GameObject("SpinDot");
        dot.transform.SetParent(visual.transform, false);
        var dsr = dot.AddComponent<SpriteRenderer>();
        dsr.sprite = square;
        dsr.color = Color.red;
        dot.transform.localScale = new Vector3(0.2f, 0.2f, 1f);
        dot.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // Trail renderer for ball
        var tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.5f;
        tr.startWidth = 0.12f;
        tr.endWidth = 0.02f;
        tr.autodestruct = false;
        tr.numCapVertices = 4;
        tr.minVertexDistance = 0.01f;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f,1f,1f,0.9f), 0f), new GradientColorKey(new Color(1f,1f,1f,0.2f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        tr.colorGradient = grad;

        string path = Path.Combine(PrefabDir, "Ball.prefab");
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // Asset helpers
    private static Sprite EnsureSprite(string name, System.Action<Texture2D> painter)
    {
        string relTexPath = Path.Combine(SpriteDir, name + ".png");
        string absDir = Path.Combine(Application.dataPath, "Games/Pong/Resources/Pong/Sprites");
        string absPath = Path.Combine(absDir, name + ".png");
        if (!Directory.Exists(absDir)) Directory.CreateDirectory(absDir);
        if (!File.Exists(absPath))
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            painter(tex);
            File.WriteAllBytes(absPath, tex.EncodeToPNG());
        }
        AssetDatabase.ImportAsset(relTexPath, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(relTexPath);
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64f;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(relTexPath);
    }

    private static void FillRect(Texture2D tex, Color color)
    {
        var cols = new Color[tex.width * tex.height];
        for (int i = 0; i < cols.Length; i++) cols[i] = color;
        tex.SetPixels(cols);
        tex.Apply();
    }

    private static void DrawCircleSprite(Texture2D tex)
    {
        int w = tex.width, h = tex.height;
        var center = new Vector2((w - 1) * 0.5f, (h - 1) * 0.5f);
        float r = Mathf.Min(center.x, center.y) - 1f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                bool inside = d <= r;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }
        tex.Apply();
    }

    private static PhysicsMaterial2D EnsurePhysicsMaterial2D(string name, float friction, float bounciness)
    {
        string path = Path.Combine(PrefabDir, name + ".physicsMaterial2D");
        if (!File.Exists(path))
        {
            var mat = new PhysicsMaterial2D(name) { friction = friction, bounciness = bounciness };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
        return AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
    }
}
