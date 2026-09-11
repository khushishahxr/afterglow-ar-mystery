using UnityEngine;
using UnityEngine.UIElements;

public static class UIHelper
{
    // ── Afterglow color palette ────────────────────────────────────────────

    // Backgrounds
    public static readonly Color BG            = new Color(0.031f, 0.043f, 0.063f);
    public static readonly Color Surface       = new Color(0.055f, 0.075f, 0.106f);
    public static readonly Color SurfaceRaised = new Color(0.082f, 0.110f, 0.153f);
    public static readonly Color Border        = new Color(0.122f, 0.165f, 0.224f);

    // Text
    public static readonly Color TextPrim      = new Color(0.820f, 0.855f, 0.894f);
    public static readonly Color TextMuted     = new Color(0.420f, 0.490f, 0.569f);

    // Accent — amber (kept for SCAN button only)
    public static readonly Color Accent        = new Color(0.820f, 0.580f, 0.220f);
    public static readonly Color AccentDim     = new Color(0.420f, 0.290f, 0.098f);
    public static readonly Color AccentGlow    = new Color(0.900f, 0.680f, 0.300f);

    // Cold blue — primary UI color
    public static readonly Color ColdBlue      = new Color(0.4f,   0.7f,   1.0f,  0.9f);
    public static readonly Color ColdBlueDim   = new Color(0.4f,   0.7f,   1.0f,  0.5f);

    // State colors
    public static readonly Color Locked        = new Color(0.063f, 0.082f, 0.110f);
    public static readonly Color LockedText    = new Color(0.200f, 0.255f, 0.318f);
    public static readonly Color Correct       = new Color(0.180f, 0.420f, 0.310f);
    public static readonly Color Wrong         = new Color(0.420f, 0.145f, 0.145f);

    // ── Icon paths ─────────────────────────────────────────────────────────
    public const string IconArrowLeft  = "UI/Icons/arrowLeft";
    public const string IconArrowRight = "UI/Icons/arrowRight";
    public const string IconAudioOff   = "UI/Icons/audioOff";
    public const string IconAudioOn    = "UI/Icons/audioOn";
    public const string IconGear       = "UI/Icons/gear";
    public const string IconHome       = "UI/Icons/home";
    public const string IconInfo       = "UI/Icons/information";
    public const string IconMusicOff   = "UI/Icons/musicOff";
    public const string IconMusicOn    = "UI/Icons/musicOn";

    // ── Layout constants ───────────────────────────────────────────────────
    public const float TopBarPaddingTop    = 50f;
    public const float TopBarPaddingLeft   = 16f;
    public const float TopBarPaddingRight  = 16f;
    public const float TopBarPaddingBottom = 16f;

    // ── Screen builder ─────────────────────────────────────────────────────

    public static VisualElement Screen()
    {
        var el = new VisualElement();
        el.style.position        = Position.Absolute;
        el.style.top             = 0;
        el.style.left            = 0;
        el.style.right           = 0;
        el.style.bottom          = 0;
        el.style.backgroundColor = BG;
        el.style.flexDirection   = FlexDirection.Column;
        el.style.alignItems      = Align.Center;
        return el;
    }

    public static VisualElement TopBar()
    {
        var el = new VisualElement();
        el.style.flexDirection  = FlexDirection.Row;
        el.style.alignItems     = Align.Center;
        el.style.justifyContent = Justify.SpaceBetween;
        el.style.width          = Length.Percent(100);
        el.style.paddingTop     = TopBarPaddingTop;
        el.style.paddingLeft    = TopBarPaddingLeft;
        el.style.paddingRight   = TopBarPaddingRight;
        el.style.paddingBottom  = TopBarPaddingBottom;
        return el;
    }

    public static Label ScreenTitle(string text)
    {
        var l = new Label { text = text };
        l.style.fontSize       = 10;
        l.style.color          = TextMuted;
        l.style.letterSpacing  = 5;
        l.style.flexGrow       = 1;
        l.style.unityTextAlign = TextAnchor.MiddleCenter;
        return l;
    }

