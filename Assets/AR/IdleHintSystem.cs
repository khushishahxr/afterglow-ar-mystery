using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class IdleHintSystem : MonoBehaviour
{
    public static IdleHintSystem Instance { get; private set; }

    [SerializeField] UIDocument uiDocument;
    [SerializeField] float idleThreshold = 60f;

    VisualElement _hintBanner;
    Label         _hintTag;
    AudioSource   _voiceSource;
    float  _idleTimer   = 0f;
    bool   _active      = false;
    bool   _hintShowing = false;

    // Generic, theme-agnostic fallback for the rare case a room's
    // narrative has no HintLine (e.g. static-fallback layer content
    // predating this system, or a room that hasn't finished generating
    // yet) — not shown once real per-room lines are available.
    static readonly string[] GenericFallbackHints = new[]
    {
        "Move slowly through the room. The objects remember.",
        "Something in this room is waiting to be found.",
        "Walk closer to objects. The truth reveals itself.",
    };

    int _hintIndex = 0;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (uiDocument == null)
            uiDocument = FindFirstObjectByType<UIDocument>();

        BuildHintBanner();
        HideHint();

        EvidenceManager.OnEvidenceAdded += OnEvidenceFound;
    }

    void OnDestroy()
    {
        EvidenceManager.OnEvidenceAdded -= OnEvidenceFound;
    }

    void OnEvidenceFound(EvidenceItem item)
    {
        // Player found something — reset idle timer
        _idleTimer = 0f;
        if (_hintShowing)
            StartCoroutine(FadeOut());
    }

    public void StartTracking()
    {
        _active    = true;
        _idleTimer = 0f;
        Debug.Log("[IdleHint] Tracking started");
    }

    public void StopTracking()
    {
        _active = false;
        // A ShowHint() coroutine already mid-flight would otherwise set
        // display back to Flex on its next Fade() call regardless of the
        // HideHint() below — same class of bug as AllCluesFoundNotification.
        StopAllCoroutines();
        _hintShowing = false;
        HideHint();
    }

    void Update()
    {
        if (!_active)      return;
        if (_hintShowing)  return;

        _idleTimer += Time.deltaTime;

        if (_idleTimer >= idleThreshold)
        {
            _idleTimer = 0f;
            StartCoroutine(ShowHint());
        }
    }

    void BuildHintBanner()
    {
        var root = uiDocument.rootVisualElement;

        _hintBanner = new VisualElement();
        _hintBanner.style.position        = Position.Absolute;
        _hintBanner.style.bottom          = 160;
        _hintBanner.style.left            = 30;
        _hintBanner.style.right           = 30;
        _hintBanner.style.backgroundColor =
            new Color(0.031f, 0.043f, 0.063f, 0.9f);
        _hintBanner.style.borderTopColor    = UIHelper.AccentDim;
        _hintBanner.style.borderBottomColor = UIHelper.AccentDim;
        _hintBanner.style.borderLeftColor   = UIHelper.AccentDim;
        _hintBanner.style.borderRightColor  = UIHelper.AccentDim;
        _hintBanner.style.borderTopWidth    = 1;
        _hintBanner.style.borderBottomWidth = 1;
        _hintBanner.style.borderLeftWidth   = 1;
        _hintBanner.style.borderRightWidth  = 1;
        _hintBanner.style.borderTopLeftRadius     = 2;
        _hintBanner.style.borderTopRightRadius    = 2;
        _hintBanner.style.borderBottomLeftRadius  = 2;
        _hintBanner.style.borderBottomRightRadius = 2;
        _hintBanner.style.paddingTop    = 12;
        _hintBanner.style.paddingBottom = 12;
        _hintBanner.style.paddingLeft   = 20;
        _hintBanner.style.paddingRight  = 20;
        _hintBanner.style.alignItems    = Align.Center;
        _hintBanner.style.opacity       = 0;

        _hintTag = new Label { text = "◉ RECORDING FOUND" };
        _hintTag.style.fontSize      = 8;
        _hintTag.style.color         = UIHelper.AccentDim;
        _hintTag.style.letterSpacing = 4;
        _hintTag.style.marginBottom  = 4;

        var hintText = new Label { text = "" };
        hintText.name              = "HintText";
        hintText.style.fontSize    = 12;
        hintText.style.color       = UIHelper.TextMuted;
        hintText.style.unityTextAlign = TextAnchor.MiddleCenter;
        hintText.style.whiteSpace  = WhiteSpace.Normal;
        hintText.style.unityFontStyleAndWeight = FontStyle.Italic;

        _hintBanner.Add(_hintTag);
        _hintBanner.Add(hintText);
        root.Add(_hintBanner);
    }

    IEnumerator ShowHint()
    {
        _hintShowing = true;

        string line = NarrativeGenerator.Instance?.CurrentNarrative?.HintLine;
        if (string.IsNullOrEmpty(line))
        {
            line = GenericFallbackHints[_hintIndex % GenericFallbackHints.Length];
            _hintIndex++;
        }

        var hintText = _hintBanner.Q<Label>("HintText");
        if (hintText != null) hintText.text = line;

        // "Tuning in..." masks the TTS request's real latency (~1-3s) —
        // the banner is already visible and readable while this resolves,
        // so the wait doesn't feel like dead time even before any audio
        // plays.
        bool ttsAttempted = DiegeticTTSManager.Instance != null && DiegeticTTSManager.Instance.IsConfigured;
        if (ttsAttempted) _hintTag.text = "◉ TUNING IN...";

        yield return StartCoroutine(Fade(_hintBanner, 0f, 1f, 0.5f));

        if (ttsAttempted)
        {
            AudioClip clip = null;
            bool done = false;
            DiegeticTTSManager.Instance.RequestSpeech(line, c => { clip = c; done = true; });
            yield return new WaitUntil(() => done);

            // Null means the call failed or was unconfigured after all —
            // same graceful text-only fallback as the twin's missing-file
            // case, just discovered at request time instead of import time.
            _hintTag.text = "◉ RECORDING FOUND";
            if (clip != null)
            {
                if (_voiceSource == null)
                {
                    _voiceSource = gameObject.AddComponent<AudioSource>();
                    _voiceSource.spatialBlend = 0f;
                }
                _voiceSource.clip = clip;
                _voiceSource.Play();
            }
        }

        // Hold
        yield return new WaitForSeconds(5f);

        // Fade out
        yield return StartCoroutine(
            Fade(_hintBanner, 1f, 0f, 0.5f));

        HideHint();
        _hintShowing = false;
    }

    IEnumerator FadeOut()
    {
        _hintShowing = true;
        yield return StartCoroutine(
            Fade(_hintBanner, 1f, 0f, 0.3f));
        HideHint();
        _hintShowing = false;
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

    void HideHint()
    {
        _hintBanner.style.display = DisplayStyle.None;
        _hintBanner.style.opacity = 0;
    }

    public void Reset()
    {
        _idleTimer   = 0f;
        _hintShowing = false;
        _hintIndex   = 0;
        if (_hintTag != null) _hintTag.text = "◉ RECORDING FOUND";
        HideHint();
    }
}