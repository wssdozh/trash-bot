using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(WeightedPrefab))]
public sealed class WeightedPrefabDrawer : PropertyDrawer
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
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_weight"));
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_guaranteed"));

        if (IsEnemyEntry(property))
        {
            height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_spawnHeight"));
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect lineRect = new Rect(position.x, position.y, position.width, LineHeight);
        SerializedProperty prefabProperty = property.FindPropertyRelative("_prefab");
        string title = label.text;

        if (prefabProperty != null)
        {
            if (prefabProperty.objectReferenceValue != null)
            {
                title = prefabProperty.objectReferenceValue.name;
            }
        }

        property.isExpanded = EditorGUI.Foldout(lineRect, property.isExpanded, title, true);

        if (property.isExpanded == false)
        {
            EditorGUI.EndProperty();

            return;
        }

        EditorGUI.indentLevel = EditorGUI.indentLevel + 1;
        lineRect.y += LineHeight + Spacing;

        DrawProperty(ref lineRect, prefabProperty, "Префаб");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_weight"), "Вес");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_guaranteed"), "Гарант");

        if (IsEnemyEntry(property))
        {
            DrawProperty(ref lineRect, property.FindPropertyRelative("_spawnHeight"), "Высота");
        }

        EditorGUI.indentLevel = EditorGUI.indentLevel - 1;
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

    private void DrawProperty(ref Rect lineRect, SerializedProperty property, string label)
    {
        if (property == null)
        {
            return;
        }

        float height = EditorGUI.GetPropertyHeight(property, true);
        Rect propertyRect = new Rect(lineRect.x, lineRect.y, lineRect.width, height);
        GUIContent content = new GUIContent(label, property.tooltip);
        EditorGUI.PropertyField(propertyRect, property, content, true);
        lineRect.y += height + Spacing;
    }

    private bool IsEnemyEntry(SerializedProperty property)
    {
        if (property == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(property.propertyPath))
        {
            return false;
        }

        return property.propertyPath.Contains("_enemyPrefabs");
    }
}
