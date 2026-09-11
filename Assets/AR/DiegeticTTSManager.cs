using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Live TTS for the diegetic hint system — a genuinely new integration,
// not a port. The Fixed Narrative condition pre-generates its hint audio
// offline (its narrative never changes), so it just plays a matching
// file. This condition's narrative is different every session, so the
// hint line has to be voiced live at runtime instead — ElevenLabs,
// WAV response (reliable to decode on Android; MP3 support through
// UnityWebRequestMultimedia is inconsistent across devices).
//
// API key comes from Assets/Resources/ElevenLabsConfig.txt (same pattern
// as GroqConfig.txt) — same file, empty by default, safe to ship without
// a key: every call gracefully no-ops to a null clip and the caller
// falls back to text-only, exactly like the twin's missing-audio-file
// fallback.
public class DiegeticTTSManager : MonoBehaviour
{
    public static DiegeticTTSManager Instance { get; private set; }

    [SerializeField] string apiKey = "";
    [SerializeField] string voiceId = ""; // ElevenLabs voice ID — set per character/theme if desired

    const string DefaultVoiceId = "21m00Tcm4TlvDq8ikWAM"; // ElevenLabs "Rachel" — stable public default
    const string BaseUrl = "https://api.elevenlabs.io/v1/text-to-speech/";

    public bool IsConfigured => !string.IsNullOrEmpty(apiKey);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrEmpty(apiKey))
        {
            var config = Resources.Load<TextAsset>("ElevenLabsConfig");
            if (config != null) apiKey = config.text.Trim();
            // No error logged — TTS is an enhancement layer, not required.
            // Its absence just means hints stay text-only.
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("DiegeticTTSManager").AddComponent<DiegeticTTSManager>();
    }

    // Requests a spoken clip for the given line. Invokes onComplete with
    // the AudioClip on success, or null on any failure/missing config —
    // callers must treat null as "fall back to text-only", never as an
    // error to surface to the player.
    public void RequestSpeech(string text, Action<AudioClip> onComplete)
    {
        if (!IsConfigured || string.IsNullOrEmpty(text))
        {
            onComplete?.Invoke(null);
            return;
        }

        StartCoroutine(RequestSpeechRoutine(text, onComplete));
    }

    IEnumerator RequestSpeechRoutine(string text, Action<AudioClip> onComplete)
    {
        string voice = string.IsNullOrEmpty(voiceId) ? DefaultVoiceId : voiceId;
        // VERIFY WHEN TESTING WITH A REAL KEY: this output_format value is
        // my best recollection of ElevenLabs' current API, not something
        // I've been able to test live. If audio fails to decode once a
        // real key is in place, check ElevenLabs' current TTS docs for the
        // exact query param/value that returns a WAV-decodable response —
        // that's the single most likely thing to have drifted.
        string url = $"{BaseUrl}{voice}?output_format=pcm_16000";

        string body = "{\"text\":\"" + EscapeJson(text) +
            "\",\"model_id\":\"eleven_turbo_v2_5\"," +
            "\"voice_settings\":{\"stability\":0.5,\"similarity_boost\":0.75}}";

        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("xi-api-key", apiKey);
        request.timeout = 10;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[DiegeticTTS] Request failed — falling back to text-only. " +
                $"error: {request.error}, code: {request.responseCode}");
            onComplete?.Invoke(null);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        onComplete?.Invoke(clip);
    }

    static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
}
