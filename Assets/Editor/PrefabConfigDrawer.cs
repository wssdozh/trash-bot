using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NookPrefabConfig))]
public sealed class NookPrefabConfigDrawer : PropertyDrawer
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

        height += GetSectionHeight();
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_prefab"));
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_weight"));

        height += GetSectionHeight();
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_guaranteed"));
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_countRange"));

        height += GetSectionHeight();
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_minimumDistanceFromCorridorInCells"));
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_wallMarginInCells"));
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_footprintRadiusInCells"));

        height += GetSectionHeight();
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_minimumDistanceToAnyNookInCells"));
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_minimumDistanceToSameTypeInCells"));
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_sameTypeNeighborRadiusInCells"));
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_maximumSameTypeWithinNeighborRadius"));

        height += GetSectionHeight();
        height += GetPropertyHeightWithSpacing(property.FindPropertyRelative("_scatterRadiusInCells"));

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect lineRect = new Rect(position.x, position.y, position.width, LineHeight);

        SerializedProperty prefabProperty = property.FindPropertyRelative("_prefab");
        SerializedProperty guaranteedProperty = property.FindPropertyRelative("_guaranteed");

        string title = "Закоулок (POI)";

        if (prefabProperty != null && prefabProperty.objectReferenceValue != null)
        {
            title = prefabProperty.objectReferenceValue.name;
        }

        if (guaranteedProperty != null && guaranteedProperty.boolValue == true)
        {
            title = "★ " + title;
        }

        property.isExpanded = EditorGUI.Foldout(lineRect, property.isExpanded, title, true);

        if (property.isExpanded == false)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;
        lineRect.y += LineHeight + Spacing;

        DrawSection(ref lineRect, "Префаб и вес");
        DrawProperty(ref lineRect, prefabProperty, "Префаб");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_weight"), "Вес");

        DrawSection(ref lineRect, "Количество на комнате");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_guaranteed"), "Гарантировать появление");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_countRange"), "Количество (мин–макс)");

        DrawSection(ref lineRect, "Требования к позиции");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_minimumDistanceFromCorridorInCells"), "Мин. дистанция от тропы");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_wallMarginInCells"), "Отступ от стен");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_footprintRadiusInCells"), "Требуемое место (радиус)");

        DrawSection(ref lineRect, "Соседство");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_minimumDistanceToAnyNookInCells"), "Мин. дистанция до любого POI");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_minimumDistanceToSameTypeInCells"), "Мин. дистанция до такого же POI");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_sameTypeNeighborRadiusInCells"), "Радиус проверки одинаковых");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_maximumSameTypeWithinNeighborRadius"), "Макс. одинаковых рядом");

        DrawSection(ref lineRect, "Выбор точки в кармане");
        DrawProperty(ref lineRect, property.FindPropertyRelative("_scatterRadiusInCells"), "Разброс от лучшей точки");

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    private float GetSectionHeight()
    {
        return LineHeight + Spacing;
    }

    private float GetPropertyHeightWithSpacing(SerializedProperty property)
    {
        if (property == null)
        {
            return 0f;
        }

        return EditorGUI.GetPropertyHeight(property, true) + Spacing;
    }

    private void DrawSection(ref Rect lineRect, string title)
    {
        EditorGUI.LabelField(lineRect, title, EditorStyles.boldLabel);
        lineRect.y += LineHeight + Spacing;
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
