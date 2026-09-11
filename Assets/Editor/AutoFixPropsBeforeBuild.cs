using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs GenericPropPrefabBuilder automatically before every build and force-
// saves the scene immediately after — removing the manual "run the tool,
// then remember to save the scene" sequence entirely. That two-step manual
// process failed to persist correctly across four separate attempts this
// session (2026-08-21): PrefabUtility.SaveAsPrefabAsset generates a new
// internal fileID every time it runs, so the scene's wiring only stays
// correct if it's saved immediately after the LAST run — any run-again-
// without-saving, or save-before-the-last-run, silently leaves ClueSpawner
// pointing at stale references. Making this an automatic build step means
// it can no longer depend on that timing being done right by hand.
public class AutoFixPropsBeforeBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("[AutoFixPropsBeforeBuild] Rebuilding all generic prop prefabs and re-wiring ClueSpawner before build...");
        GenericPropPrefabBuilder.BuildAll();

        DisableLeftoverDemoObjectSpawner();

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            Debug.Log(saved
                ? $"[AutoFixPropsBeforeBuild] Scene '{scene.name}' saved with fresh prop references."
                : $"[AutoFixPropsBeforeBuild] WARNING — failed to save scene '{scene.name}'.");
        }
        else
        {
            Debug.LogWarning("[AutoFixPropsBeforeBuild] No valid active scene to save — prop fix may not persist into this build.");
        }
    }

    // The AR Foundation sample template ships a GameObject named "Object
    // Spawner" wired to spawn one of several demo prefabs on every AR tap
    // interaction (via ARInteractorSpawnTrigger), completely independent
    // of this game's ClueSpawner/Reveal flow. Confirmed live (2026-08-21)
    // as the actual source of "a cube spawns on every tap" — unrelated to,
    // and unaffected by, any of the Reveal-lock fixes made earlier, since
    // it runs on its own AR interaction trigger rather than through
    // GameplayScreen at all. Disabling it here so it can't be missed by a
    // manual step, the same lesson as the prop-wiring fix above.
    static void DisableLeftoverDemoObjectSpawner()
    {
        var go = GameObject.Find("Object Spawner");
        if (go == null)
        {
            Debug.Log("[AutoFixPropsBeforeBuild] 'Object Spawner' demo GameObject not found active in scene — nothing to disable (already off, or not present).");
            return;
        }

        go.SetActive(false);
        Debug.Log("[AutoFixPropsBeforeBuild] Disabled leftover 'Object Spawner' demo GameObject (was spawning a demo prefab on every AR tap).");
    }
}
