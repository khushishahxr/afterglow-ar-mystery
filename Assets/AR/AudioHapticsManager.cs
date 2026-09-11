using System.Collections;
using UnityEngine;

// Short, procedurally-generated cue sounds (no external audio assets
// needed — same approach as SignalAudioManager's ambient tones) paired
// with device haptic feedback, for the key AR/decision moments: clue
// found, scan placement, room glitch sequence, final choice confirmed.
//
// Unlike SignalAudioManager's deliberately non-positional subliminal
// tones, these ARE genuinely spatial (AudioSource.spatialBlend = 1) —
// they play from the actual world position of the thing that triggered
// them, so the player can localise them, satisfying "spatial audio" as
// a real multimodal cue rather than just background ambience.
public class AudioHapticsManager : MonoBehaviour
{
    public static AudioHapticsManager Instance { get; private set; }

    const int SampleRate = 44100;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("AudioHapticsManager").AddComponent<AudioHapticsManager>();
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public cues ───────────────────────────────────────────────────────

    // A found/collected piece of evidence — bright ascending two-note chime
    // from the clue's own position, plus a short confirming buzz.
    public void PlayClueFoundCue(Vector3 worldPosition)
    {
        var clip = GenerateSweep(600f, 950f, 0.22f, 0.5f);
        PlaySpatialOneShot(clip, worldPosition, 0.6f);
        Vibrate(VibrationStrength.Short);
    }

    // A successful Scan placement — a single higher, quicker blip from
    // wherever the hit-test landed.
    public void PlayScanSuccessCue(Vector3 worldPosition)
    {
        var clip = GenerateSweep(800f, 800f, 0.12f, 0.4f);
        PlaySpatialOneShot(clip, worldPosition, 0.5f);
        Vibrate(VibrationStrength.Short);
    }

    // Scan pressed but no surface found / nothing left to place — soft,
    // duller, non-spatial (there's no world position to anchor it to).
    public void PlayScanMissCue()
    {
        var clip = GenerateSweep(260f, 220f, 0.15f, 0.3f);
        PlayFlatOneShot(clip, 0.35f);
    }

    // Room glitch sequence — short dissonant beat-frequency burst (two
    // close frequencies), non-spatial since it reads as a full-room event
    // rather than something localised, plus a longer vibration.
    public void PlayGlitchCue()
    {
        StartCoroutine(GlitchCueRoutine());
    }

    IEnumerator GlitchCueRoutine()
    {
        var clipA = GenerateSweep(210f, 190f, 0.4f, 0.35f);
        var clipB = GenerateSweep(216f, 196f, 0.4f, 0.3f);
        PlayFlatOneShot(clipA, 0.5f);
        PlayFlatOneShot(clipB, 0.5f);
        Vibrate(VibrationStrength.Long);
        yield return null;
    }

    // Final RESTORE/END choice confirmed — deep, resonant, longer tone,
    // matching the weight of the moment.
    public void PlayFinalChoiceCue()
    {
        var clip = GenerateSweep(140f, 90f, 0.9f, 0.55f);
        PlayFlatOneShot(clip, 0.6f);
        Vibrate(VibrationStrength.Long);
    }

    // ── Procedural tone generation ───────────────────────────────────────

    // Linear frequency sweep (set start==end for a plain tone) with a
    // short fade-in/out envelope so the clip never clicks at its edges.
    AudioClip GenerateSweep(float startFreq, float endFreq, float duration, float amplitude)
    {
        int samples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
        var data = new float[samples];
        float phase = 0f;
        int fadeSamples = Mathf.Min(samples / 4, SampleRate / 40); // ~25ms fade

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float freq = Mathf.Lerp(startFreq, endFreq, i / (float)Mathf.Max(1, samples - 1));
            phase += 2f * Mathf.PI * freq / SampleRate;

            float envelope = 1f;
            if (i < fadeSamples) envelope = i / (float)fadeSamples;
            else if (i > samples - fadeSamples) envelope = (samples - i) / (float)fadeSamples;

            data[i] = Mathf.Sin(phase) * amplitude * envelope;
        }

        var clip = AudioClip.Create($"cue_{startFreq}_{endFreq}", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ── Playback helpers ─────────────────────────────────────────────────

    void PlaySpatialOneShot(AudioClip clip, Vector3 position, float volume)
    {
        var go = new GameObject("SpatialCue");
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.clip         = clip;
        src.volume       = volume;
        src.spatialBlend = 1f; // genuinely positional
        src.rolloffMode  = AudioRolloffMode.Linear;
        src.maxDistance  = 6f;
        src.playOnAwake  = false;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    void PlayFlatOneShot(AudioClip clip, float volume)
    {
        var go = new GameObject("FlatCue");
        var src = go.AddComponent<AudioSource>();
        src.clip         = clip;
        src.volume       = volume;
        src.spatialBlend = 0f;
        src.playOnAwake  = false;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    // ── Haptics ───────────────────────────────────────────────────────────

    enum VibrationStrength { Short, Long }

    // Vanilla Unity only exposes a single fixed-pattern vibration on
    // Android/iOS (Handheld.Vibrate) — no duration/intensity control
    // without a native plugin. "Long" fires two pulses close together to
    // read as more substantial than "Short"'s single pulse.
    void Vibrate(VibrationStrength strength)
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
        if (strength == VibrationStrength.Long)
            StartCoroutine(SecondPulse());
#endif
    }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
    IEnumerator SecondPulse()
    {
        yield return new WaitForSeconds(0.18f);
        Handheld.Vibrate();
    }
#endif
}
