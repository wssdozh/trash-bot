using UnityEngine;

public class FadableObstacle : MonoBehaviour, IFadable
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private FadableSettings _settings;

    private float _visibleTransparencyLevel = 0f;
    private float _fadeDuration = 0.25f;
    private float _occludedTransparencyLevel = -0.35f;

    private void Awake()
    {
        if (_settings != null)
        {
            _fadeDuration = _settings.FadeDuration;
            _occludedTransparencyLevel = _settings.OccludedAlpha;
        }

        Material material = _renderer.material;

        if (material.HasProperty("_Tweak_transparency") == true)
            _visibleTransparencyLevel = material.GetFloat("_Tweak_transparency");
    }

    public void OnOccluded()
    {
        ColorerRenderer.FadeToTransparency(_renderer, _occludedTransparencyLevel, _fadeDuration);
    }

    public void OnVisible()
    {
        ColorerRenderer.FadeToTransparency(_renderer, _visibleTransparencyLevel, _fadeDuration);
    }
}
