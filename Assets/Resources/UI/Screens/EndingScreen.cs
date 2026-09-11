using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EndingScreen
{
    readonly VisualElement _screen;
    VisualElement _choiceBlock;
    VisualElement _endingBlock;
    VisualElement _reasonBlock;
    Label         _endingTitle;
    Label         _endingText;
    Label         _continueHint;
    Label         _finalLine;
    Label         _finalContinueHint;
    bool          _finalLineTapped;
    TextField     _reasonField;
    string        _pendingChoice;
    float         _choiceShownTime;
    float         _choiceDecisionTimeSecs;

    List<string> _endingChunks;
    int          _endingChunkIndex;
    Action       _onEndingChunksDone;

    public EndingScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        _screen.style.justifyContent = Justify.SpaceBetween;
        root.Add(_screen);

        var topBar  = UIHelper.TopBar();
        var spacer1 = new VisualElement(); spacer1.style.width = 44;
        var spacer2 = new VisualElement(); spacer2.style.width = 44;
        topBar.Add(spacer1);
        topBar.Add(UIHelper.ScreenTitle("FINAL DECISION"));
        topBar.Add(spacer2);
        _screen.Add(topBar);

        var scroll = new ScrollView();
        scroll.style.position     = Position.Absolute;
        scroll.style.top          = 116;
        scroll.style.left         = 0;
        scroll.style.right        = 0;
        scroll.style.bottom       = 0;
        scroll.style.paddingLeft  = 24;
        scroll.style.paddingRight = 24;
        scroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        // ── Choice block ───────────────────────────────────────────────────
        _choiceBlock = new VisualElement();
        _choiceBlock.style.alignItems = Align.Center;
        _choiceBlock.style.width      = Length.Percent(100);
        _choiceBlock.style.marginTop  = 40;

        var choiceTag = new Label { text = "YOU KNOW THE TRUTH NOW" };
        choiceTag.style.fontSize       = 9;
        choiceTag.style.color          = new Color(0.4f, 0.7f, 1.0f, 0.8f);
        choiceTag.style.letterSpacing  = 4;
        choiceTag.style.unityTextAlign = TextAnchor.MiddleCenter;
        choiceTag.style.marginBottom   = 20;

        var choiceQuestion = new Label
        {
            text = "You are the last witness.\n" +
                   "The last memory of what humanity was.\n\n" +
                   "What do you choose?"
        };
        choiceQuestion.style.fontSize       = 18;
        choiceQuestion.style.color          = UIHelper.TextPrim;
        choiceQuestion.style.unityTextAlign = TextAnchor.MiddleCenter;
        choiceQuestion.style.whiteSpace     = WhiteSpace.Normal;
        choiceQuestion.style.width          = Length.Percent(100);
        choiceQuestion.style.marginBottom   = 50;

        var btnRestore = new Button();
        btnRestore.style.width             = Length.Percent(100);
        btnRestore.style.paddingTop        = 18;
        btnRestore.style.paddingBottom     = 18;
        btnRestore.style.marginBottom      = 12;
        btnRestore.style.backgroundColor   = Color.clear;
        btnRestore.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnRestore.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnRestore.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnRestore.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnRestore.style.borderTopWidth    = 1;
        btnRestore.style.borderBottomWidth = 1;
        btnRestore.style.borderLeftWidth   = 1;
        btnRestore.style.borderRightWidth  = 1;
        btnRestore.style.borderTopLeftRadius     = 2;
        btnRestore.style.borderTopRightRadius    = 2;
        btnRestore.style.borderBottomLeftRadius  = 2;
        btnRestore.style.borderBottomRightRadius = 2;

        var restoreRow = new VisualElement();
        restoreRow.style.flexDirection  = FlexDirection.Row;
        restoreRow.style.alignItems     = Align.Center;
        restoreRow.style.justifyContent = Justify.Center;
        var restoreLabel = new Label { text = "RESTORE HUMANITY" };
        restoreLabel.style.fontSize      = 11;
        restoreLabel.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.9f);
        restoreLabel.style.letterSpacing = 3;
        restoreRow.Add(restoreLabel);
        btnRestore.Add(restoreRow);
        btnRestore.clicked += () => ShowReasonBlock("restore");

        var orContainer = new VisualElement();
        orContainer.style.flexDirection = FlexDirection.Row;
        orContainer.style.alignItems    = Align.Center;
        orContainer.style.width         = Length.Percent(100);
        orContainer.style.marginBottom  = 12;

        var orLine1 = new VisualElement();
        orLine1.style.flexGrow        = 1;
        orLine1.style.height          = 1;
        orLine1.style.backgroundColor = UIHelper.Border;

        var orLabel = new Label { text = "  OR  " };
        orLabel.style.fontSize      = 10;
        orLabel.style.color         = UIHelper.TextMuted;
        orLabel.style.letterSpacing = 2;

        var orLine2 = new VisualElement();
        orLine2.style.flexGrow        = 1;
        orLine2.style.height          = 1;
        orLine2.style.backgroundColor = UIHelper.Border;

        orContainer.Add(orLine1);
        orContainer.Add(orLabel);
        orContainer.Add(orLine2);

        var btnEnd = new Button();
        btnEnd.style.width             = Length.Percent(100);
        btnEnd.style.paddingTop        = 18;
        btnEnd.style.paddingBottom     = 18;
        btnEnd.style.marginBottom      = 40;
        btnEnd.style.backgroundColor   = Color.clear;
        btnEnd.style.borderTopColor    = UIHelper.Border;
        btnEnd.style.borderBottomColor = UIHelper.Border;
        btnEnd.style.borderLeftColor   = UIHelper.Border;
        btnEnd.style.borderRightColor  = UIHelper.Border;
        btnEnd.style.borderTopWidth    = 1;
        btnEnd.style.borderBottomWidth = 1;
        btnEnd.style.borderLeftWidth   = 1;
        btnEnd.style.borderRightWidth  = 1;
        btnEnd.style.borderTopLeftRadius     = 2;
        btnEnd.style.borderTopRightRadius    = 2;
        btnEnd.style.borderBottomLeftRadius  = 2;
        btnEnd.style.borderBottomRightRadius = 2;

        var endRow = new VisualElement();
        endRow.style.flexDirection  = FlexDirection.Row;
        endRow.style.alignItems     = Align.Center;
        endRow.style.justifyContent = Justify.Center;
        var endLabel = new Label { text = "LET IT END" };
        endLabel.style.fontSize      = 11;
        endLabel.style.color         = UIHelper.TextMuted;
        endLabel.style.letterSpacing = 3;
        endRow.Add(endLabel);
        btnEnd.Add(endRow);
        btnEnd.clicked += () => ShowReasonBlock("end");

        _choiceBlock.Add(choiceTag);
        _choiceBlock.Add(choiceQuestion);
        _choiceBlock.Add(btnRestore);
        _choiceBlock.Add(orContainer);
        _choiceBlock.Add(btnEnd);
        scroll.Add(_choiceBlock);

        // ── Reason block — shown after choice ─────────────────────────────
        _reasonBlock = new VisualElement();
        _reasonBlock.style.width   = Length.Percent(100);
        _reasonBlock.style.display = DisplayStyle.None;

        var reasonTag = new Label
        {
            text = "ONE LAST QUESTION"
        };
        reasonTag.style.fontSize       = 9;
        reasonTag.style.color          =
            new Color(0.4f, 0.7f, 1.0f, 0.7f);
        reasonTag.style.letterSpacing  = 4;
        reasonTag.style.marginBottom   = 12;
        reasonTag.style.unityTextAlign = TextAnchor.MiddleCenter;
        reasonTag.style.width          = Length.Percent(100);
        reasonTag.style.marginTop      = 40;

        var reasonHint = new Label
        {
            text = "In one sentence — why did you make this choice?"
        };
        reasonHint.style.fontSize       = 14;
        reasonHint.style.color          = UIHelper.TextPrim;
        reasonHint.style.unityTextAlign = TextAnchor.MiddleCenter;
        reasonHint.style.whiteSpace     = WhiteSpace.Normal;
        reasonHint.style.width          = Length.Percent(100);
        reasonHint.style.marginBottom   = 20;

        _reasonField = new TextField { multiline = false };
        _reasonField.style.height = 50;
        _reasonField.style.backgroundColor   = UIHelper.Surface;
        _reasonField.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        _reasonField.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        _reasonField.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        _reasonField.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        _reasonField.style.borderTopWidth    = 1;
        _reasonField.style.borderBottomWidth = 1;
        _reasonField.style.borderLeftWidth   = 1;
        _reasonField.style.borderRightWidth  = 1;
        _reasonField.style.borderTopLeftRadius     = 2;
        _reasonField.style.borderTopRightRadius    = 2;
        _reasonField.style.borderBottomLeftRadius  = 2;
        _reasonField.style.borderBottomRightRadius = 2;
        _reasonField.style.color        = UIHelper.TextPrim;
        _reasonField.style.fontSize     = 13;
        _reasonField.style.marginBottom = 20;
        _reasonField.RegisterCallback<GeometryChangedEvent>(_ => {
            var input =
                _reasonField.Q<VisualElement>("unity-text-input");
            if (input != null)
                input.style.backgroundColor = UIHelper.Surface;
        });

        var btnConfirm = new Button();
        btnConfirm.style.width             = Length.Percent(100);
        btnConfirm.style.paddingTop        = 14;
        btnConfirm.style.paddingBottom     = 14;
        btnConfirm.style.backgroundColor   = Color.clear;
        btnConfirm.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnConfirm.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnConfirm.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnConfirm.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btnConfirm.style.borderTopWidth    = 1;
        btnConfirm.style.borderBottomWidth = 1;
        btnConfirm.style.borderLeftWidth   = 1;
        btnConfirm.style.borderRightWidth  = 1;
        btnConfirm.style.borderTopLeftRadius     = 2;
        btnConfirm.style.borderTopRightRadius    = 2;
        btnConfirm.style.borderBottomLeftRadius  = 2;
        btnConfirm.style.borderBottomRightRadius = 2;

        var confirmRow = new VisualElement();
        confirmRow.style.flexDirection  = FlexDirection.Row;
        confirmRow.style.alignItems     = Align.Center;
        confirmRow.style.justifyContent = Justify.Center;
        var confirmLabel = new Label { text = "CONFIRM CHOICE" };
        confirmLabel.style.fontSize      = 11;
        confirmLabel.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.9f);
        confirmLabel.style.letterSpacing = 3;
        confirmRow.Add(confirmLabel);
        btnConfirm.Add(confirmRow);
        btnConfirm.clicked += OnConfirmChoice;

        _reasonBlock.Add(reasonTag);
        _reasonBlock.Add(reasonHint);
        _reasonBlock.Add(_reasonField);
        _reasonBlock.Add(btnConfirm);
        scroll.Add(_reasonBlock);

        // ── Ending block ───────────────────────────────────────────────────
        _endingBlock = new VisualElement();
        _endingBlock.style.width   = Length.Percent(100);
        _endingBlock.style.display = DisplayStyle.None;

        var endingCard = UIHelper.AccentCard();
        endingCard.style.marginBottom = 24;

        _endingTitle = new Label { text = "" };
        _endingTitle.style.fontSize                = 28;
        _endingTitle.style.color                   = UIHelper.TextPrim;
        _endingTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        _endingTitle.style.letterSpacing           = 2;
        _endingTitle.style.marginBottom            = 16;
        _endingTitle.style.whiteSpace              = WhiteSpace.Normal;

        _endingText = new Label { text = "" };
        _endingText.style.fontSize   = 15;
        _endingText.style.color      = UIHelper.TextMuted;
        _endingText.style.whiteSpace = WhiteSpace.Normal;

        _continueHint = new Label { text = "▸ TAP TO CONTINUE" };
        _continueHint.style.fontSize       = 9;
        _continueHint.style.color          = new Color(0.4f, 0.7f, 1.0f, 0.6f);
        _continueHint.style.letterSpacing  = 2;
        _continueHint.style.marginTop      = 10;
        _continueHint.style.display        = DisplayStyle.None;

        endingCard.Add(_endingTitle);
        endingCard.Add(_endingText);
        endingCard.Add(_continueHint);
        endingCard.RegisterCallback<ClickEvent>(_ => AdvanceEndingChunk());
        _endingBlock.Add(endingCard);

        _finalLine = new Label { text = "" };
        _finalLine.style.fontSize       = 13;
        _finalLine.style.color          =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _finalLine.style.unityTextAlign = TextAnchor.MiddleCenter;
        _finalLine.style.whiteSpace     = WhiteSpace.Normal;
        _finalLine.style.letterSpacing  = 2;
        _finalLine.style.marginBottom   = 20;
        _finalLine.style.width          = Length.Percent(100);
        _finalLine.RegisterCallback<ClickEvent>(_ => _finalLineTapped = true);
        _endingBlock.Add(_finalLine);

        // Player-paced instead of a fixed timer — the final line is read
        // at very different speeds by different people, and a flat 3s
        // auto-advance to Session End didn't give slower readers a real
        // moment with the last thing they'll read in the session.
        _finalContinueHint = new Label { text = "▸ TAP TO CONTINUE" };
        _finalContinueHint.style.fontSize       = 9;
        _finalContinueHint.style.color          = new Color(0.4f, 0.7f, 1.0f, 0.6f);
        _finalContinueHint.style.letterSpacing  = 2;
        _finalContinueHint.style.unityTextAlign = TextAnchor.MiddleCenter;
        _finalContinueHint.style.width          = Length.Percent(100);
        _finalContinueHint.style.marginBottom   = 20;
        _finalContinueHint.style.opacity        = 0;
        _finalContinueHint.RegisterCallback<ClickEvent>(_ => _finalLineTapped = true);
        _endingBlock.Add(_finalContinueHint);

        var afterglowLabel = new Label { text = "AFTERGLOW" };
        afterglowLabel.style.fontSize       = 11;
        afterglowLabel.style.color          =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        afterglowLabel.style.letterSpacing  = 8;
        afterglowLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        afterglowLabel.style.width          = Length.Percent(100);
        afterglowLabel.style.marginBottom   = 60;
        _endingBlock.Add(afterglowLabel);

        scroll.Add(_endingBlock);
        _screen.Add(scroll);

        SetVisible(false);
    }

    // ── Entry point ────────────────────────────────────────────────────────

    public void Show()
    {
        SetVisible(true);
        _choiceBlock.style.display = DisplayStyle.Flex;
        _reasonBlock.style.display = DisplayStyle.None;
        _endingBlock.style.display = DisplayStyle.None;
        _choiceBlock.style.opacity = 0;
        _choiceShownTime = Time.time;
        NavigationManager.Instance.StartCoroutine(
            FadeElement(_choiceBlock, 0f, 1f, 1.2f));
    }

    // ── Reason capture ─────────────────────────────────────────────────────

    void ShowReasonBlock(string choice)
    {
        _pendingChoice = choice;
        // Decision time is measured to this moment — picking RESTORE or
        // END — not including whatever follows while writing the reason,
        // which is a separate, secondary task.
        _choiceDecisionTimeSecs = Time.time - _choiceShownTime;
        _choiceBlock.style.display = DisplayStyle.None;
        _reasonBlock.style.display = DisplayStyle.Flex;
        _reasonBlock.style.opacity = 0;
        NavigationManager.Instance.StartCoroutine(
            FadeElement(_reasonBlock, 0f, 1f, 0.6f));
    }

    void OnConfirmChoice()
    {
        string reason = _reasonField.value;
        WorldStateManager.Instance?.SetEndingChoice(_pendingChoice);
        AudioHapticsManager.Instance?.PlayFinalChoiceCue();

        // Log to study logger
        StudyLogger.Instance?.OnFinalChoice(
            _pendingChoice, reason, Mathf.RoundToInt(_choiceDecisionTimeSecs));

        _reasonBlock.style.display = DisplayStyle.None;

        NarrativeGenerator.Instance.GenerateEnding(
            WorldStateManager.Instance?.State ?? new WorldState(),
            _pendingChoice,
            OnEndingReady);
    }

    void OnEndingReady(EndingData data)
    {
        NavigationManager.Instance.StartCoroutine(
            RevealEnding(data));
    }

    void ShowEndingChunk()
    {
        _endingText.text = _endingChunks[_endingChunkIndex];
        bool hasMore = _endingChunkIndex < _endingChunks.Count - 1;
        _continueHint.style.display = hasMore ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void AdvanceEndingChunk()
    {
        if (_endingChunks == null || _endingChunkIndex >= _endingChunks.Count - 1) return;

        _endingChunkIndex++;
        ShowEndingChunk();

        if (_endingChunkIndex >= _endingChunks.Count - 1)
            _onEndingChunksDone?.Invoke();
    }

    // ── Cinematic reveal ───────────────────────────────────────────────────

    IEnumerator RevealEnding(EndingData data)
    {
        _endingBlock.style.display = DisplayStyle.Flex;
        _endingBlock.style.opacity = 0;

        yield return new WaitForSeconds(1.5f);

        yield return NavigationManager.Instance.StartCoroutine(
            FadeElement(_endingBlock, 0f, 1f, 1.5f));

        yield return new WaitForSeconds(0.5f);

        _endingTitle.text = data.EndingTitle;
        yield return NavigationManager.Instance.StartCoroutine(
            FadeElement(_endingTitle, 0f, 1f, 1.0f));

        yield return new WaitForSeconds(0.8f);

        // Chunked, tap-to-continue reveal — this is the last thing the
        // participant reads in the whole session, so it gets the same
        // working-memory-friendly treatment as everything else rather
        // than dumping the full text at once.
        _endingChunks     = UIHelper.ChunkByWords(data.EndingText, 80);
        _endingChunkIndex = 0;
        bool chunksComplete = false;
        _onEndingChunksDone = () => chunksComplete = true;
        ShowEndingChunk();

        yield return NavigationManager.Instance.StartCoroutine(
            FadeElement(_endingText, 0f, 1f, 1.2f));

        if (_endingChunks.Count > 1)
            yield return new WaitUntil(() => chunksComplete);
        else
            yield return new WaitForSeconds(1.2f);

        _finalLine.text = data.FinalLine;
        yield return NavigationManager.Instance.StartCoroutine(
            FadeElement(_finalLine, 0f, 1f, 2.0f));

        // Minimum read time before the hint even appears, so it's never
        // possible to tap through the final line by accident the instant
        // it fades in — then wait as long as the reader actually needs.
        yield return new WaitForSeconds(1.5f);
        _finalLineTapped = false;
        yield return NavigationManager.Instance.StartCoroutine(
            FadeElement(_finalContinueHint, 0f, 1f, 0.8f));
        yield return new WaitUntil(() => _finalLineTapped);

        NavigationManager.Instance.ShowSessionEnd();
    }

    IEnumerator FadeElement(VisualElement el,
        float from, float to, float duration)
    {
        float elapsed    = 0f;
        el.style.opacity = from;
        while (elapsed < duration)
        {
            elapsed         += Time.deltaTime;
            el.style.opacity =
                Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        el.style.opacity = to;
    }

    public void SetVisible(bool v) =>
        _screen.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
}