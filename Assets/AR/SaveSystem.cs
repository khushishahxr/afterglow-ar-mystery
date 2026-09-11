using System;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    // Save file location
    string SavePath => Path.Combine(Application.persistentDataPath, "afterglow_save.json");

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Save ───────────────────────────────────────────────────────────────

    public void SaveProgress(WorldState state)
    {
        try
        {
            var saveData = new SaveData
            {
                Theme            = state.Theme.ToString(),
                ThemeDescription = state.ThemeDescription,
                AllRoomsComplete = state.AllRoomsComplete,
                EndingChoice     = state.EndingChoice,
                CompletedRooms   = new System.Collections.Generic.List<int>()
            };

            foreach (var room in state.Rooms)
                if (room.IsComplete)
                    saveData.CompletedRooms.Add(room.RoomNumber);

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveSystem] Saved — {saveData.CompletedRooms.Count} rooms complete");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
        }
    }

    // ── Load ───────────────────────────────────────────────────────────────

    public SaveData LoadProgress()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("[SaveSystem] No save file found — fresh start");
                return null;
            }

            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveSystem] Loaded — {data.CompletedRooms.Count} rooms complete");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
            return null;
        }
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] Save deleted");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Delete failed: {e.Message}");
        }
    }

    public bool HasSave => File.Exists(SavePath);
}

// ── Save data structure ────────────────────────────────────────────────────

[Serializable]
public class SaveData
{
    public string Theme;
    public string ThemeDescription;
    public bool   AllRoomsComplete;
    public string EndingChoice;
    public System.Collections.Generic.List<int> CompletedRooms = new();
}