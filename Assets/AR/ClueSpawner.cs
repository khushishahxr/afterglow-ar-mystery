using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClueSpawner : MonoBehaviour
{
    public static ClueSpawner Instance { get; private set; }

    // Generic-but-coherent evidence props — reused/adapted from the Fixed
    // Narrative project's asset library (see Assets/Props/) rather than
    // the old candle/lantern/barrel placeholder set, which read as
    // visually disconnected from the clue text describing them. These
    // stay theme- and surface-agnostic on purpose: this project ties every
    // clue to whatever real surface the player's own room actually has
    // (see PromptBuilder.cs), so a prop can't be a specific named object
    // like "a phone" the way Fixed Narrative's fixed story can.
    // Scale/Rotation fields are individually named per prop (not a list)
    // on purpose — a 30-row List<T> in the Inspector means expanding each
    // foldout one at a time to find a specific prop, which is far slower
    // to actually use than a flat, directly-named field you can just
    // scroll to and edit. ScaleMultiplier: 1 = no change. RotationOffset:
    // degrees applied on spawn, (0,0,0) = no change.
    [Header("Prop Prefabs")]
    public GameObject RadioPrefab;
    public float      RadioScale = 1f;
    public Vector3     RadioRotationOffset = Vector3.zero;
    public GameObject LedgerPrefab;
    public float      LedgerScale = 1f;
    public Vector3     LedgerRotationOffset = Vector3.zero;
    public GameObject FramedPhotoPrefab;
    public float      FramedPhotoScale = 1f;
    public Vector3     FramedPhotoRotationOffset = Vector3.zero;
    public GameObject SealedLetterPrefab;
    public float      SealedLetterScale = 1f;
    public Vector3     SealedLetterRotationOffset = Vector3.zero;
    public GameObject BookPrefab;
    public float      BookScale = 1f;
    public Vector3     BookRotationOffset = Vector3.zero;
    public GameObject PlatePrefab;
    public float      PlateScale = 1f;
    public Vector3     PlateRotationOffset = Vector3.zero;
    public GameObject BrokenMugPrefab;
    public float      BrokenMugScale = 1f;
    public Vector3     BrokenMugRotationOffset = Vector3.zero;
    public GameObject HandwrittenNotePrefab;
    public float      HandwrittenNoteScale = 1f;
    public Vector3     HandwrittenNoteRotationOffset = Vector3.zero;
    public GameObject PackedBagPrefab;
    public float      PackedBagScale = 1f;
    public Vector3     PackedBagRotationOffset = Vector3.zero;
    public GameObject DocumentStackPrefab;
    public float      DocumentStackScale = 1f;
    public Vector3     DocumentStackRotationOffset = Vector3.zero;
    public GameObject PhotographPrefab;
    public float      PhotographScale = 1f;
    public Vector3     PhotographRotationOffset = Vector3.zero;
    public GameObject SpoiledFoodPrefab;
    public float      SpoiledFoodScale = 1f;
    public Vector3     SpoiledFoodRotationOffset = Vector3.zero;
    public GameObject ChildToyPrefab;
    public float      ChildToyScale = 1f;
    public Vector3     ChildToyRotationOffset = Vector3.zero;
    public GameObject CompassPrefab;
    public float      CompassScale = 1f;
    public Vector3     CompassRotationOffset = Vector3.zero;
    public GameObject CrackedMirrorPrefab;
    public float      CrackedMirrorScale = 1f;
    public Vector3     CrackedMirrorRotationOffset = Vector3.zero;
    public GameObject FirstAidKitPrefab;
    public float      FirstAidKitScale = 1f;
    public Vector3     FirstAidKitRotationOffset = Vector3.zero;
    public GameObject GlassesPrefab;
    public float      GlassesScale = 1f;
    public Vector3     GlassesRotationOffset = Vector3.zero;
    public GameObject HandBellPrefab;
    public float      HandBellScale = 1f;
    public Vector3     HandBellRotationOffset = Vector3.zero;
    public GameObject KeysPrefab;
    public float      KeysScale = 1f;
    public Vector3     KeysRotationOffset = Vector3.zero;
    public GameObject MapPrefab;
    public float      MapScale = 1f;
    public Vector3     MapRotationOffset = Vector3.zero;
    public GameObject PadlockPrefab;
    public float      PadlockScale = 1f;
    public Vector3     PadlockRotationOffset = Vector3.zero;
    public GameObject RingPrefab;
    public float      RingScale = 1f;
    public Vector3     RingRotationOffset = Vector3.zero;
    public GameObject RopePrefab;
    public float      RopeScale = 1f;
    public Vector3     RopeRotationOffset = Vector3.zero;
    public GameObject ShoesPrefab;
    public float      ShoesScale = 1f;
    public Vector3     ShoesRotationOffset = Vector3.zero;
    public GameObject ToolboxPrefab;
    public float      ToolboxScale = 1f;
    public Vector3     ToolboxRotationOffset = Vector3.zero;
    public GameObject UmbrellaPrefab;
    public float      UmbrellaScale = 1f;
    public Vector3     UmbrellaRotationOffset = Vector3.zero;
    public GameObject WalletPrefab;
    public float      WalletScale = 1f;
    public Vector3     WalletRotationOffset = Vector3.zero;
    public GameObject WatchPrefab;
    public float      WatchScale = 1f;
    public Vector3     WatchRotationOffset = Vector3.zero;
    public GameObject WiltedPlantPrefab;
    public float      WiltedPlantScale = 1f;
    public Vector3     WiltedPlantRotationOffset = Vector3.zero;
    public GameObject CandlePrefab;
    public float      CandleScale = 1f;
    public Vector3     CandleRotationOffset = Vector3.zero;
    public GameObject DefaultPrefab;

    [Header("VFX")]
    public GameObject GlowVFX;

    Dictionary<string, GameObject> _iconToPrefab;
    Dictionary<string, float>      _iconToScale;
    Dictionary<string, Vector3>    _iconToRotation;
    readonly HashSet<string> _spawned = new(); // keyed by ClueData.EvidenceName

    // AR detection and narrative generation are prefetched while the Teaser
    // screen is showing (see NavigationManager.BeginRoomDetection), but the
    // player hasn't entered the room yet at that point. SpawnNextClue is a
    // no-op until ActivateRoom() has run (Gameplay screen becoming visible).
    bool _roomActive;
    NarrativeData _pendingNarrative;

    // Room 1's Scan is held off until the AR letter reveal (see
    // ARLetterReveal) finishes, so the first clue doesn't compete with the
    // letter for the player's attention.
    bool _waitingForLetterReveal;
    public bool IsWaitingForLetterReveal => _waitingForLetterReveal;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // Numeric string keys, not emoji — see PromptBuilder.EvidenceIcons
        // for why (emoji were an unreliable match key for LLM output).
        _iconToPrefab = new Dictionary<string, GameObject>
        {
            { "1",  RadioPrefab           },
            { "2",  LedgerPrefab          },
            { "3",  FramedPhotoPrefab     },
            { "4",  SealedLetterPrefab    },
            { "5",  BookPrefab            },
            { "6",  PlatePrefab           },
            { "7",  BrokenMugPrefab       },
            { "8",  HandwrittenNotePrefab },
            { "9",  PackedBagPrefab       },
            { "10", DocumentStackPrefab   },
            { "11", PhotographPrefab      },
            { "12", SpoiledFoodPrefab     },
            { "13", ChildToyPrefab        },
            { "14", CompassPrefab         },
            { "15", CrackedMirrorPrefab   },
            { "16", FirstAidKitPrefab     },
            { "17", GlassesPrefab         },
            { "18", HandBellPrefab        },
            { "19", KeysPrefab            },
            { "20", MapPrefab             },
            { "21", PadlockPrefab         },
            { "22", RingPrefab            },
            { "23", RopePrefab            },
            { "24", ShoesPrefab           },
            { "25", ToolboxPrefab         },
            { "26", UmbrellaPrefab        },
            { "27", WalletPrefab          },
            { "28", WatchPrefab           },
            { "29", WiltedPlantPrefab     },
            { "30", CandlePrefab          },
        };

        _iconToScale = new Dictionary<string, float>
        {
            { "1",  RadioScale           }, { "2",  LedgerScale          },
            { "3",  FramedPhotoScale     }, { "4",  SealedLetterScale    },
            { "5",  BookScale            }, { "6",  PlateScale           },
            { "7",  BrokenMugScale       }, { "8",  HandwrittenNoteScale },
            { "9",  PackedBagScale       }, { "10", DocumentStackScale   },
            { "11", PhotographScale      }, { "12", SpoiledFoodScale     },
            { "13", ChildToyScale        }, { "14", CompassScale         },
            { "15", CrackedMirrorScale   }, { "16", FirstAidKitScale     },
            { "17", GlassesScale         }, { "18", HandBellScale        },
            { "19", KeysScale            }, { "20", MapScale             },
            { "21", PadlockScale         }, { "22", RingScale            },
            { "23", RopeScale            }, { "24", ShoesScale           },
            { "25", ToolboxScale         }, { "26", UmbrellaScale        },
            { "27", WalletScale          }, { "28", WatchScale           },
            { "29", WiltedPlantScale     }, { "30", CandleScale          },
        };

        _iconToRotation = new Dictionary<string, Vector3>
        {
            { "1",  RadioRotationOffset           }, { "2",  LedgerRotationOffset          },
            { "3",  FramedPhotoRotationOffset     }, { "4",  SealedLetterRotationOffset    },
            { "5",  BookRotationOffset            }, { "6",  PlateRotationOffset           },
            { "7",  BrokenMugRotationOffset       }, { "8",  HandwrittenNoteRotationOffset },
            { "9",  PackedBagRotationOffset       }, { "10", DocumentStackRotationOffset   },
            { "11", PhotographRotationOffset      }, { "12", SpoiledFoodRotationOffset     },
            { "13", ChildToyRotationOffset        }, { "14", CompassRotationOffset         },
            { "15", CrackedMirrorRotationOffset   }, { "16", FirstAidKitRotationOffset     },
            { "17", GlassesRotationOffset         }, { "18", HandBellRotationOffset        },
            { "19", KeysRotationOffset            }, { "20", MapRotationOffset             },
            { "21", PadlockRotationOffset         }, { "22", RingRotationOffset            },
            { "23", RopeRotationOffset            }, { "24", ShoesRotationOffset           },
            { "25", ToolboxRotationOffset         }, { "26", UmbrellaRotationOffset        },
            { "27", WalletRotationOffset          }, { "28", WatchRotationOffset           },
            { "29", WiltedPlantRotationOffset     }, { "30", CandleRotationOffset          },
        };

        NarrativeGenerator.OnNarrativeReady += OnNarrativeReady;
        EvidenceManager.OnEvidenceAdded     += OnEvidenceAdded;

        Debug.Log("[ClueSpawner] Initialised and listening");
    }

    void Start()
    {
        // Catch-up in case NarrativeReady fired before this object existed.
        if (_pendingNarrative == null)
            _pendingNarrative = NarrativeGenerator.Instance?.CurrentNarrative;
    }

    void OnDestroy()
    {
        NarrativeGenerator.OnNarrativeReady -= OnNarrativeReady;
        EvidenceManager.OnEvidenceAdded     -= OnEvidenceAdded;
    }

    // Purely atmospheric now — with Scan-driven placement nothing is ever
    // sitting in the world waiting to be "released", so this just triggers
    // the glitch VFX once the player is one piece of evidence away from
    // finishing the room. No callback payload needed any more.
    void OnEvidenceAdded(EvidenceItem item)
    {
        int total = _pendingNarrative?.Clues?.Count ?? 0;
        if (total == 0) return;

        int found = EvidenceManager.Instance?.Count ?? 0;
        if (found == total - 1)
            SignalEscalationManager.Instance?.TriggerGlitchSequence(() => { });
    }

    void OnNarrativeReady(NarrativeData data)
    {
        Debug.Log($"[ClueSpawner] Narrative ready — {data.Clues?.Count} clues");
        _pendingNarrative = data;
    }

    // Called once the player actually enters the AR camera view for this
    // room (GameplayScreen.SetVisible(true)) — gates SpawnNextClue so a
    // Scan can't place anything before the room is actually on screen.
    // Room 1 additionally waits for the AR letter reveal to finish first.
    public void ActivateRoom(int roomNumber)
    {
        _roomActive = true;
        DestroyOrphanedClues();

        if (roomNumber == 1 && !_waitingForLetterReveal)
        {
            _waitingForLetterReveal = true;
            ARLetterReveal.OnLetterRevealComplete += OnLetterRevealComplete;
            // Safety net: the letter only fires once AR classifies a wall
            // plane, which depends on the physical space having one in
            // clear view. If that never happens, Scan would otherwise be
            // permanently blocked for the whole room — timeout and unblock
            // it anyway rather than risk a session getting stuck on this.
            StartCoroutine(LetterRevealTimeout());
        }
    }

    // Defensive cleanup, run every time the player lands on Gameplay —
    // destroys any ClueObject in the scene that this ClueSpawner didn't
    // itself spawn and track. Confirmed live (2026-08-21): a pile of
    // stray duplicate objects was visible even at 100%/all-evidence-found
    // for a 5-clue room — SpawnNextClue's own _spawned tracking already
    // makes it impossible to legitimately exceed the room's real clue
    // count, so anything extra is leftover cruft from an earlier bug or
    // an earlier test pass in the same session, not this room's actual
    // evidence. Cheap and idempotent — does nothing if everything present
    // is already accounted for.
    void DestroyOrphanedClues()
    {
        var existing = FindObjectsByType<ClueObject>(FindObjectsSortMode.None);
        int removed = 0;
        foreach (var clue in existing)
        {
            if (!_spawned.Contains(clue.EvidenceName))
            {
                Destroy(clue.gameObject);
                removed++;
            }
        }
        if (removed > 0)
            Debug.LogWarning($"[ClueSpawner] Destroyed {removed} orphaned clue object(s) not tracked for this room");
    }

    IEnumerator LetterRevealTimeout()
    {
        yield return new WaitForSeconds(25f);
        if (_waitingForLetterReveal)
        {
            Debug.LogWarning("[ClueSpawner] Letter reveal never fired (no wall detected?) — unblocking Scan anyway");
            ARLetterReveal.OnLetterRevealComplete -= OnLetterRevealComplete;
            _waitingForLetterReveal = false;
        }
    }

    void OnLetterRevealComplete()
    {
        ARLetterReveal.OnLetterRevealComplete -= OnLetterRevealComplete;
        _waitingForLetterReveal = false;
    }

    // Scan-driven placement: nothing spawns automatically any more. The
    // player points the camera at a surface and presses Scan; that single
    // action places exactly the next undiscovered clue, in the room's
    // defined order, at wherever the hit-test landed. Returns false once
    // every clue in the room has already been spawned.
    public bool SpawnNextClue(Vector3 pos, AnchorType hitType)
    {
        if (!_roomActive || _waitingForLetterReveal) return false;
        if (_pendingNarrative?.Clues == null) return false;

        foreach (var clue in _pendingNarrative.Clues)
        {
            if (_spawned.Contains(clue.EvidenceName)) continue;

            SpawnClue(clue, hitType.ToString(), pos);
            return true;
        }

        return false; // every clue in this room has already been spawned
    }

    void SpawnClue(ClueData clue, string anchorType, Vector3 pos)
    {
        // Pick prefab by icon
        GameObject prefab = DefaultPrefab;
        bool iconMatched = _iconToPrefab.TryGetValue(clue.EvidenceIcon, out var p) && p != null;
        if (iconMatched)
            prefab = p;

        Debug.Log($"[ClueSpawner] SpawnClue: icon='{clue.EvidenceIcon}' name='{clue.EvidenceName}' " +
                  $"iconMatched={iconMatched} resolvedPrefab='{prefab?.name}'");

        if (prefab == null)
        {
            Debug.LogWarning($"[ClueSpawner] No prefab for " +
                             $"icon: {clue.EvidenceIcon} " +
                             $"and no DefaultPrefab set!");
            return;
        }

        // Spawn on surface
        Vector3 spawnPos = pos + Vector3.up * 0.02f;
        Vector3 rotationOffset = _iconToRotation.TryGetValue(clue.EvidenceIcon, out var rot) ? rot : Vector3.zero;
        var obj = Instantiate(prefab, spawnPos, Quaternion.Euler(rotationOffset));
        obj.name = $"Clue_{clue.EvidenceName}";

        // Applied before the collider is generated below, so the
        // auto-sized collider bounds the actually-scaled object.
        if (_iconToScale.TryGetValue(clue.EvidenceIcon, out float scaleMult) && scaleMult != 1f)
            obj.transform.localScale *= scaleMult;

        // Decorative prop art isn't guaranteed to ship with a collider —
        // without one, tap-to-collect (ClueObject.CheckTapInput) silently
        // never hits it.
        if (obj.GetComponentInChildren<Collider>() == null)
        {
            var propRenderer = obj.GetComponentInChildren<Renderer>();
            if (propRenderer != null)
            {
                var box = obj.AddComponent<BoxCollider>();
                box.center = obj.transform.InverseTransformPoint(propRenderer.bounds.center);
                box.size = Vector3.Scale(
                    propRenderer.bounds.size,
                    new Vector3(
                        1f / Mathf.Max(obj.transform.lossyScale.x, 0.0001f),
                        1f / Mathf.Max(obj.transform.lossyScale.y, 0.0001f),
                        1f / Mathf.Max(obj.transform.lossyScale.z, 0.0001f)));
            }
        }

        var clueComp = obj.GetComponent<ClueObject>();
        if (clueComp == null)
            clueComp = obj.AddComponent<ClueObject>();

        string surface       = anchorType.ToLower();
        string participantId = StudyLogger.Instance?.ParticipantID ?? "P001";
        if (string.IsNullOrEmpty(participantId)) participantId = "P001";

        clueComp.AnchorType     = clue.AnchorType;
        clueComp.ClueText       = clue.ClueText
            .Replace("{SURFACE}", surface).Replace("{PARTICIPANT_ID}", participantId);
        clueComp.EvidenceDetail = clue.EvidenceDetail
            .Replace("{SURFACE}", surface).Replace("{PARTICIPANT_ID}", participantId);
        clueComp.EvidenceName   = clue.EvidenceName;
        clueComp.EvidenceIcon   = clue.EvidenceIcon;
        clueComp.IsRedHerring   = clue.IsRedHerring;
        clueComp.ReactionLine   = string.IsNullOrEmpty(clue.ReactionLine)
            ? ""
            : clue.ReactionLine.Replace("{SURFACE}", surface).Replace("{PARTICIPANT_ID}", participantId);

        // Flame VFX only makes sense on the candle (icon "30") — was
        // attaching to every single prop regardless of what it was.
        if (GlowVFX != null && clue.EvidenceIcon == "30")
        {
            var glow = Instantiate(GlowVFX, obj.transform);
            glow.transform.localPosition = Vector3.up * 0.1f;
            glow.SetActive(false);
            clueComp.GlowVFX = glow;
        }

        _spawned.Add(clue.EvidenceName);
        LastSpawnedClue = clueComp;
        Debug.Log($"[ClueSpawner] ✅ Spawned: {clue.EvidenceName} " +
                  $"at {anchorType} ({spawnPos}) " +
                  $"RedHerring: {clue.IsRedHerring}");
    }

    // The clue placed by the most recent Reveal press — lets GameplayScreen
    // track proximity (Signal readout) and time-since-spawn (look-around
    // nudge) for specifically the thing the player is currently hunting for.
    public ClueObject LastSpawnedClue { get; private set; }

    // The CURRENT room's actual clue count, from the exact same
    // _pendingNarrative SpawnNextClue itself reads — not
    // NarrativeGenerator.CurrentNarrative, which GameplayScreen was
    // reading before and can be ahead of what this room is actually
    // using if a later room's narrative finishes generating early.
    // Reported live (2026-08-21): "all evidence found" fired without all
    // items actually being collected, consistent with that total being
    // wrong at the moment of the check. Matches Fixed Narrative's
    // ClueSpawner.TotalClueCount.
    public int TotalClueCount => _pendingNarrative?.Clues?.Count ?? 0;

    public void PulseAllNearby()
    {
        var clues = FindObjectsByType<ClueObject>(
            FindObjectsSortMode.None);
        Debug.Log($"[ClueSpawner] Pulsing {clues.Length} clue objects");
        foreach (var clue in clues)
            clue.PulseHighlight();
    }

    // ── Per-room reset ─────────────────────────────────────────────────────

    // Called when the player moves on to a new physical room — removes
    // the previous room's spawned props and clears tracking so the next
    // room's anchors/narrative can spawn fresh clues.
    public void ResetForNewRoom()
    {
        var existing = FindObjectsByType<ClueObject>(FindObjectsSortMode.None);
        foreach (var clue in existing)
            Destroy(clue.gameObject);

        if (_waitingForLetterReveal)
            ARLetterReveal.OnLetterRevealComplete -= OnLetterRevealComplete;

        _spawned.Clear();
        _pendingNarrative = null;
        _roomActive = false;
        _waitingForLetterReveal = false;

        Debug.Log("[ClueSpawner] Reset for new room");
    }
}
