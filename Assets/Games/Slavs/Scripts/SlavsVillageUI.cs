using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

// Runtime UGUI builder for a simple card-driven event panel.
// Creates Canvas + EventSystem if missing and binds to SlavsVillageGame.
public class SlavsVillageUI : MonoBehaviour
{
    [SerializeField] private SlavsVillageGame _game;

    // Planning screen (management) root + refs
    private GameObject _planningRoot;
    private Text _title;
    private Text _season;
    private Text _ledger;
    private Text _cardTitle;
    private Text _cardBody;
    private Button _btn1;
    private Button _btn2;
    private Button _btn3;
    private Button _continueBtn;

    // Event screen (full image + bottom options)
    private GameObject _eventRoot;
    private RawImage _eventImage;
    private Button _eventBtn1;
    private Button _eventBtn2;
    private Button _eventBtn3;
    private Button _eventContinue;
    private SlavsCard3D _card3D;
    private GameObject _transitionRoot;
    private Text _transitionText;
    private Coroutine _transitionCo;
    private Coroutine _autoAdvanceCo;
    private bool _didPlanningFlip;
    private bool _didEventFlip;
    private bool _scheduledPlanningAdvance;
    private bool _scheduledEventAdvance;

    private void Awake()
    {
        if (_game == null)
        {
            _game = GetComponent<SlavsVillageGame>();
        }
        if (_game == null)
        {
#if UNITY_2022_2_OR_NEWER
            _game = FindFirstObjectByType<SlavsVillageGame>();
#else
            _game = FindObjectOfType<SlavsVillageGame>();
#endif
        }

        EnsureEventSystem();
        BuildCanvasAndUI();
        BindPlanningButtons();
        RenderPlanning();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (_game.IsEventScreen)
            {
                // Event choices
                if (kb.digit1Key.wasPressedThisFrame) _game.EventChoose(1);
                if (kb.digit2Key.wasPressedThisFrame) _game.EventChoose(2);
                if (kb.digit3Key.wasPressedThisFrame) _game.EventChoose(3);
            }
            else if (_game.IsEventResultScreen)
            {
                // allow Enter to skip to transition
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                    _game.EndEvent();
            }
            else if (!_game.IsResultScreen)
            {
                if (kb.digit1Key.wasPressedThisFrame) _game.Choose(1);
                if (kb.digit2Key.wasPressedThisFrame) _game.Choose(2);
                if (kb.digit3Key.wasPressedThisFrame) _game.Choose(3);
            }
            else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                _game.StartTransition();
            }
        }
#else
        if (_game.IsEventScreen)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) _game.EventChoose(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _game.EventChoose(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) _game.EventChoose(3);
        }
        else if (_game.IsEventResultScreen && Input.GetKeyDown(KeyCode.Return))
        {
            _game.EndEvent();
        }
        else if (!_game.IsResultScreen)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) _game.Choose(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _game.Choose(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) _game.Choose(3);
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            _game.StartTransition();
        }
