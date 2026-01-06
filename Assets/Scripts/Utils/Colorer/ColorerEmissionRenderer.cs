using UnityEngine;
using DG.Tweening;

public static class ToonRendererEmissiveColorer
{
    private static readonly string ToonEmissiveColorProperty = "_Emissive_Color";
    private static readonly string ToonEmissiveTexProperty = "_Emissive_Tex";

    private static readonly string ToonEmissiveKeywordSimple = "_EMISSIVE_SIMPLE";
    private static readonly string ToonEmissiveKeywordAnimation = "_EMISSIVE_ANIMATION";

    public static void LerpToEmission(Renderer renderer, Color targetColor, float duration, float intensity = 1.0f, bool useUnscaledTime = true)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;

        if (material.HasProperty(ToonEmissiveColorProperty) == false)
        {
            return;
        }

        EnableToonEmission(material);

        DOTween.Kill(material);

        material
            .DOColor(targetColor * intensity, ToonEmissiveColorProperty, duration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime)
            .SetId(material);
    }

    public static Color ReadBaseEmission(Renderer renderer)
    {
        if (renderer == null)
        {
            return Color.black;
        }

        Material material = renderer.material;

        if (material.HasProperty(ToonEmissiveColorProperty) == false)
        {
            return Color.black;
        }

        return material.GetColor(ToonEmissiveColorProperty);
    }

    public static void Stop(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;
        DOTween.Kill(material);
    }

    private static void EnableToonEmission(Material material)
    {
        material.DisableKeyword(ToonEmissiveKeywordAnimation);
        material.EnableKeyword(ToonEmissiveKeywordSimple);

        if (material.HasProperty(ToonEmissiveTexProperty) == true)
        {
            Texture emissiveTexture = material.GetTexture(ToonEmissiveTexProperty);

            if (emissiveTexture == null)
            {
                material.SetTexture(ToonEmissiveTexProperty, Texture2D.whiteTexture);
            }
        }
    }
}
