using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

// Identical in both AI and Fixed versions. Appears once, between
// ResearcherSetupScreen and Home, on every session start.
public class OnboardingScreen
{
    readonly VisualElement _screen;
    VisualElement _bgImage;
    VisualElement _bgTint;
    Label  _iconLabel;
    Label  _titleLabel;
    Label  _textLabel;
    Button _btnNext;
    Button _btnBeginInvestigation;

    int   _currentSlide;
    float _startTime;

    struct SlideData
    {
        public string Icon, Title, Text, ImageResource;
        public SlideData(string icon, string title, string text, string imageResource = null)
        {
            Icon = icon; Title = title; Text = text; ImageResource = imageResource;
        }
    }

    static readonly SlideData[] Slides =
    {
        // Onboarding2 (a piece of evidence materializing on the table
        // after a reveal-tap) reads as a mysterious cold open here —
        // something is about to be found — without explaining the
        // mechanic before the next slide actually teaches it.
        new SlideData("📖", "WELCOME",
            "You are about to investigate a series of rooms. Your goal is to discover what happened — and why the world went silent.",
            "UI/Images/Onboarding2"),
        // Onboarding1 is the scan reticle over a real surface — the exact
        // action this slide's copy describes.
        new SlideData("🎯", "SCANNING",
            "Press SCAN ROOM once to sweep the space. After that, aim at a surface and press REVEAL to find what's hidden nearby. Walk toward what you find to look closer.",
            "UI/Images/Onboarding1"),
        new SlideData("🔍", "COLLECTING EVIDENCE",
            "Each room hides a few pieces of evidence. Find everything in the room before submitting your theory. Tap evidence cards to read detective notes.",
            "UI/Images/Onboarding3"),
        new SlideData("✎", "MAKING DECISIONS",
            "After investigating each room, write your theory about what happened here. Rate your confidence. There are no right or wrong answers — trust your instincts.",
            "UI/Images/Onboarding4"),
        // No image for the last slide — deliberately, so the closing beat
        // reads as a pause rather than another photo. Falls back to a
        // centered icon instead.
        new SlideData("◈", "YOU ARE READY",
            "The investigation begins now. Take your time. Look carefully. What you find cannot be unfound."),
    };

    public OnboardingScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        root.Add(_screen);

        // Full-screen background photo — the photo IS the screen, not a
        // boxed thumbnail. No top bar/header/back button anywhere on this
        // screen — deliberately chrome-free.
        _bgImage = new VisualElement();
        _bgImage.style.position = Position.Absolute;
        _bgImage.style.top = 0; _bgImage.style.left = 0;
        _bgImage.style.right = 0; _bgImage.style.bottom = 0;
        _bgImage.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        _bgImage.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        _bgImage.style.backgroundRepeat    = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        _bgImage.style.backgroundSize      = new BackgroundSize(BackgroundSizeType.Cover);
        _bgImage.pickingMode = PickingMode.Ignore;
        _screen.Add(_bgImage);

        _bgTint = new VisualElement();
        _bgTint.style.position = Position.Absolute;
        _bgTint.style.top = 0; _bgTint.style.left = 0;
        _bgTint.style.right = 0; _bgTint.style.bottom = 0;
        _bgTint.style.backgroundColor = new Color(0.031f, 0.043f, 0.063f, 0.55f);
        _bgTint.pickingMode = PickingMode.Ignore;
        _screen.Add(_bgTint);

        // Fallback icon for the one image-less slide — pushed down to
        // top:40% specifically so it doesn't collide with the caption
        // card now living near the top of the screen.
        _iconLabel = new Label { text = "" };
        _iconLabel.style.position        = Position.Absolute;
        _iconLabel.style.left = 0; _iconLabel.style.right = 0;
        _iconLabel.style.top             = Length.Percent(40);
        _iconLabel.style.fontSize        = 56;
        _iconLabel.style.unityTextAlign  = TextAnchor.MiddleCenter;
        _iconLabel.style.opacity         = 0;
        _screen.Add(_iconLabel);

        // Caption card — pinned flush at top:50, the same y-offset every
        // other screen's top bar starts at, so this reads as aligned with
        // the rest of the app even though it has no header of its own.
        var captionBlock = new VisualElement();
        captionBlock.style.position       = Position.Absolute;
        captionBlock.style.left           = 24;
        captionBlock.style.right          = 24;
        captionBlock.style.top            = 50;
        captionBlock.style.backgroundColor = new Color(0.031f, 0.043f, 0.063f, 0.6f);
        captionBlock.style.paddingTop     = 18;
        captionBlock.style.paddingBottom  = 18;
        captionBlock.style.paddingLeft    = 20;
        captionBlock.style.paddingRight   = 20;
        captionBlock.style.borderTopLeftRadius     = 4;
        captionBlock.style.borderTopRightRadius    = 4;
        captionBlock.style.borderBottomLeftRadius  = 4;
        captionBlock.style.borderBottomRightRadius = 4;

        _titleLabel = new Label { text = "" };
        _titleLabel.style.fontSize                = 20;
        _titleLabel.style.color                   = new Color(0.4f, 0.7f, 1.0f, 0.95f);
        _titleLabel.style.letterSpacing           = 5;
        _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _titleLabel.style.unityTextAlign          = TextAnchor.MiddleCenter;
        _titleLabel.style.marginBottom            = 10;

