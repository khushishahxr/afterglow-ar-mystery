using System.Collections.Generic;
using System.Text;

public static class PromptBuilder
{
    // The full generic evidence-prop pool — theme- and surface-agnostic on
    // purpose (see ClueSpawner.cs), so the LLM can tie any of these to
    // whatever real Table/Wall/Floor the player's own room has. Exactly 30
    // entries: rooms 1-5 use 3 each (15) plus Room 6's single reserved
    // envelope, so a full playthrough never repeats a prop even though it
    // only draws 16 of the 30 available (see WorldStateManager.GetRemainingIcons).
    // Plain numeric IDs rather than emoji — emoji keys were an unreliable
    // match key for LLM output (encoding/variant-selector drift could
    // silently break the dictionary lookup in ClueSpawner, falling back to
    // DefaultPrefab for every clue). Numbers are unambiguous string
    // equality. 1=Radio, 2=Ledger, 3=FramedPhoto, 4=SealedLetter, 5=Book,
    // 6=Plate, 7=BrokenMug, 8=HandwrittenNote, 9=PackedBag, 10=DocumentStack,
    // 11=Photograph, 12=SpoiledFood, 13=ChildToy, 14=Compass,
    // 15=CrackedMirror, 16=FirstAidKit, 17=Glasses, 18=HandBell, 19=Keys,
    // 20=Map, 21=Padlock, 22=Ring, 23=Rope, 24=Shoes, 25=Toolbox,
    // 26=Umbrella, 27=Wallet, 28=Watch, 29=WiltedPlant, 30=Candle.
    public static readonly string[] EvidenceIcons =
    {
        "1",  "2",  "3",  "4",  "5",  "6",  "7",  "8",  "9",  "10",
        "11", "12", "13", "14", "15", "16", "17", "18", "19", "20",
        "21", "22", "23", "24", "25", "26", "27", "28", "29", "30"
    };

    // What each ID actually is — the LLM never sees a bare number list, it
    // sees these names too, so ClueText/EvidenceName can plausibly match
    // whatever physical prop will actually spawn. Without this the model
    // has zero signal for which object "9" is and will happily write a
    // clue about a torn photograph that then spawns as a packed bag.
    static readonly Dictionary<string, string> IconNames = new()
    {
        { "1",  "Radio" },        { "2",  "Ledger" },       { "3",  "Framed Photo" },
        { "4",  "Sealed Letter" },{ "5",  "Book" },         { "6",  "Plate" },
        { "7",  "Broken Mug" },   { "8",  "Handwritten Note" }, { "9",  "Packed Bag" },
        { "10", "Document Stack" }, { "11", "Photograph" }, { "12", "Spoiled Food" },
        { "13", "Child's Toy" },  { "14", "Compass" },      { "15", "Cracked Mirror" },
        { "16", "First Aid Kit" },{ "17", "Glasses" },      { "18", "Hand Bell" },
        { "19", "Keys" },         { "20", "Map" },          { "21", "Padlock" },
        { "22", "Ring" },         { "23", "Rope" },         { "24", "Shoes" },
        { "25", "Toolbox" },      { "26", "Umbrella" },     { "27", "Wallet" },
        { "28", "Watch" },        { "29", "Wilted Plant" }, { "30", "Candle" },
    };

