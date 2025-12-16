using UnityEngine;
using DG.Tweening;

public static class Colorer
{
    public static void LerpToColor(Renderer renderer, Color targetColor, float duration)
    {
        if (renderer == null)
            return;

        Material material = renderer.material;
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

        DOTween.Kill(material); 

        material.DOColor(targetColor, colorProperty, duration)
            .SetEase(Ease.InOutSine)
            .SetId(material);
    }

    public static void Stop(Renderer renderer, Color baseColor, float duration)
    {
        if (renderer == null)
            return;

        Material material = renderer.material;
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

        DOTween.Kill(material);
        
        material.DOColor(baseColor, colorProperty, duration)
            .SetEase(Ease.InOutSine)
            .SetId(material);
    }
}
