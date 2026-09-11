using System.Collections.Generic;

[System.Serializable]
public class RoomContext
{
    public List<DetectedAnchor> Anchors = new();

    public bool Has(AnchorType type) =>
        Anchors.Exists(a => a.Type == type);

    public DetectedAnchor Get(AnchorType type) =>
        Anchors.Find(a => a.Type == type);

    public List<AnchorType> AnchorTypes() =>
        Anchors.ConvertAll(a => a.Type);
}