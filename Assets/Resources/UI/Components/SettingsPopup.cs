using UnityEngine;
using UnityEngine.UIElements;

public class SettingsPopup
{
    readonly VisualElement _overlay;
    bool _musicOn = true;
    bool _soundOn = true;
    VisualElement _musicKnob;
    VisualElement _soundKnob;
    VisualElement _musicTrack;
    VisualElement _soundTrack;
    VisualElement _settingsPanel;
    VisualElement _infoPanel;

    public SettingsPopup(VisualElement root)
    {
        _overlay = new VisualElement();
        _overlay.style.position        = Position.Absolute;
        _overlay.style.top             = 0;
        _overlay.style.left            = 0;
        _overlay.style.right           = 0;
        _overlay.style.bottom          = 0;
        _overlay.style.backgroundColor =
            new Color(0f, 0.02f, 0.06f, 0.65f);
        _overlay.style.alignItems      = Align.Center;
        _overlay.style.justifyContent  = Justify.Center;
        root.Add(_overlay);

        BuildSettingsPanel();
        BuildInfoPanel();

        SetVisible(false);
    }

    // ── Settings panel ─────────────────────────────────────────────────────

    void BuildSettingsPanel()
    {
        _settingsPanel = MakePanel();
        _overlay.Add(_settingsPanel);

        // Header
        var header = new VisualElement();
        header.style.flexDirection  = FlexDirection.Row;
        header.style.alignItems     = Align.Center;
        header.style.marginBottom   = 8;

        var gearIcon = UIHelper.Icon(
            UIHelper.IconGear, 16f,
            new Color(0.4f, 0.7f, 1.0f, 0.7f));
        gearIcon.style.marginRight = 8;

        var titleLabel = new Label { text = "SETTINGS" };
        titleLabel.style.fontSize       = 11;
        titleLabel.style.color          =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        titleLabel.style.letterSpacing  = 5;
        titleLabel.style.flexGrow       = 1;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        var btnClose = new Button { text = "✕" };
        StyleCloseBtn(btnClose);
        btnClose.clicked += () => SetVisible(false);

        header.Add(gearIcon);
        header.Add(titleLabel);
        header.Add(btnClose);
        _settingsPanel.Add(header);

        _settingsPanel.Add(MakeDivider());

        // Music row
        _musicTrack = MakeToggleTrack(true);
        _musicKnob  = _musicTrack.Q<VisualElement>("knob");
        var musicRow = MakeRow(UIHelper.IconMusicOn, "Music", _musicTrack);
        musicRow.RegisterCallback<ClickEvent>(_ => ToggleMusic());
        _settingsPanel.Add(musicRow);

        // Sound row
        _soundTrack = MakeToggleTrack(true);
        _soundKnob  = _soundTrack.Q<VisualElement>("knob");
        var soundRow = MakeRow(UIHelper.IconAudioOn, "Sound", _soundTrack);
        soundRow.RegisterCallback<ClickEvent>(_ => ToggleSound());
        _settingsPanel.Add(soundRow);

        // Info row — tapping opens info panel
        var infoArrow = UIHelper.Icon(
            UIHelper.IconArrowRight, 16f,
            new Color(0.4f, 0.5f, 0.6f, 0.7f));
        var infoRow = MakeRow(UIHelper.IconInfo, "Info", infoArrow);
        infoRow.RegisterCallback<ClickEvent>(_ => ShowInfo());
        _settingsPanel.Add(infoRow);

        _settingsPanel.Add(MakeDivider());

        // Reset button
        var btnReset = new Button { text = "RESET PROGRESS" };
        btnReset.style.backgroundColor         =
            new Color(0.25f, 0.05f, 0.05f, 0.4f);
        btnReset.style.borderTopColor          =
            new Color(0.9f, 0.3f, 0.3f, 1.0f);
        btnReset.style.borderBottomColor       =
            new Color(0.9f, 0.3f, 0.3f, 1.0f);
        btnReset.style.borderLeftColor         =
            new Color(0.9f, 0.3f, 0.3f, 1.0f);
        btnReset.style.borderRightColor        =
            new Color(0.9f, 0.3f, 0.3f, 1.0f);
        btnReset.style.borderTopWidth          = 1;
        btnReset.style.borderBottomWidth       = 1;
        btnReset.style.borderLeftWidth         = 1;
        btnReset.style.borderRightWidth        = 1;
        btnReset.style.borderTopLeftRadius     = 8;
        btnReset.style.borderTopRightRadius    = 8;
        btnReset.style.borderBottomLeftRadius  = 8;
        btnReset.style.borderBottomRightRadius = 8;
        btnReset.style.color                   =
            new Color(0.9f, 0.3f, 0.3f, 1.0f);
        btnReset.style.fontSize                = 10;
        btnReset.style.letterSpacing           = 3;
        btnReset.style.paddingTop              = 8;
        btnReset.style.paddingBottom           = 8;
        btnReset.style.width                   = Length.Percent(100);
        btnReset.style.marginTop               = 6;
        btnReset.clicked += () =>
        {
            SaveSystem.Instance?.DeleteSave();
            SetVisible(false);
            NavigationManager.Instance.GoTo(GameScreen.Home);
        };
        _settingsPanel.Add(btnReset);
    }

