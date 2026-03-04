using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomNoiseProfile))]
[CanEditMultipleObjects]
public sealed class RoomNoiseProfileEditor : Editor
{
    private Texture2D _previewTexture;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty seedProperty = serializedObject.FindProperty("_seed");
        SerializedProperty offsetProperty = serializedObject.FindProperty("_offset");

        SerializedProperty frequencyProperty = serializedObject.FindProperty("_frequency");
        SerializedProperty fractalOctavesProperty = serializedObject.FindProperty("_fractalOctaves");
        SerializedProperty fractalGainProperty = serializedObject.FindProperty("_fractalGain");
        SerializedProperty fractalLacunarityProperty = serializedObject.FindProperty("_fractalLacunarity");

        SerializedProperty domainWarpEnabledProperty = serializedObject.FindProperty("_domainWarpEnabled");
        SerializedProperty domainWarpAmplitudeProperty = serializedObject.FindProperty("_domainWarpAmplitude");
        SerializedProperty domainWarpFrequencyProperty = serializedObject.FindProperty("_domainWarpFrequency");
        SerializedProperty domainWarpOctavesProperty = serializedObject.FindProperty("_domainWarpOctaves");
        SerializedProperty domainWarpGainProperty = serializedObject.FindProperty("_domainWarpGain");
        SerializedProperty domainWarpLacunarityProperty = serializedObject.FindProperty("_domainWarpLacunarity");

        SerializedProperty clearingEnabledProperty = serializedObject.FindProperty("_clearingEnabled");
        SerializedProperty clearingCenterProperty = serializedObject.FindProperty("_clearingCenter01");
        SerializedProperty clearingRadiusProperty = serializedObject.FindProperty("_clearingRadius01");
        SerializedProperty clearingFalloffProperty = serializedObject.FindProperty("_clearingFalloff01");
        SerializedProperty clearingStrengthProperty = serializedObject.FindProperty("_clearingStrength");
        SerializedProperty clearingEdgeFrequencyProperty = serializedObject.FindProperty("_clearingEdgeNoiseFrequency");
        SerializedProperty clearingEdgeAmplitudeProperty = serializedObject.FindProperty("_clearingEdgeNoiseAmplitude01");

        SerializedProperty contrastProperty = serializedObject.FindProperty("_contrast");
        SerializedProperty biasProperty = serializedObject.FindProperty("_bias");
        SerializedProperty invertProperty = serializedObject.FindProperty("_invert");
        SerializedProperty applySmoothstepProperty = serializedObject.FindProperty("_applySmoothstep");

        SerializedProperty previewResolutionProperty = serializedObject.FindProperty("_previewResolution");


        bool seedFoldout = DrawFoldout("RoomNoiseProfileEditor.Seed", "Сид и смещение", true);

        if (seedFoldout == true)
        {
            DrawProperty(seedProperty, "Сид");
            DrawProperty(offsetProperty, "Смещение");

            GUILayout.Space(6f);
        }


        bool baseNoiseFoldout = DrawFoldout("RoomNoiseProfileEditor.BaseNoise", "Базовый шум (fBm)", true);

        if (baseNoiseFoldout == true)
        {
            DrawProperty(frequencyProperty, "Частота");
            DrawProperty(fractalOctavesProperty, "Октавы");
            DrawProperty(fractalGainProperty, "Gain");
            DrawProperty(fractalLacunarityProperty, "Lacunarity");

            GUILayout.Space(6f);
        }


        bool warpFoldout = DrawFoldout("RoomNoiseProfileEditor.DomainWarp", "Domain Warp", true);

        if (warpFoldout == true)
        {
            DrawProperty(domainWarpEnabledProperty, "Включить");
            DrawProperty(domainWarpAmplitudeProperty, "Амплитуда");
            DrawProperty(domainWarpFrequencyProperty, "Частота");
            DrawProperty(domainWarpOctavesProperty, "Октавы");
            DrawProperty(domainWarpGainProperty, "Gain");
            DrawProperty(domainWarpLacunarityProperty, "Lacunarity");

            GUILayout.Space(6f);
        }


        bool clearingFoldout = DrawFoldout("RoomNoiseProfileEditor.Clearing", "Полянка (центр пустой)", true);

        if (clearingFoldout == true)
        {
            DrawProperty(clearingEnabledProperty, "Включить");
            DrawProperty(clearingCenterProperty, "Центр (0..1)");
            DrawProperty(clearingRadiusProperty, "Радиус (0..1)");
            DrawProperty(clearingFalloffProperty, "Переход (0..1)");
            DrawProperty(clearingStrengthProperty, "Сила (0..1)");
            DrawProperty(clearingEdgeFrequencyProperty, "Частота края");
            DrawProperty(clearingEdgeAmplitudeProperty, "Неровность края");

            GUILayout.Space(6f);
        }


        bool postFoldout = DrawFoldout("RoomNoiseProfileEditor.Post", "Постобработка", true);

        if (postFoldout == true)
        {
            DrawProperty(contrastProperty, "Контраст");
            DrawProperty(biasProperty, "Смещение");
            DrawProperty(invertProperty, "Инвертировать");
            DrawProperty(applySmoothstepProperty, "Smoothstep");

            GUILayout.Space(6f);
        }


        bool previewFoldout = DrawFoldout("RoomNoiseProfileEditor.Preview", "Превью", true);

        if (previewFoldout == true)
        {
            DrawProperty(previewResolutionProperty, "Разрешение");

            serializedObject.ApplyModifiedProperties();

            if (targets.Length != 1)
                return;


            RoomNoiseProfile noiseProfile = target as RoomNoiseProfile;

            if (noiseProfile == null)
                throw new System.InvalidOperationException(nameof(noiseProfile));


            int resolution = noiseProfile.PreviewResolution;

            if (resolution < 8)
                resolution = 8;


            float[,] noiseValues = noiseProfile.GenerateNoiseMap(resolution, resolution);

            EnsureTexture(resolution);

            Color[] pixels = new Color[resolution * resolution];

            int pixelIndex = 0;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float value = noiseValues[x, y];
                    pixels[pixelIndex] = new Color(value, value, value, 1f);
                    pixelIndex++;
                }
            }

            _previewTexture.SetPixels(pixels);
            _previewTexture.Apply(false, false);

            float size = Mathf.Min(EditorGUIUtility.currentViewWidth - 40f, 256f);
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));

            EditorGUI.DrawPreviewTexture(rect, _previewTexture, null, ScaleMode.ScaleToFit);

            return;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawProperty(SerializedProperty serializedProperty, string label)
    {
        if (serializedProperty == null)
            return;


        GUIContent guiContent = new GUIContent(label, serializedProperty.tooltip);
        EditorGUILayout.PropertyField(serializedProperty, guiContent, true);
    }

    private bool DrawFoldout(string key, string title, bool defaultValue)
    {
        bool currentValue = EditorPrefs.GetBool(key, defaultValue);
        bool newValue = EditorGUILayout.Foldout(currentValue, title, true);

        if (newValue != currentValue)
            EditorPrefs.SetBool(key, newValue);


        return newValue;
    }

    private void EnsureTexture(int resolution)
    {
        if (_previewTexture == null || _previewTexture.width != resolution || _previewTexture.height != resolution)
        {
            _previewTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
            _previewTexture.wrapMode = TextureWrapMode.Clamp;
            _previewTexture.filterMode = FilterMode.Point;
        }
    }
}
