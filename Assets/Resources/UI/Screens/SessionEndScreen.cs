using UnityEngine;
using UnityEngine.UIElements;

// Researcher-facing screen shown automatically after the Ending cinematic
// finishes. Lets the researcher export the session JSON and reset the app
// for the next participant.
public class SessionEndScreen
{
    readonly VisualElement _screen;
    readonly VisualElement _confirmOverlay;

    Label _participantValue;
    Label _conditionValue;
    Label _durationValue;
    Label _roomsValue;
    Label _theoriesValue;
    Label _apiErrorsValue;
    Label _flagValue;
    Label _exportConfirmLabel;

    public SessionEndScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        root.Add(_screen);

        var scroll = new ScrollView();
        scroll.style.position     = Position.Absolute;
        scroll.style.top          = 60;
        scroll.style.left         = 0;
        scroll.style.right        = 0;
        scroll.style.bottom       = 0;
        scroll.style.paddingLeft  = 24;
        scroll.style.paddingRight = 24;
        scroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        var title = new Label { text = "SESSION COMPLETE" };
        title.style.fontSize                = 18;
        title.style.color                   = new Color(0.4f, 0.7f, 1.0f, 0.95f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.letterSpacing           = 3;
        title.style.unityTextAlign          = TextAnchor.MiddleCenter;
        title.style.width                   = Length.Percent(100);
        title.style.marginTop               = 20;
        scroll.Add(title);

        scroll.Add(UIHelper.Divider());

        // ── Stats block ────────────────────────────────────────────────────
        var statsBlock = new VisualElement();
        statsBlock.style.backgroundColor         = UIHelper.Surface;
        statsBlock.style.borderTopColor          = UIHelper.Border;
        statsBlock.style.borderBottomColor       = UIHelper.Border;
        statsBlock.style.borderLeftColor         = new Color(0.4f, 0.7f, 1.0f, 0.6f);
        statsBlock.style.borderRightColor        = UIHelper.Border;
        statsBlock.style.borderTopWidth          = 1;
        statsBlock.style.borderBottomWidth       = 1;
        statsBlock.style.borderLeftWidth         = 2;
        statsBlock.style.borderRightWidth        = 1;
        statsBlock.style.borderTopLeftRadius     = 2;
        statsBlock.style.borderTopRightRadius    = 2;
        statsBlock.style.borderBottomLeftRadius  = 2;
        statsBlock.style.borderBottomRightRadius = 2;
        statsBlock.style.paddingTop              = 16;
        statsBlock.style.paddingBottom           = 16;
        statsBlock.style.paddingLeft             = 20;
        statsBlock.style.paddingRight            = 20;
        statsBlock.style.marginTop               = 20;
        statsBlock.style.marginBottom            = 20;

        _participantValue = AddStatRow(statsBlock, "PARTICIPANT");
        _conditionValue   = AddStatRow(statsBlock, "CONDITION");
        _durationValue    = AddStatRow(statsBlock, "DURATION");
        _roomsValue       = AddStatRow(statsBlock, "ROOMS COMPLETED");
        _theoriesValue    = AddStatRow(statsBlock, "THEORIES LOGGED");
        _apiErrorsValue   = AddStatRow(statsBlock, "API ERRORS");
        _flagValue        = AddStatRow(statsBlock, "22-MIN FLAG", isLast: true);

        scroll.Add(statsBlock);
        scroll.Add(UIHelper.Divider());

        // ── Export ─────────────────────────────────────────────────────────
        var btnExport = MakeColdBlueButton("EXPORT SESSION DATA");
        btnExport.clicked += OnExport;
        scroll.Add(btnExport);

        _exportConfirmLabel = new Label { text = "" };
        _exportConfirmLabel.style.fontSize       = 11;
        _exportConfirmLabel.style.color          = new Color(0.3f, 0.7f, 0.4f, 1.0f);
        _exportConfirmLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        _exportConfirmLabel.style.whiteSpace     = WhiteSpace.Normal;
        _exportConfirmLabel.style.marginTop      = 10;
        _exportConfirmLabel.style.width          = Length.Percent(100);
        scroll.Add(_exportConfirmLabel);

        // ── Reset ──────────────────────────────────────────────────────────
        var btnReset = MakeRedButton("RESET FOR NEXT PARTICIPANT");
        btnReset.style.marginTop    = 24;
        btnReset.style.marginBottom = 60;
        btnReset.clicked += () => SetConfirmVisible(true);
        scroll.Add(btnReset);

        _screen.Add(scroll);

        // ── Confirmation dialog ───────────────────────────────────────────
        _confirmOverlay = new VisualElement();
        _confirmOverlay.style.position        = Position.Absolute;
        _confirmOverlay.style.top             = 0;
        _confirmOverlay.style.left            = 0;
        _confirmOverlay.style.right           = 0;
        _confirmOverlay.style.bottom          = 0;
        _confirmOverlay.style.backgroundColor = new Color(0f, 0.02f, 0.06f, 0.75f);
        _confirmOverlay.style.alignItems      = Align.Center;
        _confirmOverlay.style.justifyContent  = Justify.Center;
        _confirmOverlay.style.display         = DisplayStyle.None;

        var dialog = new VisualElement();
        dialog.style.width                    = 300;
        dialog.style.backgroundColor          = UIHelper.Surface;
        dialog.style.borderTopColor           = new Color(0.9f, 0.3f, 0.3f, 0.8f);
        dialog.style.borderBottomColor        = new Color(0.9f, 0.3f, 0.3f, 0.8f);
        dialog.style.borderLeftColor          = new Color(0.9f, 0.3f, 0.3f, 0.8f);
        dialog.style.borderRightColor         = new Color(0.9f, 0.3f, 0.3f, 0.8f);
        dialog.style.borderTopWidth           = 1;
        dialog.style.borderBottomWidth        = 1;
        dialog.style.borderLeftWidth          = 1;
        dialog.style.borderRightWidth         = 1;
        dialog.style.borderTopLeftRadius      = 8;
        dialog.style.borderTopRightRadius     = 8;
        dialog.style.borderBottomLeftRadius   = 8;
        dialog.style.borderBottomRightRadius  = 8;
        dialog.style.paddingTop               = 20;
        dialog.style.paddingBottom            = 20;
        dialog.style.paddingLeft              = 20;
        dialog.style.paddingRight             = 20;

        var warning = new Label
        {
            text = "This will clear all session data.\n" +
                   "Exported files will not be deleted.\n\n" +
                   "Are you sure?"
        };
        warning.style.fontSize       = 13;
        warning.style.color          = UIHelper.TextPrim;
        warning.style.whiteSpace     = WhiteSpace.Normal;
        warning.style.unityTextAlign = TextAnchor.MiddleCenter;
        warning.style.marginBottom   = 20;
        dialog.Add(warning);

        var btnConfirm = MakeRedButton("CONFIRM RESET");
        btnConfirm.clicked += OnConfirmReset;
        dialog.Add(btnConfirm);

        var btnCancel = MakeColdBlueButton("CANCEL");
        btnCancel.style.marginTop = 10;
        btnCancel.clicked += () => SetConfirmVisible(false);
        dialog.Add(btnCancel);

        _confirmOverlay.Add(dialog);
        _screen.Add(_confirmOverlay);

        SetVisible(false);
    }