#endif

        // Refresh UI text each frame (simple for now)
        RefreshLedger();
        _season.text = _game.Season;
        if (_eventRoot != null) _eventRoot.SetActive(_game.IsEventScreen || _game.IsEventResultScreen);
        if (_planningRoot != null) _planningRoot.SetActive(!_game.IsEventScreen && !_game.IsEventResultScreen && !_game.IsTransitionScreen);
        if (_transitionRoot != null) _transitionRoot.SetActive(_game.IsTransitionScreen);

        if (_game.IsTransitionScreen)
        {
            ShowTransition();
            return;
        }

        if (_game.IsEventScreen)
        {
            RenderEvent();
        }
        else if (_game.IsEventResultScreen)
        {
            ShowEventResult();
        }
        else if (_game.IsResultScreen)
        {
            ShowResult();
        }
        else
        {
            // reset flip guards when not in result screens
            _didPlanningFlip = false;
            _didEventFlip = false;
            _scheduledPlanningAdvance = false;
            _scheduledEventAdvance = false;
        }

        // Toggle screen roots for clarity
        if (_planningRoot != null) _planningRoot.SetActive(!_game.IsEventScreen && !_game.IsEventResultScreen && !_game.IsTransitionScreen);
        if (_eventRoot != null) _eventRoot.SetActive(_game.IsEventScreen || _game.IsEventResultScreen);
        if (_transitionRoot != null) _transitionRoot.SetActive(_game.IsTransitionScreen);
    }

    private void BindPlanningButtons()
    {
        _btn1.onClick.RemoveAllListeners();
        _btn2.onClick.RemoveAllListeners();
        _btn3.onClick.RemoveAllListeners();
        _continueBtn.onClick.RemoveAllListeners();

        _btn1.onClick.AddListener(() => _game.Choose(1));
        _btn2.onClick.AddListener(() => _game.Choose(2));
        _btn3.onClick.AddListener(() => _game.Choose(3));
        _continueBtn.onClick.AddListener(RestartPlanning);
    }

    private void RestartPlanning()
    {
        // Now acts as a skip into transition; rendering handled by Update
        _game.ResetPlanning();
    }

    private void RenderPlanning()
    {
        _title.text = "Slavic Village — Poland, ca. 9th c.";
        _season.text = _game.Season;
        RefreshLedger();

        _cardTitle.text = "Early Spring: Setting the Year";
        _cardBody.text = "The frost retreats and the fields soften. The village gathers before the wiec.\n" +
                         "Our kin must set priorities for the season: planting, defense, or diplomacy.\n\n" +
                         "How shall we guide our people?";
        // planning screen does not show the 3D card image; keep focus on text and ledger
        // Planning labels
        SetButtonLabel(_btn1, "1) Prioritize sowing rye and barley");
        SetButtonLabel(_btn2, "2) Komobranie: shore the palisade together");
        SetButtonLabel(_btn3, "3) Send emissaries to a neighboring tribe");
        BindPlanningButtons();
        _btn1.gameObject.SetActive(true);
        _btn2.gameObject.SetActive(true);
        _btn3.gameObject.SetActive(true);
        _continueBtn.gameObject.SetActive(false);
    }

    private void ShowResult()
    {
        _btn1.gameObject.SetActive(false);
        _btn2.gameObject.SetActive(false);
        _btn3.gameObject.SetActive(false);
        _cardTitle.text = "Outcome";
        _cardBody.text = _game.GetResultText();
        if (!_didPlanningFlip)
        {
            if (_card3D != null) _card3D.Flip();
            _didPlanningFlip = true;
        }
        _continueBtn.gameObject.SetActive(false);
        if (!_scheduledPlanningAdvance)
        {
            StartAutoAdvance(() => _game.StartTransition());
            _scheduledPlanningAdvance = true;
        }
    }

    private void RenderEvent()
    {
        if (_eventRoot == null) return;
        var ev = _game.CurrentEvent;
        EnsureCard3D();
        if (_eventImage != null) _eventImage.texture = _card3D.Texture;
        if (ev != null)
        {
            _card3D.SetTint(ev.ImageTint);
            SetOptionLabel(_eventBtn1, 0);
            SetOptionLabel(_eventBtn2, 1);
            SetOptionLabel(_eventBtn3, 2);
        }
        _eventBtn1.onClick.RemoveAllListeners();
        _eventBtn2.onClick.RemoveAllListeners();
        _eventBtn3.onClick.RemoveAllListeners();
        _eventContinue.onClick.RemoveAllListeners();
        _eventBtn1.onClick.AddListener(() => _game.EventChoose(1));
        _eventBtn2.onClick.AddListener(() => _game.EventChoose(2));
        _eventBtn3.onClick.AddListener(() => _game.EventChoose(3));
        _eventContinue.gameObject.SetActive(false);
        _eventBtn1.gameObject.SetActive(true);
        _eventBtn2.gameObject.SetActive(true);
        _eventBtn3.gameObject.SetActive(true);
    }

    private void ShowEventResult()
    {
        if (!_didEventFlip)
        {
            if (_card3D != null) _card3D.Flip();
            _didEventFlip = true;
        }
        if (_eventBtn1 != null) _eventBtn1.gameObject.SetActive(false);
        if (_eventBtn2 != null) _eventBtn2.gameObject.SetActive(false);
        if (_eventBtn3 != null) _eventBtn3.gameObject.SetActive(false);
        if (_eventContinue != null) _eventContinue.gameObject.SetActive(false);
        if (!_scheduledEventAdvance)
        {
            StartAutoAdvance(() => _game.EndEvent());
            _scheduledEventAdvance = true;
        }
    }

    private void ShowTransition()
    {
        if (_transitionRoot == null) return;
        if (_transitionText != null)
        {
            var next = _game.GetNextSeasonName();
            _transitionText.text = next + "\n\n" + _game.TransitionText;
        }
        if (_transitionCo == null)
        {
            _transitionCo = StartCoroutine(TransitionRoutine());
        }
        // allow skipping by Enter
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
        {
            FinishTransition();
        }
#else
        if (Input.GetKeyDown(KeyCode.Return))
        {
            FinishTransition();
        }
#endif
    }

    private System.Collections.IEnumerator TransitionRoutine()
    {
        yield return new WaitForSeconds(4.0f);
        FinishTransition();
    }

    private void FinishTransition()
    {
        if (_transitionCo != null)
        {
            StopCoroutine(_transitionCo);
            _transitionCo = null;
        }
        CancelAutoAdvance();
        _game.AdvanceTurn();
        RenderPlanning();
    }

    private void StartAutoAdvance(System.Action action)
    {
        CancelAutoAdvance();
        _autoAdvanceCo = StartCoroutine(AutoAdvanceRoutine(action));
    }

    private void CancelAutoAdvance()
    {
        if (_autoAdvanceCo != null)
        {
            StopCoroutine(_autoAdvanceCo);
            _autoAdvanceCo = null;
        }
    }

    private System.Collections.IEnumerator AutoAdvanceRoutine(System.Action action)
    {
        yield return new WaitForSeconds(1.5f);
        action?.Invoke();
    }

    private void RefreshLedger()
    {
        _ledger.text =
            $"Village Ledger\n" +
            $"Population: {_game.Population}\n" +
            $"Warriors: {_game.Warriors}\n" +
            $"Food Stores: {_game.FoodStores}\n" +
            $"Livestock: {_game.Livestock}\n" +
            $"Prestige: {_game.Prestige}\n" +
            $"Faith: {_game.Faith}\n\n" +
            $"Institutions\n" +
            $"• Wiec (elders' council)\n" +
            $"• Rod/Clan alliances\n" +
            $"• Seasonal rites (jarilo/green week)";
    }

    private void EnsureEventSystem()
    {
        EventSystem existing = null;
#if UNITY_2022_2_OR_NEWER
        existing = FindFirstObjectByType<EventSystem>();
#else
        existing = FindObjectOfType<EventSystem>();
#endif
        if (existing == null)
        {
            var esGo = new GameObject("EventSystem");
            existing = esGo.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        var old = existing.GetComponent<StandaloneInputModule>();
        if (old != null)
        {
            Destroy(old);
        }
        if (existing.GetComponent<InputSystemUIInputModule>() == null)
        {
            existing.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (existing.GetComponent<StandaloneInputModule>() == null)
        {
            existing.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }

    private void BuildCanvasAndUI()
    {
        var canvasGo = new GameObject("UI Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Planning root contains status + actions; hidden during events/transitions
        _planningRoot = CreatePanel(
            canvasGo.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(0f, 0f), new Vector2(0f, 0f)
        );

        // Top bar (full width minus margins), height 80
        var top = CreatePanel(
            _planningRoot.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(16f, -80f - 16f), new Vector2(-16f, -16f)
        );
        _title = CreateText(top.transform, "Slavic Village", 20, TextAnchor.MiddleLeft, new RectOffset(16, 0, 0, 0));
        _season = CreateText(top.transform, "", 14, TextAnchor.MiddleRight, new RectOffset(0, 16, 0, 0));

        // Left ledger: fixed width 260, below top bar, stretch vertical with margins
        var left = CreatePanel(
            _planningRoot.transform,
            new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(16f, 16f), new Vector2(16f + 260f, -(16f + 80f + 8f))
        );
        _ledger = CreateText(left.transform, "", 14, TextAnchor.UpperLeft, new RectOffset(10, 10, 10, 10));

        // Right panel with the planning narrative + buttons
        var mainGo = CreatePanel(
            _planningRoot.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(16f + 260f + 8f, 16f), new Vector2(-16f, -(16f + 80f + 8f))
        );
        var main = mainGo;
        _cardTitle = CreateText(main.transform, "", 18, TextAnchor.UpperLeft, new RectOffset(12, 12, 12, 0));

        // (Planning view does not show a RenderTexture image)

        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(main.transform, false);
        _cardBody = bodyGo.AddComponent<Text>();
        _cardBody.font = GetDefaultFont();
        _cardBody.fontSize = 14;
        _cardBody.alignment = TextAnchor.UpperLeft;
        var bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0.47f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(12f, 12f);
        bodyRt.offsetMax = new Vector2(-12f, -120f);

        // Three choice buttons along the bottom area (height ~36)
        _btn1 = CreateButton(
            main.transform,
            "1) Prioritize sowing rye and barley",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(12f, 92f), new Vector2(-12f, 128f)
        );
        _btn2 = CreateButton(
            main.transform,
            "2) Komobranie: shore the palisade together",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(12f, 52f), new Vector2(-12f, 88f)
        );
        _btn3 = CreateButton(
            main.transform,
            "3) Send emissaries to a neighboring tribe",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(12f, 12f), new Vector2(-12f, 48f)
        );

        _continueBtn = CreateButton(main.transform, "Continue (next season)", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-212f, 12f), new Vector2(-12f, 48f));
        _continueBtn.gameObject.SetActive(false);

        // Event root (fullscreen image + bottom options)
        _eventRoot = CreatePanel(
            canvasGo.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        var eventImageGo = new GameObject("EventImage", typeof(RawImage));
        eventImageGo.transform.SetParent(_eventRoot.transform, false);
        _eventImage = eventImageGo.GetComponent<RawImage>();
        var evImgRt = eventImageGo.GetComponent<RectTransform>();
        evImgRt.anchorMin = new Vector2(0f, 0f);
        evImgRt.anchorMax = new Vector2(1f, 1f);
        evImgRt.offsetMin = new Vector2(0f, 0f);
        evImgRt.offsetMax = new Vector2(0f, 0f);
        var bottomBar = CreatePanel(_eventRoot.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 160f));
        var img = bottomBar.GetComponent<Image>();
        if (img != null) img.color = new Color(0f, 0f, 0f, 0.45f);
        _eventBtn1 = CreateButton(bottomBar.transform, "", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 104f), new Vector2(-16f, 140f));
        _eventBtn2 = CreateButton(bottomBar.transform, "", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 64f), new Vector2(-16f, 100f));
        _eventBtn3 = CreateButton(bottomBar.transform, "", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 24f), new Vector2(-16f, 60f));
        _eventContinue = CreateButton(bottomBar.transform, "Continue", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-160f, 24f), new Vector2(-16f, 60f));
        _eventRoot.SetActive(false);

        // Transition overlay (poetic season text)
        _transitionRoot = CreatePanel(
            canvasGo.transform,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        var transBg = _transitionRoot.GetComponent<Image>();
        if (transBg != null) transBg.color = new Color(0f, 0f, 0f, 1f); // solid black to clearly separate screen
        _transitionText = CreateText(_transitionRoot.transform, "", 28, TextAnchor.MiddleCenter, new RectOffset(40, 40, 40, 40));
        _transitionRoot.SetActive(false);
    }

    private void EnsureCard3D()
    {
        if (_card3D != null) return;
        var go = new GameObject("SlavsCard3D");
        _card3D = go.AddComponent<SlavsCard3D>();
    }

    private void SetOptionLabel(Button btn, int index)
    {
        var ev = _game.CurrentEvent;
        var label = (ev != null && ev.Options.Count > index) ? ev.Options[index].Label : "";
        var t = btn.GetComponentInChildren<Text>();
        if (t != null) t.text = $"{index + 1}) {label}";
    }

    private void SetButtonLabel(Button btn, string label)
    {
        var t = btn.GetComponentInChildren<Text>();
        if (t != null) t.text = label;
    }

    private GameObject CreatePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject("Panel", typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.35f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        return go;
    }

    private Text CreateText(Transform parent, string text, int fontSize, TextAnchor anchor, RectOffset padding)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = GetDefaultFont();
        t.text = text; t.fontSize = fontSize; t.alignment = anchor;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(padding.left, padding.bottom);
        rt.offsetMax = new Vector2(-padding.right, -padding.top);
        return t;
    }

    private Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject("Button", typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.1f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var t = textGo.AddComponent<Text>();
        t.font = GetDefaultFont();
        t.text = label; t.fontSize = 14; t.alignment = TextAnchor.MiddleCenter;
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(10f, 6f);
        trt.offsetMax = new Vector2(-10f, -6f);

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.2f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.3f);
        btn.colors = colors;
        return btn;
    }

    private static Font GetDefaultFont()
    {
        // Unity 2022+ removed Arial.ttf as a builtin; LegacyRuntime.ttf is the new default.
        // Try LegacyRuntime first, then fall back to Arial for older versions.
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null)
        {
            try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        }
        return f;
    }
}
