using UnityEngine;
using UnityEngine.UIElements;

// Adapted, not ported: the twin project's Dossier has fixed "Elena and
// Roy" bios because its story is pre-authored and always the same two
// people. This project's narrative (including who the people even are)
// is LLM-generated fresh per session across 5 different apocalypse
// themes, so there's no fixed character to write a static bio for.
// Instead this surfaces what the architecture actually tracks for
// exactly this purpose — WorldState.EstablishedFacts, the accumulating
// list of what each completed room's narrative confirmed — as a growing
// "what you've learned" record, gated on room completion the same way
// the twin's per-character facts are.
public class DossierScreen
{
    readonly VisualElement _screen;
    readonly ScrollView    _scroll;

    public DossierScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        root.Add(_screen);

        var topBar  = UIHelper.TopBar();
        var btnBack = UIHelper.IconButtonImg(
            UIHelper.IconArrowLeft, 22f,
            new Color(0.4f, 0.5f, 0.6f, 0.7f));
        btnBack.clicked += () => NavigationManager.Instance.ReturnFromDossier();
        var spacer = new VisualElement();
        spacer.style.width = 44;
        topBar.Add(btnBack);
        topBar.Add(UIHelper.ScreenTitle("WHAT YOU'VE LEARNED"));
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

        var state = WorldStateManager.Instance?.State;
        if (state == null)
        {
            _scroll.Add(EmptyState());
            return;
        }

        // Gated on AllRoomsComplete, not HasTheme — HasTheme flips true the
        // instant Room 1's narrative picks an internal theme (by design,
        // long before the player is meant to know it; see PromptBuilder's
        // "DO NOT reveal why the world ended" instruction for Room 1).
        // Showing the raw theme name here that early was spoiling the
        // entire mystery in a menu screen, well before the escalating
        // clarity across rooms 2-6 or the Ending ever got to reveal it.
        if (state.AllRoomsComplete)
        {
            var themeCard = UIHelper.AccentCard();
            themeCard.style.marginBottom = 16;

            var themeTag = new Label { text = "PROTOCOL" };
            themeTag.style.fontSize      = 8;
            themeTag.style.letterSpacing = 3;
            themeTag.style.color         = UIHelper.TextMuted;
            themeTag.style.marginBottom  = 4;

            var themeName = new Label { text = state.Theme.ToString().ToUpper() };
            themeName.style.fontSize                = 16;
            themeName.style.unityFontStyleAndWeight = FontStyle.Bold;
            themeName.style.color                   = UIHelper.TextPrim;

            themeCard.Add(themeTag);
            themeCard.Add(themeName);
            _scroll.Add(themeCard);
        }

        if (state.EstablishedFacts == null || state.EstablishedFacts.Count == 0)
        {
            _scroll.Add(EmptyState());
            return;
        }

        for (int i = 0; i < state.EstablishedFacts.Count; i++)
        {
            var card = UIHelper.Card();
            card.style.marginBottom = 10;

            var tag = new Label { text = $"ROOM {i + 1:D2} CONFIRMED" };
            tag.style.fontSize      = 8;
            tag.style.letterSpacing = 3;
            tag.style.color         = new Color(0.4f, 0.7f, 1.0f, 0.7f);
            tag.style.marginBottom  = 6;

            var fact = new Label { text = state.EstablishedFacts[i] };
            fact.style.fontSize   = 12;
            fact.style.color      = UIHelper.TextMuted;
            fact.style.whiteSpace = WhiteSpace.Normal;

            card.Add(tag);
            card.Add(fact);
            _scroll.Add(card);
        }

        UIHelper.Spacer(_scroll, 30);
    }

    VisualElement EmptyState()
    {
        var empty = new Label
        {
            text = "Nothing confirmed yet. Keep investigating."
        };
        empty.style.fontSize       = 12;
        empty.style.color          = UIHelper.TextMuted;
        empty.style.unityTextAlign = TextAnchor.MiddleCenter;
        empty.style.whiteSpace     = WhiteSpace.Normal;
        empty.style.marginTop      = 40;
        return empty;
    }

    public void SetVisible(bool v) =>
        _screen.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
}