    // ── Info panel ─────────────────────────────────────────────────────────

    void BuildInfoPanel()
    {
        _infoPanel = MakePanel();
        _infoPanel.style.display = DisplayStyle.None;
        _overlay.Add(_infoPanel);

        // Header with back arrow
        var header = new VisualElement();
        header.style.flexDirection  = FlexDirection.Row;
        header.style.alignItems     = Align.Center;
        header.style.marginBottom   = 8;

        var btnBack = UIHelper.IconButtonImg(
            UIHelper.IconArrowLeft, 18f,
            new Color(0.4f, 0.7f, 1.0f, 0.7f));
        btnBack.clicked += () => ShowSettings();

        var titleLabel = new Label { text = "INFO" };
        titleLabel.style.fontSize       = 11;
        titleLabel.style.color          =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        titleLabel.style.letterSpacing  = 5;
        titleLabel.style.flexGrow       = 1;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        var btnClose = new Button { text = "✕" };
        StyleCloseBtn(btnClose);
        btnClose.clicked += () => SetVisible(false);

        header.Add(btnBack);
        header.Add(titleLabel);
        header.Add(btnClose);
        _infoPanel.Add(header);

        _infoPanel.Add(MakeDivider());

        // Info content
        var infoItems = new[]
        {
            ("AFTERGLOW", "An AR investigation game. You are the last survivor. Find out why the world ended."),
            ("HOW TO PLAY", "Walk through real rooms. Find glowing objects. Collect evidence. Submit your theory."),
            ("SCAN", "Press SCAN to reveal nearby clues. Walk close to objects to investigate them."),
            ("CASE FILE", "Open the Case File to review evidence and submit your theory about each room."),
            ("6 ROOMS", "Investigate 6 locations. The full truth only reveals itself after all rooms are explored."),
        };

        foreach (var (label, desc) in infoItems)
        {
            var infoRow = new VisualElement();
            infoRow.style.paddingTop    = 10;
            infoRow.style.paddingBottom = 10;
            infoRow.style.borderBottomColor =
                new Color(0.4f, 0.7f, 1.0f, 0.1f);
            infoRow.style.borderBottomWidth = 1;

            var infoLabel = new Label { text = label };
            infoLabel.style.fontSize      = 9;
            infoLabel.style.color         =
                new Color(0.4f, 0.7f, 1.0f, 0.7f);
            infoLabel.style.letterSpacing = 3;
            infoLabel.style.marginBottom  = 4;

            var infoDesc = new Label { text = desc };
            infoDesc.style.fontSize   = 12;
            infoDesc.style.color      = UIHelper.TextMuted;
            infoDesc.style.whiteSpace = WhiteSpace.Normal;

            infoRow.Add(infoLabel);
            infoRow.Add(infoDesc);
            _infoPanel.Add(infoRow);
        }

        // Version
        var version = new Label { text = "AFTERGLOW — v1.0" };
        version.style.fontSize       = 9;
        version.style.color          =
            new Color(0.4f, 0.7f, 1.0f, 0.2f);
        version.style.letterSpacing  = 3;
        version.style.marginTop      = 12;
        version.style.unityTextAlign = TextAnchor.MiddleCenter;
        _infoPanel.Add(version);
    }

    // ── Panel switching ────────────────────────────────────────────────────

    void ShowInfo()
    {
        _settingsPanel.style.display = DisplayStyle.None;
        _infoPanel.style.display     = DisplayStyle.Flex;
    }

    void ShowSettings()
    {
        _infoPanel.style.display     = DisplayStyle.None;
        _settingsPanel.style.display = DisplayStyle.Flex;
    }

    // ── Toggle logic ───────────────────────────────────────────────────────

    void ToggleMusic()
    {
        _musicOn = !_musicOn;
        UpdateToggle(_musicTrack, _musicKnob, _musicOn);
    }

    void ToggleSound()
    {
        _soundOn = !_soundOn;
        UpdateToggle(_soundTrack, _soundKnob, _soundOn);
    }

    void UpdateToggle(VisualElement track,
                      VisualElement knob, bool isOn)
    {
        track.style.backgroundColor = isOn
            ? new Color(0.2f, 0.5f, 0.9f, 0.9f)
            : new Color(0.1f, 0.15f, 0.22f, 0.9f);
        knob.style.left = isOn ? 22 : 2;
    }

