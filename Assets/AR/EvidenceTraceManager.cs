using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Spawns a cold-blue wireframe box outline at every revealed clue, so the
// room visibly fills with a "mapped crime scene" grid as evidence is
// found. Self-bootstraps at scene load.
public class EvidenceTraceManager : MonoBehaviour
{
    public static EvidenceTraceManager Instance { get; private set; }

    static readonly Color ColdBlue = new Color(0.4f, 0.7f, 1.0f);

    readonly List<GameObject> _traces = new();
    Material _outlineMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("EvidenceTraceManager")
            .AddComponent<EvidenceTraceManager>();
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ClueObject.OnClueRevealed += OnClueRevealed;
    }

    void OnDestroy()
    {
        ClueObject.OnClueRevealed -= OnClueRevealed;
    }

    void OnClueRevealed(ClueObject clue)
    {
        SpawnTrace(clue);
    }

    // Disabled per request (2026-08-21) — the wireframe box outline around
    // every revealed prop was reading as visual clutter in the AR view,
    // especially stacked with several props found close together. Left
    // the rest of the trace system (FlashAllTraces, used by Room 6's
    // glitch sequence) intact — it just has nothing to flash now, which
    // degrades silently rather than breaking anything.
    void SpawnTrace(ClueObject clue)
    {
        return;
#pragma warning disable CS0162 // deliberately unreachable — kept for a quick revert
        float halfSize = 0.08f;
        var renderer = clue.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            var ext = renderer.bounds.extents;
            halfSize = Mathf.Max(ext.x, ext.y, ext.z) * 1.1f;
        }

        var outline = CreateBoxOutline(clue.transform, halfSize);
        if (outline == null) return; // no usable shader this build — see GetOutlineMaterial
        outline.name = $"Trace_{clue.EvidenceName}";
        _traces.Add(outline);
#pragma warning restore CS0162
    }

    // Parented to the clue's own transform (local space, not world space)
    // so the outline tracks the clue automatically if it's later moved —
    // no per-frame position syncing needed.
    GameObject CreateBoxOutline(Transform clueTransform, float halfSize)
    {
        var mat = GetOutlineMaterial();
        if (mat == null) return null; // logged once in GetOutlineMaterial

        var root = new GameObject("EvidenceOutline");
        root.transform.SetParent(clueTransform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        var corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            corners[i] = new Vector3(
                ((i & 1) == 0 ? -1f : 1f) * halfSize,
                ((i & 2) == 0 ? -1f : 1f) * halfSize,
                ((i & 4) == 0 ? -1f : 1f) * halfSize);
        }

        // Corners differing in exactly one bit are connected by an edge —
        // standard technique for enumerating all 12 edges of a cube.
        int[,] edges =
        {
            {0,1},{2,3},{4,5},{6,7},
            {0,2},{1,3},{4,6},{5,7},
            {0,4},{1,5},{2,6},{3,7}
        };

        for (int e = 0; e < 12; e++)
        {
            var edgeGO = new GameObject($"Edge_{e}");
            edgeGO.transform.SetParent(root.transform, false);

            var lr = edgeGO.AddComponent<LineRenderer>();
            lr.material      = mat;
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.SetPosition(0, corners[edges[e, 0]]);
            lr.SetPosition(1, corners[edges[e, 1]]);
            lr.startWidth = lr.endWidth = 0.004f;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
        }

        return root;
    }

    bool _shaderMissingLogged;

    Material GetOutlineMaterial()
    {
        if (_outlineMaterial != null) return _outlineMaterial;

        // Both of these were confirmed live (2026-08-17) to come back null
        // on-device — stripped from the Android build since nothing else
        // in the project references them via a Material asset, so Unity's
        // shader stripping drops them despite the literal Shader.Find
        // calls. Falling back to whatever shader any already-rendering
        // clue prop is using — that's guaranteed present in this exact
        // build since it's visibly rendering on screen.
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");

        if (shader == null)
        {
            var anyRenderer = FindFirstObjectByType<Renderer>();
            shader = anyRenderer != null ? anyRenderer.sharedMaterial?.shader : null;
        }

        if (shader == null)
        {
            if (!_shaderMissingLogged)
            {
                Debug.LogWarning("[EvidenceTraceManager] No usable shader found for the outline material in this build — evidence trace outlines disabled for this session.");
                _shaderMissingLogged = true;
            }
            return null;
        }

        _outlineMaterial = new Material(shader) { color = ColdBlue * 1.5f };
        return _outlineMaterial;
    }

    // ── Public API ─────────────────────────────────────────────────────────

    // Called when the Teaser screen opens for the next chapter — the
    // previous room's forensic grid shouldn't carry over.
    public void ClearTraces()
    {
        foreach (var trace in _traces)
            if (trace != null) Destroy(trace);
        _traces.Clear();
    }

    // Briefly boosts every outline to full brightness then fades back —
    // used by the room glitch sequence.
    public void FlashAllTraces()
    {
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        var lineRenderers = new List<LineRenderer>();
        foreach (var trace in _traces)
        {
            if (trace == null) continue;
            lineRenderers.AddRange(trace.GetComponentsInChildren<LineRenderer>());
        }
        if (lineRenderers.Count == 0) yield break;

        Color bright = ColdBlue * 2.2f;
        Color normal = ColdBlue * 1.5f;

        foreach (var lr in lineRenderers) lr.material.color = bright;
        yield return new WaitForSeconds(0.3f);
        foreach (var lr in lineRenderers)
            if (lr != null) lr.material.color = normal;
    }
}
