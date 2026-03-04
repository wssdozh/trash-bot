using UnityEngine;
using DG.Tweening;

public sealed class ColorerRenderer
{
    private const string BaseColorProperty = "_BaseColor";
    private const string ColorProperty = "_Color";
    private const string TransparencyProperty = "_Tweak_transparency";

    public void LerpToColor(Renderer renderer, Color targetColor, float duration, bool useUnscaledTime = true)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;
        string colorProperty = GetColorProperty(material);

        DOTween.Kill(material);

        material
            .DOColor(targetColor, colorProperty, duration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime)
            .SetId(material);
    }

    public void Stop(Renderer renderer, Color baseColor, float duration, bool useUnscaledTime = true)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;
        string colorProperty = GetColorProperty(material);

        DOTween.Kill(material);

        material
            .DOColor(baseColor, colorProperty, duration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime)
            .SetId(material);
    }

    public void FadeToTransparency(Renderer renderer, float targetTransparency, float duration, bool useUnscaledTime = true)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;

        if (material.HasProperty(TransparencyProperty) == false)
        {
            return;
        }

        DOTween.Kill(material);

        material
            .DOFloat(targetTransparency, TransparencyProperty, duration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime)
            .SetId(material);
    }

    public void StopFade(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;
        DOTween.Kill(material);
    }

    private string GetColorProperty(Material material)
    {
        if (material.HasProperty(BaseColorProperty))
        {
            return BaseColorProperty;
        }

        if (material.HasProperty(ColorProperty))
        {
            return ColorProperty;
        }

        return BaseColorProperty;
    }
}
