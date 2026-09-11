using System;
using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance { get; private set; }
    public static event Action<EvidenceItem> OnEvidenceAdded;

    readonly List<EvidenceItem> _items = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddEvidence(EvidenceItem item)
    {
        if (_items.Exists(e => e.Name == item.Name)) return;
        _items.Add(item);
        WorldStateManager.Instance?.AddClueToCurrentRoom(item.Name);
        Debug.Log($"[EvidenceManager] Added: {item.Name} " +
                  $"(RedHerring: {item.IsRedHerring})");
        OnEvidenceAdded?.Invoke(item);
    }

    public void Clear()
    {
        _items.Clear();
        Debug.Log("[EvidenceManager] Cleared");
    }

    public int Count => _items.Count;
    public List<EvidenceItem> Items => new(_items);
}