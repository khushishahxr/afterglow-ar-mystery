using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Lets the player fine-tune a found clue's placement after collection —
// tapping a revealed clue a second time opens a MOVE/ROTATE/SCALE/DONE
// toolbar instead of a full 3-axis gizmo (fiddly on touch, and gizmo
// handles read as visual clutter in AR). Self-bootstraps at scene load,
// reusing the game's existing UIDocument rather than creating a competing
// panel — see NavigationManager.cs for why panel identity matters here
// (sortingOrder / the EventSystem's input module were the actual root
// cause of an earlier, unrelated click-routing bug in this project; new
// UI built outside that shared panel would risk hitting it again).
public class ClueManipulator : MonoBehaviour
{
    public static ClueManipulator Instance { get; private set; }

    // Fired whenever the toolbar opens/closes — GameplayScreen uses this to
    // hide its own Scan/Reveal button while a clue is being repositioned,
    // so the two can never be tappable at the same time regardless of
    // on-screen layout. Confirmed live (2026-08-17): even after moving the
    // toolbar clear of the action button, relying on position alone is
    // fragile — this makes the exclusion explicit rather than incidental.
    public static event System.Action<bool> OnSelectionChanged;

    enum Mode { Move, Rotate, Scale }

    const float RotateSensitivity = 0.3f;   // degrees per pixel of horizontal drag
    const float ScaleSensitivity  = 0.003f; // scale multiplier per pixel of vertical drag
    const float MinScaleMult      = 0.5f;
    const float MaxScaleMult      = 2.0f;

    ClueObject _selected;
    Mode       _mode;
    Vector3    _originalScale;
    float      _scaleMultiplier = 1f;

    // The same tap that selects an object would otherwise also register
    // as the first frame of a drag in this per-frame polling loop — this
    // eats that specific press so selecting doesn't immediately yank the
    // object toward wherever the thumb happens to be.
    bool _suppressCurrentPress;
    bool _dragging;
    Vector3 _lastGroundHit;
    Vector2 _lastScreenPos;

    VisualElement _toolbar;
    Button _btnMove, _btnRotate, _btnScale, _btnDone;

