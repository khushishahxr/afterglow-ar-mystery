using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CrimeBoardScreen
{
    readonly VisualElement _screen;
    VisualElement _evidenceList;
    Label _chapterLabel, _chapterTitle, _chapterDesc;
    TextField _notesField, _theoryField;
    int _currentChapter  = 1;
    int _totalClues      = 5;
    int _confidenceValue = 3;
    float _decisionStartTime;
    float _lastTimeToDecide;
    readonly List<EvidenceItem> _collected = new();

    public CrimeBoardScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        root.Add(_screen);

        // ── Top bar ────────────────────────────────────────────────────────
        var topBar  = UIHelper.TopBar();
        var btnBack = UIHelper.IconButtonImg(
            UIHelper.IconArrowLeft, 22f,
            new Color(0.4f, 0.5f, 0.6f, 0.7f));
        btnBack.style.width  = 70; // widened to match btnJournal below, keeps the title centered
        btnBack.clicked += () =>
            NavigationManager.Instance.GoTo(GameScreen.Gameplay);

        // Text label, not an icon — and explicitly the same width as
        // btnBack, since a wider Journal button here would throw off the
        // title's centering (its flex-grow area is only symmetric when
        // both side elements match). NoWrap + tight letter spacing so
        // "JOURNAL" reliably sits on one line; grey (matching btnBack's
        // tone) since this is a secondary nav action, not primary cold-blue.
        var btnJournal = new Button { text = "JOURNAL" };
        btnJournal.style.width           = 70;
        btnJournal.style.height          = 44;
        btnJournal.style.backgroundColor = Color.clear;
        btnJournal.style.borderTopWidth = btnJournal.style.borderBottomWidth =
            btnJournal.style.borderLeftWidth = btnJournal.style.borderRightWidth = 0;
        btnJournal.style.fontSize       = 9;
        btnJournal.style.letterSpacing  = 1;
        btnJournal.style.color          = new Color(0.4f, 0.5f, 0.6f, 0.8f);
        btnJournal.style.whiteSpace     = WhiteSpace.NoWrap;
        btnJournal.style.unityTextAlign = TextAnchor.MiddleCenter;
        btnJournal.clicked += () =>
            NavigationManager.Instance.ShowJournal(GameScreen.CrimeBoard);

        topBar.Add(btnBack);
        topBar.Add(UIHelper.ScreenTitle("CASE FILE"));
        topBar.Add(btnJournal);
        _screen.Add(topBar);

        // ── Scroll ─────────────────────────────────────────────────────────
        var scroll = new ScrollView();
        scroll.style.position     = Position.Absolute;
        scroll.style.top          = 116;
        scroll.style.left         = 0;
        scroll.style.right        = 0;
        scroll.style.bottom       = 0;
        scroll.style.paddingLeft  = 20;
        scroll.style.paddingRight = 20;
        scroll.verticalScrollerVisibility   = ScrollerVisibility.Hidden;
        scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        // ── Chapter card ───────────────────────────────────────────────────
        var card = new VisualElement();
        card.style.backgroundColor         = UIHelper.Surface;
        card.style.borderTopColor          =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        card.style.borderBottomColor       =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        card.style.borderLeftColor         =
            new Color(0.4f, 0.7f, 1.0f, 0.6f);
        card.style.borderRightColor        =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        card.style.borderTopWidth          = 1;
        card.style.borderBottomWidth       = 1;
        card.style.borderLeftWidth         = 2;
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

        _chapterLabel = new Label { text = "LOCATION 01" };
        _chapterLabel.style.fontSize      = 9;
        _chapterLabel.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _chapterLabel.style.letterSpacing = 4;
        _chapterLabel.style.marginBottom  = 6;

        _chapterTitle = UIHelper.TitleLabel("The Last Signal", 22);
        _chapterTitle.style.marginBottom = 8;

        _chapterDesc = UIHelper.BodyLabel(
            "Investigate the room. Find what remains.");

        card.Add(_chapterLabel);
        card.Add(_chapterTitle);
        card.Add(_chapterDesc);
        scroll.Add(card);

        // ── Evidence section ───────────────────────────────────────────────
        scroll.Add(SectionHeader("COLLECTED EVIDENCE"));

        var countLabel = new Label { text = "0 found" };
        countLabel.name = "EvidenceCount";
        countLabel.style.fontSize     = 10;
        countLabel.style.color        = UIHelper.TextMuted;
        countLabel.style.marginBottom = 10;
        scroll.Add(countLabel);

        _evidenceList = new VisualElement();
        _evidenceList.style.width        = Length.Percent(100);
        _evidenceList.style.marginBottom = 8;
        BuildEmptySlots();
        scroll.Add(_evidenceList);

        scroll.Add(UIHelper.Divider());

        // ── Detective notes ────────────────────────────────────────────────
        scroll.Add(SectionHeader("DETECTIVE NOTES"));
        _notesField = new TextField { multiline = true };
        _notesField.style.height = 80;
        StyleField(_notesField);
        scroll.Add(_notesField);

        // ── Final theory ───────────────────────────────────────────────────
        scroll.Add(SectionHeader("FINAL THEORY"));

        var theoryHint = new Label
        {
            text = "What do you think caused the end of the world?"
        };
        theoryHint.style.fontSize     = 11;
        theoryHint.style.color        = UIHelper.TextMuted;
        theoryHint.style.marginBottom = 8;
        theoryHint.style.whiteSpace   = WhiteSpace.Normal;
        scroll.Add(theoryHint);

        _theoryField = new TextField { multiline = true };
        _theoryField.style.height = 90;
        StyleField(_theoryField);
        _theoryField.RegisterValueChangedCallback(_ => UpdateSubmitButtonState());
        scroll.Add(_theoryField);

        // ── Confidence slider ──────────────────────────────────────────────
        scroll.Add(SectionHeader("HOW CONFIDENT ARE YOU?"));

        var confidenceRow = new VisualElement();
        confidenceRow.style.flexDirection  = FlexDirection.Row;
        confidenceRow.style.alignItems     = Align.Center;
        confidenceRow.style.justifyContent = Justify.SpaceBetween;
        confidenceRow.style.marginBottom   = 6;

        var notSure = new Label { text = "Just guessing" };
        notSure.style.fontSize = 10;
        notSure.style.color    = UIHelper.TextMuted;

        var certain = new Label { text = "Certain" };
        certain.style.fontSize = 10;
        certain.style.color    = UIHelper.TextMuted;

        confidenceRow.Add(notSure);
        confidenceRow.Add(certain);
        scroll.Add(confidenceRow);

        var confidenceBtns = new VisualElement();
        confidenceBtns.style.flexDirection  = FlexDirection.Row;
        confidenceBtns.style.justifyContent = Justify.SpaceBetween;
        confidenceBtns.style.marginBottom   = 20;
        confidenceBtns.name = "ConfidenceBtns";

        for (int i = 1; i <= 5; i++)
        {
            int val = i;
            var btn = new Button { text = i.ToString() };
            btn.name = $"ConfBtn_{i}";
            StyleConfidenceBtn(btn, i == 3);
            btn.clicked += () => SelectConfidence(val);
            confidenceBtns.Add(btn);
        }
        scroll.Add(confidenceBtns);

        // ── Submit button ──────────────────────────────────────────────────
        var btnSubmit = new Button();
        btnSubmit.name                    = "SubmitBtn";
        btnSubmit.style.width             = Length.Percent(100);
        btnSubmit.style.marginBottom      = 40;
        btnSubmit.style.paddingTop        = 14;
        btnSubmit.style.paddingBottom     = 14;
        btnSubmit.style.backgroundColor   = Color.clear;
        btnSubmit.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        btnSubmit.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        btnSubmit.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        btnSubmit.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        btnSubmit.style.borderTopWidth    = 1;
        btnSubmit.style.borderBottomWidth = 1;
        btnSubmit.style.borderLeftWidth   = 1;
        btnSubmit.style.borderRightWidth  = 1;
        btnSubmit.style.borderTopLeftRadius     = 2;
        btnSubmit.style.borderTopRightRadius    = 2;
        btnSubmit.style.borderBottomLeftRadius  = 2;
        btnSubmit.style.borderBottomRightRadius = 2;
        btnSubmit.style.opacity           = 0.3f;

        var submitRow = new VisualElement();
        submitRow.style.flexDirection  = FlexDirection.Row;
        submitRow.style.alignItems     = Align.Center;
        submitRow.style.justifyContent = Justify.Center;

        var submitLabel = new Label { text = "SUBMIT THEORY" };
        submitLabel.style.fontSize      = 11;
        submitLabel.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.9f);
        submitLabel.style.letterSpacing = 3;

        var submitArrow = UIHelper.Icon(
            UIHelper.IconArrowRight, 14f,
            new Color(0.4f, 0.7f, 1.0f, 0.9f));
        submitArrow.style.marginLeft = 8;

        submitRow.Add(submitLabel);
        submitRow.Add(submitArrow);
        btnSubmit.Add(submitRow);
        btnSubmit.clicked += OnSubmitTheory;
        scroll.Add(btnSubmit);

        _screen.Add(scroll);

        EvidenceManager.OnEvidenceAdded += OnEvidenceAdded;
    }

    // ── Confidence selection ───────────────────────────────────────────────

    void SelectConfidence(int value)
    {
        _confidenceValue = value;
        var btns = _screen.Q("ConfidenceBtns");
        if (btns == null) return;
        for (int i = 1; i <= 5; i++)
        {
            var btn = btns.Q<Button>($"ConfBtn_{i}");
            if (btn != null)
                StyleConfidenceBtn(btn, i == value);
        }
    }

    void StyleConfidenceBtn(Button btn, bool selected)
    {
        btn.style.width             = 48;
        btn.style.height            = 48;
        btn.style.backgroundColor   = selected
            ? new Color(0.2f, 0.4f, 0.8f, 0.6f)
            : UIHelper.Surface;
        btn.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, selected ? 0.9f : 0.3f);
        btn.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, selected ? 0.9f : 0.3f);
        btn.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, selected ? 0.9f : 0.3f);
        btn.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, selected ? 0.9f : 0.3f);
        btn.style.borderTopWidth    = selected ? 2 : 1;
        btn.style.borderBottomWidth = selected ? 2 : 1;
        btn.style.borderLeftWidth   = selected ? 2 : 1;
        btn.style.borderRightWidth  = selected ? 2 : 1;
        btn.style.borderTopLeftRadius     = 2;
        btn.style.borderTopRightRadius    = 2;
        btn.style.borderBottomLeftRadius  = 2;
        btn.style.borderBottomRightRadius = 2;
        btn.style.color          = selected
            ? new Color(0.4f, 0.7f, 1.0f, 1.0f)
            : UIHelper.TextMuted;
        btn.style.fontSize       = 14;
        btn.style.unityTextAlign = TextAnchor.MiddleCenter;
    }

    // ── Submit button state ────────────────────────────────────────────────

    void UpdateSubmitButtonState()
    {
        var submitBtn = _screen.Q<Button>("SubmitBtn");
        if (submitBtn == null) return;

        bool allFound    = _collected.Count >= _totalClues;
        bool hasTheory   = !string.IsNullOrWhiteSpace(_theoryField?.value);
        bool ready       = allFound && hasTheory;
        submitBtn.style.opacity           = ready ? 1.0f : 0.3f;
        submitBtn.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, ready ? 0.8f : 0.3f);
        submitBtn.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, ready ? 0.8f : 0.3f);
        submitBtn.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, ready ? 0.8f : 0.3f);
        submitBtn.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, ready ? 0.8f : 0.3f);
    }

    // ── Evidence management ────────────────────────────────────────────────

    void OnEvidenceAdded(EvidenceItem item)
    {
        if (_collected.Exists(e => e.Name == item.Name))
            return;
        _collected.Add(item);
        RebuildEvidenceList();
    }

    void BuildEmptySlots()
    {
        _evidenceList.Clear();
        for (int i = 0; i < _totalClues; i++)
            _evidenceList.Add(MakeEmptySlot(i));
    }

    void RebuildEvidenceList()
    {
        _evidenceList.Clear();
        foreach (var item in _collected)
            _evidenceList.Add(MakeEvidenceCard(item));
        int remaining = _totalClues - _collected.Count;
        for (int i = 0; i < remaining; i++)
            _evidenceList.Add(MakeEmptySlot(_collected.Count + i));

        var countLabel = _screen.Q<Label>("EvidenceCount");
        if (countLabel != null)
            countLabel.text = FormatCountLabel();

        UpdateSubmitButtonState();
    }

    string FormatCountLabel()
    {
        int pct = _totalClues > 0
            ? Mathf.RoundToInt(_collected.Count * 100f / _totalClues) : 0;
        return $"{_collected.Count} / {_totalClues} found ({pct}%)";
    }

    VisualElement MakeEmptySlot(int index)
    {
        var emptyRow = new VisualElement();
        emptyRow.style.flexDirection      = FlexDirection.Row;
        emptyRow.style.alignItems         = Align.Center;
        emptyRow.style.marginBottom       = 10;
        emptyRow.style.paddingTop         = 10;
        emptyRow.style.paddingBottom      = 10;
        emptyRow.style.paddingLeft        = 14;
        emptyRow.style.paddingRight       = 14;
        emptyRow.style.backgroundColor    = UIHelper.Surface;
        emptyRow.style.borderTopColor     = UIHelper.Border;
        emptyRow.style.borderBottomColor  = UIHelper.Border;
        emptyRow.style.borderLeftColor    = UIHelper.Border;
        emptyRow.style.borderRightColor   = UIHelper.Border;
        emptyRow.style.borderTopWidth     = 1;
        emptyRow.style.borderBottomWidth  = 1;
        emptyRow.style.borderLeftWidth    = 1;
        emptyRow.style.borderRightWidth   = 1;
        emptyRow.style.borderTopLeftRadius     = 2;
        emptyRow.style.borderTopRightRadius    = 2;
        emptyRow.style.borderBottomLeftRadius  = 2;
        emptyRow.style.borderBottomRightRadius = 2;

        var emptySlot = new VisualElement();
        emptySlot.style.width             = 40;
        emptySlot.style.height            = 40;
        emptySlot.style.marginRight       = 12;
        emptySlot.style.flexShrink        = 0;
        emptySlot.style.backgroundColor   = UIHelper.BG;
        emptySlot.style.borderTopColor    = UIHelper.Border;
        emptySlot.style.borderBottomColor = UIHelper.Border;
        emptySlot.style.borderLeftColor   = UIHelper.Border;
        emptySlot.style.borderRightColor  = UIHelper.Border;
        emptySlot.style.borderTopWidth    = 1;
        emptySlot.style.borderBottomWidth = 1;
        emptySlot.style.borderLeftWidth   = 1;
        emptySlot.style.borderRightWidth  = 1;
        emptySlot.style.borderTopLeftRadius     = 2;
        emptySlot.style.borderTopRightRadius    = 2;
        emptySlot.style.borderBottomLeftRadius  = 2;
        emptySlot.style.borderBottomRightRadius = 2;
        emptySlot.style.alignItems     = Align.Center;
        emptySlot.style.justifyContent = Justify.Center;

        var q = new Label { text = "?" };
        q.style.fontSize = 18;
        q.style.color    = UIHelper.Border;
        emptySlot.Add(q);

        var emptyName = new Label { text = "Unknown evidence" };
        emptyName.style.fontSize   = 11;
        emptyName.style.color      = UIHelper.Border;
        emptyName.style.whiteSpace = WhiteSpace.Normal;

        emptyRow.Add(emptySlot);
        emptyRow.Add(emptyName);
        return emptyRow;
    }

    VisualElement MakeEvidenceCard(EvidenceItem item)
    {
        var container = new VisualElement();
        container.style.width             = Length.Percent(100);
        container.style.marginBottom      = 10;
        container.style.backgroundColor   = UIHelper.Surface;
        container.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        container.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        container.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.6f);
        container.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.3f);
        container.style.borderTopWidth    = 1;
        container.style.borderBottomWidth = 1;
        container.style.borderLeftWidth   = 2;
        container.style.borderRightWidth  = 1;
        container.style.borderTopLeftRadius     = 2;
        container.style.borderTopRightRadius    = 2;
        container.style.borderBottomLeftRadius  = 2;
        container.style.borderBottomRightRadius = 2;
        container.style.paddingTop    = 12;
        container.style.paddingBottom = 12;
        container.style.paddingLeft   = 14;
        container.style.paddingRight  = 14;

        var header = new VisualElement();
        header.style.flexDirection  = FlexDirection.Row;
        header.style.alignItems     = Align.Center;
        header.style.justifyContent = Justify.SpaceBetween;

        var headerLeft = new VisualElement();
        headerLeft.style.flexDirection = FlexDirection.Row;
        headerLeft.style.alignItems    = Align.Center;
        headerLeft.style.flexGrow      = 1;

        // item.Icon is an internal numeric ID (see PromptBuilder.EvidenceIcons),
        // not a glyph — displaying it directly used to print a bare digit
        // next to the name.
        var nameLabel = new Label { text = item.Name.ToUpper() };
        nameLabel.style.fontSize      = 10;
        nameLabel.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.85f);
        nameLabel.style.letterSpacing = 2;

        headerLeft.Add(nameLabel);

        // Bordered pill badge — a bare low-opacity chevron read as too
        // subtle to register as clickable; this reads unambiguously as a
        // button.
        var moreBadge = new VisualElement();
        moreBadge.style.flexDirection      = FlexDirection.Row;
        moreBadge.style.alignItems         = Align.Center;
        moreBadge.style.flexShrink         = 0;
        moreBadge.style.borderTopWidth     = 1;
        moreBadge.style.borderBottomWidth  = 1;
        moreBadge.style.borderLeftWidth    = 1;
        moreBadge.style.borderRightWidth   = 1;
        moreBadge.style.borderTopColor     = new Color(0.4f, 0.7f, 1.0f, 0.6f);
        moreBadge.style.borderBottomColor  = new Color(0.4f, 0.7f, 1.0f, 0.6f);
        moreBadge.style.borderLeftColor    = new Color(0.4f, 0.7f, 1.0f, 0.6f);
        moreBadge.style.borderRightColor   = new Color(0.4f, 0.7f, 1.0f, 0.6f);
        moreBadge.style.borderTopLeftRadius     = 10;
        moreBadge.style.borderTopRightRadius    = 10;
        moreBadge.style.borderBottomLeftRadius  = 10;
        moreBadge.style.borderBottomRightRadius = 10;
        moreBadge.style.paddingLeft   = 8;
        moreBadge.style.paddingRight  = 8;
        moreBadge.style.paddingTop    = 3;
        moreBadge.style.paddingBottom = 3;

        // Image icon, not the ⓘ/▾ Unicode glyphs — confirmed live
        // (2026-08-17) that this game's font has no coverage for those
        // characters, so the badge was silently rendering as bare "MORE"
        // on-device with no info symbol or arrow at all, even though it
        // looked correct in-editor (which falls back to a system font).
        var infoIcon = UIHelper.Icon(UIHelper.IconInfo, 11f,
            new Color(0.4f, 0.7f, 1.0f, 0.95f));
        infoIcon.style.marginRight = 4;

        var arrow = new Label { text = "MORE" };
        arrow.style.fontSize      = 9;
        arrow.style.letterSpacing = 1;
        arrow.style.color         = new Color(0.4f, 0.7f, 1.0f, 0.95f);
        moreBadge.Add(infoIcon);
        moreBadge.Add(arrow);

        header.Add(headerLeft);
        header.Add(moreBadge);
        container.Add(header);

        var clueText = new Label { text = item.ClueText };
        clueText.style.fontSize   = 12;
        clueText.style.color      = UIHelper.TextMuted;
        clueText.style.whiteSpace = WhiteSpace.Normal;
        clueText.style.marginTop  = 8;
        container.Add(clueText);

        var detail = new VisualElement();
        detail.style.display        = DisplayStyle.None;
        detail.style.marginTop      = 10;
        detail.style.paddingTop     = 10;
        detail.style.borderTopColor =
            new Color(0.4f, 0.7f, 1.0f, 0.15f);
        detail.style.borderTopWidth = 1;

        var detailHdr = new Label { text = "DETECTIVE NOTES" };
        detailHdr.style.fontSize      = 8;
        detailHdr.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.4f);
        detailHdr.style.letterSpacing = 4;
        detailHdr.style.marginBottom  = 6;

        var detailText = new Label
        {
            text = string.IsNullOrEmpty(item.EvidenceDetail)
                ? "No further notes."
                : item.EvidenceDetail
        };
        detailText.style.fontSize                = 11;
        detailText.style.color                   =
            new Color(0.4f, 0.7f, 1.0f, 0.55f);
        detailText.style.whiteSpace              = WhiteSpace.Normal;
        detailText.style.unityFontStyleAndWeight = FontStyle.Italic;

        detail.Add(detailHdr);
        detail.Add(detailText);
        container.Add(detail);

        bool expanded = false;
        container.RegisterCallback<ClickEvent>(_ =>
        {
            expanded = !expanded;
            detail.style.display = expanded
                ? DisplayStyle.Flex : DisplayStyle.None;
            arrow.text = expanded ? "LESS" : "MORE";
            if (expanded)
                StudyLogger.Instance?.OnClueExpanded(item.Name);
        });

        return container;
    }

    // ── Narrative update ───────────────────────────────────────────────────

    public void UpdateWithNarrative(NarrativeData data,
                                    int chapterNumber = 1)
    {
        _currentChapter    = chapterNumber;
        _chapterLabel.text = $"LOCATION {chapterNumber:D2}";
        _chapterTitle.text = data.ChapterTitle;
        _chapterDesc.text  = data.RoomDescription;
        _totalClues        = data.Clues?.Count ?? 5;

        _collected.Clear();
        BuildEmptySlots();

        // Confirmed live (2026-08-21): both fields were carrying over the
        // previous room's text into the new room's Case File — never
        // explicitly cleared here, so whatever was typed last room just
        // sat in the TextField until manually overwritten.
        _notesField.value  = "";
        _theoryField.value = "";

        var countLabel = _screen.Q<Label>("EvidenceCount");
        if (countLabel != null)
            countLabel.text = FormatCountLabel();

        SelectConfidence(3);
        UpdateSubmitButtonState();
    }

    // ── Theory submission ──────────────────────────────────────────────────

    void OnSubmitTheory()
    {
        // Block until all clues found
        if (_collected.Count < _totalClues)
        {
            Debug.LogWarning(
                $"[CrimeBoard] Need all clues — " +
                $"{_collected.Count}/{_totalClues} found");
            ShowNotEnoughCluesWarning();
            return;
        }

        string theory = _theoryField.value;
        if (string.IsNullOrWhiteSpace(theory))
        {
            Debug.LogWarning("[CrimeBoard] No theory entered");
            ShowNoTheoryWarning();
            return;
        }

        WorldStateManager.Instance?.SetPlayerTheory(theory);
        _lastTimeToDecide = Time.time - _decisionStartTime;

        var narrative = NarrativeGenerator.Instance?.CurrentNarrative;
        var room      = WorldStateManager.Instance?.State?.CurrentRoom;

        if (narrative != null && room != null)
        {
            NarrativeGenerator.Instance.AnalyseTheory(
                theory, narrative.TruthReveal,
                room.CluesFound, room.RoomNumber,
                OnAnalysisComplete);
        }
        else
        {
            OnAnalysisComplete(new TheoryAnalysis
            {
                Result  = "partial",
                Message = "Your theory holds weight. The room has more to tell.",
                Hint    = "Look at what was left behind intentionally."
            });
        }
    }

    void ShowNoTheoryWarning()
    {
        _theoryField.style.borderTopColor    = new Color(0.8f, 0.3f, 0.3f, 1.0f);
        _theoryField.style.borderBottomColor = new Color(0.8f, 0.3f, 0.3f, 1.0f);
        _theoryField.style.borderLeftColor   = new Color(0.8f, 0.3f, 0.3f, 1.0f);
        _theoryField.style.borderRightColor  = new Color(0.8f, 0.3f, 0.3f, 1.0f);

        NavigationManager.Instance.StartCoroutine(ResetTheoryFieldBorder());
    }

    IEnumerator ResetTheoryFieldBorder()
    {
        yield return new WaitForSeconds(2.5f);
        StyleField(_theoryField);
    }

    void ShowNotEnoughCluesWarning()
    {
        var countLabel = _screen.Q<Label>("EvidenceCount");
        if (countLabel == null) return;

        countLabel.text  =
            $"{_collected.Count} / {_totalClues} — find all clues first!";
        countLabel.style.color =
            new Color(0.8f, 0.3f, 0.3f, 1.0f);

        NavigationManager.Instance.StartCoroutine(
            ResetCountLabel());
    }

    IEnumerator ResetCountLabel()
    {
        yield return new WaitForSeconds(2.5f);
        var countLabel = _screen.Q<Label>("EvidenceCount");
        if (countLabel != null)
        {
            countLabel.text  = FormatCountLabel();
            countLabel.style.color = UIHelper.TextMuted;
        }
    }

    void OnAnalysisComplete(TheoryAnalysis analysis)
    {
        StudyLogger.Instance?.OnTheorySubmitted(
            _theoryField.value,
            _confidenceValue,
            analysis.Result,
            _lastTimeToDecide);

        WorldStateManager.Instance?.SetAIAnalysis(analysis.Result);
        var narrative = NarrativeGenerator.Instance?.CurrentNarrative;
        string truth  = narrative?.TruthReveal ?? "";
        WorldStateManager.Instance?.CompleteCurrentRoom();
        NavigationManager.Instance.ShowTheoryResult(analysis, truth);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    Label SectionHeader(string text)
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
        field.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.2f);
        field.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.2f);
        field.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.2f);
        field.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.2f);
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

    public void SetVisible(bool v)
    {
        _screen.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
        if (v) _decisionStartTime = Time.time;
    }
}