    public static string BuildMysteryPrompt(
        List<string> detectedObjects,
        int roomNumber,
        string roomType,
        WorldState worldState)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are the narrative engine for AFTERGLOW.");
        sb.AppendLine("An AR mystery game where the player investigates");
        sb.AppendLine("real physical rooms to discover why the world ended.");
        sb.AppendLine("The story is built entirely from the player's");
        sb.AppendLine("real surroundings — make every clue feel tied to");
        sb.AppendLine("the actual objects detected in their space.");
        sb.AppendLine();
        sb.AppendLine("DIFFICULTY: EASY");
        sb.AppendLine("- Early rooms hint more than confirm — see the clarity");
        sb.AppendLine("  escalation below for exactly how direct to be in this");
        sb.AppendLine("  specific room; it overrides this general guidance");
        sb.AppendLine("- The player must connect multiple clues to form a theory,");
        sb.AppendLine("  but each individual clue must be easy to understand on");
        sb.AppendLine("  its own — the challenge is piecing clues together, not");
        sb.AppendLine("  decoding what any single clue says");
        sb.AppendLine("- Every clue is genuine — none are misleading on purpose");
        sb.AppendLine("- Each clue adds one small piece, not the whole picture");
        sb.AppendLine();
        sb.AppendLine("LANGUAGE — CRITICAL, EASY MODE:");
        sb.AppendLine("Write for a general audience, not a literary one. Use");
        sb.AppendLine("short, plain, everyday words. NO rare or fancy vocabulary,");
        sb.AppendLine("NO archaic or overly poetic phrasing, NO words a typical");
        sb.AppendLine("adult would need to look up. Short sentences. One idea per");
        sb.AppendLine("sentence. It's fine for the mood to still feel eerie or");
        sb.AppendLine("sad — just say it in simple, direct language instead of");
        sb.AppendLine("ornate language. If you catch yourself reaching for an");
        sb.AppendLine("unusual word, replace it with the plainest word that means");
        sb.AppendLine("the same thing.");
        sb.AppendLine();

        if (worldState.Theme == ApocalypseTheme.Unset)
        {
            sb.AppendLine("ROOM 1 — ESTABLISH ATMOSPHERE ONLY:");
            sb.AppendLine("This is the first room. The player has just arrived.");
            sb.AppendLine("DO NOT reveal why the world ended.");
            sb.AppendLine("DO NOT name the apocalypse cause.");
            sb.AppendLine("DO NOT give away the full story.");
            sb.AppendLine("Room 1 should only establish:");
            sb.AppendLine("  - Something happened here suddenly");
            sb.AppendLine("  - The person who lived here left in an unusual way");
            sb.AppendLine("  - There is something wrong the player cannot yet name");
            sb.AppendLine("The player should finish Room 1 with a feeling,");
            sb.AppendLine("not an answer. The cause only emerges across 6 rooms.");
            sb.AppendLine();
            sb.AppendLine("Still choose an ApocalypseTheme internally for");
            sb.AppendLine("future room consistency — but do NOT reference it");
            sb.AppendLine("in any Room 1 clue text.");
        }
        else
        {
            int roomNum = worldState.Rooms.Count + 1;

            sb.AppendLine("ESTABLISHED APOCALYPSE:");
            sb.AppendLine($"Internal cause: {worldState.Theme}");
            sb.AppendLine($"Details: {worldState.ThemeDescription}");
            sb.AppendLine();
            sb.AppendLine("CONTINUITY IS CRITICAL:");
            sb.AppendLine("This story is being told one room at a time across");
            sb.AppendLine("6 real physical rooms, in order. Every room must stay");
            sb.AppendLine("strictly consistent with the internal cause above and");
            sb.AppendLine("with everything the player has already found — do NOT");
            sb.AppendLine("contradict it, rename it, or introduce a different");
            sb.AppendLine("cause. Treat the details above as fixed canon.");

            if (worldState.EstablishedFacts != null && worldState.EstablishedFacts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("ESTABLISHED STORY FACTS:");
                sb.AppendLine("These facts were established in previous rooms and");
                sb.AppendLine("MUST be consistent with what you generate:");
                foreach (var fact in worldState.EstablishedFacts)
                    sb.AppendLine($"  - {fact}");
                sb.AppendLine();
                sb.AppendLine("DO NOT contradict these facts. DO NOT introduce a");
                sb.AppendLine("different apocalypse cause. Build on what is established.");
            }

            sb.AppendLine();
            sb.AppendLine($"This is Room {roomNum} of 6.");
            sb.AppendLine("Add ONE new piece of the puzzle this room.");
            sb.AppendLine("Reference what came before — but deepen it,");
            sb.AppendLine("don't repeat it.");

            // Escalating clarity — a mystery that stays equally vague for
            // all 6 rooms isn't solvable in a 15-20 minute session; the
            // player needs to feel the fog clearing, not just accumulate
            // more of it. Confirmed via live playtesting (2026-08-17) that
            // uniform obscurity across every room left the story
            // impossible to follow even for the developer.
            sb.AppendLine();
            sb.AppendLine("CLARITY ESCALATION — HOW DIRECT TO BE THIS ROOM:");
            if (roomNum <= 2)
            {
                sb.AppendLine("Still mostly atmospheric — hint, don't confirm.");
                sb.AppendLine("The player should be forming a hypothesis here,");
                sb.AppendLine("not receiving an answer. Even so, the hint itself");
                sb.AppendLine("must be clear and concrete in plain words — vague");
                sb.AppendLine("atmosphere is fine, vague WRITING is not. A player");
                sb.AppendLine("re-reading this clue should be able to say exactly");
                sb.AppendLine("what it points toward, even if they're not certain yet.");
            }
            else if (roomNum <= 4)
            {
                sb.AppendLine("Noticeably more direct than earlier rooms. At");
                sb.AppendLine("least one clue this room MUST gesture at the cause");
                sb.AppendLine("almost openly — not naming the theme word itself,");
                sb.AppendLine("but describing plainly enough that an attentive");
                sb.AppendLine("player can state in their own words what kind of");
                sb.AppendLine("thing happened. Do not bury this in metaphor — say");
                sb.AppendLine("it plainly, in easy words, even while staying in-world.");
            }
            else
            {
                sb.AppendLine("This is a late room — at least one clue MUST let the");
                sb.AppendLine("player state the cause with real confidence after");
                sb.AppendLine("this room. Describe plainly, in easy words, what kind");
                sb.AppendLine("of event happened and roughly how — the exact theme");
                sb.AppendLine("word can wait for the ending, but the player should");
                sb.AppendLine("not be guessing blind. If in doubt, be MORE direct,");
                sb.AppendLine("not less — a mystery only the developer can solve is");
                sb.AppendLine("a failure, not a puzzle.");
            }

            if (roomNum >= 6)
            {
                sb.AppendLine();
                sb.AppendLine("THIS IS THE FINAL ROOM:");
                sb.AppendLine("Bring every previous thread to a head. The player is");
                sb.AppendLine("about to decide humanity's fate based on everything");
                sb.AppendLine("found across all 6 rooms. This room's clues should");
                sb.AppendLine("feel like the last piece falling into place, not a");
                sb.AppendLine("new direction — resolve tension, don't add more of it.");
            }

            if (roomNum == 5)
            {
                sb.AppendLine();
                sb.AppendLine("ROOM 5 SPECIAL BEAT:");
                sb.AppendLine("Exactly one of the 3 clues must include a moment");
                sb.AppendLine("where the survivor finds a written code that turns out");
                sb.AppendLine("to be a visitor identifier, addressed directly to them —");
                sb.AppendLine("proof they were never a random survivor. In that clue's");
                sb.AppendLine("ClueText or EvidenceDetail, include the literal placeholder");
                sb.AppendLine("text {PARTICIPANT_ID} exactly as written (do NOT invent");
                sb.AppendLine("your own ID — it will be substituted with the real");
                sb.AppendLine("player's ID before display). Make it feel like this room");
                sb.AppendLine("was always meant to be found by exactly this person.");
            }
        }

