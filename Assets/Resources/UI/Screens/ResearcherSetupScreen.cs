using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

// Researcher-only screen shown before Home on first launch, and reachable
// at any time via a 3-finger, 3-second hold (see NavigationManager).
// Participants are not expected to see this during a normal session.
//
// Condition is locked to A (Dynamic AI) for this build — mirrors the
// structure of the Fixed Narrative build's version of this screen, which
// locks to B instead.
public class ResearcherSetupScreen
{
    readonly VisualElement _screen;

    TextField _participantIdField;
    Button    _btnConditionA;
    Button    _btnConditionB;
    Button    _btnBeginSession;

    Label _rowParticipantIcon;
    Label _rowConditionIcon;
    Label _rowApiIcon;
    Label _rowApiStatusText;
    Label _rowArIcon;
    Label _rowArStatusText;

    Label _apiWarning;

    bool _geminiChecking = true;
    bool _geminiOk;
    bool _groqChecking   = true;
    bool _groqOk;
    bool _arPermissionGranted = false;

    public ResearcherSetupScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        root.Add(_screen);

        var scroll = new ScrollView();
        scroll.style.position     = Position.Absolute;
        scroll.style.top          = 60;
        scroll.style.left         = 0;
        scroll.style.right        = 0;
        scroll.style.bottom       = 100;
        scroll.style.paddingLeft  = 24;
        scroll.style.paddingRight = 24;
        scroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        // ── Title ──────────────────────────────────────────────────────────
        var title = new Label { text = "AFTERGLOW — RESEARCH SETUP" };
        title.style.fontSize                = 18;
        title.style.color                   = new Color(0.4f, 0.7f, 1.0f, 0.95f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.letterSpacing           = 2;
        title.style.whiteSpace              = WhiteSpace.Normal;
        title.style.unityTextAlign          = TextAnchor.MiddleCenter;
        title.style.width                   = Length.Percent(100);
        title.style.marginTop               = 20;
        scroll.Add(title);

        scroll.Add(UIHelper.Divider());

        // ── Participant ID ────────────────────────────────────────────────
        scroll.Add(SectionLabel("PARTICIPANT ID"));

        _participantIdField = new TextField { value = "" };
        StyleField(_participantIdField);
        _participantIdField.RegisterValueChangedCallback(
            _ => RefreshBeginButtonState());
        scroll.Add(_participantIdField);

#if UNITY_EDITOR
        // Editor-only fallback: the scene's EventSystem/UI panel routing has
        // been unreliable for native focus-driven typing during testing.
        // This reads the physical keyboard directly (bypassing panel/focus
        // entirely) so the field is always usable in the Editor. Real
        // Android builds never take this path — the native TextField keeps
        // its normal touch/soft-keyboard behaviour there, untouched.
        _participantIdField.focusable  = false;
        _participantIdField.pickingMode = PickingMode.Ignore;
        SetupEditorKeyboardFallback();
#endif

        // ── Condition ──────────────────────────────────────────────────────
        scroll.Add(SectionLabel("CONDITION"));

        var conditionRow = new VisualElement();
        conditionRow.style.flexDirection  = FlexDirection.Row;
        conditionRow.style.justifyContent = Justify.SpaceBetween;
        conditionRow.style.marginBottom   = 20;

        _btnConditionA = MakeConditionButton("A — DYNAMIC AI");

        _btnConditionB = MakeConditionButton("B — FIXED");
        _btnConditionB.SetEnabled(false);

        conditionRow.Add(_btnConditionA);
        conditionRow.Add(_btnConditionB);
        scroll.Add(conditionRow);

        // ── Before you begin checklist ───────────────────────────────────
        scroll.Add(SectionLabel("BEFORE YOU BEGIN"));

        var checklist = new VisualElement();
        checklist.style.backgroundColor         = UIHelper.Surface;
        checklist.style.borderTopColor          = UIHelper.Border;
        checklist.style.borderBottomColor       = UIHelper.Border;
        checklist.style.borderLeftColor         = UIHelper.Border;
        checklist.style.borderRightColor        = UIHelper.Border;
        checklist.style.borderTopWidth          = 1;
        checklist.style.borderBottomWidth       = 1;
        checklist.style.borderLeftWidth         = 1;
        checklist.style.borderRightWidth        = 1;
        checklist.style.borderTopLeftRadius     = 2;
        checklist.style.borderTopRightRadius    = 2;
        checklist.style.borderBottomLeftRadius  = 2;
        checklist.style.borderBottomRightRadius = 2;
        checklist.style.paddingTop              = 12;
        checklist.style.paddingBottom           = 12;
        checklist.style.paddingLeft             = 16;
        checklist.style.paddingRight            = 16;
        checklist.style.marginBottom            = 24;

        var (rowA, iconA) = MakeChecklistRow("Participant ID entered");
        _rowParticipantIcon = iconA;
        checklist.Add(rowA);

        var (rowB, iconB) = MakeChecklistRow("Condition selected");
        _rowConditionIcon = iconB;
        checklist.Add(rowB);

        var (rowC, iconC, statusC) = MakeChecklistRowWithStatus("API PRIMARY");
        _rowApiIcon       = iconC;
        _rowApiStatusText = statusC;
        checklist.Add(rowC);

        _apiWarning = new Label { text = "" };
        _apiWarning.style.fontSize      = 9;
        _apiWarning.style.color         = new Color(0.7f, 0.3f, 0.3f, 1.0f);
        _apiWarning.style.whiteSpace    = WhiteSpace.Normal;
        _apiWarning.style.marginTop     = -4;
        _apiWarning.style.marginBottom  = 10;
        _apiWarning.style.display       = DisplayStyle.None;
        checklist.Add(_apiWarning);

        var (rowD, iconD, statusD) = MakeChecklistRowWithStatus("AR camera permission");
        _rowArIcon       = iconD;
        _rowArStatusText = statusD;
        checklist.Add(rowD);

        var (rowE, iconE) = MakeChecklistRow("This version: AI Narrative");
        iconE.text  = "✓";
        iconE.style.color = new Color(0.3f, 0.7f, 0.4f, 1.0f);
        checklist.Add(rowE);

        scroll.Add(checklist);

        _screen.Add(scroll);

        // ── Begin session — pinned to the bottom so it's always reachable,
        // even when the checklist content overflows the screen height. ────
        var bottomBlock = new VisualElement();
        bottomBlock.style.position        = Position.Absolute;
        bottomBlock.style.bottom          = 0;
        bottomBlock.style.left            = 0;
        bottomBlock.style.right           = 0;
        bottomBlock.style.paddingLeft     = 24;
        bottomBlock.style.paddingRight    = 24;
        bottomBlock.style.paddingTop      = 14;
        bottomBlock.style.paddingBottom   = 36;
        bottomBlock.style.backgroundColor = UIHelper.BG;
        bottomBlock.style.borderTopColor  = UIHelper.Border;
        bottomBlock.style.borderTopWidth  = 1;

        _btnBeginSession = new Button();
        _btnBeginSession.style.width             = Length.Percent(100);
        _btnBeginSession.style.paddingTop        = 16;
        _btnBeginSession.style.paddingBottom     = 16;
        _btnBeginSession.style.backgroundColor   = Color.clear;
        _btnBeginSession.style.borderTopColor    = new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnBeginSession.style.borderBottomColor = new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnBeginSession.style.borderLeftColor   = new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnBeginSession.style.borderRightColor  = new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnBeginSession.style.borderTopWidth    = 1;
        _btnBeginSession.style.borderBottomWidth = 1;
        _btnBeginSession.style.borderLeftWidth   = 1;
        _btnBeginSession.style.borderRightWidth  = 1;
        _btnBeginSession.style.borderTopLeftRadius     = 2;
        _btnBeginSession.style.borderTopRightRadius    = 2;
        _btnBeginSession.style.borderBottomLeftRadius  = 2;
        _btnBeginSession.style.borderBottomRightRadius = 2;

        var beginLabel = new Label { text = "BEGIN SESSION" };
        beginLabel.style.fontSize       = 12;
        beginLabel.style.color          = new Color(0.4f, 0.7f, 1.0f, 0.9f);
        beginLabel.style.letterSpacing  = 3;
        beginLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        beginLabel.style.width          = Length.Percent(100);

        _btnBeginSession.Add(beginLabel);
        _btnBeginSession.clicked += OnBeginSession;
        bottomBlock.Add(_btnBeginSession);

        _screen.Add(bottomBlock);

        SelectCondition();
        RunApiChecks();
        RunArPermissionCheck();
        RefreshBeginButtonState();

        SetVisible(false);
    }

