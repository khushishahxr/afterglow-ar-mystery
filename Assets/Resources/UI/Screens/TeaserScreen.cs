using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TeaserScreen
{
    readonly VisualElement _screen;
    VisualElement _linesContainer;
    Button _btnEnter;
    Label  _enterLabel;
    int _currentChapter = 1;
    bool _waitingForNarrative = false;

    public TeaserScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        _screen.style.justifyContent = Justify.SpaceBetween;
        root.Add(_screen);

        // Plain dark cold-blue background (UIHelper.Screen()'s default) —
        // tried a background photo here (reusing Home's) and it read
        // wrong for this screen; reverted.
        var topBar  = UIHelper.TopBar();
        var btnBack = UIHelper.IconButtonImg(
            UIHelper.IconArrowLeft, 22f,
            new Color(0.4f, 0.5f, 0.6f, 0.7f));
        btnBack.clicked += () =>
            NavigationManager.Instance.GoTo(GameScreen.Chapters);
        var spacer = new VisualElement();
        spacer.style.width = 44;
        topBar.Add(btnBack);
        topBar.Add(UIHelper.ScreenTitle("BRIEFING"));
        topBar.Add(spacer);
        _screen.Add(topBar);

        _linesContainer = new VisualElement();
        _linesContainer.style.position   = Position.Absolute;
        _linesContainer.style.left       = 40;
        _linesContainer.style.right      = 40;
        _linesContainer.style.top        = Length.Percent(30);
        _linesContainer.style.alignItems = Align.Center;
        _screen.Add(_linesContainer);

        var bottomBlock = new VisualElement();
        bottomBlock.style.position   = Position.Absolute;
        bottomBlock.style.bottom     = 70;
        bottomBlock.style.left       = 40;
        bottomBlock.style.right      = 40;
        bottomBlock.style.alignItems = Align.Center;

        // Enter button — cold blue, no text property
        _btnEnter = new Button();
        _btnEnter.style.width             = Length.Percent(100);
        _btnEnter.style.paddingTop        = 14;
        _btnEnter.style.paddingBottom     = 14;
        _btnEnter.style.backgroundColor   = Color.clear;
        _btnEnter.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnEnter.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnEnter.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnEnter.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnEnter.style.borderTopWidth    = 1;
        _btnEnter.style.borderBottomWidth = 1;
        _btnEnter.style.borderLeftWidth   = 1;
        _btnEnter.style.borderRightWidth  = 1;
        _btnEnter.style.borderTopLeftRadius     = 2;
        _btnEnter.style.borderTopRightRadius    = 2;
        _btnEnter.style.borderBottomLeftRadius  = 2;
        _btnEnter.style.borderBottomRightRadius = 2;
        _btnEnter.style.opacity           = 0;

        var enterRow = new VisualElement();
        enterRow.style.flexDirection  = FlexDirection.Row;
        enterRow.style.alignItems     = Align.Center;
        enterRow.style.justifyContent = Justify.Center;

        _enterLabel = new Label { text = "ENTER THE ROOM" };
        _enterLabel.style.fontSize      = 11;
        _enterLabel.style.color         = new Color(0.4f, 0.7f, 1.0f, 0.9f);
        _enterLabel.style.letterSpacing = 3;

        var enterArrow = UIHelper.Icon(
            UIHelper.IconArrowRight, 14f,
            new Color(0.4f, 0.7f, 1.0f, 0.9f));
        enterArrow.style.marginLeft = 8;

        enterRow.Add(_enterLabel);
        enterRow.Add(enterArrow);
        _btnEnter.Add(enterRow);
        _btnEnter.clicked += OnEnterRoom;
        bottomBlock.Add(_btnEnter);

        var warning = new Label
        {
            text = "You are the last witness. What you find cannot be unfound."
        };
        warning.style.fontSize       = 10;
        warning.style.color          = UIHelper.TextMuted;
        warning.style.unityTextAlign = TextAnchor.MiddleCenter;
        warning.style.whiteSpace     = WhiteSpace.Normal;
        warning.style.marginTop      = 14;
        warning.style.opacity        = 0;
        warning.name                 = "WarningLabel";
        bottomBlock.Add(warning);

        _screen.Add(bottomBlock);
        SetVisible(false);
    }

    public void ShowForChapter(int chapterNumber)
    {
        _currentChapter = chapterNumber;
        SetVisible(true);
        NavigationManager.Instance.BeginRoomDetection(chapterNumber);
        NavigationManager.Instance.StartCoroutine(
            PlayTeaser(chapterNumber));
    }

    IEnumerator PlayTeaser(int chapterNumber)
    {
        _linesContainer.Clear();
        _btnEnter.style.opacity = 0;

        var warning = _screen.Q<Label>("WarningLabel");
        if (warning != null) warning.style.opacity = 0;

        string teaser   = WorldStateManager.Instance?
                          .GetTeaser(chapterNumber)
                          ?? "Something happened here.";
        string roomType = WorldStateManager.Instance?
                          .GetRoomType(chapterNumber)
                          ?? "room";
        string theme    = WorldStateManager.Instance?.HasTheme == true
                          ? WorldStateManager.Instance.State.Theme
                            .ToString().ToUpper()
                          : "";

        // Cold blue for all teaser lines
        var lines = new List<TeaserLine>
        {
            new TeaserLine(
                $"LOCATION {chapterNumber:D2}",
                true, 9,
                new Color(0.4f, 0.7f, 1.0f, 0.9f), 4),
            new TeaserLine(
                $"A {roomType}",
                false, 28,
                UIHelper.TextPrim, 2),
        };

        if (!string.IsNullOrEmpty(theme))
            lines.Add(new TeaserLine(
                $"[ {theme} PROTOCOL ACTIVE ]",
                false, 9,
                new Color(0.4f, 0.7f, 1.0f, 0.5f), 3));

        // Header lines (location/room type/theme) stack and stay visible —
        // they're persistent context, not story beats.
        foreach (var line in lines)
        {
            var label = new Label { text = line.Text };
            label.style.fontSize       = line.FontSize;
            label.style.color          = line.Color;
            label.style.letterSpacing  = line.LetterSpacing;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace     = WhiteSpace.Normal;
            label.style.width          = Length.Percent(100);
            label.style.opacity        = 0;
            label.style.marginBottom   = line.IsHeader ? 12 : 8;

            if (line.IsHeader)
                label.style.unityFontStyleAndWeight = FontStyle.Bold;

            _linesContainer.Add(label);

            yield return NavigationManager.Instance.StartCoroutine(
                FadeElement(label, 0f, 1f, 1.0f));

            yield return new WaitForSeconds(line.IsHeader ? 0.3f : 0.9f);
        }

        yield return new WaitForSeconds(0.4f);

        // Story beats — back to stacking cumulatively down the screen
        // (each line fades in and stays), matching the original build.
        // Kept the word-count-scaled pacing between reveals from the
        // one-at-a-time experiment since that part wasn't the complaint.
        var sentences = teaser.Split('.');
        foreach (var sentence in sentences)
        {
            string s = sentence.Trim();
            if (string.IsNullOrEmpty(s)) continue;
            string beatText = s + ".";

            var beatLabel = new Label { text = beatText };
            beatLabel.style.fontSize       = 14;
            beatLabel.style.color          = UIHelper.TextMuted;
            beatLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            beatLabel.style.whiteSpace     = WhiteSpace.Normal;
            beatLabel.style.width          = Length.Percent(100);
            beatLabel.style.marginBottom   = 8;
            beatLabel.style.opacity        = 0f;
            _linesContainer.Add(beatLabel);

            yield return NavigationManager.Instance.StartCoroutine(
                FadeElement(beatLabel, 0f, 1f, 0.5f));

            int wordCount = beatText.Split(
                (char[])null, System.StringSplitOptions.RemoveEmptyEntries).Length;
            float holdTime = Mathf.Clamp(wordCount * 0.35f, 2.0f, 4.5f);
            yield return new WaitForSeconds(holdTime);
        }

        yield return new WaitForSeconds(0.8f);

        yield return NavigationManager.Instance.StartCoroutine(
            FadeElement(_btnEnter, 0f, 1f, 0.8f));

        if (warning != null)
        {
            yield return new WaitForSeconds(0.3f);
            yield return NavigationManager.Instance.StartCoroutine(
                FadeElement(warning, 0f, 1f, 0.6f));
        }
    }

    void OnEnterRoom()
    {
        if (_waitingForNarrative) return;

        if (NavigationManager.Instance.RoomNarrativeReady)
        {
            NavigationManager.Instance.GoTo(GameScreen.Gameplay);
        }
        else
        {
            NavigationManager.Instance.StartCoroutine(
                WaitForNarrativeThenEnter());
        }
    }

    IEnumerator WaitForNarrativeThenEnter()
    {
        _waitingForNarrative = true;
        _enterLabel.text = "PREPARING...";

        while (!NavigationManager.Instance.RoomNarrativeReady)
            yield return null;

        _enterLabel.text = "ENTER THE ROOM";
        _waitingForNarrative = false;

        // The player may have backed out to Chapters while we waited —
        // don't yank them into Gameplay from somewhere else.
        if (NavigationManager.Instance.Current == GameScreen.Teaser)
            NavigationManager.Instance.GoTo(GameScreen.Gameplay);
    }

    IEnumerator FadeElement(VisualElement el,
        float from, float to, float duration)
    {
        float elapsed    = 0f;
        el.style.opacity = from;
        while (elapsed < duration)
        {
            elapsed         += Time.deltaTime;
            el.style.opacity = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        el.style.opacity = to;
    }

    public void SetVisible(bool v) =>
        _screen.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;

    class TeaserLine
    {
        public string Text;
        public bool   IsHeader;
        public float  FontSize;
        public Color  Color;
        public float  LetterSpacing;

        public TeaserLine(string text, bool isHeader,
            float fontSize, Color color, float letterSpacing)
        {
            Text = text; IsHeader = isHeader;
            FontSize = fontSize; Color = color;
            LetterSpacing = letterSpacing;
        }
    }
}