        string prev =
            WorldStateManager.Instance?.BuildPreviousRoomsSummary() ?? "";
        if (!string.IsNullOrEmpty(prev))
        {
            sb.AppendLine();
            sb.AppendLine("WHAT THE PLAYER HAS FOUND SO FAR:");
            sb.AppendLine(prev);

            string lastTheory = WorldStateManager.Instance?.State?.CurrentRoom?.PlayerTheory;
            if (string.IsNullOrEmpty(lastTheory))
            {
                var rooms = WorldStateManager.Instance?.State?.Rooms;
                if (rooms != null)
                    for (int i = rooms.Count - 1; i >= 0; i--)
                        if (!string.IsNullOrEmpty(rooms[i].PlayerTheory))
                        {
                            lastTheory = rooms[i].PlayerTheory;
                            break;
                        }
            }

            if (!string.IsNullOrEmpty(lastTheory))
            {
                sb.AppendLine();
                sb.AppendLine("REQUIRED — ACKNOWLEDGE THEIR LAST GUESS:");
                sb.AppendLine("The player's most recent theory, in their own words, was:");
                sb.AppendLine($"  \"{lastTheory}\"");
                sb.AppendLine("Your StoryIntro or RoomDescription for THIS room MUST");
                sb.AppendLine("visibly react to that specific guess — confirm it,");
                sb.AppendLine("complicate it, or quietly contradict it. Do not ignore");
                sb.AppendLine("it and open with something generic. The player should");
                sb.AppendLine("immediately feel that what they guessed last room");
                sb.AppendLine("mattered, even if you don't repeat their exact words.");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("REQUIRED — CONTINUE THE SAME STORY:");
                sb.AppendLine("StoryIntro's FIRST sentence MUST pick up directly from");
                sb.AppendLine("\"WHAT THE PLAYER HAS FOUND SO FAR\" above — do not open");
                sb.AppendLine("this room as if it were a fresh, unrelated scene. Name");
                sb.AppendLine("or clearly reference the specific thread this room is");
                sb.AppendLine("continuing before introducing anything new, so the");
                sb.AppendLine("player instantly feels this is room after room of the");
                sb.AppendLine("same unbroken investigation, not six separate stories.");
            }
        }

