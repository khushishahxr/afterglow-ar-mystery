using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ── Data structures ────────────────────────────────────────────────────────

[Serializable]
public enum ApocalypseTheme
{
    Unset,
    Virus,
    AI,
    War,
    Nature,
    Supernatural
}

[Serializable]
public class RoomRecord
{
    public int RoomNumber;
    public string RoomType;
    public List<string> DetectedObjects = new();
    public List<string> CluesFound      = new();
    public string PlayerTheory          = "";
    public string AIAnalysis            = "";
    public bool IsComplete              = false;
}

[Serializable]
public class WorldState
{
    public ApocalypseTheme Theme     = ApocalypseTheme.Unset;
    public string ThemeDescription   = "";
    public List<RoomRecord> Rooms    = new();
    public bool AllRoomsComplete     = false;
    public string EndingChoice       = "";

    // Narrative continuity — every room's TruthReveal accumulates here so
    // later rooms' prompts can be explicitly grounded in what's already
    // been established, not just the original theme description.
    public string EstablishedTheme       = "";
    public string EstablishedCause       = "";
    public List<string> EstablishedFacts = new();
    public string LastRoomTruthReveal    = "";

    // Every EvidenceIcon assigned so far, across every room — lets each
    // new room's prompt avoid repeating a physical prop the player has
    // already seen elsewhere in the story (see PromptBuilder.EvidenceIcons
    // for the full pool this is drawn from).
    public List<string> UsedEvidenceIcons = new();

    public RoomRecord CurrentRoom =>
        Rooms.Count > 0 ? Rooms[Rooms.Count - 1] : null;

    public int CompletedRoomCount =>
        Rooms.FindAll(r => r.IsComplete).Count;
}

