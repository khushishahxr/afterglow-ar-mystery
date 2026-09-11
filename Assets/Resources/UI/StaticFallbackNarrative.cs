using System.Collections.Generic;
using UnityEngine;

// Last-resort content used only when BOTH Gemini and Groq fail. Written to
// be atmospheric and self-contained without claiming a specific apocalypse
// cause — since we don't know why the real APIs failed, this content must
// never contradict whatever theme may or may not already be established.
//
// The "Found Xm from where you were standing" distance sentence is added
// uniformly by ClueObject.Reveal() for every clue regardless of source, so
// it is deliberately NOT embedded in this text (that would double it up).
public static class StaticFallbackNarrative
{
    public static NarrativeData GetFallback(int roomNumber)
    {
        NarrativeData data;
        switch (Mathf.Clamp(roomNumber, 1, 6))
        {
            case 1:  data = Room1(); break;
            case 2:  data = Room2(); break;
            case 3:  data = Room3(); break;
            case 4:  data = Room4(); break;
            case 5:  data = Room5(); break;
            default: data = Room6(); break;
        }

        AssignFreshIcons(data);
        return data;
    }

    // Every room's hardcoded content below reuses icons "1"-"5"
    // identically — harmless if this fallback only ever triggers for a
    // single room in a whole session, but if it triggers more than once
    // (or overlaps with icons the real LLM already picked in an earlier
    // room), those repeats defeat the entire point of
    // WorldStateManager's used-icon tracking. Confirmed live (2026-08-21)
    // as a real cause of repeated props across rooms. Reassign fresh
    // icons from whatever's actually left in the pool instead of
    // trusting the hardcoded values baked into each room's Clues list.
    static void AssignFreshIcons(NarrativeData data)
    {
        if (data?.Clues == null) return;

        var remaining = WorldStateManager.Instance != null
            ? WorldStateManager.Instance.GetRemainingIcons(PromptBuilder.EvidenceIcons)
            : new List<string>(PromptBuilder.EvidenceIcons);

        for (int i = 0; i < data.Clues.Count && i < remaining.Count; i++)
            data.Clues[i].EvidenceIcon = remaining[i];
    }

