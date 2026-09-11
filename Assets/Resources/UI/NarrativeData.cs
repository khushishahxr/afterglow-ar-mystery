using System;
using System.Collections.Generic;

[Serializable]
public class NarrativeData
{
    public string         ApocalypseTheme;
    public string         ThemeDescription;
    public string         ChapterTitle;
    public string         StoryIntro;
    public string         RoomDescription;
    public List<ClueData> Clues;
    public string         TruthReveal;

    // A short, room-specific, character-voiced line framed as a found
    // recording — replaces the old generic idle-nudge banner text
    // ("Move slowly through the room..."). Read aloud via TTS when
    // available (see DiegeticTTSManager), shown as text either way.
    public string HintLine;
}

[Serializable]
public class ClueData
{
    public string AnchorType;
    public string ClueText;
    public string EvidenceDetail;
    public string EvidenceName;
    public string EvidenceIcon;
    public bool   IsRedHerring;

    // The player-detective's own first-person in-the-moment thought about
    // this clue — not narration, not a character's voice. Distinct from
    // EvidenceDetail (the "detective notes" reference text shown later in
    // the Case File). One deduction-voiced sentence, shown ~0.3s after
    // ClueText finishes revealing.
    public string ReactionLine;
}

[Serializable]
public class TheoryAnalysis
{
    public string Result;
    public string Message;
    public string Hint;
}

[Serializable]
public class EndingData
{
    public string EndingTitle;
    public string EndingText;
    public string FinalLine;
}