        // Cross-room callback — without this, evidence found in earlier
        // rooms had no narrative payoff, so it read as inert set dressing
        // rather than something worth remembering.
        var priorClueNames = new List<string>();
        foreach (var r in worldState.Rooms)
            if (r.IsComplete)
                priorClueNames.AddRange(r.CluesFound);

        if (priorClueNames.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("CALLBACK REQUIREMENT — CONTINUITY OF EVIDENCE:");
            sb.AppendLine("The player has already found these specific pieces of");
            sb.AppendLine("evidence in earlier rooms:");
            sb.AppendLine($"  {string.Join(", ", priorClueNames)}");
            sb.AppendLine("At least ONE clue in THIS room (in its ClueText or");
            sb.AppendLine("EvidenceDetail) MUST explicitly name-check ONE of those");
            sb.AppendLine("earlier items and reveal — briefly — why it matters in");
            sb.AppendLine("light of what's being found here. This is the moment the");
            sb.AppendLine("player realises something they found earlier was");
            sb.AppendLine("important all along. Pick whichever earlier item fits");
            sb.AppendLine("this room's story best; don't force an unnatural link.");
        }

        sb.AppendLine();
        sb.AppendLine($"CURRENT ROOM: {roomType}");
        sb.AppendLine();
        sb.AppendLine("REAL OBJECTS DETECTED IN THIS PLAYER'S ACTUAL ROOM:");
        sb.AppendLine("(physical objects in their real space right now)");
        foreach (var obj in detectedObjects)
            sb.AppendLine($"  - {obj}");
        sb.AppendLine();
        sb.AppendLine("CRITICAL — MAKE IT PERSONAL:");
        sb.AppendLine("Every single clue MUST be tied to one of the");
        sb.AppendLine("detected objects above. The player should look at");
        sb.AppendLine("their real table, their real wall, their real floor");
        sb.AppendLine("and feel the story is happening in THEIR space.");
        sb.AppendLine("Use specific details about how that object looks,");
        sb.AppendLine("where it sits, what state it is in. Make it theirs.");
        sb.AppendLine();
        sb.AppendLine("IF AN IMAGE IS ATTACHED to this request, it is a live");
        sb.AppendLine("photo of the player's actual room right now — look at");
        sb.AppendLine("it and reference concrete, specific visual details you");
        sb.AppendLine("can actually see (colour, clutter, lighting, texture,");
        sb.AppendLine("what's actually sitting on the surface) instead of");
        sb.AppendLine("only naming the generic surface type. This is what");
        sb.AppendLine("makes the story feel like it was written for THIS");
        sb.AppendLine("specific room, not a generic one. If no image is");
        sb.AppendLine("attached, fall back to the detected-object list above.");
        sb.AppendLine();
        sb.AppendLine("GENERATE EXACTLY 3 CLUES, ALL REAL — NO RED HERRING:");
        sb.AppendLine("- Every clue hints at the true cause (see the clarity");
        sb.AppendLine("  escalation above for how direct to be this room)");
        sb.AppendLine("- No fake/misleading clues — every IsRedHerring MUST be false");
        sb.AppendLine("  (removed the red herring entirely — confirmed via");
        sb.AppendLine("  playtesting that any deliberately-wrong clue read as");
        sb.AppendLine("  noise rather than a puzzle in a 3-clue room)");
        sb.AppendLine();
        List<string> remainingIcons = WorldStateManager.Instance != null
            ? WorldStateManager.Instance.GetRemainingIcons(EvidenceIcons)
            : new List<string>(EvidenceIcons);

