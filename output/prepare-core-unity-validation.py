from pathlib import Path
import json, shutil
root=Path(__file__).resolve().parent.parent
target=root/'output/core-validation/unity-project'
target.mkdir(parents=True,exist_ok=True)
for folder in ['Assets','ProjectSettings']:
    shutil.copytree(root/folder,target/folder,dirs_exist_ok=True)
(target/'Packages').mkdir(exist_ok=True)
manifest=json.loads((root/'Packages/manifest.json').read_text(encoding='utf-8-sig'))
for name,value in list(manifest['dependencies'].items()):
    if value.startswith('file:'):
        manifest['dependencies'][name]='file:'+(root/'Packages'/value[5:]).resolve().as_posix()
(target/'Packages/manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
editor=target/'Assets/Editor'
editor.mkdir(exist_ok=True)
(editor/'CoreExtractionValidation.cs').write_text('''using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CoreExtractionValidation
{
    public static void Run()
    {
        try
        {
            var names = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Select(p => p.name).ToArray();
            foreach (var required in new[] { "com.secondwind.core", "com.secondwind.ads.admob", "com.secondwind.leaderboards.ugs" })
                if (!names.Contains(required)) throw new Exception("Package unresolved: " + required);
            var core = typeof(SWGUnity2DCore.Manager.CoreServices).Assembly;
            foreach (var reference in core.GetReferencedAssemblies())
                if (reference.Name.Contains("GoogleMobileAds") || reference.Name.StartsWith("Unity.Services") || reference.Name.StartsWith("Assembly-CSharp"))
                    throw new Exception("Core has unwanted dependency: " + reference.Name);
            int checkedObjects = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Check(prefab, path, ref checkedObjects);
            }
            foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                var path = entry.path;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var go in scene.GetRootGameObjects()) Check(go, path, ref checkedObjects);
            }
            var schedule = new SWGUnity2DCore.Manager.InterstitialSchedule();
            schedule.RecordRound(); schedule.RecordRound();
            if (schedule.CanShow(0)) throw new Exception("Early interstitial");
            schedule.RecordRound();
            if (!schedule.CanShow(0)) throw new Exception("Third round not eligible");
            schedule.MarkShown(0);
            schedule.RecordRound(); schedule.RecordRound(); schedule.RecordRound();
            if (schedule.CanShow(179.9) || !schedule.CanShow(180)) throw new Exception("Cooldown changed");
            Debug.Log("CORE_EXTRACTION_VALIDATION_OK: packages resolved, Core isolated, prefab/scene objects=" + checkedObjects + ", ad schedule preserved");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    private static void Check(GameObject root, string asset, ref int count)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            count++;
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                throw new Exception("Missing script in " + asset + ": " + t.name);
        }
    }
}
''',encoding='utf-8')
print(target)
