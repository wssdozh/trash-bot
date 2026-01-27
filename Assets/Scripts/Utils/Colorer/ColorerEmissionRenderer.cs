using UnityEngine;
using DG.Tweening;

public static class ToonRendererEmissiveColorer
{
    private static readonly string EmissionColorProperty = "_EmissionColor";
    private static readonly string EmissionIntensityProperty = "_EmissionIntensity";

    public static void LerpToEmission(
        Renderer renderer,
        Color targetColor,
        float duration,
        float intensity = 1.0f,
        bool useUnscaledTime = true)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;

        if (material.HasProperty(EmissionColorProperty) == false)
        {
            return;
        }

        DOTween.Kill(material);

        if (material.HasProperty(EmissionIntensityProperty) == true)
        {
            material
                .DOColor(targetColor, EmissionColorProperty, duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(useUnscaledTime)
                .SetId(material);

            material
                .DOFloat(intensity, EmissionIntensityProperty, duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(useUnscaledTime)
                .SetId(material);

            return;
        }

        material
            .DOColor(targetColor * intensity, EmissionColorProperty, duration)
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

        if (material.HasProperty(EmissionColorProperty) == false)
        {
            return Color.black;
        }

        Color emissionColor = material.GetColor(EmissionColorProperty);

        if (material.HasProperty(EmissionIntensityProperty) == false)
        {
            return emissionColor;
        }

        float emissionIntensity = material.GetFloat(EmissionIntensityProperty);

        return emissionColor * emissionIntensity;
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
}
