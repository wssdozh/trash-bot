using System;
using UnityEngine;
using DG.Tweening;

public static class ToonRendererEmissiveColorer
{
    private static readonly string EmissionColorProperty = "_EmissionColor";
    private static readonly string EmissionIntensityProperty = "_EmissionIntensity";

    public static void LerpToEmission(Renderer renderer, Color targetColor, float duration, float intensity = 1.0f, bool useUnscaledTime = true)
    {
        if (renderer == null)
        {
            throw new InvalidOperationException(nameof(renderer));
        }

        Material material = renderer.material;

        if (material.HasProperty(EmissionColorProperty) == false)
        {
            throw new InvalidOperationException(nameof(EmissionColorProperty));
        }

        if (material.HasProperty(EmissionIntensityProperty) == false)
        {
            throw new InvalidOperationException(nameof(EmissionIntensityProperty));
        }

        DOTween.Kill(material);

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
    }

    public static Color ReadBaseEmission(Renderer renderer)
    {
        if (renderer == null)
        {
            throw new InvalidOperationException(nameof(renderer));
        }

        Material material = renderer.material;

        if (material.HasProperty(EmissionColorProperty) == false)
        {
            throw new InvalidOperationException(nameof(EmissionColorProperty));
        }

        return material.GetColor(EmissionColorProperty);
    }

    public static float ReadBaseEmissionIntensity(Renderer renderer)
    {
        if (renderer == null)
        {
            throw new InvalidOperationException(nameof(renderer));
        }

        Material material = renderer.material;

        if (material.HasProperty(EmissionIntensityProperty) == false)
        {
            throw new InvalidOperationException(nameof(EmissionIntensityProperty));
        }

        return material.GetFloat(EmissionIntensityProperty);
    }

    public static void Stop(Renderer renderer)
    {
        if (renderer == null)
        {
            throw new InvalidOperationException(nameof(renderer));
        }

        Material material = renderer.material;

        DOTween.Kill(material);
    }
}
