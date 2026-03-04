using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(LevelGenerator))]
public sealed class LevelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelGenerator levelGenerator = target as LevelGenerator;

        if (levelGenerator == null)
        {
            return;
        }

        GUILayout.Space(8f);

        if (GUILayout.Button("Generate") == true)
        {
            levelGenerator.Generate();

            EditorUtility.SetDirty(levelGenerator);
            EditorSceneManager.MarkSceneDirty(levelGenerator.gameObject.scene);
        }

        if (GUILayout.Button("Clear") == true)
        {
            levelGenerator.Clear();

            EditorUtility.SetDirty(levelGenerator);
            EditorSceneManager.MarkSceneDirty(levelGenerator.gameObject.scene);
        }
    }
}
