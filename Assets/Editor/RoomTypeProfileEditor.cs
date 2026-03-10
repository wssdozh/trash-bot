using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomTypeProfile))]
[CanEditMultipleObjects]
public sealed class RoomTypeProfileEditor : Editor
{
    private struct PropertyEntry
    {
        public string PropertyPath;
        public string NormalizedName;

        public PropertyEntry(string propertyPath, string normalizedName)
        {
            PropertyPath = propertyPath;
            NormalizedName = normalizedName;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();

        List<PropertyEntry> mainEntries = new List<PropertyEntry>();
        List<PropertyEntry> fillEntries = new List<PropertyEntry>();
        List<PropertyEntry> heightEntries = new List<PropertyEntry>();
        List<PropertyEntry> bigEntries = new List<PropertyEntry>();
        List<PropertyEntry> enemyEntries = new List<PropertyEntry>();
        List<PropertyEntry> objectEntries = new List<PropertyEntry>();
        List<PropertyEntry> nookEntries = new List<PropertyEntry>();
        List<PropertyEntry> otherEntries = new List<PropertyEntry>();

        CollectEntries(mainEntries, fillEntries, heightEntries, bigEntries, enemyEntries, objectEntries, nookEntries, otherEntries);

        SortEntries(mainEntries, new string[2] { "roomType", "noiseProfile" });
        SortEntries(fillEntries, new string[2] { "blockFillPercent", "largeCubeAreaPercent" });
        SortEntries(heightEntries, new string[3] { "minimumStackHeightInBlocks", "maximumStackHeightInBlocks", "heightExponent" });
        SortEntries(bigEntries, new string[2] { "largeCubeStackHeightRange", "randomYawRotation" });
        SortEntries(enemyEntries, new string[2] { "enemySpawnCountRange", "enemyPrefabs" });
        SortEntries(objectEntries, new string[2] { "objectSpawnCountRange", "objectPrefabs" });
        SortEntries(nookEntries, new string[8]
        {
            "nookPrefabs",
            "nookSpawnCountRange",
            "nookItemsPerNookRange",
            "nookMinimumDistanceFromCorridorInCells",
            "nookMinimumAreaInCells",
            "nookScatterRadiusInCells",
            "nookWallMarginInCells",
            "nookFootprintRadiusInCells"
        });

        DrawSection("RoomTypeProfileEditor.Main", "Основное", mainEntries, true);
        DrawSection("RoomTypeProfileEditor.Fill", "Заполнение", fillEntries, true);
        DrawSection("RoomTypeProfileEditor.Height", "Высота", heightEntries, true);
        DrawSection("RoomTypeProfileEditor.Big", "Крупные блоки", bigEntries, true);
        DrawSection("RoomTypeProfileEditor.Enemies", "Враги", enemyEntries, true);
        DrawSection("RoomTypeProfileEditor.Objects", "Ресурсы", objectEntries, true);
        DrawSection("RoomTypeProfileEditor.Nooks", "Закоулки", nookEntries, true);
        DrawSection("RoomTypeProfileEditor.Other", "Прочее", otherEntries, false);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");

        if (scriptProperty == null)
        {
            return;
        }

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(scriptProperty);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(6f);
    }

    private void DrawSection(string key, string title, List<PropertyEntry> entries, bool defaultValue)
    {
        if (entries.Count == 0)
        {
            return;
        }

        bool isOpen = EditorPrefs.GetBool(key, defaultValue);
        bool nextIsOpen = EditorGUILayout.Foldout(isOpen, title, true);

        if (nextIsOpen != isOpen)
        {
            EditorPrefs.SetBool(key, nextIsOpen);
        }

        if (nextIsOpen == false)
        {
            return;
        }

        EditorGUI.indentLevel = EditorGUI.indentLevel + 1;

        for (int index = 0; index < entries.Count; index++)
        {
            SerializedProperty property = serializedObject.FindProperty(entries[index].PropertyPath);

            if (property == null)
            {
                continue;
            }

            string label = GetLocalizedLabel(entries[index].NormalizedName, ObjectNames.NicifyVariableName(property.name));
            GUIContent guiContent = new GUIContent(label, property.tooltip);
            EditorGUILayout.PropertyField(property, guiContent, true);
        }

        EditorGUI.indentLevel = EditorGUI.indentLevel - 1;

        GUILayout.Space(4f);
    }

    private void CollectEntries(
        List<PropertyEntry> mainEntries,
        List<PropertyEntry> fillEntries,
        List<PropertyEntry> heightEntries,
        List<PropertyEntry> bigEntries,
        List<PropertyEntry> enemyEntries,
        List<PropertyEntry> objectEntries,
        List<PropertyEntry> nookEntries,
        List<PropertyEntry> otherEntries
    )
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.propertyPath == "m_Script")
            {
                continue;
            }

