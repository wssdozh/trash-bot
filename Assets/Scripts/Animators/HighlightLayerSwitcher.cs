using UnityEngine;

public class HighlightLayerSwitcher : HighlighterBase
{
    [Header("Настройки слоя подсветки")]
    [SerializeField] private RenderingLayerMask _highlightLayer = RenderingLayerMask.defaultRenderingLayerMask;

    private Renderer[] _renderers;
    private RenderingLayerMask[] _originalLayers;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _originalLayers = new RenderingLayerMask[_renderers.Length];

        for (int index = 0; index < _renderers.Length; index++)
        {
            _originalLayers[index] = _renderers[index].renderingLayerMask;
        }
    }

    protected override void EnableHighlight()
    {
        for (int index = 0; index < _renderers.Length; index++)
        {
            _renderers[index].renderingLayerMask = _highlightLayer;
        }
    }

    protected override void DisableHighlight()
    {
        for (int index = 0; index < _renderers.Length; index++)
        {
            _renderers[index].renderingLayerMask = _originalLayers[index];
        }
    }
}
