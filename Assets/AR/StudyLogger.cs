using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StudyLogger : MonoBehaviour
{
    public static StudyLogger Instance { get; private set; }

    [Header("Session Info")]
    public string ParticipantID = "";
    public string Condition     = "A";  // "A" = Dynamic AI, "B" = Fixed
    public string Version       = "AI"; // "AI" or "Fixed"

    const float SessionFlagThresholdSecs = 22f * 60f;

    string SavePath => Path.Combine(
        Application.persistentDataPath,
        $"{ParticipantID}_{Condition}_{_sessionStartDateTime:yyyyMMdd_HHmm}_session.json");

    StudySession _session;
    DateTime     _sessionStartDateTime;
    float        _sessionStartUnityTime;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Fallback session so logging calls never null-ref if
        // ResearcherSetupScreen's StartSession() hasn't run yet (e.g.
        // quick editor testing that skips straight past setup).
        if (_session == null)
            StartSession(ParticipantID, Condition);
    }

    // ── Session lifecycle ──────────────────────────────────────────────────

    public void StartSession(string participantId, string condition)
    {
        ParticipantID = participantId;
        Condition     = condition;

        _sessionStartDateTime  = DateTime.Now;
        _sessionStartUnityTime = Time.time;

        _session = new StudySession
        {
            ParticipantID    = participantId,
            Condition        = condition,
            Version          = Version,
            StartTime        = _sessionStartDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Rooms            = new List<RoomLog>(),
            APIErrors        = new List<APIErrorLog>(),
            TechnicalIssues  = ""
        };

        _currentRoomLog = null;
        Debug.Log($"[StudyLogger] Session started: {participantId} — condition {condition}");
    }

    // Clears the in-memory session (used by "reset for next participant").
    // Does not touch already-exported files.
    public void ResetSession()
    {
        _currentRoomLog = null;
        _session = new StudySession
        {
            ParticipantID   = "",
            Condition       = Condition,
            Version         = Version,
            Rooms           = new List<RoomLog>(),
            APIErrors       = new List<APIErrorLog>(),
            TechnicalIssues = ""
        };
        Debug.Log("[StudyLogger] Session reset for next participant");
    }

    // ── Onboarding ──────────────────────────────────────────────────────────

    public void OnOnboardingComplete(float durationSecs)
    {
        if (_session == null) return;
        _session.OnboardingCompleted    = true;
        _session.OnboardingDurationSecs = Mathf.RoundToInt(durationSecs);
        Debug.Log($"[StudyLogger] Onboarding completed in {durationSecs:F1}s");
    }

    // ── Reliability tracking ────────────────────────────────────────────────

    // provider examples: "gemini-2.5-flash", "groq". roomNumber is passed
    // explicitly by the caller rather than read from WorldStateManager,
    // since during a failed generation the room may not have "started" yet
    // (WorldStateManager.CurrentRoomNumber lags until generation succeeds).
    public void LogAPIError(string provider, string error, int roomNumber)
    {
        if (_session == null) return;

        _session.LLMApiErrorCount++;
        if (provider != null && provider.StartsWith("gemini"))
            _session.GeminiFailures++;
        else if (provider == "groq")
            _session.GroqFailures++;

        _session.APIErrors ??= new List<APIErrorLog>();
        _session.APIErrors.Add(new APIErrorLog
        {
            Provider   = provider,
            Error      = error,
            RoomNumber = roomNumber,
            Timestamp  = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });

        Debug.LogWarning($"[StudyLogger] API error — {provider}: {error} (room {roomNumber})");
    }

    public void LogFallbackUsed(int roomNumber)
    {
        if (_session == null) return;
        _session.FallbacksUsed++;
        Debug.LogWarning($"[StudyLogger] Static fallback used in room {roomNumber}");
    }

    // Research-validity evidence: the literal prompt sent and response
    // received for every successful LLM call this session (room
    // narrative, theory analysis, ending). This is what lets an examiner
    // verify Condition A actually generated different text per
    // participant/room, rather than taking that claim on faith.
    // callType: "room_narrative" | "theory_analysis" | "ending".
    public void LogLLMCall(string callType, int roomNumber, string provider,
        string promptSent, string responseReceived)
    {
        if (_session == null) return;

        _session.LLMCalls ??= new List<LLMCallLog>();
        _session.LLMCalls.Add(new LLMCallLog
        {
            CallType         = callType,
            RoomNumber       = roomNumber,
            Provider         = provider,
            PromptSent       = promptSent,
            ResponseReceived = responseReceived,
            Timestamp        = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }

    // ── Room tracking ──────────────────────────────────────────────────────

    float _roomStartTime;
    RoomLog _currentRoomLog;

    public void OnRoomEntered(int roomNumber, string roomType, Vector3 playerPosition)
    {
        _roomStartTime  = Time.time;
        _currentRoomLog = new RoomLog
        {
            RoomNumber      = roomNumber,
            RoomType        = roomType,
            TimeEnteredSecs = (int)(Time.time - _sessionStartUnityTime),
            CluesExpanded   = new List<string>(),
            PlayerPositionX = playerPosition.x,
            PlayerPositionY = playerPosition.y,
            PlayerPositionZ = playerPosition.z
        };
        Debug.Log($"[StudyLogger] Room {roomNumber} entered");
    }

    public void OnClueExpanded(string clueName)
    {
        if (_currentRoomLog == null) return;
        if (!_currentRoomLog.CluesExpanded.Contains(clueName))
            _currentRoomLog.CluesExpanded.Add(clueName);
    }

    public void OnTheorySubmitted(
        string theoryText,
        int confidenceRating,
        string result,
        float timeToDecideSecs)
    {
        if (_currentRoomLog == null || _session == null) return;

        _currentRoomLog.TimeSubmittedSecs = (int)(Time.time - _sessionStartUnityTime);
        _currentRoomLog.TimeSpentSecs     = (int)(Time.time - _roomStartTime);
        _currentRoomLog.TimeToDecideSecs  = Mathf.RoundToInt(timeToDecideSecs);
        _currentRoomLog.TheoryText        = theoryText;
        _currentRoomLog.ConfidenceRating  = confidenceRating;
        _currentRoomLog.TheoryResult      = result;
        _currentRoomLog.TheoryWordCount   = theoryText.Split(' ').Length;
        _currentRoomLog.SubmittedAt       = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        _session.Rooms.Add(_currentRoomLog);

        if (Time.time - _sessionStartUnityTime > SessionFlagThresholdSecs)
            _session.SessionExceeded22Minutes = true;

        Debug.Log($"[StudyLogger] Theory logged — " +
                  $"Room {_currentRoomLog.RoomNumber}: " +
                  $"{result} (confidence: {confidenceRating}, " +
                  $"time to decide: {_currentRoomLog.TimeToDecideSecs}s)");
    }

    public void OnFinalChoice(string choice, string reason, int timeToDecideSecs)
    {
        if (_session == null) return;

        _session.FinalChoice           = choice;
        _session.FinalChoiceReason     = reason;
        _session.FinalChoiceTimeToDecideSecs = timeToDecideSecs;
        _session.FinalChoiceTimestamp  = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _session.EndTime               = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _session.TotalTimeSecs         = Mathf.RoundToInt(Time.time - _sessionStartUnityTime);
        SaveSession();
    }

    // ── Stats (for SessionEndScreen) ────────────────────────────────────────

    public int   RoomsCompleted      => _session?.Rooms.Count ?? 0;
    public int   TheoriesLogged      => _session?.Rooms.Count ?? 0;
    public int   ApiErrorCount       => _session?.LLMApiErrorCount ?? 0;
    public bool  Exceeded22Minutes   => _session?.SessionExceeded22Minutes ?? false;
    public int   TotalTimeSecs       => _session?.TotalTimeSecs ??
                                         Mathf.RoundToInt(Time.time - _sessionStartUnityTime);
    public string LastSavedFileName  => _lastSavedFileName;
    string _lastSavedFileName = "";

    // ── Save ───────────────────────────────────────────────────────────────

    void SaveSession()
    {
        try
        {
            var export = BuildExport();
            string json = JsonUtility.ToJson(export, true);
            File.WriteAllText(SavePath, json);
            _lastSavedFileName = Path.GetFileName(SavePath);
            Debug.Log($"[StudyLogger] Session saved: {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[StudyLogger] Save failed: {e.Message}");
        }
    }

    public void ForceSave() => SaveSession();

    // Builds the export in the participant's own words + timestamps into
    // the snake_case schema expected by the analysis pipeline.
    SessionExport BuildExport()
    {
        var export = new SessionExport
        {
            session = new SessionInfoExport
            {
                participant_id   = _session.ParticipantID,
                condition        = _session.Condition,
                version          = _session.Version,
                start_time       = _session.StartTime,
                end_time         = _session.EndTime,
                duration_seconds = _session.TotalTimeSecs
            },
            flags = new FlagsExport
            {
                onboarding_completed        = _session.OnboardingCompleted,
                onboarding_duration_secs    = _session.OnboardingDurationSecs,
                session_exceeded_22_minutes = _session.SessionExceeded22Minutes,
                llm_api_errors              = _session.LLMApiErrorCount,
                any_fallback_used           = _session.FallbacksUsed > 0,
                technical_issues            = _session.TechnicalIssues ?? ""
            },
            reliability = new ReliabilityExport
            {
                gemini_failures = _session.GeminiFailures,
                groq_failures   = _session.GroqFailures,
                fallbacks_used  = _session.FallbacksUsed,
                api_errors      = new List<ApiErrorExport>()
            },
            rooms     = new List<RoomExport>(),
            decisions = new List<DecisionExport>(),
            final_choice = new FinalChoiceExport
            {
                choice              = _session.FinalChoice,
                reason              = _session.FinalChoiceReason,
                time_to_decide_secs = _session.FinalChoiceTimeToDecideSecs,
                timestamp           = _session.FinalChoiceTimestamp
            },
            llm_calls = new List<LlmCallExport>()
        };

        foreach (var room in _session.Rooms)
        {
            export.rooms.Add(new RoomExport
            {
                room_number         = room.RoomNumber,
                room_type           = room.RoomType,
                time_entered_secs   = room.TimeEnteredSecs,
                time_submitted_secs = room.TimeSubmittedSecs,
                time_spent_secs     = room.TimeSpentSecs,
                time_to_decide_secs = room.TimeToDecideSecs,
                theory_text         = room.TheoryText,
                theory_word_count   = room.TheoryWordCount,
                confidence_rating   = room.ConfidenceRating,
                theory_result       = room.TheoryResult,
                clues_expanded      = room.CluesExpanded,
                player_position_at_entry = new PlayerPositionExport
                {
                    x = room.PlayerPositionX,
                    y = room.PlayerPositionY,
                    z = room.PlayerPositionZ
                }
            });

            export.decisions.Add(new DecisionExport
            {
                decision_point_id  = $"room_{room.RoomNumber}_theory",
                room_number        = room.RoomNumber,
                theory_submitted   = room.TheoryText,
                confidence_rating  = room.ConfidenceRating,
                time_to_decide_secs = room.TimeToDecideSecs,
                result             = room.TheoryResult,
                timestamp          = room.SubmittedAt
            });
        }

        if (_session.APIErrors != null)
        {
            foreach (var err in _session.APIErrors)
            {
                export.reliability.api_errors.Add(new ApiErrorExport
                {
                    provider    = err.Provider,
                    error       = err.Error,
                    room_number = err.RoomNumber,
                    timestamp   = err.Timestamp
                });
            }
        }

        if (_session.LLMCalls != null)
        {
            foreach (var call in _session.LLMCalls)
            {
                export.llm_calls.Add(new LlmCallExport
                {
                    call_type          = call.CallType,
                    room_number        = call.RoomNumber,
                    provider           = call.Provider,
                    prompt_sent        = call.PromptSent,
                    response_received  = call.ResponseReceived,
                    timestamp          = call.Timestamp
                });
            }
        }

        return export;
    }
}

// ── Internal data structures ─────────────────────────────────────────────────

[Serializable]
public class StudySession
{
    public string         ParticipantID;
    public string         Condition;
    public string         Version;
    public string         StartTime;
    public string         EndTime;
    public int            TotalTimeSecs;
    public string         FinalChoice;
    public string         FinalChoiceReason;
    public int            FinalChoiceTimeToDecideSecs;
    public string         FinalChoiceTimestamp;
    public List<RoomLog>  Rooms;

    public bool   OnboardingCompleted;
    public int    OnboardingDurationSecs;
    public bool   SessionExceeded22Minutes;
    public int    LLMApiErrorCount;
    public string TechnicalIssues;

    public int GeminiFailures;
    public int GroqFailures;
    public int FallbacksUsed;
    public List<APIErrorLog> APIErrors;
    public List<LLMCallLog>  LLMCalls;
}

[Serializable]
public class APIErrorLog
{
    public string Provider;
    public string Error;
    public int    RoomNumber;
    public string Timestamp;
}

[Serializable]
public class LLMCallLog
{
    public string CallType;
    public int    RoomNumber;
    public string Provider;
    public string PromptSent;
    public string ResponseReceived;
    public string Timestamp;
}

[Serializable]
public class RoomLog
{
    public int          RoomNumber;
    public string       RoomType;
    public int          TimeEnteredSecs;
    public int          TimeSubmittedSecs;
    public int          TimeSpentSecs;
    public int          TimeToDecideSecs;
    public string       TheoryText;
    public int          TheoryWordCount;
    public int          ConfidenceRating;
    public string       TheoryResult;
    public List<string> CluesExpanded;
    public string       SubmittedAt;
    public float        PlayerPositionX;
    public float        PlayerPositionY;
    public float        PlayerPositionZ;
}

// ── Export schema (snake_case, matches the study analysis pipeline) ─────────

[Serializable]
public class SessionExport
{
    public SessionInfoExport   session;
    public ReliabilityExport   reliability;
    public FlagsExport         flags;
    public List<RoomExport>    rooms;
    public List<DecisionExport> decisions;
    public FinalChoiceExport   final_choice;
    public List<LlmCallExport> llm_calls;
}

// Research-validity evidence — the literal prompt sent and response
// received for every successful LLM call this session, so contextual
// variation across participants/rooms can be verified directly rather
// than taken on faith.
[Serializable]
public class LlmCallExport
{
    public string call_type;
    public int    room_number;
    public string provider;
    public string prompt_sent;
    public string response_received;
    public string timestamp;
}

[Serializable]
public class ReliabilityExport
{
    public int gemini_failures;
    public int groq_failures;
    public int fallbacks_used;
    public List<ApiErrorExport> api_errors;
}

[Serializable]
public class ApiErrorExport
{
    public string provider;
    public string error;
    public int    room_number;
    public string timestamp;
}

[Serializable]
public class SessionInfoExport
{
    public string participant_id;
    public string condition;
    public string version;
    public string start_time;
    public string end_time;
    public int    duration_seconds;
}

[Serializable]
public class FlagsExport
{
    public bool   onboarding_completed;
    public int    onboarding_duration_secs;
    public bool   session_exceeded_22_minutes;
    public int    llm_api_errors;
    public bool   any_fallback_used;
    public string technical_issues;
}

[Serializable]
public class RoomExport
{
    public int          room_number;
    public string       room_type;
    public int          time_entered_secs;
    public int          time_submitted_secs;
    public int          time_spent_secs;
    public int          time_to_decide_secs;
    public string       theory_text;
    public int          theory_word_count;
    public int          confidence_rating;
    public string       theory_result;
    public List<string> clues_expanded;
    public PlayerPositionExport player_position_at_entry;
}

[Serializable]
public class PlayerPositionExport
{
    public float x;
    public float y;
    public float z;
}

[Serializable]
public class DecisionExport
{
    public string decision_point_id;
    public int    room_number;
    public string theory_submitted;
    public int    confidence_rating;
    public int    time_to_decide_secs;
    public string result;
    public string timestamp;
}

[Serializable]
public class FinalChoiceExport
{
    public string choice;
    public string reason;
    public int    time_to_decide_secs;
    public string timestamp;
}
