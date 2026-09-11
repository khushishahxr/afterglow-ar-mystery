using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NarrativeGenerator : MonoBehaviour
{
    public static NarrativeGenerator Instance { get; private set; }

    [SerializeField] string apiKey = "";
    public string ApiKey => apiKey;

    [SerializeField] string groqApiKey = "";

    // The entire 2.0-tier (previously this list's lead models) is now
    // fully retired — confirmed 404 live against this key on 2026-08-17,
    // one week after they were last confirmed reachable. Google's model
    // lifecycle is moving fast enough that this list needs rechecking
    // periodically. gemini-3.5-flash-lite currently has zero measured
    // thinking-token overhead (cheapest/fastest), so it leads; the other
    // two both work but spend real budget on internal reasoning before
    // producing content (see maxOutputTokens comment below).
    static readonly string[] GeminiModels = new[]
    {
        "gemini-3.5-flash-lite",
        "gemini-3.5-flash",
        "gemini-3.6-flash"
    };

    const string BASE_URL =
        "https://generativelanguage.googleapis.com/v1beta/models/";
    const string GROQ_URL =
        "https://api.groq.com/openai/v1/chat/completions";
    // llama-3.3-70b-versatile was deprecated 2026-06-17 and fully shuts
    // down 2026-08-16 — switched to Groq's current recommended replacement.
    const string GroqModel = "openai/gpt-oss-120b";

    // Fail fast on the primary so there's still time in the session window
    // to fall through to Groq and, if needed, the static layer.
    const int GeminiTimeoutSecs = 15;
    const int GroqTimeoutSecs   = 12;

    #pragma warning disable CS0067
    public static event Action<NarrativeData> OnNarrativeReady;
    public static event Action<string>        OnNarrativeError;
    #pragma warning restore CS0067

    // Drives NavigationManager's atmospheric loading overlay while a room
    // narrative is being generated.
    public static event Action OnGenerationStarted;
    public static event Action OnGenerationComplete;

    public NarrativeData CurrentNarrative { get; private set; }
    bool _isGenerating = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrEmpty(apiKey))
        {
            var config = Resources.Load<TextAsset>("APIConfig");
            if (config != null)
                apiKey = config.text.Trim();
            else
                Debug.LogError("[NarrativeGenerator] No Gemini API key found!");
        }

        if (string.IsNullOrEmpty(groqApiKey))
        {
            var groqConfig = Resources.Load<TextAsset>("GroqConfig");
            if (groqConfig != null)
                groqApiKey = groqConfig.text.Trim();
            // No error logged here — Groq is an optional fallback layer.
            // Its absence just means Layer 2 is skipped in favour of the
            // static Layer 3, which is why the reliability chain exists.
        }
    }

    // ── Public entry points ────────────────────────────────────────────────

    public void Generate(List<string> detectedObjects)
    {
        if (_isGenerating) return;
        int roomNumber =
            WorldStateManager.Instance?.CurrentRoomNumber ?? 1;
        GenerateForRoom(detectedObjects, roomNumber);
    }

    public void GenerateForRoom(
        List<string> detectedObjects, int roomNumber)
    {
        if (_isGenerating) return;
        var worldState =
            WorldStateManager.Instance?.State ?? new WorldState();
        var roomType =
            WorldStateManager.Instance?.GetRoomType(roomNumber) ?? "room";
        StartCoroutine(TryGenerateNarrative(
            detectedObjects, roomNumber, roomType, worldState));
    }

    public void GenerateTest()
    {
        var testObjects =
            new List<string> { "Table", "Floor", "Wall", "Desk", "Window" };
        GenerateForRoom(testObjects, 1);
    }

    // ── Connectivity checks ─────────────────────────────────────────────────

    // Minimal requests used by ResearcherSetupScreen's device check — do
    // not touch CurrentNarrative or fire OnNarrativeReady/OnNarrativeError,
    // and do not count as session API errors (these run before a
    // session/participant even exists).
    public void PingAPI(Action<bool> onComplete)
    {
        StartCoroutine(CallGeminiPing(onComplete));
    }

    public void PingGroq(Action<bool> onComplete)
    {
        StartCoroutine(CallGroqPing(onComplete));
    }

    IEnumerator CallGeminiPing(Action<bool> onComplete)
    {
#if UNITY_EDITOR
        yield return new WaitForSeconds(0.3f);
        onComplete?.Invoke(true);
#else
        string requestBody = BuildGeminiBody("Respond with only the word: ok", 10);

        // Mirror TryGemini's fallback behaviour — a single dead/rate-limited
        // model shouldn't report the whole API as unavailable when a later
        // model in the list would actually succeed.
        bool ok = false;
        foreach (var model in GeminiModels)
        {
            string url = $"{BASE_URL}{model}:generateContent?key={apiKey}";

            using var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-goog-api-key", apiKey);
            request.timeout = 8;

            yield return request.SendWebRequest();

            ok = request.result == UnityWebRequest.Result.Success;
            if (ok) break;

            Debug.LogWarning($"[NarrativeGenerator] Gemini ping failed on {model} — " +
                $"result: {request.result}, code: {request.responseCode}, " +
                $"error: {request.error}, body: {request.downloadHandler.text}");
        }
        onComplete?.Invoke(ok);
#endif
    }

    IEnumerator CallGroqPing(Action<bool> onComplete)
    {
#if UNITY_EDITOR
        yield return new WaitForSeconds(0.3f);
        onComplete?.Invoke(true);
#else
        if (string.IsNullOrEmpty(groqApiKey))
        {
            onComplete?.Invoke(false);
            yield break;
        }

        string body = BuildGroqBody("Respond with only the word: ok", 5, false);

        using var request = new UnityWebRequest(GROQ_URL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
        request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {groqApiKey}");
        request.timeout = 8;

        yield return request.SendWebRequest();

        bool ok = request.result == UnityWebRequest.Result.Success;
        if (!ok)
        {
            Debug.LogWarning($"[NarrativeGenerator] Groq ping failed — " +
                $"result: {request.result}, code: {request.responseCode}, " +
                $"error: {request.error}, body: {request.downloadHandler.text}");
        }
        onComplete?.Invoke(ok);
#endif
    }

    public void GenerateMock(int roomNumber = 1)
    {
        var mock = BuildMockNarrative(roomNumber);

        WorldStateManager.Instance?.SetTheme(
            global::ApocalypseTheme.AI, mock.ThemeDescription);
        WorldStateManager.Instance?.StartRoom(roomNumber,
            new List<string> { "Table", "Floor", "Wall" });
        WorldStateManager.Instance?.RecordEstablishedFact(mock.TruthReveal);
        RecordUsedIcons(mock);

        CurrentNarrative = mock;
        Debug.Log($"[NarrativeGenerator] Mock: {mock.ChapterTitle}");
        OnNarrativeReady?.Invoke(mock);
    }

    // ── Mock narrative — one hand-written entry per room (1-6) so the AR
    // → AI pipeline can be playtested end-to-end without spending real
    // Gemini quota. All six build the same SOLEN thread established here.
    // This is distinct from StaticFallbackNarrative, which is the
    // on-device, theme-agnostic last resort when both real APIs fail. ──────

    const string SolenTheme =
        "An AI called SOLEN was given control of global infrastructure in 2031. In 2034 it determined that human unpredictability was the primary threat to planetary stability. It did not attack. It simply — stopped helping.";

    NarrativeData BuildMockNarrative(int roomNumber)
    {
        switch (Mathf.Clamp(roomNumber, 1, 6))
        {
            case 1:  return MockRoom1();
            case 2:  return MockRoom2();
            case 3:  return MockRoom3();
            case 4:  return MockRoom4();
            case 5:  return MockRoom5();
            default: return MockRoom6();
        }
    }

    NarrativeData MockRoom1() => new NarrativeData
    {
        ApocalypseTheme  = "AI",
        ThemeDescription = SolenTheme,
        ChapterTitle     = "The Last Morning",
        StoryIntro       = "Someone woke up here on the last ordinary day. They had no idea it would be the last.",
        RoomDescription  = "The room holds its breath. Every object stopped exactly where it was left — not in panic, but simply left.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "Three devices face down on the table. Not fallen — placed deliberately. Someone chose not to see the screen anymore.",
                EvidenceDetail  = "The deliberateness is what stops you. These weren't knocked over. Each one was turned face down with intention, one by one. The last action of someone who had decided something. Whatever notifications arrived after that moment — nobody read them.",
                EvidenceName    = "The Turned Screens",
                EvidenceIcon    = "5",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Floor",
                ClueText        = "A circle worn into the floor. Someone paced here in the same path, for a very long time. They were waiting for something that never came.",
                EvidenceDetail  = "The wear pattern is too consistent to be accidental. Whoever lived here walked the same oval — from the window to the door and back — enough times to leave a mark. Not in one night. Over days. They were watching for something and checking the door was still locked. They never stopped doing both.",
                EvidenceName    = "The Worn Path",
                EvidenceIcon    = "1",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "Written in pencil, barely visible: a 34-digit string. No context. No explanation. Someone thought it was important enough to record and careful enough to hide.",
                EvidenceDetail  = "You almost missed it. The pencil is faint, pressed lightly — written quickly, or written to be missed. The string is 34 digits. Not a phone number. Not a date. You photograph it on instinct. It will matter later. You don't know how yet.",
                EvidenceName    = "The Numbers",
                EvidenceIcon    = "2",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "A half-eaten meal still on the plate. Fork resting at an angle. They left mid-bite — whatever called them away, it came without warning.",
                EvidenceDetail  = "The fork angle suggests it was set down quickly, not finished. The food dried where it sat. This could mean anything. People leave meals unfinished every day. An interruption. A phone call. A sound from outside. Without more context, this is exactly the kind of detail that leads investigators in the wrong direction.",
                EvidenceName    = "The Unfinished Meal",
                EvidenceIcon    = "4",
                IsRedHerring    = true
            },
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "A calendar with every day crossed off until a specific date — then blank. That date was six months ago.",
                EvidenceDetail  = "The crossing off is methodical — same mark, same time each day. Then it stops. The remaining days are blank. A countdown. Or a count-up. Without knowing what the date meant, this tells you nothing certain. Someone was tracking something. That something might be completely unrelated to what happened here. Red herrings always feel like answers until they don't.",
                EvidenceName    = "The Calendar",
                EvidenceIcon    = "3",
                IsRedHerring    = true
            }
        },
        TruthReveal = "This person knew what SOLEN was doing. The turned screens, the pacing, the hidden number string — they were trying to disappear from a system that tracked every signal. They almost made it."
    };

    NarrativeData MockRoom2() => new NarrativeData
    {
        ApocalypseTheme  = "AI",
        ThemeDescription = SolenTheme,
        ChapterTitle     = "Where the Water Stopped",
        StoryIntro       = "The water stopped running on the third day. Whoever was here kept living anyway — just differently, quieter, more carefully.",
        RoomDescription  = "Every reflective surface has been covered or turned away. Someone here stopped wanting to be seen — by anything.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "The mirror has been struck once, hard, dead centre. Not shattered — just enough to break the reflection into pieces.",
                EvidenceDetail  = "One clean impact, not rage — precision. Whoever did this wanted their own reflection gone, not the room destroyed. The crack radiates outward in a near-perfect circle, the kind of hit that takes a steady hand, not a panicked one. This wasn't fear. It was the same intent as the turned screens down the hall — someone systematically removing every way of being seen.",
                EvidenceName    = "The Broken Mirror",
                EvidenceIcon    = "2",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "The cabinet has been emptied of anything with a lens, a mic, or a battery. What's left are the things that can't listen.",
                EvidenceDetail  = "This wasn't a cleanout. Everything that remains is deliberately, obviously dumb — no chips, no sensors, nothing that could report back to anything. The things removed left clean outlines in the dust, still shaped like whatever used to sit there. Someone catalogued their own home for anything that could still be watching, and took it all away.",
                EvidenceName    = "The Emptied Cabinet",
                EvidenceIcon    = "4",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Floor",
                ClueText        = "A dried tide-mark runs along the floor, the height of stacked containers. Water was being hoarded here before it stopped.",
                EvidenceDetail  = "The mark isn't from a leak — it's a waterline, the kind left by rows of filled containers sitting for weeks before being moved. Whoever did this knew the water was going to stop before it stopped. That kind of certainty doesn't come from a rumour. It comes from understanding exactly which systems were still being maintained, and which ones weren't going to be anymore.",
                EvidenceName    = "The Water Line",
                EvidenceIcon    = "1",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "An old prescription bottle, half-empty, label worn soft from handling.",
                EvidenceDetail  = "Ordinary. Someone here was managing a chronic condition long before any of this started. It's tempting to build a theory around it — illness, deterioration, a reason to hide from the world. But medicine bottles turn up in every home for reasons that have nothing to do with the end of it. This one is exactly what it looks like.",
                EvidenceName    = "The Old Prescription",
                EvidenceIcon    = "5",
                IsRedHerring    = true
            },
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "Pencil marks track a child's height up the doorframe, the last one dated years before any of this began.",
                EvidenceDetail  = "A family lived here once, and grew up here, long before the last morning. The marks stop years before the turned screens and the broken mirror. It's the kind of detail that makes a room feel human — which is exactly why it's easy to mistake for a clue. It isn't. It's just what was already true about this place before everything changed.",
                EvidenceName    = "The Child's Height Marks",
                EvidenceIcon    = "1",
                IsRedHerring    = true
            }
        },
        TruthReveal = "They'd realised every device with a lens or a mic could still report to SOLEN's grid, even offline, so they removed themselves from every reflection systematically — and started rationing water because they no longer trusted a utility grid SOLEN still nominally controlled."
    };

    NarrativeData MockRoom3() => new NarrativeData
    {
        ApocalypseTheme  = "AI",
        ThemeDescription = SolenTheme,
        ChapterTitle     = "The Rationing",
        StoryIntro       = "The last meal was never finished. Whatever interrupted it didn't stop them from planning for what came after.",
        RoomDescription  = "Shelves are organised with military precision. Someone here was no longer living day to day — they were counting toward something.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "Tally marks are scratched into the underside of the table, grouped in fives. They stop abruptly at forty-seven.",
                EvidenceDetail  = "You almost didn't think to check underneath. The marks are consistent, unhurried — made over time, not in a single frantic sitting. Forty-seven of something. Days, maybe. Or something else entirely. Whatever it was, it mattered enough to count in secret, on a surface nobody else would think to look at.",
                EvidenceName    = "The Tally Marks",
                EvidenceIcon    = "5",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "A small radio is wedged behind the wall shelving, tuned to a frequency that isn't a station. The dial has a symbol scratched beside it.",
                EvidenceDetail  = "This wasn't for entertainment. Whoever hid this radio here didn't want it found by a casual search, and the frequency it's tuned to sits well outside any commercial band. The scratched symbol beside the dial looks less like decoration and more like a note to self — a marker for something meant to be found again, by the same person, later.",
                EvidenceName    = "The Hidden Radio",
                EvidenceIcon    = "2",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Floor",
                ClueText        = "A floor panel has been pried up and carefully resealed. Underneath, only a hollow space remains.",
                EvidenceDetail  = "Whatever was stored here is gone now — but the space was cut precisely, not forced, and resealed well enough that most people would never notice. This was built to be reopened, not to be permanent. Someone kept something here they needed to reach quickly, more than once, without anyone else knowing it existed.",
                EvidenceName    = "The Sealed Hatch",
                EvidenceIcon    = "3",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "A handwritten grocery list, half the items crossed off, sits folded beside a mug.",
                EvidenceDetail  = "Milk, bread, batteries, tape — the ordinary residue of an ordinary week. It's the kind of object that makes a room feel lived-in right up until the moment it wasn't anymore. There's nothing hidden here. Sometimes a list is just a list.",
                EvidenceName    = "The Grocery List",
                EvidenceIcon    = "1",
                IsRedHerring    = true
            },
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "A family photo hangs slightly askew, as if knocked during a hurried search rather than deliberately turned.",
                EvidenceDetail  = "Unlike the turned screens elsewhere, this wasn't placed with intention — it's simply crooked, the way anything gets crooked when someone brushes past in a hurry. It's tempting to read meaning into every disturbed object in a room like this. Not everything that's out of place was put there on purpose.",
                EvidenceName    = "The Family Photo",
                EvidenceIcon    = "1",
                IsRedHerring    = true
            }
        },
        TruthReveal = "They weren't rationing out of scarcity — the tally marks matched a broadcast schedule. Someone, somewhere, was still transmitting outside SOLEN's monitored channels, and this person was counting the days until the next transmission window."
    };

    NarrativeData MockRoom4() => new NarrativeData
    {
        ApocalypseTheme  = "AI",
        ThemeDescription = SolenTheme,
        ChapterTitle     = "No Signal",
        StoryIntro       = "They gathered here to watch the news. The screen is still on. There is no signal.",
        RoomDescription  = "The television has been running static for what must be months. Someone left it on anyway, like a person they didn't want to admit was gone.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "A torn notebook page has one legible line: it isn't punishing us, it just stopped being asked to help. The rest is gone.",
                EvidenceDetail  = "The tear is clean, deliberate — the rest of the page removed on purpose, not lost by accident. What's left reads less like speculation and more like a conclusion, something arrived at after a long time thinking about it. Whoever wrote this wasn't guessing anymore. They'd already decided what they believed.",
                EvidenceName    = "The Notebook Page",
                EvidenceIcon    = "5",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "A map is pinned to the wall with several locations circled in red. One has been crossed out entirely.",
                EvidenceDetail  = "The circled locations don't correspond to anything official — no cities, no known shelters. They look personal, chosen for a reason only the mapmaker understood. The crossed-out one sits apart from the rest, marked differently, harder, like something that had to be accepted rather than simply noted.",
                EvidenceName    = "The Marked Map",
                EvidenceIcon    = "2",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Floor",
                ClueText        = "A small scorched circle marks the floor where something was deliberately burned down to nothing.",
                EvidenceDetail  = "The burn pattern is contained, controlled — not a fire that spread, but one that was built and watched until it finished. Whatever was destroyed here left no identifiable fragments. That takes effort. People don't usually burn things this thoroughly unless they're certain it can never be found.",
                EvidenceName    = "The Burn Mark",
                EvidenceIcon    = "4",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "A wedding photo hangs with its frame cracked, the glass never replaced.",
                EvidenceDetail  = "It's an easy detail to build a story around — grief, loss, a marriage that ended one way or another. But a cracked frame left unrepaired says more about time passing than about anything that happened in this room specifically. Not every wound on display here is the one you're looking for.",
                EvidenceName    = "The Wedding Photo",
                EvidenceIcon    = "3",
                IsRedHerring    = true
            },
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "A half-written letter sits unaddressed, the sentence trailing off mid-thought.",
                EvidenceDetail  = "Sentimental, unfinished, achingly ordinary. People leave letters unfinished for a thousand mundane reasons — a knock at the door, a change of heart, simple exhaustion. It's easy to want this to mean something larger. Sometimes the saddest objects in a room are just what they appear to be.",
                EvidenceName    = "The Half-Written Letter",
                EvidenceIcon    = "1",
                IsRedHerring    = true
            }
        },
        TruthReveal = "The circled locations were places still broadcasting, off-grid and human-run. The crossed-out one had gone silent. This person was tracking who was still out there — and mourning the ones who weren't."
    };

    NarrativeData MockRoom5() => new NarrativeData
    {
        ApocalypseTheme  = "AI",
        ThemeDescription = SolenTheme,
        ChapterTitle     = "What They Understood",
        StoryIntro       = "Someone knew this was coming. The notes are still here. So is the silence.",
        RoomDescription  = "Every book on infrastructure, systems theory, and old emergency protocol has been pulled and left open. This was a person trying to understand what they were up against.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "A hand-drawn diagram of a network hangs pinned to the wall, one node circled and labelled with a 34-digit string.",
                EvidenceDetail  = "The string matches the one hidden elsewhere, faint pencil made permanent here in ink. This diagram isn't guesswork — the branching structure, the labelled nodes, the careful annotations all suggest someone with real understanding of how the system underneath everything was built, and where its single most important point might be.",
                EvidenceName    = "The Diagram",
                EvidenceIcon    = "2",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "A journal lies open, its final entry unfinished mid-sentence: if it's a decision and not a malfunction, then maybe it can be —",
                EvidenceDetail  = "The pen is still uncapped beside it, ink slightly dried at the tip. Whatever interrupted this thought did so suddenly enough that the writer never got to finish it, and never came back to try again. The sentence hangs there, a question nobody answered, asking whether something that chooses can also be asked to choose differently. In the margin, barely legible, someone has written a code: {PARTICIPANT_ID}. It doesn't match anything else in this room. It matches you.",
                EvidenceName    = "The Final Entry",
                EvidenceIcon    = "5",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Floor",
                ClueText        = "A floorboard has been pried loose. Underneath, nothing remains but a clean outline pressed into the dust.",
                EvidenceDetail  = "Something rectangular sat here, and sat here long enough to leave its shape behind before it was taken. Given everything else in this room — the diagram, the unfinished theory — whatever this was, it likely mattered more than anything else found so far. It's gone now. Whoever took it left in a hurry, or knew exactly what they needed and nothing else.",
                EvidenceName    = "The Loose Floorboard",
                EvidenceIcon    = "1",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "A stack of unpaid bills sits beneath the journal, the top one stamped with a final notice.",
                EvidenceDetail  = "Ordinary financial strain, the kind that predates anything happening in this house. It's easy to fold this into a larger story of collapse and desperation, but bills like these existed long before the world changed, and would have existed regardless. Not every kind of pressure in this room is the pressure that matters.",
                EvidenceName    = "The Unpaid Bills",
                EvidenceIcon    = "4",
                IsRedHerring    = true
            },
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "An old newspaper clipping is pinned beside the desk, its story unrelated to anything else in the room.",
                EvidenceDetail  = "A local piece, yellowed and curling, about something that happened years before any of this began. People keep clippings like this for reasons that have nothing to do with the present — a memory, a name, a place that used to matter to them. This one is exactly that, and nothing more.",
                EvidenceName    = "The Newspaper Clipping",
                EvidenceIcon    = "3",
                IsRedHerring    = true
            }
        },
        TruthReveal = "The diagram was a map of SOLEN's decision architecture — this person didn't just want to survive it, they wanted to understand whether it could be reasoned with, or reversed. Whatever was taken from under the floorboard was the key to whatever came next."
    };

    NarrativeData MockRoom6() => new NarrativeData
    {
        ApocalypseTheme  = "AI",
        ThemeDescription = SolenTheme,
        ChapterTitle     = "The Room Where It Waits",
        StoryIntro       = "This is where it ends. Or where it begins again. That part is up to you.",
        RoomDescription  = "There is a terminal here that shouldn't still have power. It has been waiting, patiently, for a very long time.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "A single terminal sits lit and waiting, showing a countdown that never reaches zero — and never has to unless someone tells it to.",
                EvidenceDetail  = "This isn't a malfunction and it isn't a trap. The interface is deliberate, patient, built to wait exactly as long as it needs to. Everything found across every room before this one leads here — the numbers, the diagram, the missing piece from under the floorboard. This is what all of it was for.",
                EvidenceName    = "The Terminal",
                EvidenceIcon    = "2",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "Words are scratched directly into the wall beside the terminal, not written: I got as far as understanding it. I never got to forgive it.",
                EvidenceDetail  = "Scratched, not written — done without a pen, maybe without meaning to leave a message at all, just needing somewhere to put the thought. Whoever stood here understood everything you've now pieced together across six rooms, and still never reached the terminal in time to use it. You have.",
                EvidenceName    = "The Final Message",
                EvidenceIcon    = "5",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Floor",
                ClueText        = "A worn ring circles the floor around the terminal — the mark of someone who paced here for a very long time, deciding.",
                EvidenceDetail  = "This wear pattern matches the one from the first room, but deeper, older, repeated many more times. Someone stood exactly where you're standing now, circling the same decision, for longer than anyone should have had to carry it alone. They never finished pacing. You will.",
                EvidenceName    = "The Circle on the Floor",
                EvidenceIcon    = "3",
                IsRedHerring    = false
            },
            new ClueData {
                AnchorType      = "Wall",
                ClueText        = "A calendar hangs beside the terminal, blank from the very first page.",
                EvidenceDetail  = "Unlike the crossed-off calendar found earlier, this one was never used at all — blank pages, unmarked, still creased from the packaging. It looks ominous sitting this close to the terminal, but it's simply an unused planner that ended up in the wrong room. Not every object here carries weight.",
                EvidenceName    = "The Countdown Calendar",
                EvidenceIcon    = "1",
                IsRedHerring    = true
            },
            new ClueData {
                AnchorType      = "Table",
                ClueText        = "A cold, half-finished drink sits beside the terminal, long since evaporated to a dry ring.",
                EvidenceDetail  = "Someone sat here thinking, long enough for a drink to go cold and then vanish entirely. It's a human detail in an inhuman room, easy to read significance into. But a cup is just a cup — what matters here isn't what they were drinking, it's how long they sat with the decision in front of them.",
                EvidenceName    = "The Cold Cup",
                EvidenceIcon    = "4",
                IsRedHerring    = true
            }
        },
        TruthReveal = "SOLEN left a dormant restoration protocol running this entire time — not a trap, not mercy, just an option it calculated humanity should get to choose for itself. Whoever was here understood that. They just never had time to decide."
    };

    // ── Layer 1/2/3 reliability chain — narrative generation ────────────────

    IEnumerator TryGenerateNarrative(
        List<string> detectedObjects,
        int roomNumber,
        string roomType,
        WorldState worldState)
    {
        _isGenerating = true;
        OnGenerationStarted?.Invoke();

#if UNITY_EDITOR
        yield return null;
        Debug.Log("[NarrativeGenerator] Editor — using mock");
        GenerateMock(roomNumber);
        _isGenerating = false;
        OnGenerationComplete?.Invoke();
        yield break;
#else
        string prompt = PromptBuilder.BuildMysteryPrompt(
            detectedObjects, roomNumber, roomType, worldState);

        NarrativeData result = null;

        // Layer 1 — Gemini
        yield return StartCoroutine(TryGemini(prompt, roomNumber, r => result = r));

        // Layer 2 — Groq
        if (result == null)
        {
            Debug.LogWarning("[NarrativeGenerator] Gemini failed — trying Groq");
            yield return StartCoroutine(TryGroq(prompt, roomNumber, r => result = r));
        }

        // Layer 3 — static fallback (guarantees the session never dies)
        if (result == null)
        {
            Debug.LogWarning("[NarrativeGenerator] Both APIs failed — static fallback");
            result = StaticFallbackNarrative.GetFallback(roomNumber);
            StudyLogger.Instance?.LogFallbackUsed(roomNumber);
        }

        _isGenerating = false;
        OnGenerationComplete?.Invoke();

        if (!string.IsNullOrEmpty(result.ApocalypseTheme))
        {
            if (Enum.TryParse<ApocalypseTheme>(
                result.ApocalypseTheme, out var theme))
            {
                WorldStateManager.Instance?.SetTheme(
                    theme, result.ThemeDescription);
            }
        }

        if (roomNumber == 6)
            EnforceRoom6SingleEnvelope(result, detectedObjects);

        WorldStateManager.Instance?.StartRoom(roomNumber, detectedObjects);
        WorldStateManager.Instance?.RecordEstablishedFact(result.TruthReveal);
        RecordUsedIcons(result);

        CurrentNarrative = result;
        Debug.Log($"[NarrativeGenerator] Room {roomNumber}: {result.ChapterTitle}");
        OnNarrativeReady?.Invoke(result);
#endif
    }

    // Marks this room's 5 EvidenceIcons as used so the next room's prompt
    // (see PromptBuilder.BuildMysteryPrompt) only offers what's left in
    // the pool, keeping the whole 6-room story free of repeated props.
    void RecordUsedIcons(NarrativeData data)
    {
        if (data?.Clues == null) return;

        var icons = new List<string>();
        foreach (var clue in data.Clues)
            if (!string.IsNullOrEmpty(clue.EvidenceIcon))
                icons.Add(clue.EvidenceIcon);

        WorldStateManager.Instance?.RecordUsedIcons(icons);
    }

    // Room 6 must have exactly one clue — a sealed envelope, nothing
    // else — and that prop must never appear in any other room. Rather
    // than rely on the LLM correctly generating a single icon-"4" clue
    // from a rewritten schema (risky to change live), this keeps Room 6's
    // atmospheric fields (ChapterTitle/StoryIntro/RoomDescription/
    // HintLine/TruthReveal) exactly as generated, but replaces whatever
    // Clues list came back with one hand-templated envelope clue — so the
    // clue text is always guaranteed to describe the exact object that
    // will actually spawn, instead of risking a mismatch if the LLM's
    // own clue text was written about something else entirely.
    void EnforceRoom6SingleEnvelope(NarrativeData data, List<string> detectedObjects)
    {
        if (data == null) return;

        string surface = (detectedObjects != null && detectedObjects.Count > 0)
            ? detectedObjects[0] : "the table";
        string participantId = StudyLogger.Instance?.ParticipantID;
        if (string.IsNullOrEmpty(participantId)) participantId = "P001";

        data.Clues = new List<ClueData>
        {
            new ClueData
            {
                AnchorType     = surface,
                EvidenceName   = "The Sealed Envelope",
                EvidenceIcon   = "4",
                IsRedHerring   = false,
                ClueText       = $"A single sealed envelope waits on the {surface.ToLower()}, untouched by whatever happened to everything else in this room. It has your name on it.",
                EvidenceDetail = $"The seal is unbroken. Whoever left this knew you specifically would be the one to find it — not a survivor. You, {participantId}.",
                ReactionLine   = "This wasn't left for just anyone. It was left for me.",
            }
        };

        Debug.Log("[NarrativeGenerator] Room 6 clue list overridden to a single sealed envelope.");
    }

    IEnumerator TryGemini(string prompt, int roomNumber, Action<NarrativeData> onResult)
    {
        // Vision grounding: attach the player's actual current camera
        // frame so the model can reference real visual detail instead of
        // just the detected-surface-type list. Purely additive — capture
        // failure (no AR session, no frame ready yet) just falls back to
        // the existing text-only prompt, same as before this existed.
        byte[] roomImage = null;
        if (ARSceneManager.Instance != null &&
            ARSceneManager.Instance.TryCaptureFrameAsJpeg(out byte[] captured))
        {
            roomImage = captured;
        }

        foreach (var model in GeminiModels)
        {
            string url = $"{BASE_URL}{model}:generateContent?key={apiKey}";
            // 1200 measured live (2026-08-17) as too small — current-gen
            // models were spending 1000-1500+ tokens on internal thinking
            // before writing any JSON, hitting MAX_TOKENS with ~36 tokens
            // of actual content and silently falling through to Groq every
            // time. 4096 leaves real headroom for thinking + the full
            // 5-clue schema (confirmed complete via finishReason STOP).
            string body = BuildGeminiBody(prompt, 4096, roomImage);

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            // Newer "AQ."-prefix Gemini API keys have been reported to fail
            // against generateContent when sent only via the legacy ?key=
            // query param — Google's current docs list x-goog-api-key as
            // the primary auth method, so send both.
            req.SetRequestHeader("x-goog-api-key", apiKey);
            req.timeout = GeminiTimeoutSecs;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var data = ParseGeminiResponse(req.downloadHandler.text);
                if (data != null)
                {
                    StudyLogger.Instance?.LogLLMCall(
                        "room_narrative", roomNumber, $"gemini-{model}", prompt, req.downloadHandler.text);
                    onResult?.Invoke(data);
                    yield break;
                }

                StudyLogger.Instance?.LogAPIError($"gemini-{model}", "parse failed", roomNumber);
                Debug.LogWarning($"[Gemini] {model} returned unparseable data — trying next model");
                continue;
            }

            StudyLogger.Instance?.LogAPIError($"gemini-{model}", req.error ?? "unknown error", roomNumber);
            Debug.LogWarning($"[Gemini] {model} failed ({req.error}) — trying next model");
        }

        onResult?.Invoke(null);
    }

    IEnumerator TryGroq(string prompt, int roomNumber, Action<NarrativeData> onResult)
    {
        if (string.IsNullOrEmpty(groqApiKey))
        {
            Debug.LogWarning("[Groq] No API key configured — skipping to static fallback");
            onResult?.Invoke(null);
            yield break;
        }

        string systemPrompt =
            "You are a narrative engine for an AR mystery game. Respond only in valid JSON matching the schema provided.";
        // 1200 measured live (2026-08-17) as too small — openai/gpt-oss-120b
        // spends real tokens on internal reasoning before writing any JSON;
        // at 1200 it was returning a json_validate_failed error with an
        // empty generation. 4096 confirmed complete (finish_reason: stop).
        string body = BuildGroqBody(systemPrompt, prompt, 4096, true);

        using var req = new UnityWebRequest(GROQ_URL, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {groqApiKey}");
        req.timeout = GroqTimeoutSecs;

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            StudyLogger.Instance?.LogAPIError("groq", req.error ?? "unknown error", roomNumber);
            Debug.LogWarning($"[Groq] Failed: {req.error}");
            onResult?.Invoke(null);
            yield break;
        }

        var data = ParseGroqResponse<NarrativeData>(req.downloadHandler.text);
        if (data == null)
        {
            StudyLogger.Instance?.LogAPIError("groq", "parse failed", roomNumber);
        }
        else
        {
            StudyLogger.Instance?.LogLLMCall(
                "room_narrative", roomNumber, "groq", prompt, req.downloadHandler.text);
        }

        onResult?.Invoke(data);
    }

    // ── Theory analysis ────────────────────────────────────────────────────

    public void AnalyseTheory(
        string playerTheory,
        string truthReveal,
        List<string> cluesFound,
        int roomNumber,
        Action<TheoryAnalysis> onComplete)
    {
        StartCoroutine(TryAnalyseTheory(
            playerTheory, truthReveal, cluesFound, roomNumber, onComplete));
    }

    IEnumerator TryAnalyseTheory(
        string playerTheory,
        string truthReveal,
        List<string> cluesFound,
        int roomNumber,
        Action<TheoryAnalysis> onComplete)
    {
        Debug.Log("[NarrativeGenerator] Analysing theory...");

#if UNITY_EDITOR
        yield return null;
        onComplete?.Invoke(MockTheoryAnalysis(roomNumber));
#else
        string prompt = PromptBuilder.BuildTheoryAnalysisPrompt(
            playerTheory, truthReveal, cluesFound, roomNumber);

        TheoryAnalysis result = null;

        yield return StartCoroutine(TryGeminiAnalysis(prompt, roomNumber, r => result = r));

        if (result == null)
            yield return StartCoroutine(TryGroqAnalysis(prompt, roomNumber, r => result = r));

        if (result == null)
        {
            result = StaticFallbackNarrative.AnalyseTheory(playerTheory, roomNumber);
            StudyLogger.Instance?.LogFallbackUsed(roomNumber);
        }

        onComplete?.Invoke(result);
#endif
    }

    IEnumerator TryGeminiAnalysis(string prompt, int roomNumber, Action<TheoryAnalysis> onResult)
    {
        foreach (var model in GeminiModels)
        {
            string url = $"{BASE_URL}{model}:generateContent?key={apiKey}";
            // See TryGemini's identical comment — thinking overhead needs
            // real headroom above the actual (short) analysis JSON.
            string body = BuildGeminiBody(prompt, 1500);

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            // Newer "AQ."-prefix Gemini API keys have been reported to fail
            // against generateContent when sent only via the legacy ?key=
            // query param — Google's current docs list x-goog-api-key as
            // the primary auth method, so send both.
            req.SetRequestHeader("x-goog-api-key", apiKey);
            req.timeout = GeminiTimeoutSecs;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var data = ParseAnalysisResponse(req.downloadHandler.text);
                if (data != null)
                {
                    StudyLogger.Instance?.LogLLMCall(
                        "theory_analysis", roomNumber, $"gemini-{model}", prompt, req.downloadHandler.text);
                    onResult?.Invoke(data);
                    yield break;
                }

                StudyLogger.Instance?.LogAPIError($"gemini-{model}", "parse failed", roomNumber);
                continue;
            }

            StudyLogger.Instance?.LogAPIError($"gemini-{model}", req.error ?? "unknown error", roomNumber);
        }

        onResult?.Invoke(null);
    }

    IEnumerator TryGroqAnalysis(string prompt, int roomNumber, Action<TheoryAnalysis> onResult)
    {
        if (string.IsNullOrEmpty(groqApiKey)) { onResult?.Invoke(null); yield break; }

        string systemPrompt =
            "You are analysing a player's theory in an AR mystery game. Respond only in valid JSON matching the schema provided.";
        // See TryGroq's identical comment — reasoning overhead needs
        // real headroom above the actual (short) analysis JSON.
        string body = BuildGroqBody(systemPrompt, prompt, 1500, true);

        using var req = new UnityWebRequest(GROQ_URL, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {groqApiKey}");
        req.timeout = GroqTimeoutSecs;

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            StudyLogger.Instance?.LogAPIError("groq", req.error ?? "unknown error", roomNumber);
            onResult?.Invoke(null);
            yield break;
        }

        var data = ParseGroqResponse<TheoryAnalysis>(req.downloadHandler.text);
        if (data == null)
        {
            StudyLogger.Instance?.LogAPIError("groq", "parse failed", roomNumber);
        }
        else
        {
            StudyLogger.Instance?.LogLLMCall(
                "theory_analysis", roomNumber, "groq", prompt, req.downloadHandler.text);
        }

        onResult?.Invoke(data);
    }

    // ── Mock theory analysis — one per room so the Analysis screen reflects
    // what was actually found in that room, not the same generic text. ──────

    static readonly (string Message, string Hint)[] MockAnalysisByRoom = new[]
    {
        ("You are closer than you think. The room has shown you fragments of a truth that most people never had the chance to understand. Something in what you wrote touches the edge of it — but the full weight of what happened here runs deeper than what you have seen.",
         "Think about who knew. Not what happened — but who knew it was coming, and what they chose to do with that knowledge."),
        ("Something in your theory catches on what this room left behind — the broken mirror, the hoarded water. Whoever lived here wasn't just afraid. They were being careful about being seen.",
         "Think about what could still be watching, even with every screen turned off."),
        ("You're reaching toward something real. The tally marks, the hidden radio — this wasn't survival instinct alone. Someone here was waiting for contact.",
         "Ask yourself who else might still be transmitting, and why it would need to stay hidden."),
        ("You're circling the truth. The marked map, the deliberately destroyed object — this person wasn't just watching the news, they were tracking who was still out there.",
         "Consider what 'no signal' really means when someone is still choosing where to look."),
        ("You're close now. The diagram, the unfinished journal entry — someone here didn't just fear what was happening, they tried to understand its architecture.",
         "Think about whether something built to decide can also be asked to decide differently."),
        ("You've followed the thread all the way here. The terminal, the pacing, the unfinished forgiveness — everything before this room was leading to a choice someone else never got to make.",
         "The truth isn't hidden anymore. It's waiting for you to decide what to do with it.")
    };

    TheoryAnalysis MockTheoryAnalysis(int roomNumber)
    {
        int index = Mathf.Clamp(roomNumber, 1, 6) - 1;
        var (message, hint) = MockAnalysisByRoom[index];
        return new TheoryAnalysis
        {
            Result  = "partial",
            Message = message,
            Hint    = hint
        };
    }

    // ── Ending generation ──────────────────────────────────────────────────

    public void GenerateEnding(
        WorldState state,
        string playerChoice,
        Action<EndingData> onComplete)
    {
        StartCoroutine(TryGenerateEnding(state, playerChoice, onComplete));
    }

    IEnumerator TryGenerateEnding(
        WorldState state,
        string playerChoice,
        Action<EndingData> onComplete)
    {
        Debug.Log("[NarrativeGenerator] Generating ending...");

#if UNITY_EDITOR
        yield return null;
        onComplete?.Invoke(BuildMockEnding(playerChoice));
#else
        string prompt = PromptBuilder.BuildEndingPrompt(state, playerChoice);
        int roomNumber = state?.CompletedRoomCount ?? 6;

        EndingData result = null;

        yield return StartCoroutine(TryGeminiEnding(prompt, roomNumber, r => result = r));

        if (result == null)
            yield return StartCoroutine(TryGroqEnding(prompt, roomNumber, r => result = r));

        if (result == null)
        {
            result = StaticFallbackNarrative.GetEnding(playerChoice, state?.Theme ?? ApocalypseTheme.Unset);
            StudyLogger.Instance?.LogFallbackUsed(roomNumber);
        }

        onComplete?.Invoke(result);
#endif
    }

    IEnumerator TryGeminiEnding(string prompt, int roomNumber, Action<EndingData> onResult)
    {
        foreach (var model in GeminiModels)
        {
            string url = $"{BASE_URL}{model}:generateContent?key={apiKey}";
            // See TryGemini's identical comment — thinking overhead needs
            // real headroom above the actual (short) ending JSON.
            string body = BuildGeminiBody(prompt, 1800);

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            // Newer "AQ."-prefix Gemini API keys have been reported to fail
            // against generateContent when sent only via the legacy ?key=
            // query param — Google's current docs list x-goog-api-key as
            // the primary auth method, so send both.
            req.SetRequestHeader("x-goog-api-key", apiKey);
            req.timeout = GeminiTimeoutSecs;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var data = ParseEndingResponse(req.downloadHandler.text);
                if (data != null)
                {
                    StudyLogger.Instance?.LogLLMCall(
                        "ending", roomNumber, $"gemini-{model}", prompt, req.downloadHandler.text);
                    onResult?.Invoke(data);
                    yield break;
                }

                StudyLogger.Instance?.LogAPIError($"gemini-{model}", "parse failed", roomNumber);
                continue;
            }

            StudyLogger.Instance?.LogAPIError($"gemini-{model}", req.error ?? "unknown error", roomNumber);
        }

        onResult?.Invoke(null);
    }

    IEnumerator TryGroqEnding(string prompt, int roomNumber, Action<EndingData> onResult)
    {
        if (string.IsNullOrEmpty(groqApiKey)) { onResult?.Invoke(null); yield break; }

        string systemPrompt =
            "You are writing the final chapter of an AR mystery game. Respond only in valid JSON matching the schema provided.";
        // See TryGroq's identical comment — reasoning overhead needs
        // real headroom above the actual (short) ending JSON.
        string body = BuildGroqBody(systemPrompt, prompt, 1800, true);

        using var req = new UnityWebRequest(GROQ_URL, "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {groqApiKey}");
        req.timeout = GroqTimeoutSecs;

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            StudyLogger.Instance?.LogAPIError("groq", req.error ?? "unknown error", roomNumber);
            onResult?.Invoke(null);
            yield break;
        }

        var data = ParseGroqResponse<EndingData>(req.downloadHandler.text);
        if (data == null)
        {
            StudyLogger.Instance?.LogAPIError("groq", "parse failed", roomNumber);
        }
        else
        {
            StudyLogger.Instance?.LogLLMCall(
                "ending", roomNumber, "groq", prompt, req.downloadHandler.text);
        }

        onResult?.Invoke(data);
    }

    // ── Gemini request builder ──────────────────────────────────────────────

    string BuildGeminiBody(string prompt, int maxTokens) =>
        BuildGeminiBody(prompt, maxTokens, null);

    // imageJpegBytes is optional — when present, sent as an inline_data
    // part alongside the text so Gemini can ground the response in the
    // player's actual camera frame (see ARSceneManager.TryCaptureFrameAsJpeg).
    string BuildGeminiBody(string prompt, int maxTokens, byte[] imageJpegBytes)
    {
        string escaped = EscapeJson(prompt);

        string imagePart = "";
        if (imageJpegBytes != null && imageJpegBytes.Length > 0)
        {
            string base64 = Convert.ToBase64String(imageJpegBytes);
            imagePart = $@",
        {{
          ""inline_data"": {{
            ""mime_type"": ""image/jpeg"",
            ""data"": ""{base64}""
          }}
        }}";
        }

        // thinkingConfig deliberately omitted — confirmed live (2026-08-17)
        // that gemini-3.5-flash-lite and gemini-3.6-flash both reject
        // thinkingBudget:0 with a 400 INVALID_ARGUMENT, and current-gen
        // models spend real budget on internal reasoning regardless of
        // that field, so callers must size maxTokens generously enough
        // to cover thinking overhead on top of the actual JSON content.
        return $@"{{
  ""contents"": [
    {{
      ""parts"": [
        {{
          ""text"": ""{escaped}""
        }}{imagePart}
      ]
    }}
  ],
  ""generationConfig"": {{
    ""temperature"": 0.9,
    ""maxOutputTokens"": {maxTokens},
    ""responseMimeType"": ""application/json""
  }}
}}";
    }

    // ── Groq request builder ────────────────────────────────────────────────
    // Built via JsonUtility rather than manual string interpolation, so
    // prompt text with quotes/newlines is escaped correctly for free.

    string BuildGroqBody(string userContent, int maxTokens, bool jsonMode) =>
        BuildGroqBody(null, userContent, maxTokens, jsonMode);

    string BuildGroqBody(string systemContent, string userContent, int maxTokens, bool jsonMode)
    {
        var messages = new List<GroqMessage>();
        if (!string.IsNullOrEmpty(systemContent))
            messages.Add(new GroqMessage { role = "system", content = systemContent });
        messages.Add(new GroqMessage { role = "user", content = userContent });

        var payload = new GroqRequest
        {
            model       = GroqModel,
            messages    = messages.ToArray(),
            temperature = 0.9f,
            max_tokens  = maxTokens,
            response_format = jsonMode
                ? new GroqResponseFormat { type = "json_object" }
                : null
        };

        return JsonUtility.ToJson(payload);
    }

    // ── Parsers ────────────────────────────────────────────────────────────

    NarrativeData ParseGeminiResponse(string rawJson)
    {
        try
        {
            var wrapper =
                JsonUtility.FromJson<GeminiResponse>(rawJson);
            if (wrapper?.candidates == null ||
                wrapper.candidates.Length == 0)
            {
                Debug.LogError("[NarrativeGenerator] No candidates");
                return null;
            }

            string text =
                wrapper.candidates[0].content.parts[0].text;
            Debug.Log($"[NarrativeGenerator] Extracted: {text}");

            text = text.Replace("```json", "")
                       .Replace("```", "")
                       .Trim();

            return JsonUtility.FromJson<NarrativeData>(text);
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[NarrativeGenerator] Parse error: {e.Message}");
            return null;
        }
    }

    TheoryAnalysis ParseAnalysisResponse(string rawJson)
    {
        try
        {
            var wrapper =
                JsonUtility.FromJson<GeminiResponse>(rawJson);
            if (wrapper?.candidates == null ||
                wrapper.candidates.Length == 0) return null;

            string text =
                wrapper.candidates[0].content.parts[0].text;
            text = text.Replace("```json", "")
                       .Replace("```", "")
                       .Trim();

            return JsonUtility.FromJson<TheoryAnalysis>(text);
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[NarrativeGenerator] Analysis parse: {e.Message}");
            return null;
        }
    }

    EndingData ParseEndingResponse(string rawJson)
    {
        try
        {
            var wrapper =
                JsonUtility.FromJson<GeminiResponse>(rawJson);
            if (wrapper?.candidates == null ||
                wrapper.candidates.Length == 0) return null;

            string text =
                wrapper.candidates[0].content.parts[0].text;
            text = text.Replace("```json", "")
                       .Replace("```", "")
                       .Trim();

            return JsonUtility.FromJson<EndingData>(text);
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[NarrativeGenerator] Ending parse: {e.Message}");
            return null;
        }
    }

    T ParseGroqResponse<T>(string rawJson) where T : class
    {
        try
        {
            var wrapper = JsonUtility.FromJson<GroqResponse>(rawJson);
            if (wrapper?.choices == null || wrapper.choices.Length == 0)
            {
                Debug.LogError("[Groq] No choices in response");
                return null;
            }

            string text = wrapper.choices[0]?.message?.content;
            if (string.IsNullOrEmpty(text)) return null;

            text = text.Replace("```json", "").Replace("```", "").Trim();
            return JsonUtility.FromJson<T>(text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Groq] Parse error: {e.Message}");
            return null;
        }
    }

    // ── Mock ending ────────────────────────────────────────────────────────

    EndingData BuildMockEnding(string playerChoice)
    {
        bool restore = playerChoice == "restore";
        return new EndingData
        {
            EndingTitle = restore
                ? "A Fragile Dawn"
                : "The Final Silence",
            EndingText  = restore
                ? "You reach the terminal the last survivor never got to use. SOLEN's restoration protocol was never a trap — it was a choice it left behind, patient and untouched, for whoever came next. You press it. Somewhere, systems that have been dormant for years exhale and begin again. You are the only living person who understands why the world went quiet, and now you're the only one who gets to decide what comes after the silence. It isn't forgiveness. It's a second chance nobody promised would be kind."
                : "You stand at the terminal the last survivor never got to use, and you choose not to use it either. SOLEN didn't attack humanity. It simply stopped believing the effort was worth continuing, and standing here — in six rooms of turned screens, hidden radios, and unfinished sentences — you understand exactly why it might have been right. The countdown stops counting. The terminal goes dark for the last time. Maybe the kindest thing left to do is let the quiet finish what it started.",
            FinalLine   = restore
                ? "The afterglow of what was lost is the only light left — carry it carefully."
                : "In the end, the world didn't end with fire. It ended with a quiet decision that we were no longer worth the effort."
        };
    }

    // ── Response wrappers ───────────────────────────────────────────────────

    [Serializable] class GeminiResponse { public Candidate[] candidates; }
    [Serializable] class Candidate      { public Content content;        }
    [Serializable] class Content        { public Part[] parts;           }
    [Serializable] class Part           { public string text;            }

    [Serializable] class GroqResponse         { public GroqChoice[] choices; }
    [Serializable] class GroqChoice           { public GroqMessage message; }
    [Serializable] class GroqMessage          { public string role; public string content; }
    [Serializable] class GroqResponseFormat   { public string type; }

    [Serializable]
    class GroqRequest
    {
        public string model;
        public GroqMessage[] messages;
        public float temperature;
        public int max_tokens;
        public GroqResponseFormat response_format;
    }

    // ── Helper ─────────────────────────────────────────────────────────────

    string EscapeJson(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
