using UnityEngine;

[System.Serializable]
public class DetectedAnchor
{
    public AnchorType Type;
    public Vector3 WorldPosition;
    public Quaternion WorldRotation;
    public float Confidence;
    public string RawLabel;
}