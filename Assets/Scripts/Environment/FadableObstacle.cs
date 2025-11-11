using UnityEngine;
using DG.Tweening;

public class FadableObstacle : MonoBehaviour, IFadable
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private FadableSettings _settings;

    private MaterialPropertyBlock _block;
    private float _currentAlpha = 1f;
    private Tween _tween;
    private float _fadeDuration = 0.25f;
    private float _occludedAlpha = 0.35f;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        
        if (_settings != null)
        {
            _fadeDuration = _settings.FadeDuration;
            _occludedAlpha = _settings.OccludedAlpha;
        }
    }

    public void OnOccluded()
    {
        FadeTo(_occludedAlpha);
    }

    public void OnVisible()
    {
        FadeTo(1f);
    }

    private void FadeTo(float targetAlpha)
    {
        if (_tween != null && _tween.IsActive() == true)
        {
            _tween.Kill();
        }
        _tween = DOTween.To(() => _currentAlpha, SetAlpha, targetAlpha, _fadeDuration);
    }

    private void SetAlpha(float alpha)
    {
        _currentAlpha = alpha;
        _renderer.GetPropertyBlock(_block);
        Color color = _renderer.sharedMaterial.GetColor("_BaseColor");
        color.a = _currentAlpha;
        _block.SetColor("_BaseColor", color);
        _renderer.SetPropertyBlock(_block);
    }
}
