using UnityEngine;

public class TraderAtGateEvent : SlavsEventBase
{
    public override string Id => "trader_at_gate";
    public override string ImageResourcePath => "Slavs/Images/Trader"; // Optional; provide PNG under Resources/Slavs/Images

    public override bool CanTrigger(SlavsGameSnapshot s)
    {
        // Trigger in spring, not more often than every 3 turns
        bool spring = s.Season.Contains("Spring");
        return spring && (s.TurnIndex - LastTriggeredTurn) >= 3;
    }

    public override void OnBegin(SlavsVillageGame game)
    {
        base.OnBegin(game);
        Title = "Stranger at the Palisade";
        Body = "A trader stops at our gate, asking leave to enter. He bears salt and fine knives. He asks for grain and honey.";
        Options.Add(new SlavsEventOption { Label = "Let him in; barter fairly", ResultText = "Salt and knives change hands; our folk are pleased.", Apply = g => { g.ModifyFood(-12); g.ModifyPrestige(+1); g.ModifyLuck(+1); } });
        Options.Add(new SlavsEventOption { Label = "Demand a toll at the gate", ResultText = "He pays grudgingly. The village gains, but tongues wag.", Apply = g => { g.ModifyFood(+6); g.ModifyPrestige(-1); g.ModifyLuck(-1); } });
        Options.Add(new SlavsEventOption { Label = "Turn him away; keep watch", ResultText = "No trouble comes, but some say we might have profited.", Apply = g => { } });
    }

    public override void OnChoose(int optionIndex, SlavsVillageGame game, out string resultText, out int continueAfterTurns)
    {
        optionIndex = Mathf.Clamp(optionIndex, 0, Options.Count - 1);
        var opt = Options[optionIndex];
        opt.Apply?.Invoke(game);
        resultText = opt.ResultText;
        IsActive = false;
        continueAfterTurns = 0; // no continuation by default
    }
}

