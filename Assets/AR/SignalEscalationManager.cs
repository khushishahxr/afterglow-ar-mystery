using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Applies room-based sensory escalation (vignette, scan lines, flashes,
// glitch sequence) to the Gameplay AR view. Deliberately condition-agnostic
// — used by both the AI and Fixed narrative builds — so atmospheric
// intensity itself is not a confound between conditions; only the
// underlying narrative generation mechanism differs between them.
//
// Self-bootstraps at scene load so no manual scene/Inspector setup is
// required.
public class SignalEscalationManager : MonoBehaviour
{
    public static SignalEscalationManager Instance { get; private set; }

    static readonly Color ColdBlue = new Color(0.4f, 0.7f, 1.0f);

    UIDocument     _uiDocument;
    VisualElement  _overlay;
    VisualElement  _vignette;
    VisualElement  _desaturate;
    VisualElement  _flash;
    VisualElement  _scanLineLayer;
    Label          _glitchText;

    int  _currentRoom;
    int  _pendingRoom = -1;
    bool _ready;

    Coroutine _scanLineRoutine;
    Coroutine _periodicFlashRoutine;
    Coroutine _sequenceRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("SignalEscalationManager")
            .AddComponent<SignalEscalationManager>();
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _uiDocument = FindFirstObjectByType<UIDocument>();
        BuildOverlay();
        _ready = true;
        if (_pendingRoom >= 0) SetRoom(_pendingRoom);
    }

    void BuildOverlay()
    {
        if (_uiDocument == null) return;
        var root = _uiDocument.rootVisualElement;

        _overlay = new VisualElement { name = "SignalOverlay" };
        _overlay.style.position     = Position.Absolute;
        _overlay.style.top          = 0;
        _overlay.style.left         = 0;
        _overlay.style.right        = 0;
        _overlay.style.bottom       = 0;
        _overlay.pickingMode        = PickingMode.Ignore;
        root.Add(_overlay);

        _vignette = new VisualElement();
        _vignette.style.position        = Position.Absolute;
        _vignette.style.top             = 0; _vignette.style.left = 0;
        _vignette.style.right           = 0; _vignette.style.bottom = 0;
        _vignette.style.opacity         = 0f;
        _vignette.style.backgroundImage = new StyleBackground(GenerateVignetteTexture());
        _vignette.style.backgroundSize  = new BackgroundSize(BackgroundSizeType.Cover);
        _vignette.pickingMode           = PickingMode.Ignore;
        _overlay.Add(_vignette);

        _desaturate = new VisualElement();
        _desaturate.style.position        = Position.Absolute;
        _desaturate.style.top             = 0; _desaturate.style.left = 0;
        _desaturate.style.right           = 0; _desaturate.style.bottom = 0;
        _desaturate.style.backgroundColor = UIHelper.Surface;
        _desaturate.style.opacity         = 0f;
        _desaturate.pickingMode           = PickingMode.Ignore;
        _overlay.Add(_desaturate);

        _flash = new VisualElement();
        _flash.style.position        = Position.Absolute;
        _flash.style.top             = 0; _flash.style.left = 0;
        _flash.style.right           = 0; _flash.style.bottom = 0;
        _flash.style.backgroundColor = new Color(ColdBlue.r, ColdBlue.g, ColdBlue.b, 1f);
        _flash.style.opacity         = 0f;
        _flash.pickingMode           = PickingMode.Ignore;
        _overlay.Add(_flash);

        _scanLineLayer = new VisualElement();
        _scanLineLayer.style.position = Position.Absolute;
        _scanLineLayer.style.top = 0; _scanLineLayer.style.left = 0;
        _scanLineLayer.style.right = 0; _scanLineLayer.style.bottom = 0;
        _scanLineLayer.pickingMode    = PickingMode.Ignore;
        _overlay.Add(_scanLineLayer);

        _glitchText = new Label { text = "THE SIGNAL REMEMBERS" };
        _glitchText.style.position        = Position.Absolute;
        _glitchText.style.top             = Length.Percent(45);
        _glitchText.style.left            = 0;
        _glitchText.style.right           = 0;
        _glitchText.style.fontSize        = 16;
        _glitchText.style.color           = new Color(ColdBlue.r, ColdBlue.g, ColdBlue.b, 0.95f);
        _glitchText.style.letterSpacing   = 4;
        _glitchText.style.unityTextAlign  = TextAnchor.MiddleCenter;
        _glitchText.style.opacity         = 0f;
        _glitchText.pickingMode           = PickingMode.Ignore;
        _overlay.Add(_glitchText);
    }

    Texture2D GenerateVignetteTexture(int size = 256)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2(size / 2f, size / 2f);
        float maxDist = center.magnitude;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float alpha = Mathf.Clamp01((dist - 0.35f) / 0.65f);
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                tex.SetPixel(x, y, new Color(ColdBlue.r, ColdBlue.g, ColdBlue.b, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    // Called by NavigationManager whenever the Teaser screen opens for a
    // chapter, so escalation matches the room the player is about to enter.
    public void SetRoom(int roomNumber)
    {
        _currentRoom = roomNumber;

        if (!_ready) { _pendingRoom = roomNumber; return; }

        StopRoomCoroutines();
        _glitchTriggeredThisRoom = false;

        switch (roomNumber)
        {
            case 1:
                _vignette.style.opacity   = 0f;
                _desaturate.style.opacity = 0f;
                break;
            case 2:
                _vignette.style.opacity   = 0.08f;
                _desaturate.style.opacity = 0f;
                break;
            case 3:
                _vignette.style.opacity   = 0.15f;
                _desaturate.style.opacity = 0f;
                _scanLineRoutine = StartCoroutine(ScanLineLoop(8f, 12f));
                break;
            case 4:
                _vignette.style.opacity   = 0.22f;
                _desaturate.style.opacity = 0f;
                _scanLineRoutine     = StartCoroutine(ScanLineLoop(4f, 6f));
                _periodicFlashRoutine = StartCoroutine(PeriodicFlashLoop());
                break;
            case 5:
                _vignette.style.opacity   = 0.30f;
                _desaturate.style.opacity = 0.2f;
                _scanLineRoutine = StartCoroutine(ScanLineLoop(2f, 3f));
                break;
            case 6:
                _sequenceRoutine = StartCoroutine(RunRoom6Sequence());
                break;
            default:
                _vignette.style.opacity   = 0f;
                _desaturate.style.opacity = 0f;
                break;
        }
    }

    // Researcher "reset for next participant" — clear everything back to
    // a blank state.
    public void Reset()
    {
        StopRoomCoroutines();
        _currentRoom = 0;
        _pendingRoom = -1;
        _glitchTriggeredThisRoom = false;
        if (!_ready) return;
        _vignette.style.opacity    = 0f;
        _desaturate.style.opacity  = 0f;
        _flash.style.opacity       = 0f;
        _glitchText.style.opacity  = 0f;
        _overlay.style.scale       = new Scale(Vector3.one);
        _scanLineLayer.Clear();
    }

    void StopRoomCoroutines()
    {
        if (_scanLineRoutine != null)      { StopCoroutine(_scanLineRoutine);      _scanLineRoutine = null; }
        if (_periodicFlashRoutine != null) { StopCoroutine(_periodicFlashRoutine); _periodicFlashRoutine = null; }
        if (_sequenceRoutine != null)      { StopCoroutine(_sequenceRoutine);      _sequenceRoutine = null; }
        if (_scanLineLayer != null) _scanLineLayer.Clear();
    }

    // ── Scan lines ─────────────────────────────────────────────────────────

    IEnumerator ScanLineLoop(float minInterval, float maxInterval)
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            yield return FlickerScanLine();
        }
    }

    IEnumerator RapidScanLineBurst(float totalDuration, float interval)
    {
        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            StartCoroutine(FlickerScanLine());
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }

    IEnumerator FlickerScanLine()
    {
        var line = new VisualElement();
        line.style.position        = Position.Absolute;
        line.style.left            = 0;
        line.style.right           = 0;
        line.style.height          = 1;
        line.style.top             = Length.Percent(Random.Range(0f, 100f));
        line.style.backgroundColor = new Color(1f, 1f, 1f, 0f);
        line.pickingMode           = PickingMode.Ignore;
        _scanLineLayer.Add(line);

        float duration = 0.1f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI) * 0.3f;
            line.style.backgroundColor = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        _scanLineLayer.Remove(line);
    }

    // ── Flashes ────────────────────────────────────────────────────────────

    IEnumerator PeriodicFlashLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(120f, 180f));
            yield return FlashOnce(new Color(ColdBlue.r, ColdBlue.g, ColdBlue.b, 1f), 0.05f, 0.3f);
        }
    }

    IEnumerator FlashOnce(float peakOpacity, float duration) =>
        FlashOnce(new Color(ColdBlue.r, ColdBlue.g, ColdBlue.b, 1f), peakOpacity, duration);

    IEnumerator FlashOnce(Color color, float peakOpacity, float duration)
    {
        _flash.style.backgroundColor = color;
        float half = duration / 2f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            _flash.style.opacity = Mathf.Lerp(0f, peakOpacity, t / half);
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            _flash.style.opacity = Mathf.Lerp(peakOpacity, 0f, t / half);
            yield return null;
        }
        _flash.style.opacity = 0f;
    }

    // ── Room 6 entry sequence ─────────────────────────────────────────────

    IEnumerator RunRoom6Sequence()
    {
        _vignette.style.opacity   = 0f;
        _desaturate.style.opacity = 0f;

        // 2. Full screen cold blue flash
        yield return FlashOnce(0.6f, 0.5f);

        // 3. "Wrong room" effect for 3 seconds — complementary tint + scale
        Color complementary = new Color(1f - ColdBlue.r, 1f - ColdBlue.g, 1f - ColdBlue.b, 1f);
        _flash.style.backgroundColor = complementary;
        _flash.style.opacity         = 0.4f;
        _overlay.style.scale         = new Scale(new Vector3(1.02f, 1.02f, 1f));

        yield return new WaitForSeconds(3f);

        // 4. Snap back to clean
        _flash.style.opacity         = 0f;
        _flash.style.backgroundColor = new Color(ColdBlue.r, ColdBlue.g, ColdBlue.b, 1f);
        _overlay.style.scale         = new Scale(Vector3.one);

        // 5. Vignette fades in softly
        yield return FadeElement(_vignette, 0f, 0.35f, 1.5f);

        _sequenceRoutine = null;
    }

    IEnumerator FadeElement(VisualElement el, float from, float to, float duration)
    {
        float t = 0f;
        el.style.opacity = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            el.style.opacity = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        el.style.opacity = to;
    }

    // ── Room glitch sequence (Block 7) ──────────────────────────────────────

    bool _glitchTriggeredThisRoom;
    public bool GlitchAlreadyTriggeredThisRoom => _glitchTriggeredThisRoom;

    public void TriggerGlitchSequence(System.Action onFinalClueReady)
    {
        if (_glitchTriggeredThisRoom) return;
        _glitchTriggeredThisRoom = true;
        AudioHapticsManager.Instance?.PlayGlitchCue();
        StartCoroutine(RunGlitchSequence(onFinalClueReady));
    }

    IEnumerator RunGlitchSequence(System.Action onFinalClueReady)
    {
        // 0.0s — rapid scan line flicker for 2 seconds
        StartCoroutine(RapidScanLineBurst(2f, 0.1f));

        yield return new WaitForSeconds(1f);
        // 1.0s — previously collected clue outlines re-flash
        EvidenceTraceManager.Instance?.FlashAllTraces();

        yield return new WaitForSeconds(1f);
        // 2.0s — strong cold blue tint
        yield return FlashOnce(0.4f, 0.3f);

        yield return new WaitForSeconds(0.2f);
        // 2.5s — "THE SIGNAL REMEMBERS"
        yield return FadeElement(_glitchText, 0f, 1f, 0.2f);
        yield return new WaitForSeconds(1.1f);
        yield return FadeElement(_glitchText, 1f, 0f, 0.2f);

        yield return new WaitForSeconds(1f);
        // 5.0s — final clue materialises
        onFinalClueReady?.Invoke();
    }
}