    // Legacy text icon button — kept for any remaining uses
    public static Button IconButton(string icon)
    {
        var b = new Button { text = icon };
        b.style.backgroundColor   = Color.clear;
        b.style.borderTopWidth    = 0;
        b.style.borderBottomWidth = 0;
        b.style.borderLeftWidth   = 0;
        b.style.borderRightWidth  = 0;
        b.style.color             = TextMuted;
        b.style.fontSize          = 18;
        b.style.width             = 44;
        b.style.height            = 44;
        b.style.unityTextAlign    = TextAnchor.MiddleCenter;
        return b;
    }

    public static Button PrimaryButton(string text)
    {
        var b = new Button { text = text };
        b.style.backgroundColor         = Color.clear;
        b.style.borderTopColor          = Accent;
        b.style.borderBottomColor       = Accent;
        b.style.borderLeftColor         = Accent;
        b.style.borderRightColor        = Accent;
        b.style.borderTopWidth          = 1;
        b.style.borderBottomWidth       = 1;
        b.style.borderLeftWidth         = 1;
        b.style.borderRightWidth        = 1;
        b.style.borderTopLeftRadius     = 2;
        b.style.borderTopRightRadius    = 2;
        b.style.borderBottomLeftRadius  = 2;
        b.style.borderBottomRightRadius = 2;
        b.style.color                   = Accent;
        b.style.fontSize                = 11;
        b.style.letterSpacing           = 3;
        b.style.paddingTop              = 14;
        b.style.paddingBottom           = 14;
        b.style.paddingLeft             = 32;
        b.style.paddingRight            = 32;
        return b;
    }

    public static Button DangerButton(string text)
    {
        var b = PrimaryButton(text);
        b.style.borderTopColor    = new Color(0.7f, 0.2f, 0.2f);
        b.style.borderBottomColor = new Color(0.7f, 0.2f, 0.2f);
        b.style.borderLeftColor   = new Color(0.7f, 0.2f, 0.2f);
        b.style.borderRightColor  = new Color(0.7f, 0.2f, 0.2f);
        b.style.color             = new Color(0.7f, 0.2f, 0.2f);
        return b;
    }

    public static Label ChapterLabel(string text)
    {
        var l = new Label { text = text };
        l.style.fontSize      = 9;
        l.style.color         = new Color(0.4f, 0.7f, 1.0f, 0.8f);
        l.style.letterSpacing = 4;
        return l;
    }

    public static Label TitleLabel(string text, float size = 22f)
    {
        var l = new Label { text = text };
        l.style.fontSize                = size;
        l.style.color                   = TextPrim;
        l.style.unityFontStyleAndWeight = FontStyle.Bold;
        l.style.letterSpacing           = 2;
        return l;
    }

    public static Label BodyLabel(string text)
    {
        var l = new Label { text = text };
        l.style.fontSize   = 13;
        l.style.color      = TextMuted;
        l.style.whiteSpace = WhiteSpace.Normal;
        return l;
    }

    // Splits text into chunks of at most maxWords, breaking on sentence
    // boundaries where possible so a chunk never ends mid-thought. Used as
    // a safety net for tap-to-continue display — the LLM is already
    // instructed to stay under these limits, but this guarantees the UI
    // never shows a wall of text even if a response runs long.
    public static System.Collections.Generic.List<string> ChunkByWords(string text, int maxWords)
    {
        var chunks = new System.Collections.Generic.List<string>();
        if (string.IsNullOrEmpty(text)) { chunks.Add(""); return chunks; }

        var sentences = text.Split(new[] { ". ", ".\n" }, System.StringSplitOptions.None);
        var current = new System.Text.StringBuilder();
        int currentWords = 0;

        for (int i = 0; i < sentences.Length; i++)
        {
            string sentence = sentences[i].Trim();
            if (sentence.Length == 0) continue;
            if (!sentence.EndsWith(".") && !sentence.EndsWith("!") && !sentence.EndsWith("?") && i < sentences.Length - 1)
                sentence += ".";

            int sentenceWords = sentence.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries).Length;

            if (currentWords > 0 && currentWords + sentenceWords > maxWords)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
                currentWords = 0;
            }

