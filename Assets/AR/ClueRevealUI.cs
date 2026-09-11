using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ClueRevealUI : MonoBehaviour
{
    public static ClueRevealUI Instance { get; private set; }

    // Distinct from EvidenceManager.OnEvidenceAdded ("found" — the clue
    // was physically revealed) — this fires when the player actually
    // dismisses this detail popup ("engaged" — they've read it). Scan/
    // Reveal flow gates the Reveal button's re-enable on this, not on the
    // find itself, so players can't chain-press past evidence unread.
    public static event System.Action OnClueEngaged;

    [SerializeField] UIDocument uiDocument;

    VisualElement _popup;
    Label         _nameLabel;
    Label         _clueLabel;
    VisualElement _reactionBlock;
    Label         _reactionLabel;
    Button        _collectBtn;

    ClueObject _pendingClue;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (uiDocument == null)
            uiDocument = FindFirstObjectByType<UIDocument>();
        BuildPopup();
        HidePopup();
    }

    void BuildPopup()
    {
        var root = uiDocument.rootVisualElement;
        if (root == null) return;

        _popup = new VisualElement();
        _popup.style.position          = Position.Absolute;
        _popup.style.bottom            = 160;
        _popup.style.left              = 16;
        _popup.style.right             = 16;
        _popup.style.backgroundColor   =
            new Color(0.031f, 0.043f, 0.063f, 0.97f);
        _popup.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.5f);
        _popup.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.5f);
        _popup.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _popup.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.5f);
        _popup.style.borderTopWidth    = 1;
        _popup.style.borderBottomWidth = 1;
        _popup.style.borderLeftWidth   = 2;
        _popup.style.borderRightWidth  = 1;
        _popup.style.borderTopLeftRadius     = 4;
        _popup.style.borderTopRightRadius    = 4;
        _popup.style.borderBottomLeftRadius  = 4;
        _popup.style.borderBottomRightRadius = 4;
        _popup.style.paddingTop    = 14;
        _popup.style.paddingBottom = 14;
        _popup.style.paddingLeft   = 16;
        _popup.style.paddingRight  = 16;

        // Tag
        var tag = new Label { text = "EVIDENCE DETECTED" };
        tag.style.fontSize      = 8;
        tag.style.color         = new Color(0.4f, 0.7f, 1.0f, 0.7f);
        tag.style.letterSpacing = 4;
        tag.style.marginBottom  = 8;

        // Name row — EvidenceIcon is an internal numeric ID (see
        // PromptBuilder.EvidenceIcons), not a glyph; displaying it
        // directly used to print a bare, unstyled digit next to the name.
        var headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.alignItems    = Align.Center;
        headerRow.style.marginBottom  = 8;

        _nameLabel = new Label { text = "" };
        _nameLabel.style.fontSize                = 15;
        _nameLabel.style.color                   =
            new Color(0.82f, 0.855f, 0.894f);
        _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _nameLabel.style.letterSpacing           = 1;

        headerRow.Add(_nameLabel);

        // Clue text — typewriter effect
        _clueLabel = new Label { text = "" };
        _clueLabel.style.fontSize   = 12;
        _clueLabel.style.color      =
            new Color(0.42f, 0.49f, 0.569f);
        _clueLabel.style.whiteSpace = WhiteSpace.Normal;
        _clueLabel.style.marginBottom = 12;

        // Reaction line — the player-detective's own first-person thought,
        // distinct from the "detective notes" reference text shown later
        // in the Case File. Left accent border + italic to read visually
        // as a different voice, in its own block below the main clue text.
        _reactionBlock = new VisualElement();
        _reactionBlock.style.borderLeftWidth = 2;
        _reactionBlock.style.borderLeftColor = new Color(0.4f, 0.7f, 1.0f, 0.7f);
        _reactionBlock.style.paddingLeft     = 10;
        _reactionBlock.style.marginBottom    = 12;
        _reactionBlock.style.display         = DisplayStyle.None;
        _reactionBlock.style.opacity         = 0f;

        _reactionLabel = new Label { text = "" };
        _reactionLabel.style.fontSize                = 12;
        _reactionLabel.style.color                   = new Color(0.7f, 0.82f, 0.95f, 0.95f);
        _reactionLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
        _reactionLabel.style.whiteSpace              = WhiteSpace.Normal;
        _reactionBlock.Add(_reactionLabel);

        // Collect button
        _collectBtn = new Button();
        _collectBtn.style.width             = Length.Percent(100);
        _collectBtn.style.paddingTop        = 10;
        _collectBtn.style.paddingBottom     = 10;
        _collectBtn.style.backgroundColor   = Color.clear;
        _collectBtn.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _collectBtn.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _collectBtn.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _collectBtn.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        _collectBtn.style.borderTopWidth    = 1;
        _collectBtn.style.borderBottomWidth = 1;
        _collectBtn.style.borderLeftWidth   = 1;
        _collectBtn.style.borderRightWidth  = 1;
        _collectBtn.style.borderTopLeftRadius     = 2;
        _collectBtn.style.borderTopRightRadius    = 2;
        _collectBtn.style.borderBottomLeftRadius  = 2;
        _collectBtn.style.borderBottomRightRadius = 2;

        var collectRow = new VisualElement();
        collectRow.style.flexDirection  = FlexDirection.Row;
        collectRow.style.alignItems     = Align.Center;
        collectRow.style.justifyContent = Justify.Center;

        var collectLabel = new Label { text = "COLLECT EVIDENCE" };
        collectLabel.style.fontSize      = 10;
        collectLabel.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.9f);
        collectLabel.style.letterSpacing = 3;

        collectRow.Add(collectLabel);
        _collectBtn.Add(collectRow);
        _collectBtn.clicked += OnCollect;

        _popup.Add(tag);
        _popup.Add(headerRow);
        _popup.Add(_clueLabel);
        _popup.Add(_reactionBlock);
        _popup.Add(_collectBtn);

        root.Add(_popup);
    }

    public void ShowClue(ClueObject clue)
    {
        _pendingClue = clue;
        _nameLabel.text = clue.EvidenceName;
        _clueLabel.text = "";
        _reactionLabel.text = clue.ReactionLine ?? "";
        _reactionBlock.style.display = DisplayStyle.None;
        _reactionBlock.style.opacity = 0f;
        _popup.style.display = DisplayStyle.Flex;
        StartCoroutine(RevealClueThenReaction(clue));
    }

    IEnumerator RevealClueThenReaction(ClueObject clue)
    {
        yield return TypeText(_clueLabel, clue.ClueText);

        if (string.IsNullOrEmpty(clue.ReactionLine)) yield break;

        yield return new WaitForSeconds(0.3f);
        _reactionBlock.style.display = DisplayStyle.Flex;

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            _reactionBlock.style.opacity = t / 0.4f;
            yield return null;
        }
        _reactionBlock.style.opacity = 1f;
    }

    void OnCollect()
    {
        HidePopup();
        var collected = _pendingClue;
        _pendingClue = null;
        collected?.MarkCollected();
        OnClueEngaged?.Invoke();
    }

    void HidePopup()
    {
        StopAllCoroutines();
        _popup.style.display = DisplayStyle.None;
    }

    // Public so NavigationManager can force this closed on any screen
    // change away from Gameplay — this popup lives on the shared
    // UIDocument outside the normal screen-visibility system, so nothing
    // else was clearing it if the player left Gameplay (e.g. the 3-finger
    // researcher gesture) while a clue was still being read.
    public void ForceHide()
    {
        HidePopup();
        _pendingClue = null;
    }

    // ── Typewriter effect ──────────────────────────────────────────────────

    IEnumerator TypeText(Label label, string text,
                         float delay = 0.025f)
    {
        label.text = "";
        foreach (char c in text)
        {
            label.text += c;
            yield return new WaitForSeconds(delay);
        }
    }
}