    // ── Checklist helpers ──────────────────────────────────────────────────

    Label SectionLabel(string text)
    {
        var l = new Label { text = text };
        l.style.fontSize      = 9;
        l.style.color         = new Color(0.4f, 0.7f, 1.0f, 0.7f);
        l.style.letterSpacing = 4;
        l.style.marginTop     = 20;
        l.style.marginBottom  = 10;
        return l;
    }

    void StyleField(TextField field)
    {
        field.style.backgroundColor   = UIHelper.Surface;
        field.style.borderTopColor    = new Color(0.4f, 0.7f, 1.0f, 0.3f);
        field.style.borderBottomColor = new Color(0.4f, 0.7f, 1.0f, 0.3f);
        field.style.borderLeftColor   = new Color(0.4f, 0.7f, 1.0f, 0.3f);
        field.style.borderRightColor  = new Color(0.4f, 0.7f, 1.0f, 0.3f);
        field.style.borderTopWidth    = 1;
        field.style.borderBottomWidth = 1;
        field.style.borderLeftWidth   = 1;
        field.style.borderRightWidth  = 1;
        field.style.borderTopLeftRadius     = 2;
        field.style.borderTopRightRadius    = 2;
        field.style.borderBottomLeftRadius  = 2;
        field.style.borderBottomRightRadius = 2;
        field.style.color        = UIHelper.TextPrim;
        field.style.fontSize     = 13;
        field.style.marginBottom = 8;
        field.RegisterCallback<GeometryChangedEvent>(_ => {
            var input = field.Q<VisualElement>("unity-text-input");
            if (input != null)
                input.style.backgroundColor = UIHelper.Surface;
        });
    }