        _textLabel = new Label { text = "" };
        _textLabel.style.fontSize       = 14;
        _textLabel.style.color          = UIHelper.TextPrim; // bright — muted was illegible over a photo
        _textLabel.style.whiteSpace     = WhiteSpace.Normal;
        _textLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        _textLabel.style.minHeight      = 80; // so the card doesn't resize between slides of different lengths

        captionBlock.Add(_titleLabel);
        captionBlock.Add(_textLabel);
        _screen.Add(captionBlock);

        // ── Bottom controls ───────────────────────────────────────────────
        var bottomBlock = new VisualElement();
        bottomBlock.style.position   = Position.Absolute;
        bottomBlock.style.bottom     = 70;
        bottomBlock.style.left       = 32;
        bottomBlock.style.right      = 32;
        bottomBlock.style.alignItems = Align.Center;

        _btnNext = MakeColdBlueButton("NEXT");
        _btnNext.clicked += OnNext;
        bottomBlock.Add(_btnNext);

        _btnBeginInvestigation = MakeColdBlueButton("BEGIN INVESTIGATION");
        _btnBeginInvestigation.clicked += OnBeginInvestigation;
        bottomBlock.Add(_btnBeginInvestigation);

        _screen.Add(bottomBlock);

        SetVisible(false);

#if UNITY_EDITOR
        NavigationManager.Instance.StartCoroutine(EditorEnterKeyFallback());
#endif
    }

#if UNITY_EDITOR
    // Editor-only fallback: mirrors ResearcherSetupScreen's keyboard
    // bypass. Pressing Enter triggers whichever button is currently
    // visible (Next or Begin Investigation), independent of the mouse
    // click path this screen otherwise relies on.
    IEnumerator EditorEnterKeyFallback()
    {
        while (true)
        {
            if (_screen.style.display == DisplayStyle.Flex && Keyboard.current != null)
            {
                var kb = Keyboard.current;
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                {
                    if (_btnBeginInvestigation.style.display == DisplayStyle.Flex)
                        OnBeginInvestigation();
                    else
                        OnNext();
                }
            }
            yield return null;
        }
    }
#endif

    Button MakeColdBlueButton(string text)
    {
        var btn = new Button();
        btn.style.width             = Length.Percent(100);
        btn.style.paddingTop        = 16;
        btn.style.paddingBottom     = 16;
        // Translucent backing plate — a transparent button read fine over
        // flat black but disappears over a busy photo.
        btn.style.backgroundColor   = new Color(0.031f, 0.043f, 0.063f, 0.75f);
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

    // ── Entry point ────────────────────────────────────────────────────────

    public void Begin()
    {
        _currentSlide = 0;
        _startTime    = Time.time;
        ShowSlide(0, animate: false);
    }

    void OnNext()
    {
        if (_currentSlide >= Slides.Length - 1) return;
        _currentSlide++;
        ShowSlide(_currentSlide, animate: true);
    }

    void OnBeginInvestigation()
    {
        float duration = Time.time - _startTime;
        StudyLogger.Instance?.OnOnboardingComplete(duration);
        NavigationManager.Instance.GoTo(GameScreen.Home);
    }

    // ── Slide display ──────────────────────────────────────────────────────
    // No forced hold timer anywhere here — pacing is entirely
    // player-controlled via the NEXT button.

    void ShowSlide(int index, bool animate)
    {
        bool isLast = index == Slides.Length - 1;
        _btnNext.style.display               = isLast ? DisplayStyle.None : DisplayStyle.Flex;
        _btnBeginInvestigation.style.display = isLast ? DisplayStyle.Flex : DisplayStyle.None;

        if (animate)
            NavigationManager.Instance.StartCoroutine(FadeToSlide(Slides[index]));
        else
            ApplySlide(Slides[index]);
    }

    void ApplySlide(SlideData slide)
    {
        _titleLabel.text = slide.Title;
        _textLabel.text  = slide.Text;

        if (!string.IsNullOrEmpty(slide.ImageResource))
        {
            var tex = Resources.Load<Texture2D>(slide.ImageResource);
            _bgImage.style.backgroundImage = tex != null ? new StyleBackground(tex) : null;
            _iconLabel.text = "";
        }
        else
        {
            _bgImage.style.backgroundImage = null;
            _iconLabel.text = slide.Icon;
        }
    }

    // Fade out old content (0.35s) -> swap text/image -> fade in new
    // (0.6s). Tried a typewriter effect first and were told to revert it
    // in favour of this.
    IEnumerator FadeToSlide(SlideData slide)
    {
        yield return Fade(1f, 0f, 0.35f);
        ApplySlide(slide);
        yield return Fade(0f, 1f, 0.6f);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        _titleLabel.style.opacity = from;
        _textLabel.style.opacity  = from;
        _bgImage.style.opacity    = from;
        _iconLabel.style.opacity  = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Lerp(from, to, elapsed / duration);
            _titleLabel.style.opacity = t;
            _textLabel.style.opacity  = t;
            _bgImage.style.opacity    = t;
            _iconLabel.style.opacity  = t;
            yield return null;
        }

        _titleLabel.style.opacity = to;
        _textLabel.style.opacity  = to;
        _bgImage.style.opacity    = to;
        _iconLabel.style.opacity  = to;
    }

    public void SetVisible(bool v) =>
        _screen.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
}
