using System.Collections;
using UnityEngine;

// A frozen, translucent humanoid silhouette that appears at the first
// floor surface detected in a room, and dissolves into rising particles
// once the player walks close. One per room, doesn't respawn on re-entry.
public class GhostSilhouette : MonoBehaviour
{
    static readonly Color ColdBlue = new Color(0.4f, 0.7f, 1.0f, 0.3f);
    const float ApproachDistance = 1.2f;
    const float DissolveDuration = 2f;

    static bool _spawnedThisRoom;

    // Disabled per request (2026-08-21) — even with the shader-crash fix
    // preventing it from throwing, its fallback shader chain still
    // rendered as a broken magenta/pink shape rather than the intended
    // translucent cold-blue silhouette. Purely atmospheric, not core
    // gameplay, so removing it entirely rather than debugging the
    // material further under time pressure.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // ARSceneManager.OnAnchorAdded += OnAnchorAdded;
    }

    static void OnAnchorAdded(DetectedAnchor anchor)
    {
        if (anchor.Type != AnchorType.Floor) return;
        if (_spawnedThisRoom) return;
        _spawnedThisRoom = true;

        var go = new GameObject("GhostSilhouette");
        go.AddComponent<GhostSilhouette>().Spawn(anchor.WorldPosition);
    }

    // Called on room change and on researcher "reset for next participant".
    public static void ResetForNewRoom() => _spawnedThisRoom = false;

    Renderer _renderer;
    Material _material;

    void Spawn(Vector3 floorPosition)
    {
        transform.position = floorPosition;
        BuildVisual();
        if (_renderer == null) { Destroy(gameObject); return; } // no usable shader this build — see MakeTransparentMaterial
        StartCoroutine(WatchForApproach());
    }

    void BuildVisual()
    {
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.transform.SetParent(transform, false);
        capsule.transform.localPosition = new Vector3(0f, 0.85f, 0f); // ~1.7m tall
        capsule.transform.localScale    = new Vector3(0.25f, 0.85f, 0.25f);
        capsule.transform.localRotation = Quaternion.Euler(15f, Random.Range(0f, 360f), 0f);

        var collider = capsule.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        var mat = MakeTransparentMaterial(ColdBlue);
        if (mat == null)
        {
            // Confirmed live (2026-08-21): both shader names below came
            // back null on this build. That threw ArgumentNullException
            // from inside ARSceneManager.ResetForNewRoom() (this object
            // spawns at the first floor plane registered during a room
            // reset), silently aborting every reset call queued after it —
            // ClueSpawner.ResetForNewRoom() and EvidenceManager.Clear()
            // never ran, so the next room inherited the previous room's
            // clues and evidence count wholesale. Skipping the visual
            // gracefully instead of throwing is what actually matters here.
            Destroy(capsule);
            return;
        }

        _renderer = capsule.GetComponent<Renderer>();
        _material = mat;
        _renderer.material = _material;
    }

    static Material MakeTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");

        if (shader == null)
        {
            var anyRenderer = FindFirstObjectByType<Renderer>();
            shader = anyRenderer != null ? anyRenderer.sharedMaterial?.shader : null;
        }

        if (shader == null) return null;

        var mat = new Material(shader) { color = color };

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
        if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend", 0f);   // Alpha
        mat.SetOverrideTag("RenderType", "Transparent");
        if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))   mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return mat;
    }

    IEnumerator WatchForApproach()
    {
        while (true)
        {
            var cam = Camera.main;
            if (cam != null &&
                Vector3.Distance(cam.transform.position, transform.position) <= ApproachDistance)
            {
                yield return Dissolve();
                yield break;
            }
            yield return null;
        }
    }

    IEnumerator Dissolve()
    {
        EmitParticles();

        float t = 0f;
        while (t < DissolveDuration)
        {
            t += Time.deltaTime;
            var c = ColdBlue;
            c.a = Mathf.Lerp(ColdBlue.a, 0f, t / DissolveDuration);
            _material.color = c;
            yield return null;
        }

        Destroy(gameObject);
    }

    void EmitParticles()
    {
        var psGO = new GameObject("GhostParticles");
        psGO.transform.SetParent(transform, false);
        psGO.transform.localPosition = Vector3.up * 0.9f;

        var ps = psGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor    = new Color(ColdBlue.r, ColdBlue.g, ColdBlue.b, 0.6f);
        main.startLifetime = 1.5f;
        main.startSpeed    = 0.33f; // ~0.5m rise over the 1.5s lifetime
        main.startSize     = 0.03f;
        main.maxParticles   = 20;
        main.duration       = 1.5f;
        main.loop           = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 8f;
        shape.rotation  = new Vector3(-90f, 0f, 0f); // cone points up

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(ColdBlue, 0f), new GradientColorKey(ColdBlue, 1f) },
            new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                  ?? Shader.Find("Particles/Standard Unlit")
                                  ?? Shader.Find("Sprites/Default");
        if (particleShader == null && _renderer != null)
            particleShader = _renderer.sharedMaterial?.shader; // reuse whatever the capsule ended up with
        if (particleShader == null)
        {
            Destroy(psGO); // no usable shader this build — skip the particle burst, not worth crashing over
            return;
        }
        renderer.material = new Material(particleShader);

        ps.Play();
        Destroy(psGO, 2f);
    }
}
