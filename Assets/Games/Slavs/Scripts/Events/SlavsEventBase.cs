using System.Collections.Generic;
using UnityEngine;

public abstract class SlavsEventBase
{
    public abstract string Id { get; }
    public virtual string Title { get; protected set; }
    public virtual string Body { get; protected set; }
    public virtual string ImageResourcePath => null; // e.g., "Slavs/Images/Trader"
    public List<SlavsEventOption> Options { get; } = new List<SlavsEventOption>();

    public int LastTriggeredTurn { get; protected set; } = -9999;
    public bool IsActive { get; protected set; }
    public int NextStageTurn { get; protected set; } = -1; // >=0 means continuation scheduled

    public virtual bool CanTrigger(SlavsGameSnapshot snapshot)
    {
        return true;
    }

    public virtual void OnBegin(SlavsVillageGame game)
    {
        IsActive = true;
        LastTriggeredTurn = game.TurnIndex;
        Options.Clear();
    }

    // Apply choice; return result text and continuation delay (turns). If delay > 0, schedule continuation.
    public abstract void OnChoose(int optionIndex, SlavsVillageGame game, out string resultText, out int continueAfterTurns);

    public virtual void OnContinue(SlavsVillageGame game)
    {
        // Default: no multi-stage logic.
        IsActive = true;
        NextStageTurn = -1;
    }

    public void ScheduleContinuation(int turnIndex)
    {
        NextStageTurn = turnIndex;
        IsActive = false;
    }

    public Texture GetImageTexture()
    {
        if (!string.IsNullOrEmpty(ImageResourcePath))
        {
            var tex = Resources.Load<Texture2D>(ImageResourcePath);
            if (tex != null) return tex;
        }
        // Placeholder 1x1 texture
        var rt = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        rt.SetPixel(0, 0, new Color(0.35f, 0.3f, 0.22f, 1f));
        rt.Apply();
        return rt;
    }
}
