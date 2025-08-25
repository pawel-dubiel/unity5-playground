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
    private SlavsEventBase _currentEvent;
    private SlavsEventBase _pendingEvent;
    private readonly System.Collections.Generic.List<SlavsEventBase> _scheduled = new System.Collections.Generic.List<SlavsEventBase>();
    private readonly System.Collections.Generic.List<SlavsEventBase> _catalog = new System.Collections.Generic.List<SlavsEventBase>();
    private int _turnIndex;
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
    public SlavsEventBase CurrentEvent => _currentEvent;
    public int TurnIndex => _turnIndex;
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
        BuildCatalog();
        NextTurn();
        _initialized = true;
    }

    private void BuildCatalog()
    {
        _catalog.Clear();
        _catalog.Add(new TraderAtGateEvent());
    }

    private void NextTurn()
    {
        _turnIndex++;
        _season = GetNextSeasonName();
        _pendingEvent = null;
        // Check scheduled continuations first
        for (int i = _scheduled.Count - 1; i >= 0; i--)
        {
            if (_scheduled[i].NextStageTurn <= _turnIndex)
            {
                _pendingEvent = _scheduled[i];
                _scheduled.RemoveAt(i);
                _pendingEvent.OnContinue(this);
                break;
            }
        }
        // Otherwise pick by triggers
        if (_pendingEvent == null)
        {
            var snap = new SlavsGameSnapshot(_turnIndex, _season, _population, _warriors, _foodStores, _livestock, _prestige, _faith, _luck);
            foreach (var ev in _catalog)
            {
                if (ev.CanTrigger(snap)) { _pendingEvent = ev; break; }
            }
        }
    }

    private bool TryTriggerEvent()
    {
        if (_pendingEvent == null) return false;
        _currentEvent = _pendingEvent;
        _currentEvent.OnBegin(this);
        _pendingEvent = null;
        return true;
    }

    public void EventChoose(int option)
    {
        if (_screen != GameScreen.Event || _currentEvent == null) return;
        // Snapshot before
        var p0 = _population; var w0 = _warriors; var f0 = _foodStores; var l0 = _livestock; var pr0 = _prestige; var fa0 = _faith; var luck0 = _luck;
        _lastEventChoice = option;
        _currentEvent.OnChoose(option - 1, this, out _eventResultText, out var afterTurns);
        if (afterTurns > 0)
        {
            _currentEvent.ScheduleContinuation(_turnIndex + afterTurns);
            _scheduled.Add(_currentEvent);
        }
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

    private string _eventResultText;
    public string GetEventResultText() => _eventResultText ?? string.Empty;

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

    // Convenience mutators for events
    public void ModifyFood(int delta) { _foodStores += delta; }
    public void ModifyPrestige(int delta) { _prestige += delta; }
    public void ModifyLuck(int delta) { _luck += delta; }

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
