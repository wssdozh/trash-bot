using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(RoomGenerator))]
public sealed class RoomGeneratorEditor : Editor
{
    private Texture2D _currentNoiseTexture;
    private Texture2D _beforeNoiseTexture;
    private Texture2D _differenceNoiseTexture;

    private float[,] _beforeNoiseValues;
    private int _beforeNoiseWidth;
    private int _beforeNoiseDepth;
    private bool _hasBeforeNoise;

    public override void OnInspectorGUI()
    {
        RoomGenerator roomGenerator = target as RoomGenerator;
        if (roomGenerator == null)
        {
            return;
        }

        bool parametersFoldout = DrawFoldout("RoomGeneratorEditor.Parameters", "Параметры", true);
        if (parametersFoldout == true)
        {
            DrawDefaultInspector();
            GUILayout.Space(6f);
        }

        bool actionsFoldout = DrawFoldout("RoomGeneratorEditor.Actions", "Действия", true);
        if (actionsFoldout == true)
        {
            if (GUILayout.Button("Regenerate") == true)
            {
                roomGenerator.Generate();
                EditorUtility.SetDirty(roomGenerator);
                EditorSceneManager.MarkSceneDirty(roomGenerator.gameObject.scene);
            }

            if (GUILayout.Button("Clear") == true)
            {
                roomGenerator.Clear();
                EditorUtility.SetDirty(roomGenerator);
                EditorSceneManager.MarkSceneDirty(roomGenerator.gameObject.scene);
            }
        }

        DrawNoisePreviewSection();
    }

    private void OnDisable()
    {
        DestroyTexture(ref _currentNoiseTexture);
        DestroyTexture(ref _beforeNoiseTexture);
        DestroyTexture(ref _differenceNoiseTexture);
    }

