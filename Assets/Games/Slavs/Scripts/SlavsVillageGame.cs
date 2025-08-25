using UnityEngine;

// Very lightweight, KoDP-inspired stub for a historical Slavic village (Poland) demo.
// Shows a text-centric decision screen with basic resources and a few choices.
public class SlavsVillageGame : MonoBehaviour
{
    private enum GameScreen
    {
        SpringPlanning,
        Result,
        Event,
        EventResult,
        Transition
    }

    [SerializeField] private int _population = 85;    // villagers including families
    [SerializeField] private int _warriors = 18;       // able-bodied militia/warband
    [SerializeField] private int _foodStores = 320;    // measures of grain/food
    [SerializeField] private int _livestock = 45;      // head of cattle/pigs/goats
    [SerializeField] private int _prestige = 10;       // standing among neighbors
    [SerializeField] private int _faith = 12;          // community piety/ritual vigor

    private string _season = "Early Spring";          // sowing season
    private GameScreen _screen = GameScreen.SpringPlanning;
    private int _lastChoice = 0;           // last planning choice (1..3)
    private int _lastEventChoice = 0;      // last event choice (1..3)
    [SerializeField] private int _luck = 0;            // -10..+10 influences season mood

    private bool _initialized;
    private SlavsEvent _currentEvent;
    private readonly System.Collections.Generic.Queue<SlavsEvent> _deck = new System.Collections.Generic.Queue<SlavsEvent>();
    private string _eventDeltaSummary;

    public int Population => _population;
    public int Warriors => _warriors;
    public int FoodStores => _foodStores;
    public int Livestock => _livestock;
    public int Prestige => _prestige;
    public int Faith => _faith;
    public string Season => _season;
    public bool IsResultScreen => _screen == GameScreen.Result;
    public bool IsEventScreen => _screen == GameScreen.Event;
    public bool IsEventResultScreen => _screen == GameScreen.EventResult;
    public bool IsTransitionScreen => _screen == GameScreen.Transition;
    public SlavsEvent CurrentEvent => _currentEvent;
    public string TransitionText => _transitionText;
    public string GetEventDeltaSummary() => _eventDeltaSummary;

    public void Choose(int option)
    {
        EnsureInit();
        if (_screen != GameScreen.SpringPlanning) return;
        _lastChoice = option;

        // Apply base planning effects
        switch (option)
        {
            case 1: // Prioritize rye and barley sowing
                _foodStores -= 30; // seed and rations during fieldwork
                _prestige += 1;    // diligence respected
                break;
            case 2: // Komobranie: communal labor to shore palisade
                _livestock -= 2;   // wood-hauling costs and feasting
                _prestige += 2;    // shows strength and unity
                _faith += 1;       // blessing rite around the gate
                break;
            case 3: // Send emissaries to neighbor for trade/peace
                _foodStores -= 10; // gifts
                _prestige += 3;    // diplomacy
                break;
        }

        if (TryTriggerEvent())
        {
            _screen = GameScreen.Event;
        }
        else
        {
            _screen = GameScreen.Result;
        }
    }

    public void ResetPlanning()
    {
        EnsureInit();
        StartTransition();
    }

    private void EnsureInit()
    {
        if (_initialized) return;
        BuildInitialDeck();
        NextTurn();
        _initialized = true;
    }

    private void BuildInitialDeck()
    {
        var e = new SlavsEvent
        {
            Id = "spring_wiec_01",
            Title = "Stranger at the Palisade",
            Body = "A trader stops at our gate, asking leave to enter. He bears salt and fine knives. He asks for grain and honey.",
            ImageTint = new Color(0.35f, 0.30f, 0.22f, 1f),
            AutoAdvance = true
        };

        e.Options.Add(new SlavsEventOption
        {
            Label = "Let him in; barter fairly",
            ResultText = "Salt and knives change hands; our folk are pleased.",
            Apply = g => { g._foodStores -= 12; g._prestige += 1; g._luck += 1; }
        });
        e.Options.Add(new SlavsEventOption
        {
            Label = "Demand a toll at the gate",
            ResultText = "He pays grudgingly. The village gains, but tongues wag.",
            Apply = g => { g._foodStores += 6; g._prestige -= 1; g._luck -= 1; }
        });
        e.Options.Add(new SlavsEventOption
        {
            Label = "Turn him away; keep watch",
            ResultText = "No trouble comes, but some say we might have profited.",
            Apply = g => { g._prestige += 0; }
        });

        _deck.Enqueue(e);
    }

