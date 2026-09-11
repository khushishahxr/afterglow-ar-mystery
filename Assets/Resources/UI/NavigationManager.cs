using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public enum GameScreen
{
    ResearcherSetup,
    Onboarding,
    Home,
    Chapters,
    Teaser,
    Gameplay,
    CrimeBoard,
    TheoryResult,
    Ending,
    SessionEnd,
    Journal,
    Dossier
}

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance { get; private set; }
    [SerializeField] UIDocument uiDocument;

    VisualElement          _root;
    ResearcherSetupScreen  _researcherSetup;
    OnboardingScreen       _onboarding;
    HomeScreen             _home;
    ChaptersScreen         _chapters;
    GameplayScreen         _gameplay;
    CrimeBoardScreen       _crimeBoard;
    SettingsPopup          _settings;
    TeaserScreen           _teaser;
    TheoryResultScreen     _theoryResult;
    EndingScreen           _ending;
    SessionEndScreen       _sessionEnd;
    JournalScreen          _journal;
    DossierScreen          _dossier;
    AcknowledgementOverlay _acknowledgement;
    LoadingOverlay         _loadingOverlay;
    VisualElement          _transitionOverlay;
    Coroutine              _transitionRoutine;

    int  _pendingRoomNumber;
    public bool RoomNarrativeReady { get; private set; }

    // ── Researcher 3-finger-hold gesture ────────────────────────────────────
    const float ResearcherHoldDuration = 3f;
    float _researcherHoldTimer = 0f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        uiDocument.sortingOrder = 100;
        _root = uiDocument.rootVisualElement;
        _root.style.position = Position.Absolute;
        _root.style.top    = 0; _root.style.left   = 0;
        _root.style.right  = 0; _root.style.bottom = 0;
        _root.style.width  = Length.Percent(100);
        _root.style.height = Length.Percent(100);

        // Build all screens
        _researcherSetup = new ResearcherSetupScreen(_root);
        _onboarding       = new OnboardingScreen(_root);
        _home         = new HomeScreen(_root);
        _chapters     = new ChaptersScreen(_root);
        _gameplay     = new GameplayScreen(_root);
        _crimeBoard   = new CrimeBoardScreen(_root);
        _settings     = new SettingsPopup(_root);
        _teaser       = new TeaserScreen(_root);
        _theoryResult = new TheoryResultScreen(_root);
        _ending       = new EndingScreen(_root);
        _sessionEnd   = new SessionEndScreen(_root);
        _journal      = new JournalScreen(_root);
        _dossier      = new DossierScreen(_root);

        // Full-screen fade-through-black used by GoTo() so every screen
        // change is a soft dip, never a hard instant cut (see
        // FadeTransition). Sits above the screens themselves but below
        // the acknowledgement/loading overlays built next.
        _transitionOverlay = new VisualElement();
        _transitionOverlay.style.position        = Position.Absolute;
        _transitionOverlay.style.top    = 0; _transitionOverlay.style.left   = 0;
        _transitionOverlay.style.right  = 0; _transitionOverlay.style.bottom = 0;
        _transitionOverlay.style.backgroundColor = UIHelper.BG;
        _transitionOverlay.style.opacity = 0f;
        _transitionOverlay.pickingMode   = PickingMode.Ignore;
        _root.Add(_transitionOverlay);

        // Added last so it renders above every other screen during the
        // brief post-submission acknowledgement.
        _acknowledgement = new AcknowledgementOverlay(_root);
        _loadingOverlay  = new LoadingOverlay(_root);

        // Subscribe to events
        NarrativeGenerator.OnNarrativeReady    += OnNarrativeReady;
        NarrativeGenerator.OnNarrativeError    += OnNarrativeError;
        NarrativeGenerator.OnGenerationStarted += OnGenerationStarted;
        NarrativeGenerator.OnGenerationComplete += OnGenerationComplete;
        WorldStateManager.OnRoomComplete     += OnRoomComplete;
        WorldStateManager.OnThemeEstablished += OnThemeEstablished;
        WorldStateManager.OnAllRoomsComplete += OnAllRoomsComplete;
        ARSceneManager.OnRoomReady           += OnRoomReady;

        // Load saved progress
        LoadSavedProgress();

        GoTo(GameScreen.ResearcherSetup);
    }

    void OnDestroy()
    {
        NarrativeGenerator.OnNarrativeReady    -= OnNarrativeReady;
        NarrativeGenerator.OnNarrativeError    -= OnNarrativeError;
        NarrativeGenerator.OnGenerationStarted -= OnGenerationStarted;
        NarrativeGenerator.OnGenerationComplete -= OnGenerationComplete;
        WorldStateManager.OnRoomComplete     -= OnRoomComplete;
        WorldStateManager.OnThemeEstablished -= OnThemeEstablished;
        WorldStateManager.OnAllRoomsComplete -= OnAllRoomsComplete;
        ARSceneManager.OnRoomReady           -= OnRoomReady;
    }

    void Update()
    {
        HandleResearcherGesture();
        HandleUniversalClickFallback();
    }

    // Universal click/tap fallback — bypasses the scene's EventSystem/UI
    // input module entirely. Originally an Editor-only workaround for
    // unreliable pointer-click routing during Editor testing; confirmed
    // on-device (2026-08-10) that real Android builds hit the exact same
    // routing failure (nothing on ResearcherSetupScreen responds to touch
    // at all), so this now runs everywhere rather than trusting the
    // EventSystem's own picking. Does its own hit-test against the panel
    // from the raw pointer position (mouse in Editor, touch on device) and
    // dispatches a simulated click/focus directly to whatever interactive
    // element is under the tap. Handles Buttons (click) and focusable
    // fields like TextField (focus, which triggers the native soft
    // keyboard on Android — no need to route real typed input through
    // here, only getting the field focused).
    void HandleUniversalClickFallback()
    {
        Vector2? pressPos = null;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            pressPos = Mouse.current.position.ReadValue();
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            pressPos = Touchscreen.current.primaryTouch.position.ReadValue();

        if (pressPos == null) { return; }
        Debug.Log($"[ClickFallback] Press detected at {pressPos.Value}");

        if (_root?.panel == null)
        {
            Debug.LogWarning("[ClickFallback] _root.panel is null — aborting");
            return;
        }

        try
        {
            // Touchscreen/Mouse position from the Input System is
            // bottom-left origin (Y=0 at the bottom of the screen).
            // RuntimePanelUtils.ScreenToPanel on this device was empirically
            // confirmed (2026-08-10 on-device log) to treat the input as
            // top-left origin instead — taps near the top of the screen were
            // resolving near the bottom of panel space and vice versa.
            // Flipping Y here corrects that before conversion.
            Vector2 screenPos = new Vector2(pressPos.Value.x, Screen.height - pressPos.Value.y);
            Vector2 panelPos  = RuntimePanelUtils.ScreenToPanel(_root.panel, screenPos);
            Debug.Log($"[ClickFallback] screenPos {screenPos} -> panelPos {panelPos}");

            VisualElement picked = _root.panel.Pick(panelPos);
            if (picked == null)
            {
                Debug.LogWarning("[ClickFallback] panel.Pick returned null — nothing under tap point");
                return;
            }
            var chain = new System.Text.StringBuilder();
            for (VisualElement e = picked; e != null; e = e.parent)
                chain.Append($"{e.GetType().Name}(name='{e.name}',pick={e.pickingMode},bounds={e.worldBound}) < ");
            Debug.Log($"[ClickFallback] picked chain: {chain}");

            VisualElement target = picked;
            while (target != null && !(target is Button) && !(target is TextField) && !target.focusable)
                target = target.parent;

            if (target == null || !target.enabledInHierarchy)
            {
                Debug.LogWarning($"[ClickFallback] no interactive target found (target null: {target == null})");
                return;
            }
            Debug.Log($"[ClickFallback] dispatching to target: {target.GetType().Name} name='{target.name}'");

            // On the device this fallback was originally built against
            // (2026-08-10), the PointerDown/Up simulation below never
            // actually fired .clicked, which is why the reflection
            // fallback exists. On another device the synthetic events DID
            // trigger .clicked on their own, so forcing it again via
            // reflection double-fired every tap (2026-08-17: onboarding
            // skipping every other slide). A first attempt to detect that
            // by listening for ClickEvent backfired worse: on THIS device,
            // dispatch produces a ClickEvent WITHOUT .clicked actually
            // running — confirmed live (2026-08-17), Begin Session logged
            // the click reaching the button but OnBeginSession() itself
            // never ran, silently dead-ending the whole screen. ClickEvent
            // reception is not a reliable proxy for .clicked having fired.
            // Hooking the real public .clicked event directly is: it's the
            // exact same multicast delegate list reflection would invoke,
            // so whatever mechanism actually calls it (native pipeline,
            // the synthetic dispatch, anything) is observed directly.
            bool clickAlreadyFired = false;
            System.Action clickMarker = null;
            if (target is Button detectorButton)
            {
                clickMarker = () => clickAlreadyFired = true;
                detectorButton.clicked += clickMarker;
            }

            var downEvt = new Event { type = EventType.MouseDown, mousePosition = panelPos, button = 0, clickCount = 1 };
            using (var pointerDown = PointerDownEvent.GetPooled(downEvt))
            {
                pointerDown.target = target;
                target.SendEvent(pointerDown);
            }

            var upEvt = new Event { type = EventType.MouseUp, mousePosition = panelPos, button = 0, clickCount = 1 };
            using (var pointerUp = PointerUpEvent.GetPooled(upEvt))
            {
                pointerUp.target = target;
                target.SendEvent(pointerUp);
            }

            if (target is Button button)
            {
                if (clickMarker != null) button.clicked -= clickMarker;

                if (clickAlreadyFired)
                {
                    Debug.Log("[ClickFallback] synthetic dispatch already fired .clicked — skipping reflection invoke");
                }
                else
                {
                    // Dispatch didn't land — invoke the delegate Clickable
                    // actually calls directly via reflection, matching the
                    // effect of a real click.
                    try
                    {
                        var clickableProp = typeof(Button).GetProperty("clickable",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        var clickable = clickableProp?.GetValue(button);
                        var clickedField = clickable?.GetType().GetField("clicked",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var del = clickedField?.GetValue(clickable) as System.Action;
                        Debug.Log($"[ClickFallback] reflection: clickable={clickable != null}, delegate={del != null}");
                        del?.Invoke();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[ClickFallback] reflection click invoke failed: {ex}");
                    }
                }
            }

            // Buttons respond to the pointer/click events above via their
            // own click detection. Text fields need an explicit focus call —
            // that's what actually triggers the native on-screen keyboard
            // on Android, independent of whatever the EventSystem does.
            if (target is TextField || target.focusable)
            {
                target.Focus();
                Debug.Log($"[ClickFallback] called Focus() on {target.name}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NavigationManager] Universal click fallback failed: {e.Message}");
        }
    }

    // Holding 3 fingers on screen for 3 seconds, from any screen, returns
    // to the researcher setup screen without clearing session data.
    void HandleResearcherGesture()
    {
        var touchscreen = Touchscreen.current;
        int activeTouches = 0;
        if (touchscreen != null)
        {
            foreach (var touch in touchscreen.touches)
                if (touch.press.isPressed) activeTouches++;
        }

        if (activeTouches >= 3)
        {
            _researcherHoldTimer += Time.deltaTime;
            if (_researcherHoldTimer >= ResearcherHoldDuration)
            {
                _researcherHoldTimer = 0f;
                ReturnToResearcherSetup();
            }
        }
        else
        {
            _researcherHoldTimer = 0f;
        }
    }

    // ── Save / Load ────────────────────────────────────────────────────────

    void LoadSavedProgress()
    {
        var saveData = SaveSystem.Instance?.LoadProgress();
        if (saveData == null) return;

        foreach (int roomNumber in saveData.CompletedRooms)
            _chapters.MarkChapterComplete(roomNumber);

        if (!string.IsNullOrEmpty(saveData.Theme) &&
            System.Enum.TryParse<ApocalypseTheme>(
                saveData.Theme, out var theme))
        {
            WorldStateManager.Instance?.SetTheme(
                theme, saveData.ThemeDescription);
        }

        Debug.Log($"[NavigationManager] Progress loaded — " +
                  $"{saveData.CompletedRooms.Count} rooms complete");
    }

    // ── Event handlers ─────────────────────────────────────────────────────

    void OnNarrativeReady(NarrativeData data)
    {
        Debug.Log($"[NavigationManager] Narrative: {data.ChapterTitle}");

        if (!string.IsNullOrEmpty(data.ApocalypseTheme))
        {
            if (System.Enum.TryParse<ApocalypseTheme>(
                data.ApocalypseTheme, out var theme))
            {
                WorldStateManager.Instance?.SetTheme(
                    theme, data.ThemeDescription);
            }
        }

        int currentRoom =
            WorldStateManager.Instance?.CurrentRoomNumber ?? 1;

        _chapters.UpdateWithNarrative(data, currentRoom);
        _crimeBoard.UpdateWithNarrative(data, currentRoom);

        RoomNarrativeReady = true;
    }

    void OnNarrativeError(string error)
    {
        Debug.LogError($"[NavigationManager] Narrative error: {error}");
    }

    // Atmospheric loading state — only shown for real on-device API calls,
    // not the instant editor mock path.
    void OnGenerationStarted()
    {
#if !UNITY_EDITOR
        _loadingOverlay?.Show();
#endif
    }

    void OnGenerationComplete()
    {
#if !UNITY_EDITOR
        _loadingOverlay?.Hide();
#endif
    }

    void OnRoomReady(RoomContext context)
    {
        var detected = context.AnchorTypes().ConvertAll(t => t.ToString());
        Debug.Log($"[NavigationManager] AR room ready — " +
                  $"{detected.Count} anchors for room {_pendingRoomNumber}");
        NarrativeGenerator.Instance?.GenerateForRoom(
            detected, _pendingRoomNumber);
    }

    // ── Room detection ─────────────────────────────────────────────────────

    // Called when the Teaser screen opens for a chapter — resets AR/clue
    // state from the previous room and starts detecting the new one so the
    // narrative can be ready by the time the player taps "Enter the Room".
    public void BeginRoomDetection(int chapterNumber)
    {
        _pendingRoomNumber = chapterNumber;
        RoomNarrativeReady = false;

        ARSceneManager.Instance?.ResetForNewRoom();
        ClueSpawner.Instance?.ResetForNewRoom();
        EvidenceManager.Instance?.Clear();
        EvidenceTraceManager.Instance?.ClearTraces();
        AnchorNarrator.Instance?.ResetForNewRoom();
        GhostSilhouette.ResetForNewRoom();
        _gameplay?.ResetScanRevealState();

        #if UNITY_EDITOR
            ARSceneManager.Instance?.SimulateRoom();
        #endif
    }

    void OnRoomComplete(RoomRecord room)
    {
        Debug.Log($"[NavigationManager] Room {room.RoomNumber} complete");
        _chapters.MarkChapterComplete(room.RoomNumber);
    }

    void OnThemeEstablished(ApocalypseTheme theme)
    {
        Debug.Log($"[NavigationManager] Theme: {theme}");
    }

    void OnAllRoomsComplete(WorldState state)
    {
        Debug.Log("[NavigationManager] All rooms complete — ending");
        ShowEnding();
    }

    // ── Navigation ─────────────────────────────────────────────────────────

    // Every screen change dips through a brief fade-to-black-and-back
    // instead of an instant hard cut — see _transitionOverlay. Current
    // updates immediately (synchronously) so callers reading it right
    // after GoTo() see the new logical state even while the visual
    // transition is still playing out.
    public void GoTo(GameScreen target)
    {
        Current = target;

        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(FadeTransition(target));
    }

    IEnumerator FadeTransition(GameScreen target)
    {
        _transitionOverlay.pickingMode = PickingMode.Position;

        // A second GoTo() can StopCoroutine() this one mid-flight, and
        // SwitchVisibility() can throw if a screen isn't ready — either way
        // this must still release the click-blocking overlay, or every
        // screen in the app is left permanently unclickable.
        try
        {
            yield return FadeTransitionOverlay(0f, 1f, 0.22f);
            SwitchVisibility(target);
            yield return FadeTransitionOverlay(1f, 0f, 0.28f);
        }
        finally
        {
            _transitionOverlay.pickingMode = PickingMode.Ignore;
            _transitionOverlay.style.opacity = 0f;
            _transitionRoutine = null;
        }
    }

    IEnumerator FadeTransitionOverlay(float from, float to, float duration)
    {
        float t = 0f;
        _transitionOverlay.style.opacity = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            _transitionOverlay.style.opacity = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _transitionOverlay.style.opacity = to;
    }

    void SwitchVisibility(GameScreen target)
    {
        _researcherSetup.SetVisible(target == GameScreen.ResearcherSetup);
        _onboarding.SetVisible(target       == GameScreen.Onboarding);
        _home.SetVisible(target         == GameScreen.Home);
        _chapters.SetVisible(target     == GameScreen.Chapters);
        _teaser.SetVisible(target       == GameScreen.Teaser);
        _gameplay.SetVisible(target     == GameScreen.Gameplay);
        _crimeBoard.SetVisible(target   == GameScreen.CrimeBoard);
        _theoryResult.SetVisible(target == GameScreen.TheoryResult);
        _ending.SetVisible(target       == GameScreen.Ending);
        _sessionEnd.SetVisible(target   == GameScreen.SessionEnd);
        _journal.SetVisible(target      == GameScreen.Journal);
        _dossier.SetVisible(target      == GameScreen.Dossier);

        // These all live outside this screen system (DontDestroyOnLoad
        // components sharing the same root UIDocument) — nothing else
        // closes them on navigation, so without this they can stay open
        // and clickable/blocking over whatever screen the player lands on
        // next.
        if (target != GameScreen.Gameplay)
        {
            ClueManipulator.Instance?.Deselect();
            ClueRevealUI.Instance?.ForceHide();
            AllCluesFoundNotification.Instance?.ForceHide();
        }
    }

    // Journal/Dossier are reachable from more than one screen (Case File
    // and the room list) — their back button needs to return to whichever
    // screen actually opened them, not a hardcoded destination.
    GameScreen _journalCaller = GameScreen.Chapters;
    GameScreen _dossierCaller = GameScreen.Chapters;

    public void ShowJournal(GameScreen caller)
    {
        _journalCaller = caller;
        GoTo(GameScreen.Journal);
        _journal.Refresh();
    }

    public void ReturnFromJournal() => GoTo(_journalCaller);

    public void ShowDossier(GameScreen caller)
    {
        _dossierCaller = caller;
        GoTo(GameScreen.Dossier);
        _dossier.Refresh();
    }

    public void ReturnFromDossier() => GoTo(_dossierCaller);

    public void ShowOnboarding()
    {
        GoTo(GameScreen.Onboarding);
        _onboarding.Begin();
    }

    public void ShowTeaser(int chapterNumber)
    {
        GoTo(GameScreen.Teaser);
        _teaser.ShowForChapter(chapterNumber);

        SignalEscalationManager.Instance?.SetRoom(chapterNumber);
        SignalAudioManager.Instance?.SetRoom(chapterNumber);
    }

    public void ShowTheoryResult(
        TheoryAnalysis analysis, string truthReveal)
    {
        Debug.Log("[NavigationManager] ShowTheoryResult called");
        StartCoroutine(ShowTheoryResultWithAcknowledgement(analysis, truthReveal));
    }

    IEnumerator ShowTheoryResultWithAcknowledgement(
        TheoryAnalysis analysis, string truthReveal)
    {
        yield return StartCoroutine(_acknowledgement.Play());
        GoTo(GameScreen.TheoryResult);
        _theoryResult.ShowResult(analysis, truthReveal);
    }

    public void ShowEnding()
    {
        GoTo(GameScreen.Ending);
        _ending.Show();
    }

    public void ShowSessionEnd()
    {
        GoTo(GameScreen.SessionEnd);
        _sessionEnd.Show();
    }

    public void ShowSettings(bool show) =>
        _settings.SetVisible(show);

    // Researcher gesture — jumps back to setup without touching session
    // data, so the researcher can peek at device status mid-session.
    public void ReturnToResearcherSetup()
    {
        GoTo(GameScreen.ResearcherSetup);
        _researcherSetup.RefreshChecklist();
    }

    // "RESET FOR NEXT PARTICIPANT" on the Session End screen — clears
    // in-memory story/evidence/study state and saved progress, then
    // returns to setup with a blank participant form.
    public void ResetForNextParticipant()
    {
        WorldStateManager.Instance?.ResetAll();
        EvidenceManager.Instance?.Clear();
        StudyLogger.Instance?.ResetSession();
        SaveSystem.Instance?.DeleteSave();
        _chapters.ResetAll();

        SignalEscalationManager.Instance?.Reset();
        SignalAudioManager.Instance?.Stop();
        EvidenceTraceManager.Instance?.ClearTraces();
        AnchorNarrator.Instance?.ResetForNewRoom();
        GhostSilhouette.ResetForNewRoom();
        ARLetterReveal.ResetSession();

        GoTo(GameScreen.ResearcherSetup);
        _researcherSetup.ResetForNewParticipant();
    }

    public GameScreen Current { get; private set; }
}