    // ── Builders ───────────────────────────────────────────────────────────

    Label AddStatRow(VisualElement parent, string label, bool isLast = false)
    {
        var row = new VisualElement();
        row.style.flexDirection  = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom   = isLast ? 0 : 10;

        var l = new Label { text = label };
        l.style.fontSize      = 10;
        l.style.color         = UIHelper.TextMuted;
        l.style.letterSpacing = 2;

        var v = new Label { text = "—" };
        v.style.fontSize      = 12;
        v.style.color         = UIHelper.TextPrim;
        v.style.unityFontStyleAndWeight = FontStyle.Bold;

        row.Add(l);
        row.Add(v);
        parent.Add(row);
        return v;
    }

    Button MakeColdBlueButton(string text)
    {
        var btn = new Button();
        btn.style.width             = Length.Percent(100);
        btn.style.paddingTop        = 14;
        btn.style.paddingBottom     = 14;
        btn.style.backgroundColor   = Color.clear;
        btn.style.borderTopColor    = new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btn.style.borderBottomColor = new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btn.style.borderLeftColor   = new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btn.style.borderRightColor  = new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btn.style.borderTopWidth    = 1;
        btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth   = 1;
        btn.style.borderRightWidth  = 1;
        btn.style.borderTopLeftRadius     = 2;
        btn.style.borderTopRightRadius    = 2;
        btn.style.borderBottomLeftRadius  = 2;
        btn.style.borderBottomRightRadius = 2;

        var lbl = new Label { text = text };
        lbl.style.fontSize       = 11;
        lbl.style.color          = new Color(0.4f, 0.7f, 1.0f, 0.9f);
        lbl.style.letterSpacing  = 3;
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        lbl.style.width          = Length.Percent(100);
        btn.Add(lbl);
        return btn;
    }