// ── Manager ────────────────────────────────────────────────────────────────

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance { get; private set; }

    public static event Action<RoomRecord>      OnRoomStarted;
    public static event Action<RoomRecord>      OnRoomComplete;
    public static event Action<WorldState>      OnAllRoomsComplete;
    public static event Action<ApocalypseTheme> OnThemeEstablished;

    public WorldState State { get; private set; } = new();

    static readonly string[] RoomTypes = new[]
    {
        "bedroom",
        "bathroom",
        "kitchen",
        "living room",
        "study",
        "final room"
    };

    static readonly string[] Teasers = new[]
    {
        "They were here when it started. The bed is still made. They never came back.",
        "The water stopped running on day three. Someone kept counting.",
        "The last meal was never finished. Whatever interrupted it — didn't stop.",
        "They gathered here to watch the news. The screen is still on. There is no signal.",
        "Someone knew this was coming. The notes are still here. So is the silence.",
        "This is where it ends. Or where it begins again. That part is up to you."
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Clears all story/room progress — used by the researcher "reset for
    // next participant" flow so a new session starts from a clean state.
    public void ResetAll()
    {
        State = new WorldState();
        Debug.Log("[WorldState] State reset for next participant");
    }

    // ── Room management ────────────────────────────────────────────────────

    public void StartRoom(int roomNumber, List<string> detectedObjects)
    {
        int index = Mathf.Clamp(roomNumber - 1, 0, RoomTypes.Length - 1);

        var room = new RoomRecord
        {
            RoomNumber      = roomNumber,
            RoomType        = RoomTypes[index],
            DetectedObjects = new List<string>(detectedObjects)
        };

        State.Rooms.Add(room);
        Debug.Log($"[WorldState] Started room {roomNumber}: {room.RoomType}");
        OnRoomStarted?.Invoke(room);
    }

    public void AddClueToCurrentRoom(string clueName)
    {
        var room = State.CurrentRoom;
        if (room == null) return;

        if (!room.CluesFound.Contains(clueName))
        {
            room.CluesFound.Add(clueName);
            Debug.Log($"[WorldState] Clue added: {clueName} " +
                      $"({room.CluesFound.Count}/3)");
        }
    }

    public void SetPlayerTheory(string theory)
    {
        var room = State.CurrentRoom;
        if (room == null) return;
        room.PlayerTheory = theory;
        Debug.Log($"[WorldState] Theory recorded for room {room.RoomNumber}");
    }

    public void SetAIAnalysis(string analysis)
    {
        var room = State.CurrentRoom;
        if (room == null) return;
        room.AIAnalysis = analysis;
    }

    public void CompleteCurrentRoom()
    {
        var room = State.CurrentRoom;
        if (room == null) return;

        room.IsComplete = true;
        Debug.Log($"[WorldState] Room {room.RoomNumber} complete");
        OnRoomComplete?.Invoke(room);

        // TODO: SaveSystem.Instance?.SaveProgress(State);
        SaveSystem.Instance?.SaveProgress(State);

        if (State.CompletedRoomCount >= 6)
        {
            State.AllRoomsComplete = true;
            OnAllRoomsComplete?.Invoke(State);
        }
    }

    // ── Theme management ───────────────────────────────────────────────────

    public void SetTheme(ApocalypseTheme theme, string description)
    {
        if (State.Theme != ApocalypseTheme.Unset) return;

        State.Theme            = theme;
        State.ThemeDescription = description;
        State.EstablishedTheme = theme.ToString();
        State.EstablishedCause = description;
        Debug.Log($"[WorldState] Theme: {theme} — {description}");
        OnThemeEstablished?.Invoke(theme);
    }

    // Accumulates each room's internal TruthReveal so later rooms' prompts
    // can be grounded in every fact established so far, not just the
    // original theme description. Safe to call with an empty/null reveal.
    public void RecordEstablishedFact(string truthReveal)
    {
        if (string.IsNullOrEmpty(truthReveal)) return;
        State.EstablishedFacts.Add(truthReveal);
        State.LastRoomTruthReveal = truthReveal;
    }

    // Called once a room's clues are finalised so the next room's prompt
    // can be built against what's actually left in the prop pool.
    public void RecordUsedIcons(IEnumerable<string> icons)
    {
        if (icons == null) return;
        foreach (var icon in icons)
        {
            if (!string.IsNullOrEmpty(icon) && !State.UsedEvidenceIcons.Contains(icon))
                State.UsedEvidenceIcons.Add(icon);
        }
    }

    // The subset of the full prop pool not yet used in any previous room
    // this session. Falls back to the full pool if everything has somehow
    // already been used (should only happen after room 6, if at all).
    public List<string> GetRemainingIcons(IReadOnlyList<string> fullPool)
    {
        var remaining = new List<string>();
        foreach (var icon in fullPool)
            if (!State.UsedEvidenceIcons.Contains(icon))
                remaining.Add(icon);

        return remaining.Count > 0 ? remaining : new List<string>(fullPool);
    }

    // ── Ending ─────────────────────────────────────────────────────────────

    public void SetEndingChoice(string choice)
    {
        State.EndingChoice = choice;
        SaveSystem.Instance?.SaveProgress(State);
        Debug.Log($"[WorldStateManager] Ending choice saved: {choice}");
    }

    // ── Summary for prompt ─────────────────────────────────────────────────

    public string BuildPreviousRoomsSummary()
    {
        if (State.Rooms.Count == 0) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("PREVIOUS ROOMS INVESTIGATED:");
        sb.AppendLine();

        foreach (var room in State.Rooms)
        {
            if (!room.IsComplete) continue;
            sb.AppendLine($"Room {room.RoomNumber} — {room.RoomType}");
            sb.AppendLine($"Clues found: {string.Join(", ", room.CluesFound)}");
            if (!string.IsNullOrEmpty(room.PlayerTheory))
                sb.AppendLine($"Player's theory: {room.PlayerTheory}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    public string GetTeaser(int roomNumber)
    {
        int index = Mathf.Clamp(roomNumber - 1, 0, Teasers.Length - 1);
        return Teasers[index];
    }

    public string GetRoomType(int roomNumber)
    {
        int index = Mathf.Clamp(roomNumber - 1, 0, RoomTypes.Length - 1);
        return RoomTypes[index];
    }

    public bool HasTheme       => State.Theme != ApocalypseTheme.Unset;
    public int CurrentRoomNumber => State.Rooms.Count > 0 ? State.Rooms.Count : 1;
}