using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameplayScreen
{
    readonly VisualElement _screen;

    // Frequency scanner mode (reused for both the one-time Scan Room
    // ritual and each Reveal's placement confirmation)
    VisualElement    _scannerDesaturate;
    VisualElement    _sineWaveContainer;
    SineWaveElement  _sineWave;
    Label            _signalDetectedLabel;

    // Scan → Reveal split: SCAN ROOM is a one-time per-room ritual that
    // unlocks the repeated REVEAL (find-next-clue) action. Kept as two
    // distinct buttons rather than one relabeled button so the ritual
    // read as a deliberate, separate beat instead of a hindrance.
    Button        _btnScanRoom;
    Button        _btnReveal;
    VisualElement _allFoundPanel;
    VisualElement _bottomBar;
    Label         _engagementToast;
    bool          _toastActive; // guards against the toast being stomped by another status write mid-display

    bool _hasScannedRoom;
    bool _revealLocked; // true between a successful find and the player dismissing that clue's detail popup
    bool _scannerActive;
    bool _scannerCooldown;

    // ── Progress bar ──────────────────────────────────────────────────────
    VisualElement _progressBar;
    VisualElement _progressFill;
    Label         _progressPercentLabel;
    int           _progressTotal = 5;

    // ── Signal readout + look-around nudge ──────────────────────────────────
    // "Getting warmer" proximity feedback for whatever clue the last Reveal
    // press just placed, plus a nudge if the player loses track of it —
    // both track ClueSpawner.LastSpawnedClue specifically, since this
    // project only ever has one un-revealed clue in the world at a time
    // (Reveal places exactly one, and locks until it's found).
    VisualElement _signalReadout;
    Label         _signalPercentLabel;
    VisualElement _signalFillBar;
    Coroutine     _proximityRoutine;

    const float SignalNearDistance = 0.3f;  // metres — 100% at or closer than this
    const float SignalFarDistance  = 5f;    // metres — 0% at or farther than this
    const float NudgeDelaySeconds  = 8f;

    // Was 2.2s x2 passes + 1.5s cooldown (~6.1s total to get from tapping
    // SCAN ROOM to REVEAL being usable) — cut down per feedback that the
    // wait felt too long.
    const float ScannerDuration = 0.6f;
    const float ScannerCooldown = 0.25f;

    public GameplayScreen(VisualElement root)
    {
        _screen = UIHelper.Screen();
        _screen.style.backgroundColor = Color.clear;
        _screen.style.justifyContent  = Justify.SpaceBetween;
        root.Add(_screen);

        // ── Top bar ────────────────────────────────────────────────────────
        var topBar  = UIHelper.TopBar();
        var btnBack = UIHelper.IconButtonImg(
            UIHelper.IconArrowLeft, 22f,
            new Color(0.8f, 0.85f, 0.9f, 0.8f));
        btnBack.clicked += () =>
            NavigationManager.Instance.GoTo(GameScreen.Chapters);

        var btnCrimeBoard = new Button();
        btnCrimeBoard.style.backgroundColor   =
            new Color(0.031f, 0.043f, 0.063f, 0.92f);
        btnCrimeBoard.style.borderTopColor    =
            new Color(0.4f, 0.7f, 1.0f, 0.6f);
        btnCrimeBoard.style.borderBottomColor =
            new Color(0.4f, 0.7f, 1.0f, 0.6f);
        btnCrimeBoard.style.borderLeftColor   =
            new Color(0.4f, 0.7f, 1.0f, 0.6f);
        btnCrimeBoard.style.borderRightColor  =
            new Color(0.4f, 0.7f, 1.0f, 0.6f);
        btnCrimeBoard.style.borderTopWidth    = 1;
        btnCrimeBoard.style.borderBottomWidth = 1;
        btnCrimeBoard.style.borderLeftWidth   = 1;
        btnCrimeBoard.style.borderRightWidth  = 1;
        btnCrimeBoard.style.borderTopLeftRadius     = 2;
        btnCrimeBoard.style.borderTopRightRadius    = 2;
        btnCrimeBoard.style.borderBottomLeftRadius  = 2;
        btnCrimeBoard.style.borderBottomRightRadius = 2;
        btnCrimeBoard.style.paddingTop    = 8;
        btnCrimeBoard.style.paddingBottom = 8;
        btnCrimeBoard.style.paddingLeft   = 14;
        btnCrimeBoard.style.paddingRight  = 14;

        var caseBtnRow = new VisualElement();
        caseBtnRow.style.flexDirection  = FlexDirection.Row;
        caseBtnRow.style.alignItems     = Align.Center;
        caseBtnRow.style.justifyContent = Justify.Center;

        var caseBtnLabel = new Label { text = "CASE FILE" };
        caseBtnLabel.style.fontSize      = 9;
        caseBtnLabel.style.color         =
            new Color(0.4f, 0.7f, 1.0f, 0.9f);
        caseBtnLabel.style.letterSpacing = 3;

        caseBtnRow.Add(caseBtnLabel);
        btnCrimeBoard.Add(caseBtnRow);
        btnCrimeBoard.clicked += () =>
            NavigationManager.Instance.GoTo(GameScreen.CrimeBoard);

        topBar.Add(btnBack);
        topBar.Add(btnCrimeBoard);
        _screen.Add(topBar);

        // ── Room-completion progress bar ─────────────────────────────────────
        // A third normal-flow child in a SpaceBetween container would get
        // pushed to vertical center instead of sitting under the top bar —
        // absolute-positioned at the very top edge instead, independent of
        // flex layout entirely.
        // Single continuous fill bar — was a row of 5 segmented blocks plus
        // a separate "X% · Y/Z FOUND" label underneath, which read as two
        // redundant progress indicators. Just one bar, filled by
        // percentage, labelled with the percentage.
        _progressBar = new VisualElement();
        _progressBar.style.position        = Position.Absolute;
        _progressBar.style.top             = 0;
        _progressBar.style.left            = 0;
        _progressBar.style.right           = 0;
        _progressBar.style.height          = 4;
        _progressBar.style.backgroundColor = new Color(0.4f, 0.5f, 0.6f, 0.2f);
        _progressBar.pickingMode           = PickingMode.Ignore;
        _screen.Add(_progressBar);

        _progressFill = new VisualElement();
        _progressFill.style.width           = Length.Percent(0);
        _progressFill.style.height          = Length.Percent(100);
        _progressFill.style.backgroundColor = new Color(0.4f, 0.7f, 1.0f, 0.95f);
        _progressBar.Add(_progressFill);

        _progressPercentLabel = new Label { text = "0%" };
        _progressPercentLabel.style.position        = Position.Absolute;
        _progressPercentLabel.style.top             = 12;
        _progressPercentLabel.style.left            = 0;
        _progressPercentLabel.style.right           = 0;
        _progressPercentLabel.style.fontSize        = 9;
        _progressPercentLabel.style.letterSpacing   = 2;
        _progressPercentLabel.style.color           = new Color(0.4f, 0.7f, 1.0f, 0.7f);
        _progressPercentLabel.style.unityTextAlign  = TextAnchor.MiddleCenter;
        _progressPercentLabel.pickingMode           = PickingMode.Ignore;
        _screen.Add(_progressPercentLabel);

        // ── Frequency scanner overlay ────────────────────────────────────────
        _scannerDesaturate = new VisualElement();
        _scannerDesaturate.style.position        = Position.Absolute;
        _scannerDesaturate.style.top = 0; _scannerDesaturate.style.left = 0;
        _scannerDesaturate.style.right = 0; _scannerDesaturate.style.bottom = 0;
        _scannerDesaturate.style.backgroundColor =
            new Color(UIHelper.Surface.r, UIHelper.Surface.g, UIHelper.Surface.b, 0.35f);
        _scannerDesaturate.style.display = DisplayStyle.None;
        _screen.Add(_scannerDesaturate);

        _signalDetectedLabel = new Label { text = "SIGNAL DETECTED" };
        _signalDetectedLabel.style.position        = Position.Absolute;
        _signalDetectedLabel.style.top             = 100;
        _signalDetectedLabel.style.left            = 0;
        _signalDetectedLabel.style.right           = 0;
        _signalDetectedLabel.style.fontSize        = 12;
        _signalDetectedLabel.style.color           = new Color(0.4f, 0.7f, 1.0f, 0.95f);
        _signalDetectedLabel.style.letterSpacing   = 4;
        _signalDetectedLabel.style.unityTextAlign  = TextAnchor.MiddleCenter;
        _signalDetectedLabel.style.opacity         = 0f;
        _screen.Add(_signalDetectedLabel);

        _sineWaveContainer = new VisualElement();
        _sineWaveContainer.style.position = Position.Absolute;
        _sineWaveContainer.style.left  = 0;
        _sineWaveContainer.style.right = 0;
        _sineWaveContainer.style.bottom = 160;
        _sineWaveContainer.style.height = 60;
        _sineWaveContainer.style.display = DisplayStyle.None;

        _sineWave = new SineWaveElement();
        _sineWave.style.width  = Length.Percent(100);
        _sineWave.style.height = Length.Percent(100);
        _sineWaveContainer.Add(_sineWave);
        _screen.Add(_sineWaveContainer);

        // ── Engagement toast — shown when a clue is found, until dismissed ───
        _engagementToast = new Label { text = "Take a moment with what you found" };
        _engagementToast.style.position        = Position.Absolute;
        _engagementToast.style.left            = 24;
        _engagementToast.style.right           = 24;
        _engagementToast.style.bottom          = 172;
        _engagementToast.style.fontSize        = 11;
        _engagementToast.style.unityFontStyleAndWeight = FontStyle.Italic;
        _engagementToast.style.color           = new Color(0.4f, 0.7f, 1.0f, 0.95f);
        _engagementToast.style.unityTextAlign  = TextAnchor.MiddleCenter;
        _engagementToast.style.whiteSpace      = WhiteSpace.Normal;
        _engagementToast.style.backgroundColor = new Color(0.031f, 0.043f, 0.063f, 0.85f);
        _engagementToast.style.paddingTop      = 8;
        _engagementToast.style.paddingBottom   = 8;
        _engagementToast.style.paddingLeft     = 14;
        _engagementToast.style.paddingRight    = 14;
        _engagementToast.style.borderTopLeftRadius     = 6;
        _engagementToast.style.borderTopRightRadius    = 6;
        _engagementToast.style.borderBottomLeftRadius  = 6;
        _engagementToast.style.borderBottomRightRadius = 6;
        _engagementToast.style.opacity         = 0f;
        _engagementToast.pickingMode           = PickingMode.Ignore;
        _screen.Add(_engagementToast);

        // ── Bottom — Scan Room / Reveal / All Found ─────────────────────────
        var bottomBar = new VisualElement();
        bottomBar.style.alignItems    = Align.Center;
        bottomBar.style.width         = Length.Percent(100);
        bottomBar.style.paddingBottom = 60;

        _btnScanRoom = MakeCircleButton("SCAN ROOM");
        _btnScanRoom.clicked += OnScanRoomPressed;

        // Signal readout — "getting warmer" proximity feedback for
        // whatever the last Reveal press just placed. Purely passive,
        // sits just above the action button.
        _signalReadout = new VisualElement();
        _signalReadout.style.alignItems  = Align.Center;
        _signalReadout.style.marginBottom = 14;
        _signalReadout.style.display     = DisplayStyle.None;
        _signalReadout.pickingMode       = PickingMode.Ignore;

        _signalPercentLabel = new Label { text = "SIGNAL: 0%" };
        _signalPercentLabel.style.fontSize      = 9;
        _signalPercentLabel.style.letterSpacing = 2;
        _signalPercentLabel.style.color         = new Color(0.4f, 0.7f, 1.0f, 0.85f);
        _signalPercentLabel.style.marginBottom  = 4;

        var fillTrack = new VisualElement();
        fillTrack.style.width           = 140;
        fillTrack.style.height          = 3;
        fillTrack.style.backgroundColor = new Color(0.4f, 0.5f, 0.6f, 0.25f);

        _signalFillBar = new VisualElement();
        _signalFillBar.style.width           = Length.Percent(0);
        _signalFillBar.style.height          = Length.Percent(100);
        _signalFillBar.style.backgroundColor = new Color(0.4f, 0.7f, 1.0f, 0.9f);
        fillTrack.Add(_signalFillBar);

        _signalReadout.Add(_signalPercentLabel);
        _signalReadout.Add(fillTrack);

        _btnReveal = MakeCircleButton("REVEAL");
        _btnReveal.clicked += OnRevealPressed;
        _btnReveal.style.display = DisplayStyle.None;

        _allFoundPanel = BuildAllFoundPanel();
        _allFoundPanel.style.display = DisplayStyle.None;

        bottomBar.Add(_signalReadout);
        bottomBar.Add(_btnScanRoom);
        bottomBar.Add(_btnReveal);
        bottomBar.Add(_allFoundPanel);
        _screen.Add(bottomBar);
        _bottomBar = bottomBar;

        EvidenceManager.OnEvidenceAdded += OnClueFound;
        ClueRevealUI.OnClueEngaged      += OnClueEngaged;
        // The manipulator toolbar and this screen's Scan/Reveal button
        // both live in the bottom third of the screen — hiding this
        // entirely while a clue is selected for repositioning guarantees
        // they can never both be tappable at once, regardless of exact
        // pixel positions. Confirmed live (2026-08-17): taps meant for
        // MOVE/ROTATE/SCALE were also landing on Reveal underneath,
        // silently spawning extra clues every time the toolbar was open.
        ClueManipulator.OnSelectionChanged += isSelected =>
            _bottomBar.style.display = isSelected ? DisplayStyle.None : DisplayStyle.Flex;
    }

    Button MakeCircleButton(string text)
    {
        var btn = new Button();
        btn.style.backgroundColor         =
            new Color(0.031f, 0.043f, 0.063f, 0.92f);
        btn.style.borderTopColor          =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btn.style.borderBottomColor       =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btn.style.borderLeftColor         =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btn.style.borderRightColor        =
            new Color(0.4f, 0.7f, 1.0f, 0.8f);
        btn.style.borderTopWidth          = 1;
        btn.style.borderBottomWidth       = 1;
        btn.style.borderLeftWidth         = 1;
        btn.style.borderRightWidth        = 1;
        btn.style.borderTopLeftRadius     = 50;
        btn.style.borderTopRightRadius    = 50;
        btn.style.borderBottomLeftRadius  = 50;
        btn.style.borderBottomRightRadius = 50;
        btn.style.width                   = 90;
        btn.style.height                  = 90;
        btn.style.alignItems              = Align.Center;
        btn.style.justifyContent          = Justify.Center;

        var row = new VisualElement();
        row.style.flexDirection  = FlexDirection.Column;
        row.style.alignItems     = Align.Center;
        row.style.justifyContent = Justify.Center;

        var label = new Label { text = text };
        label.style.fontSize       = 10;
        label.style.color          = new Color(0.4f, 0.7f, 1.0f, 0.9f);
        label.style.letterSpacing  = 2;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.whiteSpace     = WhiteSpace.Normal;

        row.Add(label);
        btn.Add(row);
        return btn;
    }

    VisualElement BuildAllFoundPanel()
    {
        var panel = new VisualElement();
        panel.style.alignItems         = Align.Center;
        panel.style.backgroundColor    = new Color(0.031f, 0.043f, 0.063f, 0.92f);
        panel.style.borderTopColor     = new Color(0.3f, 0.7f, 0.4f, 0.8f);
        panel.style.borderBottomColor  = new Color(0.3f, 0.7f, 0.4f, 0.8f);
        panel.style.borderLeftColor    = new Color(0.3f, 0.7f, 0.4f, 0.8f);
        panel.style.borderRightColor   = new Color(0.3f, 0.7f, 0.4f, 0.8f);
        panel.style.borderTopWidth     = 1;
        panel.style.borderBottomWidth  = 1;
        panel.style.borderLeftWidth    = 1;
        panel.style.borderRightWidth   = 1;
        panel.style.borderTopLeftRadius     = 8;
        panel.style.borderTopRightRadius    = 8;
        panel.style.borderBottomLeftRadius  = 8;
        panel.style.borderBottomRightRadius = 8;
        panel.style.paddingTop    = 14;
        panel.style.paddingBottom = 14;
        panel.style.paddingLeft   = 24;
        panel.style.paddingRight  = 24;

        var line1 = new Label { text = "ALL EVIDENCE FOUND" };
        line1.style.fontSize      = 10;
        line1.style.letterSpacing = 3;
        line1.style.color         = new Color(0.3f, 0.8f, 0.4f, 0.95f);
        line1.style.marginBottom  = 4;

        var line2 = new Label { text = "→ OPEN CASE FILE" };
        line2.style.fontSize                = 13;
        line2.style.letterSpacing           = 1;
        line2.style.unityFontStyleAndWeight = FontStyle.Bold;
        line2.style.color                   = UIHelper.TextPrim;

        panel.Add(line1);
        panel.Add(line2);
        panel.RegisterCallback<ClickEvent>(_ =>
            NavigationManager.Instance.GoTo(GameScreen.CrimeBoard));
        return panel;
    }

    // ── Scan Room (one-time ritual, no spawn) ────────────────────────────────

    void OnScanRoomPressed()
    {
        if (_hasScannedRoom || _scannerActive) return;

        if (ClueSpawner.Instance != null && ClueSpawner.Instance.IsWaitingForLetterReveal)
        {
            NavigationManager.Instance.StartCoroutine(FlashMessage("..."));
            return;
        }

        NavigationManager.Instance.StartCoroutine(RunScanRoomSweep());
    }

    IEnumerator RunScanRoomSweep()
    {
        _scannerActive = true;
        // Single pass now (was two, ~4.6s just for this part) — still
        // reads as a deliberate "calibrating the room" beat without
        // dragging.
        yield return RunSweepVisual("CALIBRATING");

        _hasScannedRoom = true;
        _scannerActive  = false;
        _btnScanRoom.style.display = DisplayStyle.None;
        _btnReveal.style.display   = DisplayStyle.Flex;
    }

    // ── Reveal (repeated find-next-clue) ─────────────────────────────────────

    void OnRevealPressed()
    {
        if (_scannerActive || _scannerCooldown || _revealLocked)
        {
            Debug.Log($"[GameplayScreen] Reveal ignored — scannerActive={_scannerActive} " +
                      $"scannerCooldown={_scannerCooldown} revealLocked={_revealLocked}");
            return;
        }

        var cam = Camera.main;
        if (cam == null) return;

        var ray = new Ray(cam.transform.position, cam.transform.forward);
        Vector3 hitPoint = Vector3.zero;
        AnchorType hitType = AnchorType.Unknown;
        bool hit = ARSceneManager.Instance != null &&
                   ARSceneManager.Instance.TryHitTestSurface(ray, out hitPoint, out hitType);

        if (!hit)
        {
            // No console log here previously — a failed hit-test (no AR
            // surface under the crosshair) was completely silent in
            // logcat, indistinguishable from the button being stuck/
            // locked. Confirmed live (2026-08-17): dozens of Reveal taps
            // in a row producing zero log output turned out to be this —
            // ARCore genuinely not detecting any surface in the room, not
            // a lock/state bug.
            Debug.Log("[GameplayScreen] Reveal miss — no AR surface hit under the camera's forward ray");
            AudioHapticsManager.Instance?.PlayScanMissCue();
            NavigationManager.Instance.StartCoroutine(FlashMessage("AIM AT A SURFACE"));
            return;
        }

        bool spawned = ClueSpawner.Instance != null &&
                       ClueSpawner.Instance.SpawnNextClue(hitPoint, hitType);

        if (!spawned)
        {
            AudioHapticsManager.Instance?.PlayScanMissCue();
            NavigationManager.Instance.StartCoroutine(FlashMessage("ALL EVIDENCE FOUND IN THIS ROOM"));
            return;
        }

        AudioHapticsManager.Instance?.PlayScanSuccessCue(hitPoint);
        Debug.Log($"[GameplayScreen] Reveal placed next clue at {hitPoint} ({hitType})");

        // Locks immediately — the player still has to physically walk to
        // and reveal the clue before Reveal becomes usable again (gated
        // on ClueRevealUI.OnClueEngaged, not on this placement). Dimmed +
        // relabeled so the lock is visible, not just a silently-ignored tap.
        _revealLocked = true;
        _btnReveal.style.opacity = 0.35f;
        var revealLabel = _btnReveal.Q<Label>();
        if (revealLabel != null)
        {
            // Smaller + no-wrap — at the button's normal 10px size
            // "ANALYZING..." wrapped mid-word inside the 90px circle.
            revealLabel.text            = "ANALYZING...";
            revealLabel.style.fontSize  = 8;
            revealLabel.style.whiteSpace = WhiteSpace.NoWrap;
        }
        NavigationManager.Instance.StartCoroutine(RunSweepVisual("SIGNAL DETECTED"));
        NavigationManager.Instance.StartCoroutine(RevealStuckTimeout());

        if (_proximityRoutine != null) NavigationManager.Instance.StopCoroutine(_proximityRoutine);
        var justSpawned = ClueSpawner.Instance?.LastSpawnedClue;
        if (justSpawned != null)
            _proximityRoutine = NavigationManager.Instance.StartCoroutine(TrackClueProximity(justSpawned));
    }

    // "Getting warmer" readout + the look-around nudge, both for
    // specifically the clue this Reveal press just placed. Runs until
    // that clue is found or a new one replaces it as the tracking target.
    IEnumerator TrackClueProximity(ClueObject clue)
    {
        _signalReadout.style.display = DisplayStyle.Flex;
        float elapsed = 0f;
        bool nudged = false;

        while (clue != null && !clue.IsRevealed)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                float dist = Vector3.Distance(cam.transform.position, clue.transform.position);
                float pct = Mathf.Clamp01(1f - (dist - SignalNearDistance) / (SignalFarDistance - SignalNearDistance));
                _signalPercentLabel.text  = $"SIGNAL: {Mathf.RoundToInt(pct * 100)}%";
                _signalFillBar.style.width = Length.Percent(pct * 100f);
            }

            elapsed += 0.2f;
            if (!nudged && elapsed >= NudgeDelaySeconds)
            {
                nudged = true;
                NavigationManager.Instance.StartCoroutine(
                    FlashMessage("SOMETHING'S NEARBY — WALK TOWARD THE GLOW", 3f));
            }

            yield return new WaitForSeconds(0.2f); // ~5x/second
        }

        _signalReadout.style.display = DisplayStyle.None;
        _proximityRoutine = null;
    }

    // Fires when a clue is physically revealed (ClueObject.Reveal ->
    // EvidenceManager.AddEvidence) — the "found" signal. Distinct from
    // "engaged" (ClueRevealUI.OnClueEngaged, the popup being dismissed),
    // which is what actually re-enables Reveal.
    void OnClueFound(EvidenceItem item)
    {
        // No engagement toast here any more — it sat directly on top of
        // ClueRevealUI's "COLLECT EVIDENCE" popup (both are visible for
        // the exact same window, found -> engaged), rendering as
        // unreadable overlapping text. The popup already covers what the
        // toast was for.
        UpdateProgressBar();

        int total = ClueSpawner.Instance?.TotalClueCount ?? 5;
        if (EvidenceManager.Instance != null && total > 0 && EvidenceManager.Instance.Count >= total)
        {
            _btnReveal.style.display     = DisplayStyle.None;
            _allFoundPanel.style.display = DisplayStyle.Flex;
        }
    }

    void OnClueEngaged()
    {
        HideEngagementToast();
        UnlockReveal();
    }

    void UnlockReveal()
    {
        _revealLocked = false;
        _btnReveal.style.opacity = 1f;
        var revealLabel = _btnReveal.Q<Label>();
        if (revealLabel != null)
        {
            revealLabel.text            = "REVEAL";
            revealLabel.style.fontSize  = 10;
            revealLabel.style.whiteSpace = WhiteSpace.Normal;
        }
    }

    // Safety net — reported live (2026-08-21): a room got permanently
    // stuck on "ANALYZING..." after the 3rd of 5 clues, with Reveal never
    // coming back. Most likely cause is a spawned clue landing somewhere
    // the player physically can't reach or see (e.g. behind furniture,
    // through a wall) — CheckTapInput/proximity reveal in ClueObject can
    // never fire for an object the player can't get to, so OnClueEngaged
    // never comes. Rather than debug every possible bad-placement case,
    // force-unlock after a generous wait so a session can never be
    // completely blocked by one unlucky spawn position.
    const float RevealStuckTimeoutSeconds = 45f;

    IEnumerator RevealStuckTimeout()
    {
        yield return new WaitForSeconds(RevealStuckTimeoutSeconds);
        if (!_revealLocked) yield break; // engaged normally before the timeout

        Debug.LogWarning("[GameplayScreen] Reveal was stuck locked for " +
            $"{RevealStuckTimeoutSeconds}s with no engagement — force-unlocking.");
        UnlockReveal();
    }

    void ShowEngagementToast()
    {
        _toastActive = true;
        _engagementToast.style.opacity = 1f;
    }

    void HideEngagementToast()
    {
        _toastActive = false;
        _engagementToast.style.opacity = 0f;
    }

    // ── Progress bar ──────────────────────────────────────────────────────

    // Rebuilds from ground truth (found count vs. total) rather than
    // resetting — safe to call whenever the screen becomes visible,
    // including a mid-room Case File round-trip, without losing progress.
    void RebuildProgressBar()
    {
        int spawnerTotal = ClueSpawner.Instance?.TotalClueCount ?? 0;
        _progressTotal = spawnerTotal > 0 ? spawnerTotal : 5;
        int found = EvidenceManager.Instance?.Count ?? 0;
        ApplyProgress(found);
    }

    void UpdateProgressBar()
    {
        int found = EvidenceManager.Instance?.Count ?? 0;
        ApplyProgress(found);
    }

    void ApplyProgress(int found)
    {
        int pct = _progressTotal > 0 ? Mathf.RoundToInt(found * 100f / _progressTotal) : 0;
        _progressFill.style.width  = Length.Percent(pct);
        _progressPercentLabel.text = $"{pct}%";
    }

    // Called from NavigationManager.BeginRoomDetection — fires once per
    // genuine new-room entry, not on every SetVisible (which also fires
    // when the player round-trips through the Case File mid-room and
    // shouldn't lose their Scan Room / Reveal-lock progress).
    public void ResetScanRevealState()
    {
        _hasScannedRoom = false;
        _revealLocked   = false;
        _btnScanRoom.style.display   = DisplayStyle.Flex;
        _btnReveal.style.display     = DisplayStyle.None;
        _btnReveal.style.opacity     = 1f;
        var revealLabel = _btnReveal.Q<Label>();
        if (revealLabel != null)
        {
            revealLabel.text            = "REVEAL";
            revealLabel.style.fontSize  = 10;
            revealLabel.style.whiteSpace = WhiteSpace.Normal;
        }
        _allFoundPanel.style.display = DisplayStyle.None;
        HideEngagementToast();

        if (_proximityRoutine != null)
        {
            NavigationManager.Instance.StopCoroutine(_proximityRoutine);
            _proximityRoutine = null;
        }
        if (_signalReadout != null) _signalReadout.style.display = DisplayStyle.None;
    }

    // ── Shared sweep visual (Scan Room ritual + Reveal placement) ────────────

    IEnumerator RunSweepVisual(string label)
    {
        _scannerActive = true;
        _scannerDesaturate.style.display = DisplayStyle.Flex;
        _sineWaveContainer.style.display = DisplayStyle.Flex;
        ClueSpawner.Instance?.PulseAllNearby();

        float elapsed      = 0f;
        float phase        = 0f;
        float labelOpacity = 0f;
        float amplitude    = 5f;
        if (!_toastActive) _signalDetectedLabel.text = label;

        while (elapsed < ScannerDuration)
        {
            elapsed += Time.deltaTime;
            phase   += Time.deltaTime * 6f;

            amplitude = Mathf.Lerp(amplitude, 40f, Time.deltaTime * 6f);

            _sineWave.Amplitude = amplitude;
            _sineWave.Phase     = phase;
            _sineWave.Redraw();

            labelOpacity = Mathf.MoveTowards(labelOpacity, 1f, Time.deltaTime * 3f);
            // Guard against stomping a concurrently-active status write —
            // same class of bug as a periodic readout silently overwriting
            // a one-off flash message.
            if (!_toastActive)
                _signalDetectedLabel.style.opacity = labelOpacity;

            yield return null;
        }

        _scannerDesaturate.style.display   = DisplayStyle.None;
        _sineWaveContainer.style.display   = DisplayStyle.None;
        if (!_toastActive) _signalDetectedLabel.style.opacity = 0f;
        _scannerActive = false;

        yield return RunCooldown();
    }

    // Brief, non-blocking message for "no surface"/"nothing left to find" —
    // deliberately skips the scanner animation and cooldown entirely so a
    // missed aim or a finished room doesn't cost the player time.
    IEnumerator FlashMessage(string text, float holdSeconds = 1.4f)
    {
        _toastActive = true; // reuse the same guard so RunSweepVisual can't stomp this either
        _signalDetectedLabel.text = text;

        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            _signalDetectedLabel.style.opacity = t / 0.2f;
            yield return null;
        }

        yield return new WaitForSeconds(holdSeconds);

        t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            _signalDetectedLabel.style.opacity = 1f - t / 0.35f;
            yield return null;
        }
        _signalDetectedLabel.style.opacity = 0f;
        _toastActive = false;
    }

    IEnumerator RunCooldown()
    {
        _scannerCooldown = true;
        _btnReveal.style.opacity   = 0.4f;
        _btnScanRoom.style.opacity = 0.4f;
        yield return new WaitForSeconds(ScannerCooldown);
        _btnReveal.style.opacity   = 1f;
        _btnScanRoom.style.opacity = 1f;
        _scannerCooldown = false;
    }

    public void SetVisible(bool v)
    {
        _screen.style.display =
            v ? DisplayStyle.Flex : DisplayStyle.None;

        if (v)
        {
            IdleHintSystem.Instance?.StartTracking();
            AllCluesFoundNotification.Instance?.Reset();

            var room = WorldStateManager.Instance?.State?.CurrentRoom;
            ClueSpawner.Instance?.ActivateRoom(room?.RoomNumber ?? 1);
            RebuildProgressBar();

            // Log room entry
            if (room != null)
            {
                var cam = Camera.main;
                StudyLogger.Instance?.OnRoomEntered(
                    room.RoomNumber, room.RoomType,
                    cam != null ? cam.transform.position : Vector3.zero);
            }
        }
        else
        {
            IdleHintSystem.Instance?.StopTracking();
        }
    }
}

// Draws an animated sine wave using UI Toolkit's Painter2D API — amplitude
// and phase are updated externally each frame by GameplayScreen while the
// frequency scanner is active.
class SineWaveElement : VisualElement
{
    public float Amplitude = 5f;
    public float Phase     = 0f;

    static readonly Color WaveColor = new Color(0.4f, 0.7f, 1.0f, 0.9f);

    public SineWaveElement()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        float width  = contentRect.width;
        float height = contentRect.height;
        if (width <= 0f || height <= 0f) return;

        float midY = height / 2f;
        var painter = mgc.painter2D;
        painter.strokeColor = WaveColor;
        painter.lineWidth   = 2f;
        painter.BeginPath();

        const int segments = 80;
        for (int i = 0; i <= segments; i++)
        {
            float x = width * i / segments;
            float t = (x / width) * 4f * Mathf.PI + Phase;
            float y = midY + Mathf.Sin(t) * Amplitude;

            if (i == 0) painter.MoveTo(new Vector2(x, y));
            else        painter.LineTo(new Vector2(x, y));
        }

        painter.Stroke();
    }

    public void Redraw() => MarkDirtyRepaint();
}
