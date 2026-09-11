using UnityEngine;
using UnityEngine.UIElements;

// Full (non-truncated) record of every completed room's theory, result,
// and evidence considered — Chapters/room-list only ever shows a
// truncated one-liner summary, this is the complete version. Reachable
// from both Case File and the room-list screen (see
// NavigationManager.ShowJournal), so its back button returns to whichever
// of those actually opened it rather than a hardcoded destination.
public class JournalScreen
{
    readonly VisualElement _screen;
    readonly ScrollView    _scroll;

    public JournalScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        root.Add(_screen);

        var topBar  = UIHelper.TopBar();
        var btnBack = UIHelper.IconButtonImg(
            UIHelper.IconArrowLeft, 22f,
            new Color(0.4f, 0.5f, 0.6f, 0.7f));
        btnBack.clicked += () => NavigationManager.Instance.ReturnFromJournal();
        var spacer = new VisualElement();
        spacer.style.width = 44;
        topBar.Add(btnBack);
        topBar.Add(UIHelper.ScreenTitle("JOURNAL"));
        topBar.Add(spacer);
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
    }

    public void Refresh()
    {
        _scroll.Clear();

        var rooms = WorldStateManager.Instance?.State?.Rooms;
        bool anyComplete = rooms != null && rooms.Exists(r => r.IsComplete);

        if (!anyComplete)
        {
            var empty = new Label
            {
                text = "No theories submitted yet. Once you finish investigating a room, your reasoning will be recorded here."
            };
            empty.style.fontSize       = 12;
            empty.style.color          = UIHelper.TextMuted;
            empty.style.unityTextAlign = TextAnchor.MiddleCenter;
            empty.style.whiteSpace     = WhiteSpace.Normal;
            empty.style.marginTop      = 40;
            _scroll.Add(empty);
            return;
        }

        foreach (var room in rooms)
        {
            if (!room.IsComplete) continue;
            _scroll.Add(BuildEntry(room));
        }

        UIHelper.Spacer(_scroll, 30);
    }

    VisualElement BuildEntry(RoomRecord room)
    {
        var card = UIHelper.Card();
        card.style.marginBottom = 16;

        var headerRow = new VisualElement();
        headerRow.style.flexDirection  = FlexDirection.Row;
        headerRow.style.alignItems     = Align.Center;
        headerRow.style.justifyContent = Justify.SpaceBetween;
        headerRow.style.marginBottom   = 10;

        var location = new Label { text = $"LOCATION {room.RoomNumber:D2} — {room.RoomType.ToUpper()}" };
        location.style.fontSize      = 9;
        location.style.letterSpacing = 3;
        location.style.color         = new Color(0.4f, 0.7f, 1.0f, 0.8f);

        headerRow.Add(location);
        headerRow.Add(BuildResultBadge(room.AIAnalysis));
        card.Add(headerRow);

        var theoryLabel = new Label { text = "YOUR THEORY" };
        theoryLabel.style.fontSize      = 8;
        theoryLabel.style.letterSpacing = 3;
        theoryLabel.style.color         = UIHelper.TextMuted;
        theoryLabel.style.marginBottom  = 4;
        card.Add(theoryLabel);

        var theoryText = new Label
        {
            text = string.IsNullOrEmpty(room.PlayerTheory)
                ? "No theory recorded."
                : room.PlayerTheory
        };
        theoryText.style.fontSize   = 13;
        theoryText.style.color      = UIHelper.TextPrim;
        theoryText.style.whiteSpace = WhiteSpace.Normal;
        theoryText.style.marginBottom = 12;
        card.Add(theoryText);

        if (room.CluesFound != null && room.CluesFound.Count > 0)
        {
            var evidenceLabel = new Label { text = "EVIDENCE CONSIDERED" };
            evidenceLabel.style.fontSize      = 8;
            evidenceLabel.style.letterSpacing = 3;
            evidenceLabel.style.color         = UIHelper.TextMuted;
            evidenceLabel.style.marginBottom  = 4;
            card.Add(evidenceLabel);

            var evidenceText = new Label { text = string.Join(", ", room.CluesFound) };
            evidenceText.style.fontSize   = 11;
            evidenceText.style.color      = UIHelper.TextMuted;
            evidenceText.style.whiteSpace = WhiteSpace.Normal;
            card.Add(evidenceText);
        }

        return card;
    }

    // Dot + label, matched to whatever result categories this project's
    // theory analysis actually returns (see TheoryAnalysis.Result /
    // CrimeBoardScreen.OnAnalysisComplete): "correct", "partial", or
    // anything else (including empty, for a room completed before this
    // field existed) reads as incomplete rather than guessing a category.
    VisualElement BuildResultBadge(string result)
    {
        Color color; string label;
        switch (result)
        {
            case "correct": color = new Color(0.3f, 0.8f, 0.4f, 0.95f); label = "THEORY CONFIRMED"; break;
            case "partial": color = new Color(0.4f, 0.7f, 1.0f, 0.95f); label = "PARTIALLY CORRECT"; break;
            default:         color = new Color(0.5f, 0.55f, 0.6f, 0.85f); label = "THEORY INCOMPLETE"; break;
        }

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems    = Align.Center;

        var dot = new VisualElement();
        dot.style.width  = 6;
        dot.style.height = 6;
        dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius =
            dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 3;
        dot.style.backgroundColor = color;
        dot.style.marginRight     = 6;

        var text = new Label { text = label };
        text.style.fontSize      = 8;
        text.style.letterSpacing = 2;
        text.style.color         = color;

        row.Add(dot);
        row.Add(text);
        return row;
    }

    public void SetVisible(bool v) =>
        _screen.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
}