    // ── Builders ───────────────────────────────────────────────────────────

    VisualElement MakePanel()
    {
        var panel = new VisualElement();
        panel.style.backgroundColor         =
            new Color(0.055f, 0.075f, 0.11f, 0.75f);
        panel.style.borderTopColor          =
            new Color(0.4f, 0.7f, 1.0f, 0.7f);
        panel.style.borderBottomColor       =
            new Color(0.4f, 0.7f, 1.0f, 0.7f);
        panel.style.borderLeftColor         =
            new Color(0.4f, 0.7f, 1.0f, 0.7f);
        panel.style.borderRightColor        =
            new Color(0.4f, 0.7f, 1.0f, 0.7f);
        panel.style.borderTopWidth          = 2;
        panel.style.borderBottomWidth       = 2;
        panel.style.borderLeftWidth         = 2;
        panel.style.borderRightWidth        = 2;
        panel.style.borderTopLeftRadius     = 16;
        panel.style.borderTopRightRadius    = 16;
        panel.style.borderBottomLeftRadius  = 16;
        panel.style.borderBottomRightRadius = 16;
        panel.style.paddingTop              = 14;
        panel.style.paddingBottom           = 14;
        panel.style.paddingLeft             = 20;
        panel.style.paddingRight            = 20;
        panel.style.width                   = 340;
        return panel;
    }

    VisualElement MakeToggleTrack(bool isOn)
    {
        var track = new VisualElement();
        track.style.width                   = 46;
        track.style.height                  = 26;
        track.style.borderTopLeftRadius     = 13;
        track.style.borderTopRightRadius    = 13;
        track.style.borderBottomLeftRadius  = 13;
        track.style.borderBottomRightRadius = 13;
        track.style.backgroundColor         = isOn
            ? new Color(0.2f, 0.5f, 0.9f, 0.9f)
            : new Color(0.1f, 0.15f, 0.22f, 0.9f);
        track.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        track.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        track.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        track.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        track.style.borderTopWidth    = 1;
        track.style.borderBottomWidth = 1;
        track.style.borderLeftWidth   = 1;
        track.style.borderRightWidth  = 1;
        track.style.position          = Position.Relative;

        var knob = new VisualElement();
        knob.name                          = "knob";
        knob.style.position                = Position.Absolute;
        knob.style.top                     = 3;
        knob.style.left                    = isOn ? 22 : 2;
        knob.style.width                   = 20;
        knob.style.height                  = 20;
        knob.style.borderTopLeftRadius     = 10;
        knob.style.borderTopRightRadius    = 10;
        knob.style.borderBottomLeftRadius  = 10;
        knob.style.borderBottomRightRadius = 10;
        knob.style.backgroundColor         = UIHelper.TextPrim;

        track.Add(knob);
        return track;
    }

    VisualElement MakeRow(string iconPath,
                          string labelText,
                          VisualElement control)
    {
        var row = new VisualElement();
        row.style.flexDirection  = FlexDirection.Row;
        row.style.alignItems     = Align.Center;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.paddingTop     = 6;
        row.style.paddingBottom  = 6;

        var left = new VisualElement();
        left.style.flexDirection = FlexDirection.Row;
        left.style.alignItems    = Align.Center;
        left.style.flexGrow      = 1;

        var icon = UIHelper.Icon(
            iconPath, 18f,
            new Color(0.4f, 0.7f, 1.0f, 0.7f));
        icon.style.marginRight = 10;

        var label = new Label { text = labelText };
        label.style.fontSize = 13;
        label.style.color    = UIHelper.TextPrim;

        left.Add(icon);
        left.Add(label);
        row.Add(left);
        row.Add(control);
        return row;
    }

    VisualElement MakeDivider()
    {
        var d = new VisualElement();
        d.style.height          = 1;
        d.style.width           = Length.Percent(100);
        d.style.backgroundColor =
            new Color(0.4f, 0.7f, 1.0f, 0.4f);
        d.style.marginTop       = 0;
        d.style.marginBottom    = 0;
        return d;
    }

    void StyleCloseBtn(Button btn)
    {
        btn.style.backgroundColor   = Color.clear;
        btn.style.borderTopWidth    = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth   = 0;
        btn.style.borderRightWidth  = 0;
        btn.style.color             =
            new Color(0.4f, 0.7f, 1.0f, 0.6f);
        btn.style.fontSize          = 14;
        btn.style.width             = 28;
        btn.style.height            = 28;
        btn.style.unityTextAlign    = TextAnchor.MiddleCenter;
    }

    public void SetVisible(bool v)
    {
        _overlay.style.display = v
            ? DisplayStyle.Flex : DisplayStyle.None;
        if (v) ShowSettings();
    }
}