using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

// Fires a tiny, cheap Gemini call per distinctive surface the player's
// device detects, and shows the atmospheric flavour text floating above
// that surface. Purely decorative — failures are swallowed rather than
// shown to the player. Self-bootstraps at scene load.
public class AnchorNarrator : MonoBehaviour
{
    public static AnchorNarrator Instance { get; private set; }

    const string Model   = "gemini-2.0-flash-lite";
    const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

    static readonly Color ColdBlue = new Color(0.4f, 0.7f, 1.0f, 0f);

    readonly Dictionary<string, bool> _narrated = new(); // keyed by AnchorType, per room

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("AnchorNarrator").AddComponent<AnchorNarrator>();
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ARSceneManager.OnAnchorNarrationNeeded += OnAnchorNarrationNeeded;
    }

    void OnDestroy()
    {
        ARSceneManager.OnAnchorNarrationNeeded -= OnAnchorNarrationNeeded;
    }

    // Called on room change and researcher "reset for next participant".
    public void ResetForNewRoom() => _narrated.Clear();

    void OnAnchorNarrationNeeded(AnchorType anchorType, string roomType)
    {
        string key = anchorType.ToString();
        if (_narrated.TryGetValue(key, out bool done) && done) return;
        _narrated[key] = true;

        Vector3 position = Vector3.zero;
        bool found = false;
        var anchors = ARSceneManager.Instance?.GetAnchors();
        if (anchors != null)
        {
            foreach (var a in anchors)
                if (a.Type == anchorType) { position = a.WorldPosition; found = true; }
        }
        if (!found) return;

        int roomNumber = WorldStateManager.Instance?.CurrentRoomNumber ?? 1;
        StartCoroutine(NarrateAnchor(anchorType, roomType, roomNumber, position));
    }

    // Purely atmospheric — on any failure this silently falls back to a
    // static description rather than showing an error or nothing at all.
    // The player never sees a failure state, just slightly less
    // personalised text.
    IEnumerator NarrateAnchor(AnchorType anchorType, string roomType, int roomNumber, Vector3 position)
    {
        string text;

#if UNITY_EDITOR
        yield return new WaitForSeconds(0.3f);
        text = $"The {anchorType.ToString().ToLower()} holds its own quiet story — one nobody was left to tell.";
#else
        string themeDesc = WorldStateManager.Instance?.State?.ThemeDescription ?? "unknown";
        string prompt =
            "You are narrating an AR mystery game.\n" +
            $"Room type: {roomType}\n" +
            $"Object detected: {anchorType}\n" +
            $"Apocalypse theme: {themeDesc}\n" +
            "In exactly 2 sentences, write atmospheric flavour text describing " +
            "this surface as if it holds a story. Cold, intimate tone. " +
            "No more than 30 words total. Return plain text only — no JSON.";

        string apiKey = NarrativeGenerator.Instance?.ApiKey ?? "";
        string url  = $"{BaseUrl}{Model}:generateContent?key={apiKey}";
        string body = BuildGeminiBody(prompt);

        using var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
        request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 8;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            StudyLogger.Instance?.LogAPIError(Model, request.error ?? "unknown error", roomNumber);
            text = StaticFallbackNarrative.GetAnchorDescription(anchorType.ToString(), roomNumber);
        }
        else
        {
            text = ParseText(request.downloadHandler.text);
            if (string.IsNullOrEmpty(text))
            {
                StudyLogger.Instance?.LogAPIError(Model, "parse failed", roomNumber);
                text = StaticFallbackNarrative.GetAnchorDescription(anchorType.ToString(), roomNumber);
            }
        }
#endif

        ShowNarration(text, position);
    }

    string BuildGeminiBody(string prompt)
    {
        string escaped = prompt
            .Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("\n", "\\n").Replace("\r", "\\r");

        return $@"{{
  ""contents"": [
    {{ ""parts"": [ {{ ""text"": ""{escaped}"" }} ] }}
  ],
  ""generationConfig"": {{
    ""temperature"": 0.9,
    ""maxOutputTokens"": 60
  }}
}}";
    }

    string ParseText(string rawJson)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<GeminiResponse>(rawJson);
            if (wrapper?.candidates == null || wrapper.candidates.Length == 0) return null;

            var parts = wrapper.candidates[0]?.content?.parts;
            if (parts == null || parts.Length == 0) return null;

            return parts[0]?.text?.Trim();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AnchorNarrator] Parse failed: {e.Message}");
            return null;
        }
    }

    void ShowNarration(string text, Vector3 anchorPosition)
    {
        var go = new GameObject("AnchorNarration");
        go.transform.position = anchorPosition + Vector3.up * 0.25f;

        var cam = Camera.main;
        if (cam != null)
            go.transform.rotation = Quaternion.LookRotation(
                go.transform.position - cam.transform.position);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text                = text;
        tmp.fontSize             = 0.07f;
        tmp.color                = ColdBlue;
        tmp.alignment            = TextAlignmentOptions.Center;
        tmp.enableWordWrapping   = true;
        tmp.rectTransform.sizeDelta = new Vector2(1.2f, 0.4f);

        StartCoroutine(FadeSequence(tmp, go));
    }

    IEnumerator FadeSequence(TextMeshPro tmp, GameObject go)
    {
        yield return Fade(tmp, 0f, 1f, 0.5f);
        yield return new WaitForSeconds(5f);
        yield return Fade(tmp, 1f, 0f, 1f);
        Destroy(go);
    }

    IEnumerator Fade(TextMeshPro tmp, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            tmp.color = new Color(ColdBlue.r, ColdBlue.g, ColdBlue.b,
                Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        tmp.color = new Color(ColdBlue.r, ColdBlue.g, ColdBlue.b, to);
    }

    [Serializable] class GeminiResponse { public Candidate[] candidates; }
    [Serializable] class Candidate      { public Content content;        }
    [Serializable] class Content        { public Part[] parts;           }
    [Serializable] class Part           { public string text;            }
}
