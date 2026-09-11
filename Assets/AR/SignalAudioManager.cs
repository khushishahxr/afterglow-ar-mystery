using System.Collections;
using UnityEngine;

// Procedurally generated low-frequency ambient tones that escalate room by
// room, staying quiet enough to sit below conscious attention while
// contributing to atmospheric unease. Self-bootstraps at scene load.
//
// Note for the researcher: this is a deliberately subliminal manipulation
// (see design brief, Block 10). Confirm it's covered by your ethics
// approval / informed consent materials before running real sessions.
public class SignalAudioManager : MonoBehaviour
{
    public static SignalAudioManager Instance { get; private set; }

    const int   SampleRate    = 44100;
    const int   ClipDurationS = 10;
    const float ProximityRange = 0.8f;
    const float ProximityVolume = 0.15f;

    AudioSource _toneA; // 40Hz — base signal, room 2+
    AudioSource _toneB; // 43Hz — secondary beat tone, room 5+

    int   _currentRoom;
    float _baseVolume;
    bool  _transitioning;
    Coroutine _transitionRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("SignalAudioManager").AddComponent<SignalAudioManager>();
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _toneA = BuildToneSource(40f);
        _toneB = BuildToneSource(43f);
    }

    AudioSource BuildToneSource(float frequency)
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.clip         = GenerateSineWave(frequency);
        src.loop         = true;
        src.volume       = 0f;
        src.spatialBlend = 0f; // ambient, not positional
        src.playOnAwake  = false;
        return src;
    }

    // Baked at a safe near-full amplitude — actual perceived loudness is
    // entirely controlled via AudioSource.volume so it can track the
    // room table (0.02-0.10) and the proximity boost live.
    AudioClip GenerateSineWave(float frequency)
    {
        int samples = SampleRate * ClipDurationS;
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / SampleRate) * 0.8f;

        var clip = AudioClip.Create($"signal_{frequency}Hz", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void Update()
    {
        if (_currentRoom <= 0 || _transitioning) return;

        float target = _baseVolume;
        var cam = Camera.main;
        if (cam != null)
        {
            var clues = FindObjectsByType<ClueObject>(FindObjectsSortMode.None);
            foreach (var clue in clues)
            {
                if (clue.IsRevealed) continue;
                if (Vector3.Distance(cam.transform.position, clue.transform.position) <= ProximityRange)
                {
                    target = ProximityVolume;
                    break;
                }
            }
        }

        _toneA.volume = Mathf.MoveTowards(_toneA.volume, target, Time.deltaTime * 0.3f);
        if (_toneB.isPlaying)
            _toneB.volume = Mathf.MoveTowards(_toneB.volume, target, Time.deltaTime * 0.3f);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    // Called by NavigationManager whenever the Teaser screen opens for a
    // chapter, so the ambient signal matches the room being entered.
    public void SetRoom(int roomNumber)
    {
        _currentRoom = roomNumber;
        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);

        switch (roomNumber)
        {
            case 1:
                _transitionRoutine = StartCoroutine(TransitionTo(0f, 0f, 1f));
                break;
            case 2:
                EnsurePlaying(_toneA);
                _toneB.Stop();
                _transitionRoutine = StartCoroutine(TransitionTo(0.02f, 0f, 30f));
                break;
            case 3:
                EnsurePlaying(_toneA);
                _toneB.Stop();
                _transitionRoutine = StartCoroutine(TransitionTo(0.04f, 0f, 2f));
                break;
            case 4:
                EnsurePlaying(_toneA);
                _toneB.Stop();
                _transitionRoutine = StartCoroutine(TransitionTo(0.06f, 0f, 2f));
                break;
            case 5:
                EnsurePlaying(_toneA);
                EnsurePlaying(_toneB);
                _transitionRoutine = StartCoroutine(TransitionTo(0.08f, 0.08f, 2f));
                break;
            case 6:
                EnsurePlaying(_toneA);
                EnsurePlaying(_toneB);
                _transitionRoutine = StartCoroutine(Room6EntrySpike());
                break;
            default:
                _transitionRoutine = StartCoroutine(TransitionTo(0f, 0f, 1f));
                break;
        }
    }

    // Researcher "reset for next participant" — silence immediately.
    public void Stop()
    {
        if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        _currentRoom = 0;
        _baseVolume  = 0f;
        _transitioning = false;
        _toneA.volume = 0f;
        _toneB.volume = 0f;
        _toneA.Stop();
        _toneB.Stop();
    }

    void EnsurePlaying(AudioSource src)
    {
        if (!src.isPlaying) src.Play();
    }

    IEnumerator TransitionTo(float toneAVolume, float toneBVolume, float duration)
    {
        _transitioning = true;
        float startA = _toneA.volume;
        float startB = _toneB.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float frac = Mathf.Clamp01(t / duration);
            _toneA.volume = Mathf.Lerp(startA, toneAVolume, frac);
            _toneB.volume = Mathf.Lerp(startB, toneBVolume, frac);
            yield return null;
        }

        _toneA.volume = toneAVolume;
        _toneB.volume = toneBVolume;
        _baseVolume   = Mathf.Max(toneAVolume, toneBVolume);

        if (toneAVolume <= 0f) _toneA.Stop();
        if (toneBVolume <= 0f) _toneB.Stop();

        _transitioning = false;
    }

    IEnumerator Room6EntrySpike()
    {
        _transitioning = true;
        _toneA.volume = 0.4f;
        _toneB.volume = 0.4f;
        yield return new WaitForSeconds(0.5f);
        _transitioning = false;
        yield return TransitionTo(0.10f, 0.10f, 1f);
    }
}
