using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TmpFontMaterialBuildSanitizer : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        bool hasChanges = SanitizeBuildAssets();

        if (hasChanges == true)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/TMP/Sanitize Build Font Materials")]
    private static void SanitizeBuildFontMaterials()
    {
        bool hasChanges = SanitizeBuildAssets();

        if (hasChanges == true)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static bool SanitizeBuildAssets()
    {
        bool hasChanges = false;

        hasChanges |= SanitizeBuildScenes();
        hasChanges |= SanitizeBuildPrefabs();

        return hasChanges;
    }

    private static bool SanitizeBuildScenes()
    {
        SceneSetup[] currentSetup = EditorSceneManager.GetSceneManagerSetup();
        bool hasChanges = false;

        try
        {
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

            for (int i = 0; i < buildScenes.Length; i++)
            {
                EditorBuildSettingsScene buildScene = buildScenes[i];

                if (buildScene.enabled == false)
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                bool sceneChanged = SanitizeScene(scene, buildScene.path);

                if (sceneChanged == false)
                {
                    continue;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                hasChanges = true;
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(currentSetup);
        }

        return hasChanges;
    }

    private static bool SanitizeBuildPrefabs()
    {
        HashSet<string> prefabPaths = CollectBuildPrefabPaths();
        bool hasChanges = false;

        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                bool prefabChanged = SanitizeGameObject(prefabRoot, prefabPath);

                if (prefabChanged == false)
                {
                    continue;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                hasChanges = true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        return hasChanges;
    }

    private static HashSet<string> CollectBuildPrefabPaths()
    {
        HashSet<string> prefabPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

        for (int i = 0; i < buildScenes.Length; i++)
        {
            EditorBuildSettingsScene buildScene = buildScenes[i];

            if (buildScene.enabled == false)
            {
                continue;
            }

            string[] dependencies = AssetDatabase.GetDependencies(buildScene.path, true);

            for (int j = 0; j < dependencies.Length; j++)
            {
                string dependencyPath = dependencies[j];

                if (dependencyPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                prefabPaths.Add(dependencyPath);
            }
        }

        return prefabPaths;
    }

    private static bool SanitizeScene(Scene scene, string assetPath)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();
        bool hasChanges = false;

        for (int i = 0; i < rootObjects.Length; i++)
        {
            hasChanges |= SanitizeGameObject(rootObjects[i], assetPath);
        }

        return hasChanges;
    }

    private static bool SanitizeGameObject(GameObject rootObject, string assetPath)
    {
        TMP_Text[] textComponents = rootObject.GetComponentsInChildren<TMP_Text>(true);
        bool hasChanges = false;

        for (int i = 0; i < textComponents.Length; i++)
        {
            hasChanges |= SanitizeTextComponent(textComponents[i], assetPath);
        }

        return hasChanges;
    }

    private static bool SanitizeTextComponent(TMP_Text textComponent, string assetPath)
    {
        TMP_FontAsset fontAsset = textComponent.font;

        if (fontAsset == null)
        {
            return false;
        }

        Material expectedMaterial = fontAsset.material;

        if (expectedMaterial == null)
        {
            string objectPath = GetObjectPath(textComponent.transform);
            throw new BuildFailedException("TMP font has no material: " + assetPath + " -> " + objectPath + " (" + fontAsset.name + ")");
        }

        Material currentMaterial = textComponent.fontSharedMaterial;

        if (currentMaterial != null && IsMaterialCompatible(fontAsset, currentMaterial) == true)
        {
            return false;
        }

        textComponent.fontSharedMaterial = expectedMaterial;
        EditorUtility.SetDirty(textComponent);

        Debug.Log("TMP material normalized: " + assetPath + " -> " + GetObjectPath(textComponent.transform), textComponent);

        return true;
    }

    private static bool IsMaterialCompatible(TMP_FontAsset fontAsset, Material material)
    {
        Texture materialTexture = material.mainTexture;

        if (materialTexture == null)
        {
            return false;
        }

        Texture[] atlasTextures = fontAsset.atlasTextures;

        if (atlasTextures == null || atlasTextures.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < atlasTextures.Length; i++)
        {
            Texture atlasTexture = atlasTextures[i];

            if (atlasTexture == materialTexture)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetObjectPath(Transform transform)
    {
        string path = transform.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
