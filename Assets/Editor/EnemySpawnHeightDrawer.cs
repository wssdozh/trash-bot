using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnemySpawnHeight))]
public sealed class EnemySpawnHeightDrawer : PropertyDrawer
{
    private float LineHeight => EditorGUIUtility.singleLineHeight;
    private float Spacing => EditorGUIUtility.standardVerticalSpacing;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = LineHeight;

        if (property.isExpanded == false)
        {
            return height;
        }

        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_prefab"));
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_spawnHeight"));

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect lineRect = new Rect(position.x, position.y, position.width, LineHeight);
        SerializedProperty prefabProperty = property.FindPropertyRelative("_prefab");
        string title = "Высота врага";

        if (prefabProperty != null && prefabProperty.objectReferenceValue != null)
        {
            title = prefabProperty.objectReferenceValue.name;
        }

        property.isExpanded = EditorGUI.Foldout(lineRect, property.isExpanded, title, true);

        if (property.isExpanded == false)
        {
            EditorGUI.EndProperty();

            return;
        }

        EditorGUI.indentLevel++;
        lineRect.y += LineHeight + Spacing;

        DrawProperty(ref lineRect, prefabProperty, "Префаб");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_spawnHeight"), "Высота");

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    private float GetPropertyHeightWithSpacing(SerializedProperty property)
    {
        if (property == null)
        {
            return 0f;
        }

        return EditorGUI.GetPropertyHeight(property, true) + Spacing;
    }

    private void DrawProperty(ref Rect lineRect, SerializedProperty property, string russianLabel)
    {
        if (property == null)
        {
            return;
        }

        float height = EditorGUI.GetPropertyHeight(property, true);
        Rect rect = new Rect(lineRect.x, lineRect.y, lineRect.width, height);
        GUIContent content = new GUIContent(russianLabel, property.tooltip);
        EditorGUI.PropertyField(rect, property, content, true);
        lineRect.y += height + Spacing;
    }
}
