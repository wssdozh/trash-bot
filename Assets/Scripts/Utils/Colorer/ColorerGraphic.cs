using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public static class ColorerGraphic
{
    public static void LerpToColor(Graphic graphic, Color targetColor, float duration, bool useUnscaledTime)
    {
        if (graphic == null)
        {
            return;
        }

        DOTween.Kill(graphic);

        graphic
            .DOColor(targetColor, duration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime)
            .SetId(graphic);
    }

    public static void Stop(Graphic graphic, Color baseColor, float duration, bool useUnscaledTime)
    {
        if (graphic == null)
        {
            return;
        }

        DOTween.Kill(graphic);

        graphic
            .DOColor(baseColor, duration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime)
            .SetId(graphic);
    }

    public static void Flash(Graphic graphic, Color flashColor, Color baseColor, float flashDuration, float returnDuration, bool useUnscaledTime)
    {
        if (graphic == null)
        {
            return;
        }

        DOTween.Kill(graphic);

        Tweener flashTweener = graphic
            .DOColor(flashColor, flashDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime)
            .SetId(graphic);

        flashTweener.OnComplete(() =>
        {
            graphic
                .DOColor(baseColor, returnDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(useUnscaledTime)
                .SetId(graphic);
        });
    }

    public static void FadeToTransparency(Graphic graphic, float targetTransparency, float duration, bool useUnscaledTime)
    {
        if (graphic == null)
        {
            return;
        }

        DOTween.Kill(graphic);

        graphic
            .DOFade(targetTransparency, duration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime)
            .SetId(graphic);
    }

    public static void StopFade(Graphic graphic)
    {
        if (graphic == null)
        {
            return;
        }

        DOTween.Kill(graphic);
    }
}