        // Icon "4" (Sealed Letter) is reserved exclusively for Room 6's
        // single envelope clue — never offered to any other room's pool,
        // so it can never appear early and never repeat once Room 6 uses
        // it (Room 6's own generation doesn't go through this path at
        // all — see NarrativeGenerator, which force-overrides Room 6 to
        // a single icon-"4" clue regardless of what gets generated here).
        if (roomNumber != 6)
            remainingIcons.RemoveAll(id => id == "4");

        // Pre-lock one icon per clue slot in CODE rather than letting the
        // model choose freely — a free choice is only as reliable as the
        // model's adherence to a text instruction, which is exactly what
        // was slipping (props coming out unrelated to the clue text).
        // Locking the assignment up front makes a mismatch structurally
        // impossible: the model is just describing an object it's told,
        // not simultaneously picking which object to describe.
        var shuffled = new List<string>(remainingIcons);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        string[] fallbackIcons = { "1", "2", "3" };
        string slotIcon1 = shuffled.Count > 0 ? shuffled[0] : fallbackIcons[0];
        string slotIcon2 = shuffled.Count > 1 ? shuffled[1] : fallbackIcons[1];
        string slotIcon3 = shuffled.Count > 2 ? shuffled[2] : fallbackIcons[2];

        sb.AppendLine("EVIDENCE ASSIGNMENT — LOCKED, NOT YOUR CHOICE:");
        sb.AppendLine("Each of the 3 clues below is already locked to a specific");
        sb.AppendLine("physical prop that WILL spawn in the player's room. You are");
        sb.AppendLine("NOT choosing which object each clue is about — that part is");
        sb.AppendLine("decided already. Your only job for EvidenceIcon is to copy");
        sb.AppendLine("the exact ID given below for that slot, and write ClueText/");
        sb.AppendLine("EvidenceDetail/EvidenceName that are genuinely, specifically");
        sb.AppendLine("about that exact object — not a generic description, not a");
        sb.AppendLine("different object, not the AnchorType surface it sits on.");
        sb.AppendLine($"  Clue 1 evidence (use EvidenceIcon \"{slotIcon1}\" exactly): {IconNames[slotIcon1]}");
        sb.AppendLine($"  Clue 2 evidence (use EvidenceIcon \"{slotIcon2}\" exactly): {IconNames[slotIcon2]}");
        sb.AppendLine($"  Clue 3 evidence (use EvidenceIcon \"{slotIcon3}\" exactly): {IconNames[slotIcon3]}");
        sb.AppendLine("Return EvidenceIcon as the bare digit string only (e.g. \"9\",");
        sb.AppendLine("not \"9=Packed Bag\").");
        sb.AppendLine();
        sb.AppendLine("DO NOT CONFUSE AnchorType WITH EvidenceIcon:");
        sb.AppendLine("AnchorType is only WHERE the evidence sits in the room (a");
        sb.AppendLine("surface from the detected list — a table, a wall, the floor).");
        sb.AppendLine("EvidenceIcon (locked above) is WHAT the evidence actually IS —");
        sb.AppendLine("the physical prop that gets spawned on top of that surface.");
        sb.AppendLine("ClueText and EvidenceDetail must describe the EvidenceIcon");
        sb.AppendLine("object, never the AnchorType surface. Example: AnchorType=");
        sb.AppendLine("\"table\", EvidenceIcon=\"9=Packed Bag\" → ClueText is about");
        sb.AppendLine("the bag sitting on the table, not about the table itself.");
        sb.AppendLine();
        sb.AppendLine("TEXT LENGTH — CRITICAL:");
        sb.AppendLine("Working-memory limits mean walls of text hurt this");
        sb.AppendLine("experience — every field below has a hard maximum word");
        sb.AppendLine("count. Stay under it. Short, dense, atmospheric beats,");
        sb.AppendLine("not paragraphs.");
        sb.AppendLine();
        sb.AppendLine("REACTION LINE — CRITICAL:");
        sb.AppendLine("ReactionLine is the PLAYER-DETECTIVE'S own first-person");
        sb.AppendLine("in-the-moment thought about this specific clue — not");
        sb.AppendLine("narration, not the voice of whoever lived here, not a");
        sb.AppendLine("restatement of ClueText. It should read as a private deduction, the");
        sb.AppendLine("kind of thing someone thinks but doesn't say out loud.");
        sb.AppendLine("One sentence. Example tone: \"Not hers. Draped, not hung");
        sb.AppendLine("— like someone meant to come back for it.\" Avoid simply");
        sb.AppendLine("describing what the object looks like — that's what");
        sb.AppendLine("ClueText already does.");
        sb.AppendLine();
        sb.AppendLine("TRUTH REVEAL — CRITICAL, THIS IS PLAYER-FACING:");
        sb.AppendLine("TruthReveal is NOT a private dev note — it is shown");
        sb.AppendLine("directly to the player in the \"What You've Learned\"");
        sb.AppendLine("dossier, as this room's confirmed takeaway, and it also");
        sb.AppendLine("feeds the next room's continuity. Write it as real,");
        sb.AppendLine("satisfying prose the player will actually read — 2-3");
        sb.AppendLine("full sentences, MAX 50 WORDS, plain easy language. Do NOT");
        sb.AppendLine("write a terse internal summary — write something worth");
        sb.AppendLine("reading that clearly states what this room confirmed.");
        sb.AppendLine();
        sb.AppendLine("HINT LINE — CRITICAL:");
        sb.AppendLine("HintLine is a single short line framed as something the");
        sb.AppendLine("player found recorded in this room — a diegetic nudge,");
        sb.AppendLine("not a UI tooltip. It should sound like it was actually");
        sb.AppendLine("said or written by whoever was last here, in character");
        sb.AppendLine("for this room and theme.");
        if (roomNumber <= 2)
            sb.AppendLine("Gently point toward investigating further without giving anything away.");
        else
            sb.AppendLine("This is a later room — point much more directly at the cause than an early-room hint would.");
        sb.AppendLine("MAX 20 WORDS. One sentence.");
        sb.AppendLine();
        sb.AppendLine("Respond with ONLY this JSON:");
        sb.AppendLine("{");

