using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Brief, unobtrusive confirmation shown right after a theory is submitted
// and before TheoryResultScreen appears — confirms the choice was captured
// without interrupting the cinematic flow.
public class AcknowledgementOverlay
{
    readonly VisualElement _overlay;

    public AcknowledgementOverlay(VisualElement root)
    {
        _overlay = new VisualElement();
        _overlay.style.position        = Position.Absolute;
        _overlay.style.top             = 0;
        _overlay.style.left            = 0;
        _overlay.style.right           = 0;
        _overlay.style.bottom          = 0;
        _overlay.style.backgroundColor =
            new Color(0.031f, 0.043f, 0.063f, 0.7f);
        _overlay.style.alignItems      = Align.Center;
        _overlay.style.justifyContent  = Justify.Center;
        _overlay.style.opacity         = 0;
        _overlay.style.display         = DisplayStyle.None;

        var label = new Label { text = "Your theory has been recorded." };
        label.style.fontSize       = 14;
        label.style.color          = new Color(0.4f, 0.7f, 1.0f, 0.9f);
        label.style.letterSpacing  = 3;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.whiteSpace     = WhiteSpace.Normal;

        _overlay.Add(label);
        root.Add(_overlay);
    }

    public IEnumerator Play()
    {
        _overlay.style.display = DisplayStyle.Flex;

        yield return Fade(0f, 1f, 0.3f);
        yield return new WaitForSeconds(1.5f);
        yield return Fade(1f, 0f, 0.3f);

        _overlay.style.display = DisplayStyle.None;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        _overlay.style.opacity = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _overlay.style.opacity = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _overlay.style.opacity = to;
    }
}
