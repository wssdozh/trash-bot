using UnityEngine;
using DG.Tweening;

public sealed class ToonRendererEmissiveColorer
{
    private const string EmissionColorProperty = "_EmissionColor";
    private const string EmissionIntensityProperty = "_EmissionIntensity";

    public void LerpToEmission(Renderer renderer, Color targetColor, float duration, float intensity = 1.0f, bool useUnscaledTime = true)
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

        if (material.HasProperty(EmissionIntensityProperty))
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

    public Color ReadBaseEmission(Renderer renderer)
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

    public void Stop(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;

        DOTween.Kill(material);
    }
}
