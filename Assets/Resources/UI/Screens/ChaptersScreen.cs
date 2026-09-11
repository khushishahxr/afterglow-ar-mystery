using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class ChaptersScreen
{
    readonly VisualElement _screen;
    ScrollView _scroll;

    static readonly string[] BannerNames = new[]
    {
        "banner_bedroom",
        "banner_bathroom",
        "banner_kitchen",
        "banner_livingroom",
        "banner_study",
        "banner_finalroom"
    };

    List<ChapterData> _chapters = BuildDefaultChapters();

    static List<ChapterData> BuildDefaultChapters() => new()
    {
        new ChapterData("LOCATION 01", "The Bedroom",
            "They were here when it started. The bed is still made.",
            true),
        new ChapterData("LOCATION 02", "The Bathroom",
            "The water stopped running on day three.", false),
        new ChapterData("LOCATION 03", "The Kitchen",
            "The last meal was never finished.", false),
        new ChapterData("LOCATION 04", "The Living Room",
            "They gathered here to watch the news.", false),
        new ChapterData("LOCATION 05", "The Study",
            "Someone knew this was coming.", false),
        new ChapterData("LOCATION 06", "The Final Room",
            "This is where it ends.", false),
    };

    public ChaptersScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        root.Add(_screen);

        var topBar  = UIHelper.TopBar();
        var btnBack = UIHelper.IconButtonImg(
            UIHelper.IconArrowLeft, 22f,
            new Color(0.4f, 0.5f, 0.6f, 0.7f));
        btnBack.style.width = 70; // widened to match btnJournal below, keeps the title centered
        btnBack.clicked += () =>
            NavigationManager.Instance.GoTo(GameScreen.Home);

        // Text label, not an icon — same width as btnBack so the title
        // stays centered (mismatched side-element widths throw it off).
        // NoWrap + tight letter spacing so "JOURNAL" reliably sits on one
        // line at this width; grey (matching btnBack's tone) since this
        // is a secondary nav action, not a primary cold-blue one.
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
            NavigationManager.Instance.ShowJournal(GameScreen.Chapters);

        topBar.Add(btnBack);
        topBar.Add(UIHelper.ScreenTitle("INVESTIGATION LOG"));
        topBar.Add(btnJournal);
        _screen.Add(topBar);

        _scroll = new ScrollView();
        _scroll.style.position     = Position.Absolute;
        _scroll.style.top          = 116;
        _scroll.style.left         = 0;
        _scroll.style.right        = 0;
        _scroll.style.bottom       = 0;
        _scroll.style.paddingLeft  = 20;
        _scroll.style.paddingRight = 20;
        _scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        _screen.Add(_scroll);

        BuildList();
    }

    VisualElement BuildDossierCard()
    {
        var card = UIHelper.AccentCard();
        card.style.marginBottom = 16;

        var row = new VisualElement();
        row.style.flexDirection  = FlexDirection.Row;
        row.style.alignItems     = Align.Center;
        row.style.justifyContent = Justify.SpaceBetween;

        var left = new VisualElement();
        var tag = new Label { text = "DOSSIER" };
        tag.style.fontSize      = 8;
        tag.style.letterSpacing = 3;
        tag.style.color         = UIHelper.TextMuted;
        tag.style.marginBottom  = 4;
        var title = new Label { text = "What You've Learned" };
        title.style.fontSize                = 14;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color                   = UIHelper.TextPrim;
        left.Add(tag);
        left.Add(title);

        var arrow = UIHelper.Icon(
            UIHelper.IconArrowRight, 16f,
            new Color(0.4f, 0.7f, 1.0f, 0.8f));

        row.Add(left);
        row.Add(arrow);
        card.Add(row);

        card.RegisterCallback<ClickEvent>(_ =>
            NavigationManager.Instance.ShowDossier(GameScreen.Chapters));

        return card;
    }

    void BuildList()
    {
        _scroll.Clear();
        _scroll.Add(BuildDossierCard());
        for (int i = 0; i < _chapters.Count; i++)
            _scroll.Add(BuildCard(_chapters[i], i + 1, i));
    }

    public void UpdateWithNarrative(NarrativeData data, int chapterNumber)
    {
        int index = chapterNumber - 1;
        if (index < 0 || index >= _chapters.Count) return;

        var ch = _chapters[index];
        _chapters[index] = new ChapterData(
            $"LOCATION {chapterNumber:D2}",
            data.ChapterTitle,
            data.StoryIntro,
            true, ch.Completed, ch.Summary);
        BuildList();
    }

    public void MarkChapterComplete(int chapterNumber)
    {
        int index = chapterNumber - 1;
        if (index < 0 || index >= _chapters.Count) return;

        var ch      = _chapters[index];
        string summary = GetRoomSummary(chapterNumber);

        _chapters[index] = new ChapterData(
            ch.Tag, ch.Title, ch.Description,
            ch.Unlocked, true, summary);

        // Unlock next chapter
        int nextIndex = chapterNumber;
        if (nextIndex < _chapters.Count)
        {
            var next = _chapters[nextIndex];
            _chapters[nextIndex] = new ChapterData(
                next.Tag, next.Title, next.Description,
                true, false, "");
        }

        BuildList();
        Debug.Log($"[ChaptersScreen] Chapter {chapterNumber} complete");
    }

    // Restores all six cards to their initial locked/unlocked state —
    // used by the researcher "reset for next participant" flow.
    public void ResetAll()
    {
        _chapters = BuildDefaultChapters();
        BuildList();
        Debug.Log("[ChaptersScreen] Reset for next participant");
    }

    string GetRoomSummary(int roomNumber)
    {
        var room = WorldStateManager.Instance?.State?.Rooms
            .Find(r => r.RoomNumber == roomNumber);
        if (room == null) return "";
        if (!string.IsNullOrEmpty(room.PlayerTheory))
        {
            string t = room.PlayerTheory.Trim();
            return t.Length > 60 ? t.Substring(0, 57) + "..." : t;
        }
        return "";
    }

    VisualElement BuildCard(ChapterData data,
                            int chapterNumber, int index)
    {
        var unlockColor   = new Color(0.4f, 0.7f, 1.0f, 0.5f);
        var completeColor = new Color(0.3f, 0.7f, 0.4f, 1.0f);

        // Enforced here regardless of what data.Unlocked actually says —
        // a room can only ever render as tappable if every room before it
        // is genuinely marked Completed. Reported live (2026-08-21): all
        // rooms became reachable after finishing only Room 2, which
        // shouldn't be structurally possible given MarkChapterComplete
        // only ever flips the single next chapter — this closes the gap
        // at render time no matter what upstream state ends up corrupted,
        // rather than trying to find every possible cause of it.
        bool effectiveUnlocked = data.Unlocked &&
            (index == 0 || _chapters[index - 1].Completed);

        var card = new VisualElement();
        card.style.backgroundColor   = effectiveUnlocked
            ? UIHelper.Surface : UIHelper.Locked;
        card.style.borderTopColor    = data.Completed
            ? completeColor
            : effectiveUnlocked ? unlockColor : UIHelper.Locked;
        card.style.borderBottomColor = effectiveUnlocked
            ? UIHelper.Border : UIHelper.Locked;
        card.style.borderLeftColor   = data.Completed
            ? completeColor
            : effectiveUnlocked ? unlockColor : UIHelper.Locked;
        card.style.borderRightColor  = effectiveUnlocked
            ? UIHelper.Border : UIHelper.Locked;
        card.style.borderTopWidth    = effectiveUnlocked ? 2 : 1;
        card.style.borderBottomWidth = 1;
        card.style.borderLeftWidth   = effectiveUnlocked ? 2 : 1;
        card.style.borderRightWidth  = 1;
        card.style.borderTopLeftRadius     = 4;
        card.style.borderTopRightRadius    = 4;
        card.style.borderBottomLeftRadius  = 4;
        card.style.borderBottomRightRadius = 4;
        card.style.marginBottom            = 14;
        card.style.overflow                = Overflow.Hidden;

        // ── Banner ─────────────────────────────────────────────────────────
        var bannerContainer = new VisualElement();
        bannerContainer.style.width    = Length.Percent(100);
        bannerContainer.style.height   = 140;
        bannerContainer.style.overflow = Overflow.Hidden;

        if (index < BannerNames.Length)
        {
            var bannerTexture = Resources.Load<Texture2D>(
                $"UI/Images/{BannerNames[index]}");

            if (bannerTexture != null)
            {
                var bannerImg = new VisualElement();
                bannerImg.style.width        = Length.Percent(100);
                bannerImg.style.height       = 140;
                bannerImg.style.backgroundImage =
                    new StyleBackground(bannerTexture);
                bannerImg.style.backgroundPositionX =
                    new BackgroundPosition(
                        BackgroundPositionKeyword.Center);
                bannerImg.style.backgroundPositionY =
                    new BackgroundPosition(
                        BackgroundPositionKeyword.Center);
                bannerImg.style.backgroundRepeat =
                    new BackgroundRepeat(
                        Repeat.NoRepeat, Repeat.NoRepeat);
                bannerImg.style.backgroundSize =
                    new BackgroundSize(BackgroundSizeType.Cover);
                bannerContainer.Add(bannerImg);
            }
            else
            {
                var fallback = new VisualElement();
                fallback.style.width           = Length.Percent(100);
                fallback.style.height          = 140;
                fallback.style.backgroundColor = UIHelper.SurfaceRaised;
                bannerContainer.Add(fallback);
            }
        }

        // ── Overlay by state ───────────────────────────────────────────────
        if (effectiveUnlocked && !data.Completed)
        {
            var gradient = new VisualElement();
            gradient.style.position        = Position.Absolute;
            gradient.style.bottom          = 0;
            gradient.style.left            = 0;
            gradient.style.right           = 0;
            gradient.style.height          = 60;
            gradient.style.backgroundColor =
                new Color(0.055f, 0.075f, 0.106f, 0.75f);
            bannerContainer.Add(gradient);
        }
        else if (data.Completed)
        {
            var completedOverlay = new VisualElement();
            completedOverlay.style.position        = Position.Absolute;
            completedOverlay.style.top             = 0;
            completedOverlay.style.left            = 0;
            completedOverlay.style.right           = 0;
            completedOverlay.style.bottom          = 0;
            completedOverlay.style.backgroundColor =
                new Color(0.1f, 0.3f, 0.15f, 0.3f);
            bannerContainer.Add(completedOverlay);

            var checkContainer = new VisualElement();
            checkContainer.style.position       = Position.Absolute;
            checkContainer.style.top            = 0;
            checkContainer.style.left           = 0;
            checkContainer.style.right          = 0;
            checkContainer.style.bottom         = 0;
            checkContainer.style.alignItems     = Align.FlexEnd;
            checkContainer.style.justifyContent = Justify.FlexStart;
            checkContainer.style.paddingTop     = 10;
            checkContainer.style.paddingRight   = 14;

            var checkLabel = new Label { text = "✓" };
            checkLabel.style.fontSize = 18;
            checkLabel.style.color    =
                new Color(0.3f, 0.8f, 0.4f, 0.9f);
            checkContainer.Add(checkLabel);
            bannerContainer.Add(checkContainer);
        }
        else
        {
            var darkOverlay = new VisualElement();
            darkOverlay.style.position        = Position.Absolute;
            darkOverlay.style.top             = 0;
            darkOverlay.style.left            = 0;
            darkOverlay.style.right           = 0;
            darkOverlay.style.bottom          = 0;
            darkOverlay.style.backgroundColor =
                new Color(0.031f, 0.043f, 0.063f, 0.82f);
            bannerContainer.Add(darkOverlay);

            var lockContainer = new VisualElement();
            lockContainer.style.position       = Position.Absolute;
            lockContainer.style.top            = 0;
            lockContainer.style.left           = 0;
            lockContainer.style.right          = 0;
            lockContainer.style.bottom         = 0;
            lockContainer.style.alignItems     = Align.Center;
            lockContainer.style.justifyContent = Justify.Center;

            var lockIcon = new Label { text = "LOCKED" };
            lockIcon.style.fontSize      = 9;
            lockIcon.style.color         =
                new Color(0.4f, 0.5f, 0.6f, 0.5f);
            lockIcon.style.letterSpacing = 4;
            lockContainer.Add(lockIcon);
            bannerContainer.Add(lockContainer);
        }

        card.Add(bannerContainer);

        // ── Text content ───────────────────────────────────────────────────
        var content = new VisualElement();
        content.style.paddingTop     = 14;
        content.style.paddingBottom  = 14;
        content.style.paddingLeft    = 20;
        content.style.paddingRight   = 20;
        content.style.flexDirection  = FlexDirection.Row;
        content.style.alignItems     = Align.Center;
        content.style.justifyContent = Justify.SpaceBetween;

        var left = new VisualElement();
        left.style.flexGrow = 1;

        var tag = new Label { text = data.Tag };
        tag.style.fontSize      = 9;
        tag.style.color         = data.Completed
            ? completeColor
            : effectiveUnlocked
                ? new Color(0.4f, 0.7f, 1.0f, 0.8f)
                : UIHelper.LockedText;
        tag.style.letterSpacing = 4;
        tag.style.marginBottom  = 4;

        var cardTitle = new Label { text = data.Title };
        cardTitle.style.fontSize                = 18;
        cardTitle.style.color                   = effectiveUnlocked
            ? UIHelper.TextPrim : UIHelper.LockedText;
        cardTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        cardTitle.style.letterSpacing           = 1;

        left.Add(tag);
        left.Add(cardTitle);

        // Description — show for unlocked not completed
        if (!string.IsNullOrEmpty(data.Description)
            && effectiveUnlocked && !data.Completed)
        {
            var desc = new Label { text = data.Description };
            desc.style.fontSize   = 11;
            desc.style.color      = UIHelper.TextMuted;
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginTop  = 4;
            desc.style.maxWidth   = 240;
            left.Add(desc);
        }

        // Theory summary — show for completed rooms
        if (data.Completed && !string.IsNullOrEmpty(data.Summary))
        {
            var summaryLabel = new Label
            {
                text = $"\"{data.Summary}\""
            };
            summaryLabel.style.fontSize                = 10;
            summaryLabel.style.color                   =
                new Color(0.4f, 0.7f, 1.0f, 0.5f);
            summaryLabel.style.whiteSpace              = WhiteSpace.Normal;
            summaryLabel.style.marginTop               = 6;
            summaryLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            summaryLabel.style.maxWidth                = 240;
            left.Add(summaryLabel);
        }

        content.Add(left);

        // Arrow or check indicator — this is the actual gate on whether
        // the card can be tapped into (RegisterCallback below only
        // happens in this branch), so effectiveUnlocked here is what
        // actually enforces the previous-room-must-finish rule.
        if (effectiveUnlocked)
        {
            if (data.Completed)
            {
                var check = new Label { text = "✓" };
                check.style.fontSize  = 14;
                check.style.color     = completeColor;
                check.style.alignSelf = Align.Center;
                content.Add(check);
            }
            else
            {
                var arrow = UIHelper.Icon(
                    UIHelper.IconArrowRight, 20f,
                    new Color(0.4f, 0.7f, 1.0f, 0.8f));
                arrow.style.alignSelf = Align.Center;
                content.Add(arrow);
                card.RegisterCallback<ClickEvent>(_ =>
                    NavigationManager.Instance
                        .ShowTeaser(chapterNumber));
            }
        }
        else
        {
            var lockLabel = new Label { text = "—" };
            lockLabel.style.fontSize  = 16;
            lockLabel.style.color     = UIHelper.LockedText;
            lockLabel.style.alignSelf = Align.Center;
            content.Add(lockLabel);
        }

        card.Add(content);
        return card;
    }

    public void SetVisible(bool v) =>
        _screen.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;

    class ChapterData
    {
        public string Tag, Title, Description, Summary;
        public bool   Unlocked, Completed;

        public ChapterData(string tag, string title,
            string desc, bool unlocked,
            bool completed = false, string summary = "")
        {
            Tag         = tag;
            Title       = title;
            Description = desc;
            Unlocked    = unlocked;
            Completed   = completed;
            Summary     = summary;
        }
    }
}