using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARSceneManager : MonoBehaviour
{
    public static ARSceneManager Instance { get; private set; }

    [Header("AR Components")]
    [SerializeField] ARPlaneManager planeManager;

    [Header("Detection Settings")]
    [SerializeField] int   minimumAnchorsRequired   = 2;
    [SerializeField] float detectionTimeoutSeconds  = 30f;
    [SerializeField] float deduplicationRadius      = 0.4f;

    public static event Action<RoomContext>    OnRoomReady;
    public static event Action<DetectedAnchor> OnAnchorAdded;
    public static event Action<AnchorType, string> OnAnchorNarrationNeeded;

    static readonly HashSet<AnchorType> NarratableTypes = new()
    {
        AnchorType.Table, AnchorType.Floor, AnchorType.Wall,
        AnchorType.Desk, AnchorType.Window
    };

    readonly List<DetectedAnchor> _registry = new();
    readonly HashSet<TrackableId> _classifiedPlanes = new();
    bool _roomReady = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        if (planeManager != null)
            planeManager.planesChanged += OnPlanesChanged;

        // Detection/timeout only makes sense once the player has actually
        // entered a room — ResetForNewRoom() (called from
        // NavigationManager.BeginRoomDetection, i.e. only when a Teaser
        // screen opens for a real room) is what should start this timer.
        // Starting it here too meant it ran from app launch regardless of
        // what screen the player was on — Researcher Setup, Onboarding,
        // Home, sitting on a menu mid-testing — and if 30s elapsed before
        // the player ever reached Room 1, this fired a spurious
        // OnRoomReady with _pendingRoomNumber still at its default (0),
        // generating a bogus "room 0" (clamped to mock Room 1 content)
        // that polluted WorldState with a phantom room/fact every time.
    }

    void OnDisable()
    {
        if (planeManager != null)
            planeManager.planesChanged -= OnPlanesChanged;
    }

    // ── Plane detection events ─────────────────────────────────────────────

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var plane in args.added)
            TryRegisterPlane(plane);

        foreach (var plane in args.updated)
            TryRegisterPlane(plane);
    }

    void TryRegisterPlane(ARPlane plane)
    {
        // A plane's boundary keeps growing for many frames after first
        // detection (normal AR Foundation behavior). Classifying on every
        // update lets the same physical surface register as both Table
        // (while still small) and Floor (once it grows past the
        // threshold), clustering clues at nearly the same position. Each
        // physical plane is classified once, the first time it's seen.
        if (_classifiedPlanes.Contains(plane.trackableId)) return;

        var classified = ClassifyPlane(plane);
        if (classified == null) return;

        string rawLabel   = classified == AnchorType.Table ? "table_surface"
                           : classified == AnchorType.Floor ? "floor_surface"
                           : "wall_surface";
        float  confidence = classified == AnchorType.Table ? 0.8f
                           : classified == AnchorType.Floor ? 0.9f
                           : 0.7f;

        TryRegister(classified.Value, plane.transform.position,
                    plane.transform.rotation, rawLabel, confidence);
        _classifiedPlanes.Add(plane.trackableId);
    }

    // Shared by both passive plane registration above and the active
    // Scan-button hit-test below, so both use identical thresholds.
    static AnchorType? ClassifyPlane(ARPlane plane)
    {
        float area = plane.size.x * plane.size.y;

        if (plane.alignment == PlaneAlignment.HorizontalUp)
        {
            if (area > 0.3f && area < 2.0f) return AnchorType.Table;
            if (area >= 2.0f)               return AnchorType.Floor;
            return null;
        }

        if (plane.alignment == PlaneAlignment.Vertical && area > 1.5f)
            return AnchorType.Wall;

        return null;
    }

    // ── Scan-driven clue placement hit-test ─────────────────────────────────

    // Manual ray/plane intersection against currently-tracked planes
    // (rather than ARRaycastManager, to avoid depending on plane collider
    // meshes that may be disabled for visual reasons). Used by the Scan
    // button to place exactly one clue wherever the player is currently
    // aiming, instead of clues auto-spawning when an anchor is detected.
    public bool TryHitTestSurface(Ray ray, out Vector3 hitPoint, out AnchorType hitType)
    {
        hitPoint = Vector3.zero;
        hitType  = AnchorType.Unknown;
        if (planeManager == null) return false;

        float closestDist = float.MaxValue;
        bool  found        = false;

        foreach (var plane in planeManager.trackables)
        {
            var mathPlane = new Plane(plane.normal, plane.center);
            if (!mathPlane.Raycast(ray, out float enter)) continue;
            if (enter < 0f || enter >= closestDist) continue;

            Vector3 point = ray.GetPoint(enter);

            // Reject hits that land outside the plane's rough bounding
            // radius — the math plane above is infinite, the physical
            // surface isn't.
            float roughRadius = Mathf.Max(plane.size.x, plane.size.y) * 0.5f;
            if (Vector3.Distance(point, plane.center) > roughRadius) continue;

            var classified = ClassifyPlane(plane);
            if (classified == null) continue;

            closestDist = enter;
            hitPoint    = point;
            hitType     = classified.Value;
            found       = true;
        }

        return found;
    }

    // ── Camera frame capture (for vision-grounded narrative) ────────────────

    ARCameraManager _cameraManager;

    ARCameraManager CameraManager
    {
        get
        {
            if (_cameraManager == null)
                _cameraManager = Camera.main?.GetComponent<ARCameraManager>();
            return _cameraManager;
        }
    }

    // Grabs the current AR camera frame (not a screenshot — this reads the
    // raw camera feed directly, so it never picks up UI Toolkit overlays)
    // and returns it as JPEG bytes, downscaled to keep the request payload
    // small. Used by NarrativeGenerator to ground room narration in what
    // the player's camera is actually looking at, not just the detected
    // surface type. Returns false (and no bytes) if no frame is available
    // right now — callers should fall back to text-only generation.
    public bool TryCaptureFrameAsJpeg(out byte[] jpegBytes, int targetWidth = 640, int jpegQuality = 75)
    {
        jpegBytes = null;
        var mgr = CameraManager;
        if (mgr == null) return false;
        if (!mgr.TryAcquireLatestCpuImage(out XRCpuImage image)) return false;

        using (image)
        {
            int targetHeight = Mathf.Max(1,
                Mathf.RoundToInt(targetWidth * (image.height / (float)image.width)));

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect         = new RectInt(0, 0, image.width, image.height),
                outputDimensions  = new Vector2Int(targetWidth, targetHeight),
                outputFormat      = TextureFormat.RGBA32,
                transformation    = XRCpuImage.Transformation.MirrorY
            };

            int size = image.GetConvertedDataSize(conversionParams);
            var buffer = new NativeArray<byte>(size, Allocator.Temp);

            try
            {
                image.Convert(conversionParams, buffer);

                var tex = new Texture2D(
                    conversionParams.outputDimensions.x,
                    conversionParams.outputDimensions.y,
                    TextureFormat.RGBA32, false);
                tex.LoadRawTextureData(buffer);
                tex.Apply();

                jpegBytes = tex.EncodeToJPG(jpegQuality);
                Destroy(tex);
            }
            finally
            {
                buffer.Dispose();
            }
        }

        return jpegBytes != null && jpegBytes.Length > 0;
    }

    // ── Manual registration (for editor testing) ───────────────────────────

    public void RegisterAnchor(AnchorType type, Vector3 pos,
                               Quaternion rot, string rawLabel,
                               float confidence)
    {
        TryRegister(type, pos, rot, rawLabel, confidence);
    }

    // ── Registry ───────────────────────────────────────────────────────────

    void TryRegister(AnchorType type, Vector3 pos,
                     Quaternion rot, string rawLabel, float confidence)
    {
        // Deduplicate
        foreach (var existing in _registry)
        {
            if (existing.Type == type &&
                Vector3.Distance(existing.WorldPosition, pos)
                    < deduplicationRadius)
                return;
        }

        var anchor = new DetectedAnchor
        {
            Type          = type,
            WorldPosition = pos,
            WorldRotation = rot,
            Confidence    = confidence,
            RawLabel      = rawLabel
        };

        _registry.Add(anchor);
        OnAnchorAdded?.Invoke(anchor);
        Debug.Log($"[ARSceneManager] Registered {type} at {pos}");

        // Room 1 opener — first wall detected fires the AR letter reveal.
        if (type == AnchorType.Wall &&
            (WorldStateManager.Instance?.CurrentRoomNumber ?? 1) == 1)
        {
            ARLetterReveal.TryTrigger(pos, rot);
        }

        // Atmospheric flavour text for the surface — only once narrative
        // context exists, and only for surfaces distinctive enough to
        // narrate (not Chair/Door, which read as too generic).
        if (NarratableTypes.Contains(type) &&
            NarrativeGenerator.Instance?.CurrentNarrative != null)
        {
            string roomType = WorldStateManager.Instance?.GetRoomType(
                WorldStateManager.Instance.CurrentRoomNumber) ?? "room";
            OnAnchorNarrationNeeded?.Invoke(type, roomType);
        }

        CheckRoomReady();
    }

    // ── Room ready ─────────────────────────────────────────────────────────

    void CheckRoomReady()
    {
        if (_roomReady) return;
        if (_registry.Count < minimumAnchorsRequired) return;

        _roomReady = true;
        var context = BuildRoomContext();
        Debug.Log($"[ARSceneManager] Room ready — {context.Anchors.Count} anchors");
        OnRoomReady?.Invoke(context);
    }

    IEnumerator DetectionTimeout()
    {
        yield return new WaitForSeconds(detectionTimeoutSeconds);
        if (_roomReady) yield break;

        _roomReady = true;
        Debug.LogWarning("[ARSceneManager] Timeout — proceeding with partial room");
        OnRoomReady?.Invoke(BuildRoomContext());
    }

    RoomContext BuildRoomContext()
    {
        var ctx = new RoomContext();
        ctx.Anchors.AddRange(_registry);
        return ctx;
    }

    // ── Editor simulation ──────────────────────────────────────────────────

    // Call this in editor to simulate room detection without a real device
    public void SimulateRoom()
    {
        Debug.Log("[ARSceneManager] Simulating room detection...");

        var positions = new[]
        {
            new Vector3( 0.0f, 0.7f,  1.5f),  // Table in front
            new Vector3( 1.2f, 0.0f,  0.5f),  // Floor right
            new Vector3(-0.5f, 0.7f,  2.0f),  // Desk left
            new Vector3( 0.0f, 0.0f, -0.5f),  // Floor behind
        };

        var types = new[]
        {
            AnchorType.Table,
            AnchorType.Floor,
            AnchorType.Desk,
            AnchorType.Wall
        };

        for (int i = 0; i < types.Length; i++)
            TryRegister(types[i], positions[i],
                        Quaternion.identity, types[i].ToString(), 0.9f);
    }

    public List<DetectedAnchor> GetAnchors() => new(_registry);
    public int AnchorCount => _registry.Count;

    // ── Per-room reset ─────────────────────────────────────────────────────

    // Called when the player moves on to investigate a new physical room —
    // clears anchors from the previous room and restarts detection so
    // OnRoomReady fires again for the new space.
    public void ResetForNewRoom()
    {
        StopAllCoroutines();
        _registry.Clear();
        _classifiedPlanes.Clear();
        _roomReady = false;
        StartCoroutine(DetectionTimeout());

        // ARCore keeps tracking surfaces it already found in the previous
        // room silently — clearing our own bookkeeping doesn't make it
        // re-fire planesChanged for a surface that hasn't itself changed.
        // Re-process everything already tracked so OnRoomReady can fire
        // again for the new room instead of waiting for events that may
        // never come.
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
                TryRegisterPlane(plane);
        }

        Debug.Log("[ARSceneManager] Reset for new room");
    }
}