    private void NextTurn()
    {
        if (_deck.Count == 0)
        {
            // Simple year progression placeholder
            _season = _season == "Early Spring" ? "Late Spring" : "Early Spring";
            BuildInitialDeck();
        }
        _currentEvent = _deck.Dequeue();
    }

    private bool TryTriggerEvent()
    {
        // For now, always trigger the current queued event after planning
        return _currentEvent != null;
    }

    public void EventChoose(int option)
    {
        if (_screen != GameScreen.Event || _currentEvent == null) return;
        // Snapshot before
        var p0 = _population; var w0 = _warriors; var f0 = _foodStores; var l0 = _livestock; var pr0 = _prestige; var fa0 = _faith; var luck0 = _luck;
        _lastEventChoice = option;
        var idx = Mathf.Clamp(option - 1, 0, _currentEvent.Options.Count - 1);
        var opt = _currentEvent.Options[idx];
        opt.Apply?.Invoke(this);
        // Compose delta summary
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        void AddDelta(string name, int before, int after)
        {
            var d = after - before;
            if (d != 0)
            {
                if (sb.Length > 0) sb.Append(" \u2022 ");
                sb.Append(name).Append(' ').Append(d > 0 ? "+" : "").Append(d);
            }
        }
        AddDelta("Food", f0, _foodStores);
        AddDelta("Livestock", l0, _livestock);
        AddDelta("Prestige", pr0, _prestige);
        AddDelta("Faith", fa0, _faith);
        AddDelta("Luck", luck0, _luck);
        _eventDeltaSummary = sb.Length > 0 ? sb.ToString() : "No notable changes.";
        _screen = GameScreen.EventResult;
    }

    public void EndEvent()
    {
        if (_screen != GameScreen.EventResult) return;
        // Advance turn after an event, but show transition first
        StartTransition();
    }
    public string GetResultText()
    {
        // Planning result text
        switch (_lastChoice)
        {
            case 1:
                return "Families work the strips under openfield custom. Seed is dear, but a good harvest will feed winter.";
            case 2:
                return "Men and women raise stakes and pack earth. The gate is blessed; strangers will think twice before trying it.";
            case 3:
                return "Our messengers trade salt and honey, returning with tidings and goodwill. Paths may be safer this year.";
            default:
                return "Time passes quietly.";
        }
    }

    public string GetEventResultText()
    {
        if (_currentEvent == null) return "";
        var idx = Mathf.Clamp(_lastEventChoice - 1, 0, _currentEvent.Options.Count - 1);
        return _currentEvent.Options[idx].ResultText;
    }

    // --- Season transition handling ---
    private string _transitionText;

    public void StartTransition()
    {
        // Build poetic transition text based on next season and luck
        var next = GetNextSeasonName();
        _transitionText = ComposeSeasonText(next, _luck);
        _screen = GameScreen.Transition;
        // Nudge luck back toward neutral a bit over time
        if (_luck > 0) _luck -= 1; else if (_luck < 0) _luck += 1;
    }

    public void AdvanceTurn()
    {
        NextTurn();
        _screen = GameScreen.SpringPlanning;
    }

    public string GetNextSeasonName()
    {
        if (_season == "Early Spring") return "Late Spring";
        if (_season == "Late Spring") return "Summer";
        if (_season == "Summer") return "Autumn";
        if (_season == "Autumn") return "Early Spring";
        return "Spring";
    }

    private static string ComposeSeasonText(string next, int luck)
    {
        // Simple mood lines; later we can branch by resources or events
        var good = luck >= 2;
        var bad = luck <= -2;
        switch (next)
        {
            case "Late Spring":
                return good ? "The rains ease and the rye greens thickly. Late spring smiles upon our fields." :
                       bad ? "Cold winds linger; shoots shiver in the furrows. Late spring asks patience." :
                             "Frost fades from the ditches. Late spring draws near, steady and unremarked.";
            case "Summer":
                return good ? "Warm days and long light. Bees hum over clover; summer ripens our hope." :
                       bad ? "Heat beats harshly and clouds pass us by. Summer tests our stores." :
                             "The sun stands high, and days stretch easy. Summer comes in its time.";
            case "Autumn":
                return good ? "Granaries swell and the smoke is sweet. Autumn brings a kindly reckoning." :
                       bad ? "Thin shocks in the fields, and knives are few. Autumn tightens its belt." :
                             "Leaves turn and carts creak homeward. Autumn settles over the village.";
            default: // Early Spring again
                return good ? "Snowmelt sings under the planks. Early spring lifts hearts and hands to work." :
                       bad ? "Mud clings and seed is light. Early spring comes with a sigh." :
                             "The ice loosens its grip. Early spring returns, as it always does.";
        }
    }
}