    Button MakeConditionButton(string label)
    {
        var btn = new Button();
        btn.style.width             = Length.Percent(48);
        btn.style.paddingTop        = 14;
        btn.style.paddingBottom     = 14;
        btn.style.backgroundColor   = Color.clear;
        btn.style.borderTopWidth    = 1;
        btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth   = 1;
        btn.style.borderRightWidth  = 1;
        btn.style.borderTopLeftRadius     = 2;
        btn.style.borderTopRightRadius    = 2;
        btn.style.borderBottomLeftRadius  = 2;
        btn.style.borderBottomRightRadius = 2;

        var lbl = new Label { text = label };
        lbl.style.fontSize       = 10;
        lbl.style.letterSpacing  = 1;
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        lbl.style.whiteSpace     = WhiteSpace.Normal;
        lbl.name = "ConditionLabel";
        btn.Add(lbl);

        return btn;
    }

    // Condition is fixed for this build — A is always selected, B is
    // shown greyed out purely so the researcher can see it's disabled
    // rather than simply missing.
    void SelectCondition()
    {
        StyleConditionButton(_btnConditionA, true, true);
        StyleConditionButton(_btnConditionB, false, false);
        UpdateRowIcon(_rowConditionIcon, true);
        RefreshBeginButtonState();
    }