            current.Append(sentence).Append(' ');
            currentWords += sentenceWords;
        }

        if (current.Length > 0) chunks.Add(current.ToString().Trim());
        if (chunks.Count == 0) chunks.Add(text);
        return chunks;
    }

    public static VisualElement Divider()
    {
        var d = new VisualElement();
        d.style.height          = 1;
        d.style.width           = Length.Percent(100);
        d.style.backgroundColor = Border;
        d.style.marginTop       = 16;
        d.style.marginBottom    = 16;
        return d;
    }

    public static VisualElement Card()
    {
        var card = new VisualElement();
        card.style.backgroundColor         = Surface;
        card.style.borderTopColor          = Border;
        card.style.borderBottomColor       = Border;
        card.style.borderLeftColor         = Border;
        card.style.borderRightColor        = Border;
        card.style.borderTopWidth          = 1;
        card.style.borderBottomWidth       = 1;
        card.style.borderLeftWidth         = 1;
        card.style.borderRightWidth        = 1;
        card.style.borderTopLeftRadius     = 4;
        card.style.borderTopRightRadius    = 4;
        card.style.borderBottomLeftRadius  = 4;
        card.style.borderBottomRightRadius = 4;
        card.style.paddingTop              = 16;
        card.style.paddingBottom           = 16;
        card.style.paddingLeft             = 20;
        card.style.paddingRight            = 20;
        card.style.marginBottom            = 12;
        return card;
    }

    public static VisualElement AccentCard()
    {
        var card = Card();
        card.style.borderTopColor    = new Color(0.4f, 0.7f, 1.0f, 0.3f);
        card.style.borderBottomColor = new Color(0.4f, 0.7f, 1.0f, 0.3f);
        card.style.borderLeftColor   = new Color(0.4f, 0.7f, 1.0f, 0.6f);
        card.style.borderRightColor  = new Color(0.4f, 0.7f, 1.0f, 0.3f);
        return card;
    }

    public static void Spacer(VisualElement parent, float height)
    {
        var s = new VisualElement();
        s.style.height = height;
        parent.Add(s);
    }

    // ── Icon helpers ───────────────────────────────────────────────────────

    public static VisualElement Icon(
        string resourcePath,
        float size = 24f,
        Color? tint = null)
    {
        var texture = Resources.Load<Texture2D>(resourcePath);

        var icon = new VisualElement();
        icon.style.width      = size;
        icon.style.height     = size;
        icon.style.flexShrink = 0;

        if (texture != null)
        {
            icon.style.backgroundImage = new StyleBackground(texture);
            icon.style.backgroundPositionX =
                new BackgroundPosition(BackgroundPositionKeyword.Center);
            icon.style.backgroundPositionY =
                new BackgroundPosition(BackgroundPositionKeyword.Center);
            icon.style.backgroundRepeat =
                new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            icon.style.backgroundSize =
                new BackgroundSize(BackgroundSizeType.Contain);
            icon.style.unityBackgroundImageTintColor =
                tint ?? TextPrim;
        }
        else
        {
            Debug.LogWarning(
                $"[UIHelper] Icon not found: {resourcePath}");
        }

        return icon;
    }

    public static Button IconButtonImg(
        string resourcePath,
        float size = 22f,
        Color? tint = null)
    {
        var btn = new Button();
        btn.style.backgroundColor   = Color.clear;
        btn.style.borderTopWidth    = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth   = 0;
        btn.style.borderRightWidth  = 0;
        btn.style.width             = 44;
        btn.style.height            = 44;
        btn.style.alignItems        = Align.Center;
        btn.style.justifyContent    = Justify.Center;
        btn.style.paddingTop        = 0;
        btn.style.paddingBottom     = 0;
        btn.style.paddingLeft       = 0;
        btn.style.paddingRight      = 0;

        var icon = Icon(resourcePath, size, tint);
        btn.Add(icon);

        return btn;
    }
}