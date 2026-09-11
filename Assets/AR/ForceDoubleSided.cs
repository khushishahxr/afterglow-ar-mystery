using UnityEngine;

// Opt-in fix for a known FBX winding-order quirk on a handful of the
// reimported story props: they can render partially see-through/missing
// faces on-device while looking fine in the Editor. Add this component to
// a specific prop's prefab root only if that prop actually shows the issue
// on-device — don't apply it blanket, most props don't need it.
public class ForceDoubleSided : MonoBehaviour
{
    void Awake()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            foreach (var mat in renderer.materials)
                mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
    }
}