    Button MakeRedButton(string text)
    {
        var btn = new Button();
        btn.style.width             = Length.Percent(100);
        btn.style.paddingTop        = 14;
        btn.style.paddingBottom     = 14;
        btn.style.backgroundColor   = Color.clear;
        btn.style.borderTopColor    = new Color(0.9f, 0.3f, 0.3f, 0.8f);
        btn.style.borderBottomColor = new Color(0.9f, 0.3f, 0.3f, 0.8f);
        btn.style.borderLeftColor   = new Color(0.9f, 0.3f, 0.3f, 0.8f);
        btn.style.borderRightColor  = new Color(0.9f, 0.3f, 0.3f, 0.8f);
        btn.style.borderTopWidth    = 1;
        btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth   = 1;
        btn.style.borderRightWidth  = 1;
        btn.style.borderTopLeftRadius     = 2;
        btn.style.borderTopRightRadius    = 2;
        btn.style.borderBottomLeftRadius  = 2;
        btn.style.borderBottomRightRadius = 2;

        var lbl = new Label { text = text };
        lbl.style.fontSize       = 11;
        lbl.style.color          = new Color(0.9f, 0.4f, 0.4f, 0.95f);
        lbl.style.letterSpacing  = 3;
        lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        lbl.style.width          = Length.Percent(100);
        btn.Add(lbl);
        return btn;
    }

    // ── Stats population ──────────────────────────────────────────────────

    public void Show()
    {
        var logger = StudyLogger.Instance;

        _participantValue.text = logger != null && !string.IsNullOrEmpty(logger.ParticipantID)
            ? logger.ParticipantID : "—";
        _conditionValue.text = logger != null
            ? $"{logger.Condition} — {(logger.Condition == "A" ? "Dynamic AI" : "Fixed")}" : "—";
        _durationValue.text  = logger != null ? FormatDuration(logger.TotalTimeSecs) : "—";
        _roomsValue.text     = logger != null ? $"{logger.RoomsCompleted} / 6" : "0 / 6";
        _theoriesValue.text  = logger != null ? logger.TheoriesLogged.ToString() : "0";
        _apiErrorsValue.text = logger != null ? logger.ApiErrorCount.ToString() : "0";

        bool exceeded = logger != null && logger.Exceeded22Minutes;
        _flagValue.text  = exceeded ? "Yes" : "No";
        _flagValue.style.color = exceeded
            ? new Color(0.9f, 0.6f, 0.3f, 1.0f)
            : UIHelper.TextPrim;

        _exportConfirmLabel.text = "";
        SetConfirmVisible(false);
        SetVisible(true);
    }

    string FormatDuration(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes}m {seconds}s";
    }

    // ── Actions ────────────────────────────────────────────────────────────

    void OnExport()
    {
        StudyLogger.Instance?.ForceSave();
        string fileName = StudyLogger.Instance?.LastSavedFileName ?? "";
        _exportConfirmLabel.text = string.IsNullOrEmpty(fileName)
            ? "Save failed — check device storage."
            : $"Saved: {fileName}";
    }

    void SetConfirmVisible(bool v) =>
        _confirmOverlay.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;

    void OnConfirmReset()
    {
        SetConfirmVisible(false);
        NavigationManager.Instance.ResetForNextParticipant();
    }

    public void SetVisible(bool v) =>
        _screen.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
}
