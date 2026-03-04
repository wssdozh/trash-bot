using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public sealed class FractureFx : MonoBehaviour
{
    [SerializeField] private float _explosionForce = 3f;
    [SerializeField] private float _explosionRadius = 2f;
    [SerializeField] private float _upwardsModifier = 0.5f;

    [SerializeField] private float _fadeDelay = 0.3f;
    [SerializeField] private float _fadeDuration = 0.8f;
    [SerializeField] private float _targetTransparency = 1f;

    [SerializeField] private float _destroyDelayAfterFade = 0.05f;

    [SerializeField] private List<Rigidbody> _rigidbodies = new List<Rigidbody>();
    [SerializeField] private List<Renderer> _renderers = new List<Renderer>();

    private readonly ColorerRenderer _colorerRenderer = new ColorerRenderer();
    private bool _played = false;

    [ContextMenu("Collect Children")]
    public void CollectChildren()
    {
        _rigidbodies = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>(true));
        _renderers = new List<Renderer>(GetComponentsInChildren<Renderer>(true));
    }

    public void Play()
    {
        if (_played)
        {
            return;
        }

        _played = true;

        if (_rigidbodies == null || _rigidbodies.Count == 0)
        {
            CollectChildren();
        }

        Vector3 explosionPosition = transform.position;

        int rigidbodyIndex = 0;

        while (rigidbodyIndex < _rigidbodies.Count)
        {
            Rigidbody rigidbody = _rigidbodies[rigidbodyIndex];

            if (rigidbody != null)
            {
                rigidbody.isKinematic = false;
                rigidbody.useGravity = true;
                rigidbody.AddExplosionForce(_explosionForce, explosionPosition, _explosionRadius, _upwardsModifier, ForceMode.Impulse);
            }

            rigidbodyIndex++;
        }

        StartCoroutine(FadeAfterDelayCoroutine());

        float totalDelay = _fadeDelay + _fadeDuration + _destroyDelayAfterFade;

        transform
            .DOScale(transform.localScale, 0.0001f)
            .SetDelay(totalDelay)
            .SetUpdate(true)
            .OnComplete(OnFadeComplete);
    }

    private IEnumerator FadeAfterDelayCoroutine()
    {
        if (_fadeDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(_fadeDelay);
        }

        int rendererIndex = 0;

        while (rendererIndex < _renderers.Count)
        {
            Renderer renderer = _renderers[rendererIndex];

            if (renderer != null)
            {
                _colorerRenderer.FadeToTransparency(renderer, _targetTransparency, _fadeDuration, false);
            }

            rendererIndex++;
        }
    }

    private void OnFadeComplete()
    {
        Destroy(gameObject);
    }
}