    private void DrawNoisePreviewSection()
    {
        bool previewFoldout = DrawFoldout("RoomGeneratorEditor.NoisePreview", "Noise Preview (Before/After)", true);
        if (previewFoldout == false)
        {
            return;
        }

        serializedObject.Update();

        SerializedProperty roomSizeProperty = serializedObject.FindProperty("_roomSizeInBlocks");
        SerializedProperty roomTypeProfileProperty = serializedObject.FindProperty("_roomTypeProfile");
        SerializedProperty randomSeedProperty = serializedObject.FindProperty("_randomSeed");

        if (roomSizeProperty == null || roomTypeProfileProperty == null || randomSeedProperty == null)
        {
            EditorGUILayout.HelpBox("Cannot read room generator properties for noise preview.", MessageType.Warning);
            return;
        }

        RoomTypeProfile roomTypeProfile = roomTypeProfileProperty.objectReferenceValue as RoomTypeProfile;
        if (roomTypeProfile == null)
        {
            EditorGUILayout.HelpBox("Assign Room Type Profile to preview noise.", MessageType.Info);
            return;
        }

        RoomNoiseProfile noiseProfile = roomTypeProfile.NoiseProfile;
        if (noiseProfile == null)
        {
            EditorGUILayout.HelpBox("Assigned Room Type Profile has no Noise Profile.", MessageType.Info);
            return;
        }

        Vector3Int roomSizeInBlocks = roomSizeProperty.vector3IntValue;
        int width = Mathf.Max(1, roomSizeInBlocks.x);
        int depth = Mathf.Max(1, roomSizeInBlocks.z);
        int seed = randomSeedProperty.intValue;

        float[,] currentNoiseValues;

        noiseProfile.SetRuntimeSeed(seed);

        try
        {
            currentNoiseValues = noiseProfile.GenerateNoiseMap(width, depth);
        }
        finally
        {
            noiseProfile.ClearRuntimeSeed();
        }

        EditorGUILayout.LabelField("Profile", noiseProfile.name);
        EditorGUILayout.LabelField("Size", width + " x " + depth);
        EditorGUILayout.LabelField("Seed", seed.ToString());

        GUILayout.Space(4f);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Capture Before") == true)
        {
            CaptureBeforeNoise(currentNoiseValues, width, depth);
        }

        EditorGUI.BeginDisabledGroup(_hasBeforeNoise == false);
        if (GUILayout.Button("Clear Before") == true)
        {
            ClearBeforeNoise();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        EnsureTexture(ref _currentNoiseTexture, width, depth);
        FillNoiseTexture(_currentNoiseTexture, currentNoiseValues);
        DrawTexturePreview("Current", _currentNoiseTexture);

        if (_hasBeforeNoise == false)
        {
            EditorGUILayout.HelpBox("Press 'Capture Before' and then tweak settings to compare before/after.", MessageType.None);
            return;
        }

        if (_beforeNoiseWidth != width || _beforeNoiseDepth != depth)
        {
            EditorGUILayout.HelpBox("Before snapshot size differs from current room size. Capture again.", MessageType.Warning);
            return;
        }

        EnsureTexture(ref _beforeNoiseTexture, width, depth);
        FillNoiseTexture(_beforeNoiseTexture, _beforeNoiseValues);
        DrawTexturePreview("Before", _beforeNoiseTexture);

        EnsureTexture(ref _differenceNoiseTexture, width, depth);
        FillDifferenceTexture(_differenceNoiseTexture, _beforeNoiseValues, currentNoiseValues);
        DrawTexturePreview("Difference", _differenceNoiseTexture);
    }

    private void CaptureBeforeNoise(float[,] noiseValues, int width, int depth)
    {
        _beforeNoiseWidth = width;
        _beforeNoiseDepth = depth;
        _beforeNoiseValues = CloneNoiseValues(noiseValues, width, depth);
        _hasBeforeNoise = true;
    }

    private void ClearBeforeNoise()
    {
        _beforeNoiseValues = null;
        _beforeNoiseWidth = 0;
        _beforeNoiseDepth = 0;
        _hasBeforeNoise = false;
    }

    private float[,] CloneNoiseValues(float[,] source, int width, int depth)
    {
        float[,] copy = new float[width, depth];

        for (int y = 0; y < depth; y++)
        {
            for (int x = 0; x < width; x++)
            {
                copy[x, y] = source[x, y];
            }
        }

        return copy;
    }

    private void DrawTexturePreview(string title, Texture2D texture)
    {
        GUILayout.Space(6f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        float maxWidth = Mathf.Max(64f, EditorGUIUtility.currentViewWidth - 40f);

        float ratio = 1f;
        if (texture != null && texture.height > 0)
        {
            ratio = (float)texture.width / texture.height;
        }

        float previewWidth = Mathf.Min(maxWidth, 320f);
        float previewHeight = previewWidth;

        if (ratio > 0.0001f)
        {
            previewHeight = previewWidth / ratio;
        }

        Rect rect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false));
        EditorGUI.DrawPreviewTexture(rect, texture, null, ScaleMode.ScaleToFit);
    }

    private void EnsureTexture(ref Texture2D texture, int width, int height)
    {
        if (texture != null && texture.width == width && texture.height == height)
        {
            return;
        }

        DestroyTexture(ref texture);

        texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
    }

    private void DestroyTexture(ref Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        Object.DestroyImmediate(texture);
        texture = null;
    }

    private void FillNoiseTexture(Texture2D texture, float[,] values)
    {
        int width = texture.width;
        int height = texture.height;

        Color[] pixels = new Color[width * height];
        int pixelIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = values[x, y];
                pixels[pixelIndex] = new Color(value, value, value, 1f);
                pixelIndex++;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
    }

    private void FillDifferenceTexture(Texture2D texture, float[,] beforeValues, float[,] currentValues)
    {
        int width = texture.width;
        int height = texture.height;

        Color[] pixels = new Color[width * height];
        int pixelIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float before = beforeValues[x, y];
                float current = currentValues[x, y];

                float diff = Mathf.Abs(current - before) * 3f;
                diff = Mathf.Clamp01(diff);

                pixels[pixelIndex] = new Color(diff, diff, diff, 1f);
                pixelIndex++;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
    }

    private bool DrawFoldout(string key, string title, bool defaultValue)
    {
        bool currentValue = EditorPrefs.GetBool(key, defaultValue);
        bool newValue = EditorGUILayout.Foldout(currentValue, title, true);

        if (newValue != currentValue)
        {
            EditorPrefs.SetBool(key, newValue);
        }

        return newValue;
    }
}
