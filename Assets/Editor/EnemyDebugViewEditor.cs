using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyDebugView))]
public sealed class EnemyDebugViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(6f);

        EnemyDebugView enemyDebugView = target as EnemyDebugView;
        if (enemyDebugView == null)
        {
            return;
        }

        EditorGUI.BeginDisabledGroup(Application.isPlaying == false);
        if (GUILayout.Button("Copy Debug Snapshot") == true)
        {
            string snapshot = enemyDebugView.BuildDebugSnapshot();
            EditorGUIUtility.systemCopyBuffer = snapshot;
        }
        EditorGUI.EndDisabledGroup();
    }
}
