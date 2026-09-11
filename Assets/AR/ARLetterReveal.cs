using System;
using System.Collections;
using TMPro;
using UnityEngine;

// Room 1 opener: a typewritten message on the first real wall the player's
// device detects. Fires once per session. ClueSpawner waits for
// OnLetterRevealComplete before spawning Room 1 clues so the letter isn't
// competing for attention with evidence objects.
public class ARLetterReveal : MonoBehaviour
{
    public static event Action OnLetterRevealComplete;

    static bool _hasFiredThisSession;

    const string LetterText =
        "When the silence comes,\ndon't let it find you listening.";
    const float CharDelay  = 0.05f;
    const float HoldTime   = 4f;
    const float FadeTime   = 1.5f;

    static readonly Color ColdBlue = new Color(0.4f, 0.7f, 1.0f, 1f);

    TextMeshPro _tmp;

    public static void TryTrigger(Vector3 wallPosition, Quaternion wallRotation)
    {
        if (_hasFiredThisSession) return;
        _hasFiredThisSession = true;

        // AR Foundation vertical planes report their outward normal along
        // local up — offset toward the room, not into the wall.
        Vector3 outward = wallRotation * Vector3.up;
        if (outward.sqrMagnitude < 0.0001f) outward = Vector3.forward;

        var go = new GameObject("ARLetterReveal");
        go.transform.position = wallPosition + outward.normalized * 0.05f;
        go.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);

        go.AddComponent<ARLetterReveal>().Begin();
    }

    // Used by the researcher "reset for next participant" flow so the
    // letter can fire again for the next session.
    public static void ResetSession() => _hasFiredThisSession = false;

    void Begin()
    {
        _tmp = gameObject.AddComponent<TextMeshPro>();
        _tmp.text          = "";
        _tmp.fontSize      = 0.09f;
        _tmp.color         = ColdBlue;
        _tmp.alignment     = TextAlignmentOptions.Center;
        _tmp.enableWordWrapping = false;

        StartCoroutine(RevealSequence());
    }

    IEnumerator RevealSequence()
    {
        foreach (char c in LetterText)
        {
            _tmp.text += c;
            yield return new WaitForSeconds(CharDelay);
        }

        yield return new WaitForSeconds(HoldTime);

        float t = 0f;
        while (t < FadeTime)
        {
            t += Time.deltaTime;
            _tmp.color = new Color(ColdBlue.r, ColdBlue.g, ColdBlue.b,
                Mathf.Lerp(1f, 0f, t / FadeTime));
            yield return null;
        }

        OnLetterRevealComplete?.Invoke();
        Destroy(gameObject);
    }
}
