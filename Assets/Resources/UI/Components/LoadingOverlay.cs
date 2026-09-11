using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Atmospheric full-screen loading state shown while a room narrative is
// being generated on-device. No spinner — just a pulsing title and
// cycling subtitle text.
public class LoadingOverlay
{
    static readonly string[] Subtitles =
    {
        "READING THE ROOM...",
        "RECONSTRUCTING WHAT HAPPENED...",
        "THE SIGNAL LEAVES TRACES...",
        "PIECING IT TOGETHER..."
    };

    readonly VisualElement _overlay;
    readonly Label _title;
    readonly Label _subtitle;

    Coroutine _pulseRoutine;
    Coroutine _subtitleRoutine;

    public LoadingOverlay(VisualElement root)
    {
        _overlay = new VisualElement();
        _overlay.style.position        = Position.Absolute;
        _overlay.style.top = 0; _overlay.style.left = 0;
        _overlay.style.right = 0; _overlay.style.bottom = 0;
        _overlay.style.backgroundColor = UIHelper.BG;
        _overlay.style.alignItems      = Align.Center;
        _overlay.style.justifyContent  = Justify.Center;
        _overlay.style.display         = DisplayStyle.None;
        root.Add(_overlay);

        var block = new VisualElement();
        block.style.alignItems = Align.Center;

        _title = new Label { text = "AFTERGLOW" };
        _title.style.fontSize                = 32;
        _title.style.color                   = new Color(0.4f, 0.7f, 1.0f, 1f);
        _title.style.unityFontStyleAndWeight = FontStyle.Bold;
        _title.style.letterSpacing           = 8;
        _title.style.marginBottom            = 20;

        _subtitle = new Label { text = Subtitles[0] };
        _subtitle.style.fontSize       = 11;
        _subtitle.style.color          = UIHelper.TextMuted;
        _subtitle.style.letterSpacing  = 3;
        _subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;

        block.Add(_title);
        block.Add(_subtitle);
        _overlay.Add(block);
    }

    public void Show()
    {
        if (_overlay.style.display == DisplayStyle.Flex) return;
        _overlay.style.display = DisplayStyle.Flex;
        _pulseRoutine    = NavigationManager.Instance.StartCoroutine(PulseTitle());
        _subtitleRoutine = NavigationManager.Instance.StartCoroutine(CycleSubtitle());
    }

    public void Hide()
    {
        _overlay.style.display = DisplayStyle.None;
        if (_pulseRoutine != null)    NavigationManager.Instance.StopCoroutine(_pulseRoutine);
        if (_subtitleRoutine != null) NavigationManager.Instance.StopCoroutine(_subtitleRoutine);
        _title.style.opacity = 1f;
    }

    IEnumerator PulseTitle()
    {
        while (true)
        {
            yield return Fade(0f, 1f, 0.75f);
            yield return Fade(1f, 0f, 0.75f);
        }
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _title.style.opacity = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        _title.style.opacity = to;
    }

    IEnumerator CycleSubtitle()
    {
        int index = 0;
        while (true)
        {
            _subtitle.text = Subtitles[index % Subtitles.Length];
            index++;
            yield return new WaitForSeconds(2f);
        }
    }
}