    static NarrativeData Room1() => new NarrativeData
    {
        ApocalypseTheme  = "",
        ThemeDescription = "",
        ChapterTitle     = "The Last Morning",
        StoryIntro       = "Someone was here when it started. They had no warning. Nobody did.",
        RoomDescription  = "The room holds its breath. Everything stopped mid-moment.",
        HintLine         = "If you're hearing this, look for what was left behind on purpose.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType     = "Table",
                ClueText       = "Something left on the {SURFACE}. Mid-use. Not finished.",
                EvidenceDetail = "Whatever was happening here stopped without conclusion. The surface holds the shape of an interrupted life.",
                EvidenceName   = "The Interrupted Moment",
                EvidenceIcon   = "5",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Wall",
                ClueText       = "The {SURFACE} carries a mark. Something was here. Then wasn't.",
                EvidenceDetail = "An absence more than a presence. Whatever this surface witnessed, it did not survive to tell. The silence that followed was total.",
                EvidenceName   = "The Absence",
                EvidenceIcon   = "1",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Floor",
                ClueText       = "The {SURFACE} tells a story of the last path walked here.",
                EvidenceDetail = "Movement stopped abruptly. Whatever direction they were going, they did not complete the journey.",
                EvidenceName   = "The Last Path",
                EvidenceIcon   = "2",
                IsRedHerring   = false
            }
        },
        TruthReveal = "Something ended here suddenly. The person who lived here did not know it was coming. Almost nobody did."
    };

    static NarrativeData Room2() => new NarrativeData
    {
        ApocalypseTheme  = "",
        ThemeDescription = "",
        ChapterTitle     = "What Was Left Behind",
        StoryIntro       = "This room remembers what happened, even if no one else does.",
        RoomDescription  = "Everything here has a story it isn't telling directly.",
        HintLine         = "Don't just look. Listen for what's out of place.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType     = "Table",
                ClueText       = "Something on the {SURFACE} was left mid-task, never returned to.",
                EvidenceDetail = "Whatever this was, it mattered enough to start and then, suddenly, stopped mattering enough to finish. The gap between those two moments is where the story lives.",
                EvidenceName   = "The Unfinished Task",
                EvidenceIcon   = "5",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Wall",
                ClueText       = "A mark on the {SURFACE} suggests something was fixed here, then removed.",
                EvidenceDetail = "The outline is still visible, cleaner than the surface around it. Whatever hung here was taken down deliberately, not lost. Someone decided this space needed to be empty.",
                EvidenceName   = "The Watching Mark",
                EvidenceIcon   = "2",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Floor",
                ClueText       = "The {SURFACE} shows wear in one particular spot, more than anywhere else.",
                EvidenceDetail = "Repetition leaves a signature. Someone stood here, or paced here, far more than the rest of this space would suggest. Whatever drew them back to this exact spot, it happened again and again.",
                EvidenceName   = "The Worn Ground",
                EvidenceIcon   = "3",
                IsRedHerring   = false
            }
        },
        TruthReveal = "Whatever happened here, it happened gradually enough for someone to notice and prepare — and suddenly enough that they never finished preparing."
    };

    static NarrativeData Room3() => new NarrativeData
    {
        ApocalypseTheme  = "",
        ThemeDescription = "",
        ChapterTitle     = "The Careful Hours",
        StoryIntro       = "Someone spent their last ordinary hours here being careful. About what, you don't yet know.",
        RoomDescription  = "Nothing here is where it would normally be. Everything has been considered.",
        HintLine         = "Someone rearranged this room on purpose. Find out why.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType     = "Table",
                ClueText       = "Items on the {SURFACE} are arranged with unusual precision, as if for a reason.",
                EvidenceDetail = "Nothing here is casual. Every object's position looks chosen rather than settled. Someone was organising for something specific, something that required this exact configuration.",
                EvidenceName   = "The Careful Arrangement",
                EvidenceIcon   = "5",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Wall",
                ClueText       = "Something is tucked against the {SURFACE}, easy to miss unless you're looking.",
                EvidenceDetail = "Whoever left this wanted it found, but not by just anyone. It's positioned to be discovered by someone who already knew roughly where to look.",
                EvidenceName   = "The Hidden Note",
                EvidenceIcon   = "2",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Floor",
                ClueText       = "A section of the {SURFACE} has been deliberately cleared, more than practicality would require.",
                EvidenceDetail = "Empty space that used to hold something. The clearing looks purposeful, like preparation for movement, or for something that needed room to happen.",
                EvidenceName   = "The Cleared Space",
                EvidenceIcon   = "3",
                IsRedHerring   = false
            }
        },
        TruthReveal = "Someone here was preparing carefully for something they expected but couldn't fully explain, even to themselves."
    };

    static NarrativeData Room4() => new NarrativeData
    {
        ApocalypseTheme  = "",
        ThemeDescription = "",
        ChapterTitle     = "The Silence After",
        StoryIntro       = "Whatever used to fill this room with sound has gone quiet. The silence has a shape.",
        RoomDescription  = "This was a room built for other people. It doesn't feel that way anymore.",
        HintLine         = "This room was for gathering. Notice who isn't gathered here anymore.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType     = "Table",
                ClueText       = "The {SURFACE} was clearly a place people gathered around, once.",
                EvidenceDetail = "The wear pattern around this surface tells you people stood here together, more than once, for long enough to leave a mark. Whatever gathered them here stopped happening.",
                EvidenceName   = "The Gathering Point",
                EvidenceIcon   = "5",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Wall",
                ClueText       = "Something on the {SURFACE} tracks a sequence — dates, maybe, or a count.",
                EvidenceDetail = "A record of some kind, kept carefully and then abandoned mid-sequence. The marks stop without any indication of why.",
                EvidenceName   = "The Marked Passage",
                EvidenceIcon   = "2",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Floor",
                ClueText       = "Marks on the {SURFACE} suggest a specific direction people moved, repeatedly.",
                EvidenceDetail = "Everyone who passed through here went the same way, toward the same thing. Whatever that destination was, it was important enough to matter every time.",
                EvidenceName   = "The Direction Taken",
                EvidenceIcon   = "3",
                IsRedHerring   = false
            }
        },
        TruthReveal = "This room was a place people came together to understand what was happening — until there was no one left to gather, or nothing left worth gathering for."
    };

    static NarrativeData Room5() => new NarrativeData
    {
        ApocalypseTheme  = "",
        ThemeDescription = "",
        ChapterTitle     = "What They Were Trying to Understand",
        StoryIntro       = "Someone here was working something out, methodically, right up until they weren't.",
        RoomDescription  = "This room belonged to a mind trying to make sense of something too large to hold at once.",
        HintLine         = "The notes in this room were written for someone to find. Maybe you.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType     = "Wall",
                ClueText       = "A sketch on the {SURFACE} tries to map something complicated into something understandable.",
                EvidenceDetail = "Whoever drew this was thinking systematically, not emotionally. This is the work of someone trying to find the shape of a problem, not just survive it.",
                EvidenceName   = "The Working Diagram",
                EvidenceIcon   = "2",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Table",
                ClueText       = "Writing on the {SURFACE} stops mid-thought, unfinished.",
                EvidenceDetail = "The final line trails off exactly where the next idea should begin. Something interrupted this thought before its author could finish it, and they never got the chance to come back to it. In the margin, barely legible, is a code: {PARTICIPANT_ID}. It shouldn't mean anything to you. It does.",
                EvidenceName   = "The Interrupted Notes",
                EvidenceIcon   = "5",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Floor",
                ClueText       = "Something has been removed from beneath the {SURFACE}, leaving only its outline.",
                EvidenceDetail = "Whatever sat here mattered enough to hide, and later, to take. The shape it left behind is the only proof it ever existed.",
                EvidenceName   = "The Missing Piece",
                EvidenceIcon   = "1",
                IsRedHerring   = false
            }
        },
        TruthReveal = "Someone here understood more than almost anyone else did — not the whole picture, but enough to know something specific and important was still missing when they were interrupted."
    };

    static NarrativeData Room6() => new NarrativeData
    {
        ApocalypseTheme  = "",
        ThemeDescription = "",
        ChapterTitle     = "The Room Where It Waits",
        StoryIntro       = "This is the last room. Whatever happens next, it happens here.",
        RoomDescription  = "Something in this room has been waiting, patiently, for a very long time.",
        HintLine         = "Everything ends here. Look closely before you decide what that means.",
        Clues = new List<ClueData>
        {
            new ClueData {
                AnchorType     = "Table",
                ClueText       = "Something on the {SURFACE} is still active, somehow, after everything else has stopped.",
                EvidenceDetail = "This shouldn't still be running. Whatever it's waiting for, it's been ready for a very long time — patient in a way that feels deliberate, not accidental.",
                EvidenceName   = "The Waiting Terminal",
                EvidenceIcon   = "2",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Wall",
                ClueText       = "Something is marked into the {SURFACE}, not written but scratched, urgent.",
                EvidenceDetail = "Whoever left this understood they were running out of time to say it. This is the last thing they wanted known, left the only way they had left to leave it.",
                EvidenceName   = "The Final Words",
                EvidenceIcon   = "5",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Floor",
                ClueText       = "Wear on the {SURFACE} circles a fixed point, over and over.",
                EvidenceDetail = "Someone paced here, around the same spot, for longer than anyone should have to carry a single decision alone. The path never quite finishes — it just stops.",
                EvidenceName   = "The Circling Path",
                EvidenceIcon   = "3",
                IsRedHerring   = false
            },
            new ClueData {
                AnchorType     = "Wall",
                ClueText       = "Something meant to be filled in on the {SURFACE} was never used.",
                EvidenceDetail = "This was prepared for and never needed, or never reached. Either way, it stayed empty, and that emptiness doesn't mean what it might seem to.",
                EvidenceName   = "The Blank Page",
                EvidenceIcon   = "1",
                IsRedHerring   = true
            },
            new ClueData {
                AnchorType     = "Table",
                ClueText       = "A small, mundane object sits on the {SURFACE}, undisturbed.",
                EvidenceDetail = "Not every object in the last room is significant. This one is simply here, the way ordinary things are here right up until the end of anything.",
                EvidenceName   = "The Ordinary Object",
                EvidenceIcon   = "4",
                IsRedHerring   = true
            }
        },
        TruthReveal = "Whatever waits in this room was left as a choice, not an answer — for whoever came next to decide what happens now."
    };

    // ── Theory analysis fallback ─────────────────────────────────────────────

    public static TheoryAnalysis AnalyseTheory(string playerTheory, int roomNumber)
    {
        string trimmed = (playerTheory ?? "").Trim();

        if (trimmed.Length < 5)
        {
            return new TheoryAnalysis
            {
                Result  = "wrong",
                Message = "The truth is here but you have not yet named it. Look at what the objects have in common. What connects them?",
                Hint    = "Think about what ties every clue in this room together."
            };
        }

        if (trimmed.Length < 30)
        {
            return new TheoryAnalysis
            {
                Result  = "partial",
                Message = "Something in your theory carries weight. The room has shown you fragments. The full picture requires the rooms that follow.",
                Hint    = "Keep watching for patterns as you move through the remaining rooms."
            };
        }

        return new TheoryAnalysis
        {
            Result  = "partial",
            Message = "Something in your theory carries weight. The room has shown you fragments, and you've clearly been piecing them together carefully. The full picture requires the rooms that follow.",
            Hint    = "You're on the right track — carry what you've noticed here into the next room."
        };
    }

    // ── Ending fallback ───────────────────────────────────────────────────────

    // Plain-English, one-line explanation of each cause — used only when
    // BOTH Gemini and Groq fail at the ending step. Without this, the old
    // generic ending text never actually named what happened, so a player
    // could finish the whole game and still not know the answer if this
    // last-resort path ever triggered.
    static readonly Dictionary<ApocalypseTheme, string> CauseDescriptions = new()
    {
        { ApocalypseTheme.Virus,        "A virus spread faster than anyone could stop it, and no cure came in time." },
        { ApocalypseTheme.AI,           "A machine built to help people turned against the people who built it." },
        { ApocalypseTheme.War,          "People turned on each other, and the fighting never stopped in time." },
        { ApocalypseTheme.Nature,       "The natural world turned violent and unstoppable, faster than anyone could prepare for." },
        { ApocalypseTheme.Supernatural, "Something no one could explain came for the world, and nothing could stop it." },
        { ApocalypseTheme.Unset,        "something no one fully understood, and no one was ready for." },
    };

    public static EndingData GetEnding(string playerChoice, ApocalypseTheme theme)
    {
        bool restore = playerChoice == "restore";
        string cause = CauseDescriptions.TryGetValue(theme, out var d)
            ? d : CauseDescriptions[ApocalypseTheme.Unset];

        return new EndingData
        {
            EndingTitle = restore ? "What Remains" : "The Final Silence",
            EndingText  = restore
                ? $"Now you know the truth: {cause} That is what ended the world. You have walked through six rooms and read what was left behind. You carry that now. Whatever comes next begins with what you know."
                : $"Now you know the truth: {cause} That is what ended the world. You have seen what the world left behind when it stopped. You choose to let that be the end. Not every story needs a continuation.",
            FinalLine   = restore
                ? "The afterglow of what was lost is the only light left — carry it carefully."
                : "Some silences are answers. This is one of them."
        };
    }

    // ── Anchor narration fallback (used by AnchorNarrator on API failure) ────

    static readonly Dictionary<string, string> AnchorDescriptions = new()
    {
        { "1_Table", "A surface where ordinary life paused and never resumed." },
        { "1_Wall",  "Something came through here once. You cannot see it. You can feel it." },
        { "1_Floor", "The last path taken in this room ended without completion." },

        { "2_Table", "A surface of careful habits. Interrupted." },
        { "2_Wall",  "Evidence was here. Then it wasn't. The absence is louder than any presence." },
        { "2_Floor", "What was disturbed here left traces even time hasn't erased." },

        { "3_Table", "Arranged with a precision that suggests someone was still planning." },
        { "3_Wall",  "A surface someone trusted enough to leave something behind on." },
        { "3_Floor", "Cleared deliberately, for a reason this room no longer explains." },

        { "4_Table", "Where people used to gather before there was no reason left to." },
        { "4_Wall",  "A record kept faithfully, until faithfulness stopped being enough." },
        { "4_Floor", "Worn by footsteps that all led the same direction, once." },

        { "5_Table", "A surface for thinking, used right up until the thinking stopped." },
        { "5_Wall",  "A working theory, half-mapped, left for someone else to finish." },
        { "5_Floor", "Something was kept here, close, and then it wasn't." },

        { "6_Table", "It has been waiting here longer than it should still be able to." },
        { "6_Wall",  "The last words left in this room were the hardest ones to leave." },
        { "6_Floor", "A path worn by someone circling a decision they never got to make." },
    };

    public static string GetAnchorDescription(string anchorType, int roomNumber)
    {
        int room = Mathf.Clamp(roomNumber, 1, 6);
        string key = $"{room}_{anchorType}";
        return AnchorDescriptions.TryGetValue(key, out var text)
            ? text
            : "This surface holds its own quiet story, one nobody was left to tell.";
    }
}