    Label _hint;
    bool  _hasShownHintThisSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("ClueManipulator").AddComponent<ClueManipulator>();
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        BuildToolbar();
    }

    // ── Toolbar UI ────────────────────────────────────────────────────────

    void BuildToolbar()
    {
        var doc = FindFirstObjectByType<UIDocument>();
        if (doc == null)
        {
            Debug.LogWarning("[ClueManipulator] No UIDocument found — toolbar unavailable.");
            return;
        }

        _toolbar = new VisualElement();
        _toolbar.style.position        = Position.Absolute;
        _toolbar.style.left            = 0;
        _toolbar.style.right           = 0;
        // Was bottom:40, directly underneath GameplayScreen's circular
        // Scan/Reveal button (which occupies roughly the 60-150px range
        // from the bottom edge) — taps meant for this toolbar were also
        // landing on Reveal underneath, silently spawning extra clues
        // every time the player tried to reposition a found object.
        // Matches the twin (Fixed Narrative) project's EvidenceManipulator,
        // which sits well clear of its own action button at the same spot.
        _toolbar.style.bottom          = 210;
        _toolbar.style.flexDirection   = FlexDirection.Row;
        _toolbar.style.justifyContent  = Justify.Center;
        _toolbar.style.display         = DisplayStyle.None;
        _toolbar.pickingMode           = PickingMode.Position;

        _btnMove   = MakeToolbarButton("MOVE");
        _btnRotate = MakeToolbarButton("ROTATE");
        _btnScale  = MakeToolbarButton("SCALE");
        _btnDone   = MakeToolbarButton("DONE");

        _btnMove.clicked   += () => SetMode(Mode.Move);
        _btnRotate.clicked += () => SetMode(Mode.Rotate);
        _btnScale.clicked  += () => SetMode(Mode.Scale);
        _btnDone.clicked   += Deselect;

        _toolbar.Add(_btnMove);
        _toolbar.Add(_btnRotate);
        _toolbar.Add(_btnScale);
        _toolbar.Add(_btnDone);
        doc.rootVisualElement.Add(_toolbar);

        // One-time instructional hint — fades in above the toolbar the
        // first time a clue is selected this session, holds briefly, fades
        // back out. Never shown again after that.
        _hint = new Label
        {
            text = "Pick MOVE, ROTATE, or SCALE above, then drag on the object to adjust it"
        };
        _hint.style.position         = Position.Absolute;
        _hint.style.left             = 24;
        _hint.style.right            = 24;
        _hint.style.bottom           = 278; // sits just above the repositioned toolbar
        _hint.style.fontSize         = 11;
        _hint.style.color            = new Color(0.4f, 0.7f, 1.0f, 0.95f);
        _hint.style.unityTextAlign   = TextAnchor.MiddleCenter;
        _hint.style.whiteSpace       = WhiteSpace.Normal;
        _hint.style.backgroundColor  = new Color(0.031f, 0.043f, 0.063f, 0.85f);
        _hint.style.paddingTop       = 8;
        _hint.style.paddingBottom    = 8;
        _hint.style.paddingLeft      = 14;
        _hint.style.paddingRight     = 14;
        _hint.style.borderTopLeftRadius     = 6;
        _hint.style.borderTopRightRadius    = 6;
        _hint.style.borderBottomLeftRadius  = 6;
        _hint.style.borderBottomRightRadius = 6;
        _hint.style.opacity          = 0f;
        _hint.pickingMode            = PickingMode.Ignore;
        doc.rootVisualElement.Add(_hint);
    }

    IEnumerator ShowHintOnce()
    {
        _hasShownHintThisSession = true;

        float t = 0f;
        while (t < 0.4f) { t += Time.deltaTime; _hint.style.opacity = t / 0.4f; yield return null; }
        _hint.style.opacity = 1f;

        yield return new WaitForSeconds(4f);

        t = 0f;
        while (t < 0.5f) { t += Time.deltaTime; _hint.style.opacity = 1f - t / 0.5f; yield return null; }
        _hint.style.opacity = 0f;
    }

    Button MakeToolbarButton(string label)
    {
        var btn = new Button();
        btn.style.marginLeft    = 5;
        btn.style.marginRight   = 5;
        btn.style.paddingLeft   = 18;
        btn.style.paddingRight  = 18;
        btn.style.paddingTop    = 13;
        btn.style.paddingBottom = 13;
        btn.style.backgroundColor = new Color(0.031f, 0.043f, 0.063f, 0.92f);
        btn.style.borderTopWidth = btn.style.borderBottomWidth =
            btn.style.borderLeftWidth = btn.style.borderRightWidth = 1;
        btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius =
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 6;

        var lbl = new Label { text = label };
        lbl.style.fontSize                = 11;
        lbl.style.letterSpacing           = 2;
        lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        lbl.style.color                   = new Color(0.4f, 0.7f, 1.0f, 0.95f);
        lbl.name = "ToolbarLabel";
        btn.Add(lbl);

        return btn;
    }

    void SetMode(Mode mode)
    {
        _mode = mode;
        RefreshModeHighlight();
    }

    void RefreshModeHighlight()
    {
        StyleModeButton(_btnMove,   _mode == Mode.Move);
        StyleModeButton(_btnRotate, _mode == Mode.Rotate);
        StyleModeButton(_btnScale,  _mode == Mode.Scale);
    }

    void StyleModeButton(Button btn, bool active)
    {
        Color c = active
            ? new Color(0.4f, 0.7f, 1.0f, 0.95f)
            : new Color(0.4f, 0.7f, 1.0f, 0.3f);
        btn.style.borderTopColor = btn.style.borderBottomColor =
            btn.style.borderLeftColor = btn.style.borderRightColor = c;
    }

    // ── Public API ────────────────────────────────────────────────────────

    // True once anything OTHER than the requester is selected — clue
    // objects check this at the top of their own Update() so proximity
    // reveal / tap-to-collect / tap-to-select are all suppressed for every
    // other piece of evidence while one is being adjusted.
    public bool IsLocked(ClueObject requester) => _selected != null && _selected != requester;

    public void Select(ClueObject clue)
    {
        if (_toolbar == null || clue == null || _selected == clue) return;

        _selected             = clue;
        _originalScale        = clue.transform.localScale;
        _scaleMultiplier      = 1f;
        _mode                 = Mode.Move;
        _suppressCurrentPress = true;
        _dragging             = false;

        _toolbar.style.display = DisplayStyle.Flex;
        RefreshModeHighlight();
        OnSelectionChanged?.Invoke(true);

        if (!_hasShownHintThisSession && _hint != null)
            StartCoroutine(ShowHintOnce());
    }

    // Public so NavigationManager can force this closed when the player
    // leaves Gameplay — this toolbar lives on a DontDestroyOnLoad object
    // outside the normal screen-visibility system, so nothing else was
    // clearing it, and it was staying selected/visible over the Case File
    // and every other screen if the player navigated away mid-adjustment.
    public void Deselect()
    {
        bool wasSelected = _selected != null;
        _selected  = null;
        _dragging  = false;
        if (_toolbar != null) _toolbar.style.display = DisplayStyle.None;
        if (wasSelected) OnSelectionChanged?.Invoke(false);
    }

    // ── Drag handling ─────────────────────────────────────────────────────

    void Update()
    {
        if (_selected == null) return;

        var pointer = Pointer.current;
        if (pointer == null) return;

        if (_suppressCurrentPress)
        {
            // Wait out the release of the press that performed selection
            // before drag-detection is allowed to see a fresh press.
            if (!pointer.press.isPressed) _suppressCurrentPress = false;
            return;
        }

        if (pointer.press.wasPressedThisFrame)
        {
            _dragging      = true;
            _lastScreenPos = pointer.position.ReadValue();
            if (!TryGetGroundHit(_lastScreenPos, out _lastGroundHit))
                _lastGroundHit = _selected.transform.position;
        }
        else if (pointer.press.wasReleasedThisFrame)
        {
            _dragging = false;
        }

        if (!_dragging) return;

        Vector2 currentScreenPos = pointer.position.ReadValue();
        Vector2 screenDelta      = currentScreenPos - _lastScreenPos;

        switch (_mode)
        {
            case Mode.Move:
                // Delta between consecutive raycast hits, not an absolute
                // teleport-to-touch-point — otherwise the object jumps to
                // wherever the thumb happens to be the instant it lands.
                if (TryGetGroundHit(currentScreenPos, out Vector3 currentHit))
                {
                    _selected.transform.position += currentHit - _lastGroundHit;
                    _lastGroundHit = currentHit;
                }
                break;

            case Mode.Rotate:
                // Was world-Y (yaw) only, driven by horizontal drag. Now
                // also drives pitch (tilt forward/back) from vertical
                // drag, around the camera's own right axis so "up" on
                // screen always tilts the object the way it visually
                // looks like it should, regardless of which way the
                // player is currently facing. A single-finger 2D drag can
                // only ever carry 2 independent rotation inputs — full
                // free-roll (Z/twist) would need a second, separate
                // gesture (e.g. two-finger twist), which isn't wired up.
                _selected.transform.Rotate(
                    Vector3.up, screenDelta.x * RotateSensitivity, Space.World);

                var cam = Camera.main;
                if (cam != null)
                {
                    _selected.transform.Rotate(
                        cam.transform.right, -screenDelta.y * RotateSensitivity, Space.World);
                }
                break;

            case Mode.Scale:
                // Uniform scale, proportional to vertical drag distance,
                // clamped to roughly 0.5x-2x the object's original size.
                _scaleMultiplier = Mathf.Clamp(
                    _scaleMultiplier + screenDelta.y * ScaleSensitivity,
                    MinScaleMult, MaxScaleMult);
                _selected.transform.localScale = _originalScale * _scaleMultiplier;
                break;
        }

        _lastScreenPos = currentScreenPos;
    }

    bool TryGetGroundHit(Vector2 screenPos, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        var cam = Camera.main;
        if (cam == null || ARSceneManager.Instance == null) return false;

        Ray ray = cam.ScreenPointToRay(screenPos);
        return ARSceneManager.Instance.TryHitTestSurface(ray, out hitPoint, out _);
    }
}
