using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class AllCluesFoundNotification : MonoBehaviour
{
    public static AllCluesFoundNotification Instance { get; private set; }

    [SerializeField] UIDocument uiDocument;

    VisualElement _banner;
    bool _shown = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (uiDocument == null)
            uiDocument = FindFirstObjectByType<UIDocument>();

        BuildBanner();
        HideBanner();

        EvidenceManager.OnEvidenceAdded += OnEvidenceAdded;
    }

    void OnDestroy()
    {
        EvidenceManager.OnEvidenceAdded -= OnEvidenceAdded;
    }

    void OnEvidenceAdded(EvidenceItem item)
    {
        if (_shown) return;

        int collected = EvidenceManager.Instance?.Count ?? 0;
        int total     = NarrativeGenerator.Instance?
                        .CurrentNarrative?.Clues?.Count ?? 3;

        if (collected >= total)
        {
            _shown = true;
            StartCoroutine(ShowThenHide());
        }
    }

    void BuildBanner()
    {
        var root = uiDocument.rootVisualElement;

        _banner = new VisualElement();
        _banner.style.position        = Position.Absolute;
        _banner.style.top             = 100;
        _banner.style.left            = 20;
        _banner.style.right           = 20;
        _banner.style.backgroundColor =
            new Color(0.031f, 0.043f, 0.063f, 0.96f);
        _banner.style.borderTopColor    = UIHelper.ColdBlue;
        _banner.style.borderBottomColor = UIHelper.ColdBlue;
        _banner.style.borderLeftColor   = UIHelper.ColdBlue;
        _banner.style.borderRightColor  = UIHelper.ColdBlue;
        _banner.style.borderTopWidth    = 1;
        _banner.style.borderBottomWidth = 1;
        _banner.style.borderLeftWidth   = 1;
        _banner.style.borderRightWidth  = 1;
        _banner.style.borderTopLeftRadius     = 2;
        _banner.style.borderTopRightRadius    = 2;
        _banner.style.borderBottomLeftRadius  = 2;
        _banner.style.borderBottomRightRadius = 2;
        _banner.style.paddingTop    = 14;
        _banner.style.paddingBottom = 14;
        _banner.style.paddingLeft   = 20;
        _banner.style.paddingRight  = 20;
        _banner.style.alignItems    = Align.Center;
        _banner.style.opacity       = 0;

        var tag = new Label { text = "ALL EVIDENCE COLLECTED" };
        tag.style.fontSize      = 9;
        tag.style.color         = UIHelper.ColdBlue;
        tag.style.letterSpacing = 4;
        tag.style.marginBottom  = 4;

        var msg = new Label
        {
            text = "Open the Case File and submit your theory."
        };
        msg.style.fontSize      = 13;
        msg.style.color         = UIHelper.TextPrim;
        msg.style.unityTextAlign = TextAnchor.MiddleCenter;
        msg.style.whiteSpace    = WhiteSpace.Normal;

        _banner.Add(tag);
        _banner.Add(msg);
        root.Add(_banner);
    }

    IEnumerator ShowThenHide()
    {
        // Fade in
        yield return StartCoroutine(
            Fade(_banner, 0f, 1f, 0.4f));

        // Hold
        yield return new WaitForSeconds(4f);

        // Fade out
        yield return StartCoroutine(
            Fade(_banner, 1f, 0f, 0.4f));

        HideBanner();
    }

    IEnumerator Fade(VisualElement el,
        float from, float to, float duration)
    {
        float elapsed    = 0f;
        el.style.display = DisplayStyle.Flex;
        el.style.opacity = from;

        while (elapsed < duration)
        {
            elapsed         += Time.deltaTime;
            el.style.opacity = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        el.style.opacity = to;
    }

    void HideBanner()
    {
        _banner.style.display = DisplayStyle.None;
        _banner.style.opacity = 0;
    }

    // Call this when entering a new room to reset
    public void Reset()
    {
        _shown = false;
        HideBanner();
    }

    // Public so NavigationManager can force this closed on any screen
    // change away from Gameplay. Reset() alone isn't enough here — if
    // ShowThenHide() is still mid-fade, its next Fade() call sets display
    // back to Flex regardless, so the banner would silently reappear over
    // whatever screen the player just navigated to unless the coroutine
    // itself is stopped too.
    public void ForceHide()
    {
        StopAllCoroutines();
        _shown = false;
        HideBanner();
    }
}