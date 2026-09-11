using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class TheoryResultScreen
{
    readonly VisualElement _screen;
    VisualElement _resultCard;
    Label _resultTag;
    Label _resultTitle;
    Label _resultMessage;
    Label _hintLabel;
    Button _btnContinue;
    Label _truthReveal;
    VisualElement _summaryCard;
    VisualElement _summaryList;

    public TheoryResultScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        _screen.style.justifyContent = Justify.SpaceBetween;
        root.Add(_screen);

        // Top bar
        var topBar  = UIHelper.TopBar();
        var spacer1 = new VisualElement(); spacer1.style.width = 44;
        var spacer2 = new VisualElement(); spacer2.style.width = 44;
        topBar.Add(spacer1);
        topBar.Add(UIHelper.ScreenTitle("ANALYSIS"));
        topBar.Add(spacer2);
        _screen.Add(topBar);

        // Scroll
        var scroll = new ScrollView();
        scroll.style.position     = Position.Absolute;
        scroll.style.top          = 116;
        scroll.style.left         = 0;
        scroll.style.right        = 0;
        scroll.style.bottom       = 100;
        scroll.style.paddingLeft  = 24;
        scroll.style.paddingRight = 24;
        scroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        // Result card
        _resultCard = new VisualElement();
        _resultCard.style.backgroundColor         = UIHelper.Surface;
        _resultCard.style.borderTopColor          = UIHelper.Border;
        _resultCard.style.borderBottomColor       = UIHelper.Border;
        _resultCard.style.borderLeftColor         =
            new Color(0.4f, 0.7f, 1.0f, 0.6f);
        _resultCard.style.borderRightColor        = UIHelper.Border;
        _resultCard.style.borderTopWidth          = 1;
        _resultCard.style.borderBottomWidth       = 1;
        _resultCard.style.borderLeftWidth         = 3;
        _resultCard.style.borderRightWidth        = 1;
        _resultCard.style.borderTopLeftRadius     = 2;
        _resultCard.style.borderTopRightRadius    = 2;
        _resultCard.style.borderBottomLeftRadius  = 2;
        _resultCard.style.borderBottomRightRadius = 2;
        _resultCard.style.paddingTop              = 20;
        _resultCard.style.paddingBottom           = 20;
        _resultCard.style.paddingLeft             = 20;
        _resultCard.style.paddingRight            = 20;
        _resultCard.style.marginBottom            = 20;

        _resultTag = new Label { text = "ANALYSING..." };
        _resultTag.style.fontSize      = 9;
        _resultTag.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _resultTag.style.letterSpacing = 4;
        _resultTag.style.marginBottom  = 10;

        _resultTitle = new Label { text = "" };
        _resultTitle.style.fontSize                = 26;
        _resultTitle.style.color                   = UIHelper.TextPrim;
        _resultTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        _resultTitle.style.whiteSpace              = WhiteSpace.Normal;
        _resultTitle.style.marginBottom            = 14;
        _resultTitle.style.letterSpacing           = 1;

        _resultMessage = new Label { text = "" };
        _resultMessage.style.fontSize   = 14;
        _resultMessage.style.color      = UIHelper.TextMuted;
        _resultMessage.style.whiteSpace = WhiteSpace.Normal;

        _resultCard.Add(_resultTag);
        _resultCard.Add(_resultTitle);
        _resultCard.Add(_resultMessage);
        scroll.Add(_resultCard);

        // Hint card
        var hintCard = new VisualElement();
        hintCard.name = "HintCard";
        hintCard.style.backgroundColor         = UIHelper.Surface;
        hintCard.style.borderTopColor          = UIHelper.Border;
        hintCard.style.borderBottomColor       = UIHelper.Border;
        hintCard.style.borderLeftColor         = UIHelper.Border;
        hintCard.style.borderRightColor        = UIHelper.Border;
        hintCard.style.borderTopWidth          = 1;
        hintCard.style.borderBottomWidth       = 1;
        hintCard.style.borderLeftWidth         = 1;
        hintCard.style.borderRightWidth        = 1;
        hintCard.style.borderTopLeftRadius     = 2;
        hintCard.style.borderTopRightRadius    = 2;
        hintCard.style.borderBottomLeftRadius  = 2;
        hintCard.style.borderBottomRightRadius = 2;
        hintCard.style.paddingTop              = 16;
        hintCard.style.paddingBottom           = 16;
        hintCard.style.paddingLeft             = 20;
        hintCard.style.paddingRight            = 20;
        hintCard.style.marginBottom            = 20;

        var hintHeader = new Label { text = "KEEP LOOKING" };
        hintHeader.style.fontSize      = 9;
        hintHeader.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.5f);
        hintHeader.style.letterSpacing = 4;
        hintHeader.style.marginBottom  = 8;

        _hintLabel = new Label { text = "" };
        _hintLabel.style.fontSize                = 13;
        _hintLabel.style.color                   = UIHelper.TextMuted;
        _hintLabel.style.whiteSpace              = WhiteSpace.Normal;
        _hintLabel.style.unityFontStyleAndWeight = FontStyle.Italic;

        hintCard.Add(hintHeader);
        hintCard.Add(_hintLabel);
        scroll.Add(hintCard);

        // Truth card
        var truthCard = new VisualElement();
        truthCard.name = "TruthCard";
        truthCard.style.backgroundColor         = UIHelper.Surface;
        truthCard.style.borderTopColor          =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        truthCard.style.borderBottomColor       =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        truthCard.style.borderLeftColor         =
            new Color(0.4f, 0.7f, 1.0f, 0.6f);
        truthCard.style.borderRightColor        =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        truthCard.style.borderTopWidth          = 1;
        truthCard.style.borderBottomWidth       = 1;
        truthCard.style.borderLeftWidth         = 2;
        truthCard.style.borderRightWidth        = 1;
        truthCard.style.borderTopLeftRadius     = 2;
        truthCard.style.borderTopRightRadius    = 2;
        truthCard.style.borderBottomLeftRadius  = 2;
        truthCard.style.borderBottomRightRadius = 2;
        truthCard.style.paddingTop              = 16;
        truthCard.style.paddingBottom           = 16;
        truthCard.style.paddingLeft             = 20;
        truthCard.style.paddingRight            = 20;
        truthCard.style.marginBottom            = 20;

        var truthHeader = new Label { text = "THE TRUTH" };
        truthHeader.style.fontSize      = 9;
        truthHeader.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        truthHeader.style.letterSpacing = 4;
        truthHeader.style.marginBottom  = 8;

        _truthReveal = new Label { text = "" };
        _truthReveal.style.fontSize   = 14;
        _truthReveal.style.color      = UIHelper.TextPrim;
        _truthReveal.style.whiteSpace = WhiteSpace.Normal;

        truthCard.Add(truthHeader);
        truthCard.Add(_truthReveal);
        scroll.Add(truthCard);

        // Narrative state summary — shown only after rooms 2, 4, 6
        _summaryCard = new VisualElement();
        _summaryCard.name = "SummaryCard";
        _summaryCard.style.display              = DisplayStyle.None;
        _summaryCard.style.backgroundColor      = UIHelper.Surface;
        _summaryCard.style.borderTopColor       = UIHelper.Border;
        _summaryCard.style.borderBottomColor    = UIHelper.Border;
        _summaryCard.style.borderLeftColor      = UIHelper.Border;
        _summaryCard.style.borderRightColor     = UIHelper.Border;
        _summaryCard.style.borderTopWidth       = 1;
        _summaryCard.style.borderBottomWidth    = 1;
        _summaryCard.style.borderLeftWidth      = 1;
        _summaryCard.style.borderRightWidth     = 1;
        _summaryCard.style.borderTopLeftRadius     = 2;
        _summaryCard.style.borderTopRightRadius    = 2;
        _summaryCard.style.borderBottomLeftRadius  = 2;
        _summaryCard.style.borderBottomRightRadius = 2;
        _summaryCard.style.paddingTop           = 16;
        _summaryCard.style.paddingBottom        = 16;
        _summaryCard.style.paddingLeft          = 20;
        _summaryCard.style.paddingRight         = 20;
        _summaryCard.style.marginBottom         = 20;
        _summaryCard.style.opacity              = 0;

        var summaryHeader = new Label { text = "YOUR INVESTIGATION SO FAR" };
        summaryHeader.style.fontSize      = 9;
        summaryHeader.style.color         = new Color(0.4f, 0.7f, 1.0f, 0.6f);
        summaryHeader.style.letterSpacing = 4;
        summaryHeader.style.marginBottom  = 10;
        _summaryCard.Add(summaryHeader);

        _summaryList = new VisualElement();
        _summaryCard.Add(_summaryList);

        scroll.Add(_summaryCard);

        _screen.Add(scroll);

        // Continue button
        var bottomBlock = new VisualElement();
        bottomBlock.style.position          = Position.Absolute;
        bottomBlock.style.bottom            = 0;
        bottomBlock.style.left              = 0;
        bottomBlock.style.right             = 0;
        bottomBlock.style.paddingLeft       = 24;
        bottomBlock.style.paddingRight      = 24;
        bottomBlock.style.paddingTop        = 16;
        bottomBlock.style.paddingBottom     = 40;
        bottomBlock.style.backgroundColor   = UIHelper.BG;
        bottomBlock.style.borderTopColor    = UIHelper.Border;
        bottomBlock.style.borderTopWidth    = 1;

        _btnContinue = new Button();
        _btnContinue.style.width             = Length.Percent(100);
        _btnContinue.style.paddingTop        = 14;
        _btnContinue.style.paddingBottom     = 14;
        _btnContinue.style.opacity           = 0;
        _btnContinue.style.backgroundColor   = Color.clear;
        _btnContinue.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnContinue.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnContinue.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnContinue.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _btnContinue.style.borderTopWidth    = 1;
        _btnContinue.style.borderBottomWidth = 1;
        _btnContinue.style.borderLeftWidth   = 1;
        _btnContinue.style.borderRightWidth  = 1;
        _btnContinue.style.borderTopLeftRadius     = 2;
        _btnContinue.style.borderTopRightRadius    = 2;
        _btnContinue.style.borderBottomLeftRadius  = 2;
        _btnContinue.style.borderBottomRightRadius = 2;

        var continueRow = new VisualElement();
        continueRow.style.flexDirection  = FlexDirection.Row;
        continueRow.style.alignItems     = Align.Center;
        continueRow.style.justifyContent = Justify.Center;

        var continueLabel = new Label { text = "CONTINUE INVESTIGATION" };
        continueLabel.style.fontSize      = 11;
        continueLabel.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.9f);
        continueLabel.style.letterSpacing = 3;

        var continueArrow = UIHelper.Icon(
            UIHelper.IconArrowRight, 14f,
            new Color(0.4f, 0.7f, 1.0f, 0.9f));
        continueArrow.style.marginLeft = 8;

        continueRow.Add(continueLabel);
        continueRow.Add(continueArrow);
        _btnContinue.Add(continueRow);
        _btnContinue.clicked += OnContinue;
        bottomBlock.Add(_btnContinue);
        _screen.Add(bottomBlock);

        SetVisible(false);
    }

    // ── Public entry point ─────────────────────────────────────────────────

    public void ShowResult(TheoryAnalysis analysis, string truthReveal)
    {
        SetVisible(true);
        NavigationManager.Instance.StartCoroutine(
            RevealResult(analysis, truthReveal));
    }

    // ── Cinematic reveal ───────────────────────────────────────────────────

    IEnumerator RevealResult(TheoryAnalysis analysis, string truthReveal)
    {
        _resultTag.text     = "ANALYSING...";
        _resultTitle.text   = "";
        _resultMessage.text = "";
        _hintLabel.text     = "";
        _truthReveal.text   = "";
        _btnContinue.style.opacity = 0;

        var hintCard  = _screen.Q("HintCard");
        var truthCard = _screen.Q("TruthCard");
        if (hintCard  != null) hintCard.style.display  = DisplayStyle.None;
        if (truthCard != null) truthCard.style.display = DisplayStyle.None;
        _summaryCard.style.display = DisplayStyle.None;
        _summaryCard.style.opacity = 0;

        yield return new WaitForSeconds(1.2f);

        // All outcomes use cold blue — only brightness varies
        string resultTitle;
        Color  tagColor;
        Color  borderColor;

        switch (analysis.Result.ToLower())
        {
            case "correct":
                resultTitle = "You found the truth.";
                tagColor    = new Color(0.4f, 0.8f, 1.0f, 1.0f);
                borderColor = new Color(0.4f, 0.8f, 1.0f, 0.8f);
                _resultTag.text = "THEORY CONFIRMED";
                break;
            case "partial":
                resultTitle = "You are close.";
                tagColor    = new Color(0.4f, 0.65f, 0.9f, 0.9f);
                borderColor = new Color(0.4f, 0.65f, 0.9f, 0.6f);
                _resultTag.text = "PARTIALLY CORRECT";
                break;
            default:
                resultTitle = "The truth runs deeper.";
                tagColor    = new Color(0.3f, 0.45f, 0.65f, 0.8f);
                borderColor = new Color(0.3f, 0.45f, 0.65f, 0.5f);
                _resultTag.text = "THEORY INCOMPLETE";
                break;
        }

        _resultTag.style.color         = tagColor;
        _resultCard.style.borderLeftColor = borderColor;

        yield return NavigationManager.Instance.StartCoroutine(
            FadeElement(_resultTag, 0f, 1f, 0.5f));

        yield return new WaitForSeconds(0.4f);

        _resultTitle.text = resultTitle;
        yield return NavigationManager.Instance.StartCoroutine(
            FadeElement(_resultTitle, 0f, 1f, 0.8f));

        yield return new WaitForSeconds(0.5f);

        _resultMessage.text = analysis.Message;
        yield return NavigationManager.Instance.StartCoroutine(
            FadeElement(_resultMessage, 0f, 1f, 1.0f));

        yield return new WaitForSeconds(0.6f);

        if (analysis.Result.ToLower() != "correct" &&
            !string.IsNullOrEmpty(analysis.Hint))
        {
            if (hintCard != null)
            {
                hintCard.style.display = DisplayStyle.Flex;
                _hintLabel.text        = analysis.Hint;
                yield return NavigationManager.Instance.StartCoroutine(
                    FadeElement(hintCard, 0f, 1f, 0.8f));
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (analysis.Result.ToLower() == "correct" &&
            !string.IsNullOrEmpty(truthReveal))
        {
            if (truthCard != null)
            {
                truthCard.style.display = DisplayStyle.Flex;
                _truthReveal.text       = truthReveal;
                yield return NavigationManager.Instance.StartCoroutine(
                    FadeElement(truthCard, 0f, 1f, 0.8f));
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (TryBuildSummary())
        {
            _summaryCard.style.display = DisplayStyle.Flex;
            yield return NavigationManager.Instance.StartCoroutine(
                FadeElement(_summaryCard, 0f, 1f, 0.8f));
            yield return new WaitForSeconds(0.5f);
        }

        yield return NavigationManager.Instance.StartCoroutine(
            FadeElement(_btnContinue, 0f, 1f, 0.6f));
    }

    // ── Narrative state summary — shown only after rooms 2, 4 and 6 ────────

    bool TryBuildSummary()
    {
        var state = WorldStateManager.Instance?.State;
        int currentRoom = state?.CurrentRoom?.RoomNumber ?? 0;

        if (state == null ||
            (currentRoom != 2 && currentRoom != 4 && currentRoom != 6))
            return false;

        _summaryList.Clear();
        bool any = false;

        foreach (var room in state.Rooms)
        {
            if (!room.IsComplete) continue;
            _summaryList.Add(MakeSummaryRow(room));
            any = true;
        }

        return any;
    }

    VisualElement MakeSummaryRow(RoomRecord room)
    {
        var row = new VisualElement();
        row.style.flexDirection  = FlexDirection.Row;
        row.style.alignItems     = Align.Center;
        row.style.marginBottom   = 8;

        var roomLabel = new Label { text = $"Room {room.RoomNumber}" };
        roomLabel.style.fontSize      = 11;
        roomLabel.style.color         = UIHelper.TextPrim;
        roomLabel.style.marginRight   = 8;
        roomLabel.style.flexShrink    = 0;

        string result = (room.AIAnalysis ?? "").ToLower();
        Color dotColor = result switch
        {
            "correct" => new Color(0.3f, 0.7f, 0.4f, 1.0f),
            "partial" => new Color(0.4f, 0.7f, 1.0f, 0.5f),
            _         => new Color(0.4f, 0.5f, 0.6f, 1.0f)
        };

        var dot = new Label { text = "●" };
        dot.style.fontSize    = 10;
        dot.style.color       = dotColor;
        dot.style.marginRight = 6;
        dot.style.flexShrink  = 0;

        var resultLabel = new Label
        {
            text = string.IsNullOrEmpty(result) ? "—" : result
        };
        resultLabel.style.fontSize    = 11;
        resultLabel.style.color       = UIHelper.TextMuted;
        resultLabel.style.marginRight = 10;
        resultLabel.style.flexShrink  = 0;

        var theoryLabel = new Label { text = Truncate(room.PlayerTheory, 40) };
        theoryLabel.style.fontSize                = 11;
        theoryLabel.style.color                   = UIHelper.TextMuted;
        theoryLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
        theoryLabel.style.whiteSpace              = WhiteSpace.Normal;
        theoryLabel.style.flexShrink              = 1;

        row.Add(roomLabel);
        row.Add(dot);
        row.Add(resultLabel);
        row.Add(theoryLabel);
        return row;
    }

    string Truncate(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.Trim();
        return text.Length <= maxChars ? text : text.Substring(0, maxChars) + "...";
    }

    // ── Continue ───────────────────────────────────────────────────────────

    void OnContinue()
    {
        var state = WorldStateManager.Instance?.State;
        if (state != null && state.AllRoomsComplete)
            NavigationManager.Instance.ShowEnding();
        else
            NavigationManager.Instance.GoTo(GameScreen.Chapters);
    }

    // ── Animation ──────────────────────────────────────────────────────────

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
}