    void StyleConditionButton(Button btn, bool selected, bool enabled)
    {
        Color borderColor = !enabled
            ? new Color(0.2f, 0.25f, 0.3f, 0.6f)
            : selected
                ? new Color(0.4f, 0.7f, 1.0f, 0.9f)
                : new Color(0.4f, 0.7f, 1.0f, 0.3f);
        Color textColor = !enabled
            ? UIHelper.LockedText
            : selected ? new Color(0.4f, 0.7f, 1.0f, 1.0f) : UIHelper.TextMuted;

        btn.style.borderTopColor    = borderColor;
        btn.style.borderBottomColor = borderColor;
        btn.style.borderLeftColor   = borderColor;
        btn.style.borderRightColor  = borderColor;

        var lbl = btn.Q<Label>("ConditionLabel");
        if (lbl != null) lbl.style.color = textColor;
    }

    (VisualElement, Label) MakeChecklistRow(string label)
    {
        var row = new VisualElement();
        row.style.flexDirection  = FlexDirection.Row;
        row.style.alignItems     = Align.Center;
        row.style.marginBottom   = 10;

        var icon = new Label { text = "✗" };
        icon.style.fontSize      = 13;
        icon.style.color         = new Color(0.6f, 0.3f, 0.3f, 1.0f);
        icon.style.width         = 20;
        icon.style.marginRight   = 10;

        var lbl = new Label { text = label };
        lbl.style.fontSize   = 12;
        lbl.style.color      = UIHelper.TextMuted;
        lbl.style.whiteSpace = WhiteSpace.Normal;
        lbl.style.flexGrow   = 1;

        row.Add(icon);
        row.Add(lbl);
        return (row, icon);
    }

    (VisualElement, Label, Label) MakeChecklistRowWithStatus(string label)
    {
        var (row, icon) = MakeChecklistRow(label);

        var status = new Label { text = "CHECKING..." };
        status.style.fontSize      = 9;
        status.style.color         = UIHelper.TextMuted;
        status.style.letterSpacing = 1;

        row.Add(status);
        return (row, icon, status);
    }

    void UpdateRowIcon(Label icon, bool ok)
    {
        if (icon == null) return;
        icon.text  = ok ? "✓" : "✗";
        icon.style.color = ok
            ? new Color(0.3f, 0.7f, 0.4f, 1.0f)
            : new Color(0.6f, 0.3f, 0.3f, 1.0f);
    }

    // ── API check — Gemini primary, Groq fallback, static always available ──

    void RunApiChecks()
    {
        _geminiChecking = true;
        _groqChecking   = true;
        _geminiOk = false;
        _groqOk   = false;
        UpdateApiRow();

        NarrativeGenerator.Instance?.PingAPI(success =>
        {
            _geminiChecking = false;
            _geminiOk       = success;
            UpdateApiRow();
        });

        NarrativeGenerator.Instance?.PingGroq(success =>
        {
            _groqChecking = false;
            _groqOk       = success;
            UpdateApiRow();
        });
    }

    void UpdateApiRow()
    {
        bool stillChecking = _geminiChecking || _groqChecking;

        if (stillChecking)
        {
            UpdateRowIcon(_rowApiIcon, false);
            _rowApiStatusText.text        = "CHECKING...";
            _rowApiStatusText.style.color = UIHelper.TextMuted;
            _apiWarning.style.display     = DisplayStyle.None;
            return;
        }

        if (_geminiOk)
        {
            UpdateRowIcon(_rowApiIcon, true);
            _rowApiStatusText.text        = "GEMINI ✓";
            _rowApiStatusText.style.color = new Color(0.3f, 0.7f, 0.4f, 1.0f);
            _apiWarning.style.display     = DisplayStyle.None;
        }
        else if (_groqOk)
        {
            UpdateRowIcon(_rowApiIcon, true);
            _rowApiStatusText.text        = "GEMINI ✗ — GROQ FALLBACK READY";
            _rowApiStatusText.style.color = new Color(0.85f, 0.65f, 0.2f, 1.0f);
            _apiWarning.style.display     = DisplayStyle.None;
        }
        else
        {
            UpdateRowIcon(_rowApiIcon, false);
            _rowApiStatusText.text        = "BOTH APIS UNAVAILABLE";
            _rowApiStatusText.style.color = new Color(0.7f, 0.3f, 0.3f, 1.0f);
            _apiWarning.text = "Session will use pre-written fallback narrative. " +
                                "AI personalisation will not be available.";
            _apiWarning.style.display = DisplayStyle.Flex;
        }
    }