        if (worldState.Theme == ApocalypseTheme.Unset)
        {
            sb.AppendLine("  \"ApocalypseTheme\": \"Virus OR AI OR War OR Nature OR Supernatural\",");
            sb.AppendLine("  \"ThemeDescription\": \"internal sentence only — will NOT appear in Room 1\",");
        }

        sb.AppendLine("  \"ChapterTitle\": \"short haunting title, max 6 words\",");
        sb.AppendLine("  \"StoryIntro\": \"2 sentences, MAX 40 WORDS — atmosphere only, no answers\",");
        sb.AppendLine("  \"RoomDescription\": \"2 sentences, MAX 40 WORDS — what the survivor senses\",");
        sb.AppendLine("  \"HintLine\": \"ONE sentence, MAX 20 WORDS — a found recording, in character\",");
        sb.AppendLine("  \"Clues\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"AnchorType\": \"exact object name from detected list\",");
        sb.AppendLine("      \"ClueText\": \"2-3 sentences, MAX 60 WORDS. Hints at true cause. Must describe the locked EvidenceIcon object for this slot, NOT the AnchorType surface.\",");
        sb.AppendLine("      \"EvidenceDetail\": \"MAX 80 WORDS. Deep detective note. More specific.\",");
        sb.AppendLine("      \"EvidenceName\": \"short name for this evidence\",");
        sb.AppendLine($"      \"EvidenceIcon\": \"{slotIcon1}\" (locked — see assignment above, do not change),");
        sb.AppendLine("      \"ReactionLine\": \"ONE sentence, the detective's own in-the-moment thought — a deduction, not a description\",");
        sb.AppendLine("      \"IsRedHerring\": false");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"AnchorType\": \"exact object from detected list\",");
        sb.AppendLine("      \"ClueText\": \"2-3 sentences, MAX 60 WORDS. Hints at true cause. Describes the locked EvidenceIcon object for this slot, not the AnchorType surface.\",");
        sb.AppendLine("      \"EvidenceDetail\": \"MAX 80 WORDS. Deeper detective note.\",");
        sb.AppendLine("      \"EvidenceName\": \"short name\",");
        sb.AppendLine($"      \"EvidenceIcon\": \"{slotIcon2}\" (locked — see assignment above, do not change),");
        sb.AppendLine("      \"ReactionLine\": \"ONE sentence, the detective's own in-the-moment thought\",");
        sb.AppendLine("      \"IsRedHerring\": false");
        sb.AppendLine("    },");
        sb.AppendLine("    {");
        sb.AppendLine("      \"AnchorType\": \"exact object from detected list\",");
        sb.AppendLine("      \"ClueText\": \"2-3 sentences, MAX 60 WORDS. Hints at true cause. Describes the locked EvidenceIcon object for this slot, not the AnchorType surface.\",");
        sb.AppendLine("      \"EvidenceDetail\": \"MAX 80 WORDS. Deeper detective note.\",");
        sb.AppendLine("      \"EvidenceName\": \"short name\",");
        sb.AppendLine($"      \"EvidenceIcon\": \"{slotIcon3}\" (locked — see assignment above, do not change),");
        sb.AppendLine("      \"ReactionLine\": \"ONE sentence, the detective's own in-the-moment thought\",");
        sb.AppendLine("      \"IsRedHerring\": false");
        sb.AppendLine("    }");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"TruthReveal\": \"2-3 full sentences, MAX 50 WORDS — shown to the player, what this room truly confirms\"");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string BuildTheoryAnalysisPrompt(
        string playerTheory,
        string truthReveal,
        List<string> cluesFound,
        int roomNumber)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are analysing a theory in AFTERGLOW.");
        sb.AppendLine("The player investigates why the world ended.");
        sb.AppendLine();
        sb.AppendLine("DIFFICULTY: EASY");
        sb.AppendLine("- Atmospheric, never clinical");
        sb.AppendLine("- Wrong: haunting not harsh");
        sb.AppendLine("- Partial: acknowledge what they sensed");
        sb.AppendLine("- Correct: validate with weight and gravity");
        sb.AppendLine("- LANGUAGE: plain, easy, everyday words only — no rare or");
        sb.AppendLine("  literary vocabulary, short direct sentences, even while");
        sb.AppendLine("  keeping the atmospheric tone");
        if (roomNumber <= 1)
        {
            sb.AppendLine("- Room 1 theories should only be judged on whether");
            sb.AppendLine("  the player sensed something was wrong, not whether");
            sb.AppendLine("  they named the full cause. Do not name the cause");
            sb.AppendLine("  even if they happen to be right.");
        }
        else
        {
            // Confirmed via playtesting (2026-08-17) that a blanket "never
            // name the cause" rule made even a fully correct guess feel
            // unrewarded — the player had no way to know they were right
            // until the very last room, which made the whole mystery feel
            // unsolvable rather than hard.
            sb.AppendLine("- From Room 2 onward: if Result is \"correct\", you");
            sb.AppendLine("  MUST actually confirm it — name the general cause");
            sb.AppendLine("  category (e.g. \"it wasn't a virus — it was the");
            sb.AppendLine("  network\") so the player gets a real payoff for");
            sb.AppendLine("  being right. Do not stay vague just for atmosphere");
            sb.AppendLine("  once they've actually guessed correctly. If Result");
            sb.AppendLine("  is \"partial\" or \"wrong\", keep it cryptic as before.");
        }
        sb.AppendLine();
        sb.AppendLine($"ROOM {roomNumber} — what actually happened:");
        sb.AppendLine(truthReveal);
        sb.AppendLine();
        sb.AppendLine("EVIDENCE THE PLAYER FOUND:");
        foreach (var clue in cluesFound)
            sb.AppendLine($"  - {clue}");
        sb.AppendLine();
        sb.AppendLine("THE PLAYER'S THEORY:");
        sb.AppendLine(playerTheory);
        sb.AppendLine();
        sb.AppendLine("CRITICAL: Your response MUST reference specific words");
        sb.AppendLine("or phrases from the player's theory. Quote 2-4 words");
        sb.AppendLine("from what they wrote and respond to them directly.");
        sb.AppendLine("If they wrote 'signal' use that word back. If they wrote");
        sb.AppendLine("'government' address that directly. Make the player feel");
        sb.AppendLine("you READ what they wrote.");
        sb.AppendLine();
        sb.AppendLine("TEXT LENGTH — CRITICAL: keep every field short and dense,");
        sb.AppendLine("not a paragraph. Working-memory limits matter here.");
        sb.AppendLine();
        sb.AppendLine("Respond with ONLY this JSON:");
        sb.AppendLine("{");
        sb.AppendLine("  \"Result\": \"correct OR partial OR wrong\",");
        sb.AppendLine("  \"Message\": \"2-3 sentences, MAX 60 WORDS. Cold intimate post-apocalyptic tone.\",");
        sb.AppendLine("  \"Hint\": \"if partial or wrong: one cryptic sentence, MAX 25 WORDS, pointing toward truth. If correct: empty string.\"");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public static string BuildEndingPrompt(
        WorldState state,
        string playerChoice)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are writing the final chapter of AFTERGLOW.");
        sb.AppendLine("The survivor investigated 6 real rooms.");
        sb.AppendLine("They now know the full truth.");
        sb.AppendLine();
        sb.AppendLine($"THE APOCALYPSE: {state.Theme}");
        sb.AppendLine($"What happened: {state.ThemeDescription}");
        sb.AppendLine();
        sb.AppendLine("THE SURVIVOR'S JOURNEY:");
        foreach (var room in state.Rooms)
        {
            if (!string.IsNullOrEmpty(room.PlayerTheory))
            {
                sb.AppendLine($"Room {room.RoomNumber} ({room.RoomType}):");
                sb.AppendLine(
                    $"  Found: {string.Join(", ", room.CluesFound)}");
                sb.AppendLine($"  Theory: {room.PlayerTheory}");
            }
        }
        sb.AppendLine();
        sb.AppendLine($"FINAL CHOICE: {playerChoice}");
        if (playerChoice == "restore")
            sb.AppendLine("The survivor chose to RESTORE HUMANITY.");
        else
            sb.AppendLine("The survivor chose to LET IT END.");
        sb.AppendLine();
        sb.AppendLine("Write an ending personal to their specific story.");
        sb.AppendLine("Reference the rooms they actually explored.");
        sb.AppendLine("Reference what they found. Make it earned.");
        sb.AppendLine("This is the last thing the survivor will ever read.");
        sb.AppendLine();
        sb.AppendLine("REQUIRED — ACTUALLY STATE THE CAUSE, CLEARLY:");
        sb.AppendLine("Every earlier room only hinted — this is the one place the");
        sb.AppendLine("full cause MUST be stated plainly, not just gestured at.");
        sb.AppendLine("EndingText MUST explicitly say, in plain everyday words,");
        sb.AppendLine("what actually happened to end the world (the apocalypse");
        sb.AppendLine("cause and what happened above should be clearly recognisable");
        sb.AppendLine("in the text, not left implicit or purely symbolic). A player");
        sb.AppendLine("who reads only this ending, with no other context, must come");
        sb.AppendLine("away knowing exactly what happened — do not leave the cause");
        sb.AppendLine("as something only a developer reading internal notes would");
        sb.AppendLine("understand.");
        sb.AppendLine();
        sb.AppendLine("LANGUAGE — plain, easy, everyday words. No rare or literary");
        sb.AppendLine("vocabulary, no phrasing that needs a second read to parse.");
        sb.AppendLine("Short, direct sentences. It can still feel cinematic and");
        sb.AppendLine("emotional — just say it in simple language, not ornate language.");
        sb.AppendLine();
        sb.AppendLine("TEXT LENGTH — CRITICAL: this is read at the very end of a");
        sb.AppendLine("15-20 minute session — keep it tight, not a paragraph.");
        sb.AppendLine();
        sb.AppendLine("Respond with ONLY this JSON:");
        sb.AppendLine("{");
        sb.AppendLine("  \"EndingTitle\": \"short cinematic title, max 6 words\",");
        sb.AppendLine("  \"EndingText\": \"4-5 sentences, MAX 80 WORDS. Cinematic and personal.\",");
        sb.AppendLine("  \"FinalLine\": \"one haunting last sentence, MAX 20 WORDS.\"");
        sb.AppendLine("}");

        return sb.ToString();
    }
}