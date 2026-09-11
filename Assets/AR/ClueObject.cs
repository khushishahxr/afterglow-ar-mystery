using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClueObject : MonoBehaviour
{
    [Header("Clue Data")]
    public string AnchorType;
    public string ClueText;
    public string EvidenceDetail;
    public string EvidenceName;
    public string EvidenceIcon;
    public bool   IsRedHerring;
    public string ReactionLine;

    [Header("Reveal Settings")]
    public float RevealDistance = 1.5f;

    [Header("References")]
    public GameObject GlowVFX;

    // Research-instrument data — captured at the moment the player actually
    // finds this clue (not at spawn time), used by StudyLogger's export.
    public Vector3 PlayerPositionAtReveal;
    public float   DistanceAtReveal;

    bool _revealed  = false;
    bool _collected = false;
    bool _idleRunning = false;
    public bool IsRevealed => _revealed;
    public bool IsCollected => _collected;

    // Called by ClueRevealUI when the player presses "COLLECT EVIDENCE" —
    // distinct from Reveal() (which only marks the clue as found). The
    // Move/Rotate/Scale toolbar gates on this, not on _revealed, so props
    // can't be dragged around before the player has actually collected them.
    public void MarkCollected() => _collected = true;

    public static event Action<ClueObject> OnClueRevealed;

    void Start()
    {
        if (GlowVFX != null)
            GlowVFX.SetActive(false);
        StartCoroutine(AnimateIn());
    }

    void Update()
    {
        // Missing entirely before (confirmed by diffing against the twin
        // project, which has this exact guard): the AR camera feed and
        // this object's Update() keep running underneath whatever UI
        // screen is on top — Case File, Journal, anywhere — since only the
        // 2D UI Toolkit overlay actually changes on screen switches, not
        // the 3D AR scene. Without this check, walking around with the
        // Case File open could silently proximity-reveal a clue, or a tap
        // that lands on the AR view behind a UI screen could collect one,
        // both invisibly, with no indication anything happened until the
        // player came back to Gameplay and found state had changed.
        if (NavigationManager.Instance == null ||
            NavigationManager.Instance.Current != GameScreen.Gameplay)
            return;

        // While another clue is selected in the manipulator toolbar, every
        // other piece of evidence ignores taps and proximity entirely —
        // otherwise the player could accidentally collect/select a
        // different clue mid-adjustment.
        if (ClueManipulator.Instance != null && ClueManipulator.Instance.IsLocked(this))
            return;

        var cam = Camera.main;
        if (cam == null) return;

        if (!_revealed)
        {
            float dist = Vector3.Distance(
                transform.position, cam.transform.position);
            if (dist <= RevealDistance)
            {
                Reveal();
                return;
            }

            CheckTapInput(cam);
        }
        else if (_collected)
        {
            CheckSelectTapInput(cam);
        }
    }

    // Proximity alone doesn't match how players instinctively try to
    // interact with something they see in AR — they tap it. This gives a
    // direct, reliable path to reveal a clue independent of the walk-up
    // distance check above.
    void CheckTapInput(Camera cam)
    {
        var pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        Vector2 screenPos = pointer.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 15f) &&
            hit.transform.IsChildOf(transform))
        {
            Reveal();
        }
    }

    // Tapping an already-found clue again opens the Move/Rotate/Scale
    // toolbar so the player can fine-tune where it sits.
    void CheckSelectTapInput(Camera cam)
    {
        var pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        Vector2 screenPos = pointer.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 15f) &&
            hit.transform.IsChildOf(transform))
        {
            ClueManipulator.Instance?.Select(this);
        }
    }

    void Reveal()
    {
        if (_revealed) return;
        _revealed = true;

        var cam = Camera.main;
        PlayerPositionAtReveal = cam != null ? cam.transform.position : Vector3.zero;
        DistanceAtReveal = Vector3.Distance(PlayerPositionAtReveal, transform.position);

        if (GlowVFX != null)
            GlowVFX.SetActive(true);

        StartCoroutine(PulseScale());
        ClueRevealUI.Instance?.ShowClue(this);
        AudioHapticsManager.Instance?.PlayClueFoundCue(transform.position);

        string detail = $"{EvidenceDetail} Found {DistanceAtReveal:F1}m from where you were standing.";

        EvidenceManager.Instance?.AddEvidence(new EvidenceItem
        {
            Name           = EvidenceName,
            Icon           = EvidenceIcon,
            ClueText       = ClueText,
            EvidenceDetail = detail,
            AnchorType     = AnchorType,
            IsRedHerring   = IsRedHerring,
            ReactionLine   = ReactionLine
        });

        OnClueRevealed?.Invoke(this);

        Debug.Log($"[ClueObject] Revealed: {EvidenceName} " +
                  $"({DistanceAtReveal:F1}m away)");
    }

    public void PulseHighlight()
    {
        if (_revealed) return;
        StartCoroutine(ScanPulse());
    }

    // ── Animations ─────────────────────────────────────────────────────────

    IEnumerator AnimateIn()
    {
        float duration = 1.2f;
        float elapsed  = 0f;

        // Was hardcoded to Vector3.one as both the animation's lerp target
        // and its final value — silently wiping out whatever
        // ClueSpawner.ScaleMultiplier had already set on this object
        // before this coroutine even started, since the wipe happens
        // regardless of what the object's actual intended scale is.
        // Capturing it here instead means any per-prop scale correction
        // survives the pop-in animation intact.
        Vector3 targetScale = transform.localScale;

        Vector3 startPos = transform.position - Vector3.up * 0.05f;
        Vector3 endPos   = transform.position;
        transform.position   = startPos;
        transform.localScale = Vector3.zero;

        // Try to fade material in
        var renderer = GetComponentInChildren<Renderer>();
        Material mat       = null;
        Color    origColor = Color.white;

        if (renderer != null)
        {
            mat       = renderer.material;
            origColor = mat.color;
            mat.color = new Color(
                origColor.r, origColor.g,
                origColor.b, 0f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t      = elapsed / duration;
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            transform.position   =
                Vector3.Lerp(startPos, endPos, smooth);
            transform.localScale =
                targetScale * smooth;

            if (mat != null)
                mat.color = new Color(
                    origColor.r, origColor.g,
                    origColor.b,
                    Mathf.Lerp(0f, 1f, smooth));

            yield return null;
        }

        transform.position   = endPos;
        transform.localScale = targetScale;
        if (mat != null) mat.color = origColor;

        // Start idle after arriving
        if (!_idleRunning)
            StartCoroutine(IdleAnimation());
    }

    IEnumerator IdleAnimation()
    {
        _idleRunning = true;
        Vector3 basePos = transform.localPosition;

        while (!_revealed)
        {
            float offset = Mathf.Sin(Time.time * 1.5f) * 0.002f;
            transform.localPosition = new Vector3(
                basePos.x, basePos.y + offset, basePos.z);
            yield return null;
        }

        transform.localPosition = basePos;
        _idleRunning = false;
    }

    IEnumerator ScanPulse()
    {
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer == null) yield break;

        Color orig     = renderer.material.color;
        Color scanBlue = new Color(0.4f, 0.7f, 1.0f, 1.0f);

        for (int i = 0; i < 2; i++)
        {
            renderer.material.color = scanBlue;
            yield return new WaitForSeconds(0.12f);
            renderer.material.color = orig;
            yield return new WaitForSeconds(0.12f);
        }
    }

    IEnumerator PulseScale()
    {
        // Same fix as AnimateIn() above — was hardcoded to Vector3.one,
        // which would snap a scale-corrected prop back to 1x the instant
        // this "found" pulse played.
        Vector3 original = transform.localScale;
        Vector3 big      = original * 1.25f;
        float   duration = 0.15f;
        float   elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale =
                Vector3.Lerp(original, big, elapsed / duration);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale =
                Vector3.Lerp(big, original, elapsed / duration);
            yield return null;
        }

        transform.localScale = original;
    }
}