    // ── AR permission check ────────────────────────────────────────────────

    void RunArPermissionCheck()
    {
#if UNITY_EDITOR
        _arPermissionGranted = true;
#elif UNITY_ANDROID
        _arPermissionGranted =
            UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                UnityEngine.Android.Permission.Camera);
        if (!_arPermissionGranted)
        {
            UnityEngine.Android.Permission.RequestUserPermission(
                UnityEngine.Android.Permission.Camera);
            NavigationManager.Instance.StartCoroutine(PollArPermission());
        }
#else
        _arPermissionGranted = true;
#endif
        UpdateArRow();
    }

    IEnumerator PollArPermission()
    {
        for (int i = 0; i < 10 && !_arPermissionGranted; i++)
        {
            yield return new WaitForSeconds(0.5f);
#if UNITY_ANDROID && !UNITY_EDITOR
            _arPermissionGranted =
                UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                    UnityEngine.Android.Permission.Camera);
#endif
            UpdateArRow();
        }
    }

    void UpdateArRow()
    {
        UpdateRowIcon(_rowArIcon, _arPermissionGranted);
        _rowArStatusText.text  = _arPermissionGranted ? "READY" : "NOT AVAILABLE";
        _rowArStatusText.style.color = _arPermissionGranted
            ? new Color(0.3f, 0.7f, 0.4f, 1.0f)
            : new Color(0.7f, 0.3f, 0.3f, 1.0f);
    }

    // ── Begin session ──────────────────────────────────────────────────────

    void RefreshBeginButtonState()
    {
        UpdateRowIcon(_rowParticipantIcon, ParticipantIdValid);
    }

    bool ParticipantIdValid =>
        !string.IsNullOrEmpty(_participantIdField.value) &&
        _participantIdField.value.Trim().Length >= 2;

    void OnBeginSession()
    {
        if (!ParticipantIdValid)
        {
            Debug.LogWarning("[ResearcherSetupScreen] BEGIN SESSION clicked with invalid participant ID — ignored.");
            return;
        }

        string participantId = _participantIdField.value.Trim();
        Debug.Log($"[ResearcherSetupScreen] BEGIN SESSION clicked, participantId='{participantId}'");
        StudyLogger.Instance?.StartSession(participantId, "A");
        NavigationManager.Instance.ShowOnboarding();
    }

    // ── External refresh hooks (called by NavigationManager) ───────────────

    // Re-runs device checks without touching the entered participant ID —
    // used when the researcher returns here via the 3-finger hold gesture.
    public void RefreshChecklist()
    {
        RunApiChecks();
        RunArPermissionCheck();
        RefreshBeginButtonState();
    }

    // Clears the form for a new participant — used after "reset for next
    // participant" on the Session End screen.
    public void ResetForNewParticipant()
    {
        _participantIdField.value = "";
        RefreshChecklist();
    }

    public void SetVisible(bool v) =>
        _screen.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;

#if UNITY_EDITOR
    // ── Editor-only physical-keyboard fallback ──────────────────────────────

    void SetupEditorKeyboardFallback()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput += OnEditorTextInput;
        NavigationManager.Instance.StartCoroutine(EditorKeyboardControlLoop());
    }

    void OnEditorTextInput(char c)
    {
        if (_screen.style.display != DisplayStyle.Flex) return;
        if (char.IsControl(c)) return;
        _participantIdField.value += c;
    }

    IEnumerator EditorKeyboardControlLoop()
    {
        while (true)
        {
            if (_screen.style.display == DisplayStyle.Flex && Keyboard.current != null)
            {
                var kb = Keyboard.current;

                if (kb.backspaceKey.wasPressedThisFrame && _participantIdField.value.Length > 0)
                    _participantIdField.value =
                        _participantIdField.value.Substring(0, _participantIdField.value.Length - 1);

                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                    OnBeginSession();
            }
            yield return null;
        }
    }
#endif
}
