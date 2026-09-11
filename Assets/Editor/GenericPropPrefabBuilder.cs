using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-click batch tool: builds a Material (with a matching texture where
// one exists, or a distinct flat-color fallback otherwise), applies it to
// each generic evidence prop FBX, saves the result as a prefab under
// Assets/Props/Prefabs/, and auto-assigns every prefab into the matching
// field on the ClueSpawner component in the currently open scene.
// Run via AFTERGLOW > Build Generic Prop Prefabs.
public static class GenericPropPrefabBuilder
{
    const string ModelsDir    = "Assets/Props/Models";
    const string TexturesDir  = "Assets/Props/Textures";
    const string MaterialsDir = "Assets/Props/Materials";
    const string PrefabsDir   = "Assets/Props/Prefabs";

    class PropEntry
    {
        public string FieldName;
        public string FbxFile;      // relative to ModelsDir
        public string TextureFile;  // relative to TexturesDir, or null
        public Color  FallbackColor;

        public PropEntry(string field, string fbx, string tex, Color fallback)
        {
            FieldName     = field;
            FbxFile       = fbx;
            TextureFile   = tex;
            FallbackColor = fallback;
        }
    }

    static readonly List<PropEntry> Props = new()
    {
        new("RadioPrefab",           "GenericProp-Radio.fbx",         "Radio1.png",         new Color(0.55f, 0.45f, 0.3f)),
        new("LedgerPrefab",          "GenericProp-Ledger.fbx",        null,                 new Color(0.35f, 0.22f, 0.12f)),
        new("FramedPhotoPrefab",     "GenericProp-FramedPhoto.fbx",   "family photos.png",  new Color(0.6f, 0.55f, 0.45f)),
        new("SealedLetterPrefab",    "GenericProp-SealedLetter.fbx",  "Envelope.png",       new Color(0.85f, 0.8f, 0.7f)),
        new("BookPrefab",            "GenericProp-Book.fbx",          "BookUV.png",         new Color(0.4f, 0.15f, 0.15f)),
        new("PlatePrefab",           "GenericProp-Plate.fbx",         "PLates.png",         new Color(0.8f, 0.8f, 0.78f)),
        new("BrokenMugPrefab",       "GenericProp-BrokenMug.fbx",     "Broken Mug.png",     new Color(0.7f, 0.7f, 0.68f)),
        new("HandwrittenNotePrefab", "GenericProp-HandwrittenNote.fbx","note.png",           new Color(0.9f, 0.87f, 0.75f)),
        new("PackedBagPrefab",       "GenericProp-PackedBag.fbx",     null,                 new Color(0.5f, 0.42f, 0.28f)),
        new("DocumentStackPrefab",   "GenericProp-DocumentStack.fbx", "RsearchPpaer.png",   new Color(0.88f, 0.85f, 0.75f)),
        new("PhotographPrefab",      "GenericProp-Photograph.fbx",    "Photograph.png",     new Color(0.6f, 0.55f, 0.45f)),
        new("SpoiledFoodPrefab",     "GenericProp-SpoiledFood.fbx",   "rottenfood.png",     new Color(0.35f, 0.4f, 0.2f)),
        new("ChildToyPrefab",        "GenericProp-ChildToy.fbx",      "Toys.png",           new Color(0.8f, 0.3f, 0.3f)),
        new("CompassPrefab",         "GenericProp-Compass.fbx",       "Compass.jpg",        new Color(0.6f, 0.5f, 0.2f)),
        new("CrackedMirrorPrefab",   "GenericProp-CrackedMirror.fbx", null,                 new Color(0.75f, 0.78f, 0.8f)),
        new("FirstAidKitPrefab",     "GenericProp-FirstAidKit.fbx",   "FIRSTAIDBOX.png",    new Color(0.85f, 0.2f, 0.2f)),
        new("GlassesPrefab",         "GenericProp-Glasses.fbx",       null,                 new Color(0.15f, 0.15f, 0.17f)),
        new("HandBellPrefab",        "GenericProp-HandBell.fbx",      "Bell.png",           new Color(0.7f, 0.6f, 0.25f)),
        new("KeysPrefab",            "GenericProp-Keys.fbx",          "Keys1.jpg",          new Color(0.65f, 0.6f, 0.4f)),
        new("MapPrefab",             "GenericProp-Map.fbx",           "Map.png",            new Color(0.82f, 0.76f, 0.6f)),
        new("PadlockPrefab",         "GenericProp-Padlock.fbx",       "PadlockandChain.png",new Color(0.4f, 0.4f, 0.42f)),
        new("RingPrefab",            "GenericProp-Ring.fbx",          "RINGBOX.png",        new Color(0.7f, 0.6f, 0.3f)),
        new("RopePrefab",            "GenericProp-Rope.fbx",          null,                 new Color(0.55f, 0.42f, 0.25f)),
        new("ShoesPrefab",           "GenericProp-Shoes.fbx",         "Shoes.png",          new Color(0.3f, 0.22f, 0.15f)),
        new("ToolboxPrefab",         "GenericProp-Toolbox.fbx",       "tOOLBOX.png",        new Color(0.5f, 0.15f, 0.1f)),
        new("UmbrellaPrefab",        "GenericProp-Umbrella.fbx",      null,                 new Color(0.15f, 0.2f, 0.35f)),
        new("WalletPrefab",          "GenericProp-Wallet.fbx",        "Wallet.png",         new Color(0.3f, 0.2f, 0.15f)),
        new("WatchPrefab",           "GenericProp-Watch.fbx",         "Digitalwristwatch.png", new Color(0.1f, 0.1f, 0.1f)),
        new("WiltedPlantPrefab",     "GenericProp-WiltedPlant.fbx",   "Plant.jpg",          new Color(0.35f, 0.4f, 0.2f)),
    };

    // Field names sourced from the Fixed Narrative project's finalized
    // 25-prop story-specific library (re-imported for correct real-world
    // scale — see GenericProp-*.fbx.meta, copied verbatim from their
    // ModelImporter settings). Every other field in Props[] was modeled
    // independently for this project and is untouched by this subset.
    static readonly HashSet<string> ReimportedFromFixedNarrative = new()
    {
        "BookPrefab", "BrokenMugPrefab", "PlatePrefab", "PhotographPrefab",
        "DocumentStackPrefab", "FramedPhotoPrefab", "HandwrittenNotePrefab",
        "SpoiledFoodPrefab", "RadioPrefab", "LedgerPrefab", "PackedBagPrefab",
    };

    [MenuItem("AFTERGLOW/Build Generic Prop Prefabs")]
    public static void BuildAll() => Build(Props, "Build Generic Prop Prefabs");

    // Rebuilds only the props whose source FBX was just re-imported from
    // the Fixed Narrative project — leaves the other prefabs/materials in
    // Props[] completely alone.
    [MenuItem("AFTERGLOW/Rebuild Reimported Fixed-Narrative Props")]
    public static void RebuildReimportedFromFixedNarrative() =>
        Build(Props.FindAll(p => ReimportedFromFixedNarrative.Contains(p.FieldName)),
              "Rebuild Reimported Fixed-Narrative Props");

    static void Build(List<PropEntry> props, string logLabel)
    {
        EnsureFolder(MaterialsDir);
        EnsureFolder(PrefabsDir);

        var results   = new Dictionary<string, GameObject>();
        var texturedCount = 0;
        var fallbackCount = 0;
        var missingModelCount = 0;

        foreach (var entry in props)
        {
            string fbxPath = $"{ModelsDir}/{entry.FbxFile}";
            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxAsset == null)
            {
                Debug.LogWarning($"[GenericPropPrefabBuilder] Missing model, skipped: {fbxPath}");
                missingModelCount++;
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);

            Texture2D tex = null;
            if (entry.TextureFile != null)
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesDir}/{entry.TextureFile}");

            if (tex != null)
            {
                mat.mainTexture = tex;
                texturedCount++;
            }
            else
            {
                mat.color = entry.FallbackColor;
                fallbackCount++;
            }

            string matName = entry.FieldName.Replace("Prefab", "");
            string matPath = AssetDatabase.GenerateUniqueAssetPath($"{MaterialsDir}/Mat_{matName}.mat");
            AssetDatabase.CreateAsset(mat, matPath);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[renderer.sharedMaterials.Length == 0 ? 1 : renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                renderer.sharedMaterials = mats;
            }

            // Always save at the exact canonical path, deleting whatever is
            // there first. The previous "overwrite if loadable" check used
            // AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null
            // to decide whether to overwrite in place — but that check
            // returns null for a prefab that exists on disk but is
            // corrupted/unloadable (e.g. a PrefabInstance whose base lives
            // in a different Unity project's asset database, which is
            // exactly what these reimported props were). That silently
            // took the "else" branch every time, generating a fresh
            // "Name 2.prefab", "Name 3.prefab", etc. on every single run
            // instead of ever actually fixing the original path — and the
            // GUID changing each run meant the scene's reference went
            // stale again as soon as the tool was re-run. Confirmed live
            // (2026-08-17): all 11 reimported props had accumulated 3
            // duplicate files each this way. DeleteAsset+recreate can't
            // accumulate duplicates — there's only ever one file at
            // prefabPath after this runs, regardless of what was there
            // before.
            string prefabPath = $"{PrefabsDir}/{matName}.prefab";
            // Check the filesystem directly, not AssetDatabase.LoadAssetAtPath
            // — that's the exact check that silently failed before, since it
            // returns null for a file that exists but is unloadable/corrupt.
            string absolutePrefabPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName, prefabPath);
            if (File.Exists(absolutePrefabPath))
                AssetDatabase.DeleteAsset(prefabPath);
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            results[entry.FieldName] = prefabAsset;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int wired = WireIntoClueSpawner(results);

        Debug.Log(
            $"[GenericPropPrefabBuilder] {logLabel} done. {texturedCount} used a matched texture, " +
            $"{fallbackCount} used a flat-color fallback (no texture found — you can swap " +
            $"a real one in later), {missingModelCount} models were missing entirely. " +
            $"{wired} fields auto-assigned on ClueSpawner in the open scene. " +
            "Save the scene (Ctrl+S) to keep the assignment.");
    }

    static int WireIntoClueSpawner(Dictionary<string, GameObject> prefabsByField)
    {
        var spawner = Object.FindFirstObjectByType<ClueSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[GenericPropPrefabBuilder] No ClueSpawner found in the open scene — " +
                              "open SampleScene first if you want auto-wiring, or assign the " +
                              $"{PrefabsDir} prefabs manually.");
            return 0;
        }

        var so = new SerializedObject(spawner);
        int count = 0;

        foreach (var kvp in prefabsByField)
        {
            var prop = so.FindProperty(kvp.Key);
            if (prop == null)
            {
                Debug.LogWarning($"[GenericPropPrefabBuilder] ClueSpawner has no field named {kvp.Key}.");
                continue;
            }
            prop.objectReferenceValue = kvp.Value;
            count++;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        return count;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string leaf   = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