            if (iterator.depth != 0)
            {
                continue;
            }

            string normalizedName = NormalizeName(iterator.name);
            PropertyEntry propertyEntry = new PropertyEntry(iterator.propertyPath, normalizedName);

            if (IsMain(normalizedName))
            {
                mainEntries.Add(propertyEntry);

                continue;
            }

            if (IsFill(normalizedName))
            {
                fillEntries.Add(propertyEntry);

                continue;
            }

            if (IsHeight(normalizedName))
            {
                heightEntries.Add(propertyEntry);

                continue;
            }

            if (IsBig(normalizedName))
            {
                bigEntries.Add(propertyEntry);

                continue;
            }

            if (IsEnemies(normalizedName))
            {
                enemyEntries.Add(propertyEntry);

                continue;
            }

            if (IsObjects(normalizedName))
            {
                objectEntries.Add(propertyEntry);

                continue;
            }

            if (IsNooks(normalizedName))
            {
                nookEntries.Add(propertyEntry);

                continue;
            }

            otherEntries.Add(propertyEntry);
        }
    }

    private void SortEntries(List<PropertyEntry> entries, string[] preferredOrder)
    {
        entries.Sort((left, right) =>
        {
            int leftIndex = IndexOf(preferredOrder, left.NormalizedName);
            int rightIndex = IndexOf(preferredOrder, right.NormalizedName);

            if (leftIndex == -1 && rightIndex == -1)
            {
                return string.Compare(left.NormalizedName, right.NormalizedName, System.StringComparison.Ordinal);
            }

            if (leftIndex == -1)
            {
                return 1;
            }

            if (rightIndex == -1)
            {
                return -1;
            }

            return leftIndex.CompareTo(rightIndex);
        });
    }

    private int IndexOf(string[] array, string value)
    {
        for (int index = 0; index < array.Length; index++)
        {
            if (array[index] == value)
            {
                return index;
            }
        }

        return -1;
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        if (name.StartsWith("_"))
        {
            return name.Substring(1);
        }

        return name;
    }

    private bool IsMain(string normalizedName)
    {
        return normalizedName == "roomType" || normalizedName == "noiseProfile";
    }

    private bool IsFill(string normalizedName)
    {
        return normalizedName.StartsWith("blockFill") || normalizedName.StartsWith("largeCubeArea");
    }

    private bool IsHeight(string normalizedName)
    {
        return normalizedName.Contains("stackHeight") || normalizedName.Contains("heightExponent");
    }

    private bool IsBig(string normalizedName)
    {
        return normalizedName.Contains("largeCube") || normalizedName.Contains("randomYaw");
    }

    private bool IsEnemies(string normalizedName)
    {
        return normalizedName.StartsWith("enemy");
    }

    private bool IsObjects(string normalizedName)
    {
        return normalizedName.StartsWith("object");
    }

    private bool IsNooks(string normalizedName)
    {
        return normalizedName.StartsWith("nook");
    }

    private string GetLocalizedLabel(string normalizedName, string fallback)
    {
        if (normalizedName == "roomType") return "Тип комнаты";
        if (normalizedName == "noiseProfile") return "Профиль шума";
        if (normalizedName == "blockFillPercent") return "Заполнение (%)";
        if (normalizedName == "largeCubeAreaPercent") return "Доля 2x2";
        if (normalizedName == "minimumStackHeightInBlocks") return "Мин. высота";
        if (normalizedName == "maximumStackHeightInBlocks") return "Макс. высота";
        if (normalizedName == "heightExponent") return "Экспонента";
        if (normalizedName == "largeCubeStackHeightRange") return "Ярусы 2x2";
        if (normalizedName == "randomYawRotation") return "Случайный Y";
        if (normalizedName == "enemySpawnCountRange") return "Количество врагов";
        if (normalizedName == "enemyPrefabs") return "Префабы врагов";
        if (normalizedName == "objectSpawnCountRange") return "Количество ресурсов";
        if (normalizedName == "objectPrefabs") return "Префабы ресурсов";
        if (normalizedName == "nookPrefabs") return "Префабы POI";
        if (normalizedName == "nookSpawnCountRange") return "Кол-во закоулков";
        if (normalizedName == "nookItemsPerNookRange") return "Предметов в закоулке";
        if (normalizedName == "nookMinimumDistanceFromCorridorInCells") return "Мин. дистанция";
        if (normalizedName == "nookMinimumAreaInCells") return "Мин. размер";
        if (normalizedName == "nookScatterRadiusInCells") return "Разброс";
        if (normalizedName == "nookWallMarginInCells") return "Отступ от стен";
        if (normalizedName == "nookFootprintRadiusInCells") return "Требуемая площадь";

        return fallback;
    }
}
