using UnityEngine;
using UnityEngine.UIElements;

public class HomeScreen
{
    readonly VisualElement _screen;

    public HomeScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        root.Add(_screen);

        // ── Full screen background image ───────────────────────────────────
        var bgTexture = Resources.Load<Texture2D>("UI/Images/bg_home");
        if (bgTexture != null)
        {
            var bg = new VisualElement();
            bg.style.position        = Position.Absolute;
            bg.style.top             = 0;
            bg.style.left            = 0;
            bg.style.right           = 0;
            bg.style.bottom          = 0;
            bg.style.backgroundImage = new StyleBackground(bgTexture);
            bg.style.backgroundPositionX =
                new BackgroundPosition(BackgroundPositionKeyword.Center);
            bg.style.backgroundPositionY =
                new BackgroundPosition(BackgroundPositionKeyword.Center);
            bg.style.backgroundRepeat =
                new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            bg.style.backgroundSize =
                new BackgroundSize(BackgroundSizeType.Cover);
            _screen.Add(bg);

            var overlay = new VisualElement();
            overlay.style.position        = Position.Absolute;
            overlay.style.top             = 0;
            overlay.style.left            = 0;
            overlay.style.right           = 0;
            overlay.style.bottom          = 0;
            overlay.style.backgroundColor =
                new Color(0.031f, 0.043f, 0.063f, 0.45f);
            _screen.Add(overlay);
        }

        // ── Settings button — gear icon ────────────────────────────────────
        var btnSettings = UIHelper.IconButtonImg(
            UIHelper.IconGear, 24f,
            new Color(0.4f, 0.7f, 1.0f, 0.8f));
        btnSettings.style.position = Position.Absolute;
        btnSettings.style.top      = 52;
        btnSettings.style.right    = 16;
        btnSettings.clicked += () =>
            NavigationManager.Instance.ShowSettings(true);
        _screen.Add(btnSettings);

        // ── Center title block ─────────────────────────────────────────────
        var centerBlock = new VisualElement();
        centerBlock.style.position       = Position.Absolute;
        centerBlock.style.left           = 0;
        centerBlock.style.right          = 0;
        centerBlock.style.top            = Length.Percent(30);
        centerBlock.style.alignItems     = Align.Center;
        centerBlock.style.justifyContent = Justify.Center;
        centerBlock.style.width          = Length.Percent(100);

        var subtitle = new Label { text = "SOMETHING STILL AWAITS" };
        subtitle.style.fontSize       = 10;
        subtitle.style.color          = new Color(0.4f, 0.7f, 1.0f, 0.9f);
        subtitle.style.letterSpacing  = 4;
        subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        subtitle.style.marginBottom   = 12;
        subtitle.style.width          = Length.Percent(100);

        var title = new Label { text = "AFTERGLOW" };
        title.style.fontSize                = 54;
        title.style.color                   = UIHelper.TextPrim;
        title.style.unityTextAlign          = TextAnchor.MiddleCenter;
        title.style.letterSpacing           = 10;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.width                   = Length.Percent(100);

        var lineContainer = new VisualElement();
        lineContainer.style.alignItems = Align.Center;
        lineContainer.style.width      = Length.Percent(100);
        lineContainer.style.marginTop  = 16;

        var line = new VisualElement();
        line.style.width           = 60;
        line.style.height          = 1;
        line.style.backgroundColor =
            new Color(0.4f, 0.7f, 1.0f, 0.6f);

        lineContainer.Add(line);
        centerBlock.Add(subtitle);
        centerBlock.Add(title);
        centerBlock.Add(lineContainer);
        _screen.Add(centerBlock);

        // ── Bottom block ───────────────────────────────────────────────────
        var bottomBlock = new VisualElement();
        bottomBlock.style.position   = Position.Absolute;
        bottomBlock.style.bottom     = 80;
        bottomBlock.style.left       = 32;
        bottomBlock.style.right      = 32;
        bottomBlock.style.alignItems = Align.Center;

        // Button — no text property, only child row
        var btnInvestigate = new Button();
        btnInvestigate.style.width             = Length.Percent(100);
        btnInvestigate.style.paddingTop        = 16;
        btnInvestigate.style.paddingBottom     = 16;
        btnInvestigate.style.backgroundColor   = Color.clear;
        btnInvestigate.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnInvestigate.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnInvestigate.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnInvestigate.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnInvestigate.style.borderTopWidth    = 1;
        btnInvestigate.style.borderBottomWidth = 1;
        btnInvestigate.style.borderLeftWidth   = 1;
        btnInvestigate.style.borderRightWidth  = 1;
        btnInvestigate.style.borderTopLeftRadius     = 2;
        btnInvestigate.style.borderTopRightRadius    = 2;
        btnInvestigate.style.borderBottomLeftRadius  = 2;
        btnInvestigate.style.borderBottomRightRadius = 2;

        var btnRow = new VisualElement();
        btnRow.style.flexDirection  = FlexDirection.Row;
        btnRow.style.alignItems     = Align.Center;
        btnRow.style.justifyContent = Justify.Center;

        var btnLabel = new Label { text = "BEGIN INVESTIGATION" };
        btnLabel.style.fontSize      = 11;
        btnLabel.style.color         = new Color(0.4f, 0.7f, 1.0f, 0.9f);
        btnLabel.style.letterSpacing = 3;

        var btnArrow = UIHelper.Icon(
            UIHelper.IconArrowRight, 14f,
            new Color(0.4f, 0.7f, 1.0f, 0.9f));
        btnArrow.style.marginLeft = 8;

        btnRow.Add(btnLabel);
        btnRow.Add(btnArrow);
        btnInvestigate.Add(btnRow);
        btnInvestigate.clicked +=
            () => NavigationManager.Instance.GoTo(GameScreen.Chapters);

        var tagline = new Label
        {
            text = "The world ended. You survived.\nFind out why."
        };
        tagline.style.fontSize       = 12;
        tagline.style.color          = UIHelper.TextMuted;
        tagline.style.unityTextAlign = TextAnchor.MiddleCenter;
        tagline.style.whiteSpace     = WhiteSpace.Normal;
        tagline.style.marginTop      = 16;
        tagline.style.width          = Length.Percent(100);

        bottomBlock.Add(btnInvestigate);
        bottomBlock.Add(tagline);
        _screen.Add(bottomBlock);
    }

    public void SetVisible(bool v) =>
        _screen.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
}