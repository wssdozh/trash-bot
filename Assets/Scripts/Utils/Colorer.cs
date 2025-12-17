using UnityEngine;
using DG.Tweening;

public static class Colorer
{
    private static readonly string BaseColorProperty = "_BaseColor";
    private static readonly string ColorProperty = "_Color";
    private static readonly string TransparencyProperty = "_Tweak_transparency";

    public static void LerpToColor(Renderer renderer, Color targetColor, float duration)
    {
        if (renderer == null)
            return;

        Material material = renderer.material;

        string colorProperty = GetColorProperty(material);

        DOTween.Kill(material);

        material
            .DOColor(targetColor, colorProperty, duration)
            .SetEase(Ease.InOutSine)
            .SetId(material);
    }

    public static void Stop(Renderer renderer, Color baseColor, float duration)
    {
        if (renderer == null)
            return;

        Material material = renderer.material;

        string colorProperty = GetColorProperty(material);

        DOTween.Kill(material);

        material
            .DOColor(baseColor, colorProperty, duration)
            .SetEase(Ease.InOutSine)
            .SetId(material);
    }

    public static void FadeToTransparency(Renderer renderer, float targetTransparency, float duration)
    {
        if (renderer == null)
            return;

        Material material = renderer.material;

        if (material.HasProperty(TransparencyProperty) == false)
            return;

        DOTween.Kill(material);

        material
            .DOFloat(targetTransparency, TransparencyProperty, duration)
            .SetEase(Ease.InOutSine)
            .SetId(material);
    }

    public static void StopFade(Renderer renderer)
    {
        if (renderer == null)
            return;

        Material material = renderer.material;
        DOTween.Kill(material);
    }

    private static string GetColorProperty(Material material)
    {
        if (material.HasProperty(BaseColorProperty) == true)
            return BaseColorProperty;

        if (material.HasProperty(ColorProperty) == true)
            return ColorProperty;

        return BaseColorProperty;
    }
}
