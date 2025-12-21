using UnityEngine;

public class HighlightAnimator : HighlighterBase
{
    [Header("Настройки подсветки")]
    [SerializeField] private Color _highlightColor = Color.yellow;
    [SerializeField] private float _blinkTime = 0.7f;

    private Renderer[] _renderers;
    private Color[] _originalColors;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _originalColors = new Color[_renderers.Length];

        for (int index = 0; index < _renderers.Length; index++)
        {
            Material material = _renderers[index].sharedMaterial;

            if (material.HasProperty("_BaseColor") == true)
            {
                _originalColors[index] = material.GetColor("_BaseColor");
            }
            else
            {
                _originalColors[index] = Color.white;
            }
        }
    }

    protected override void EnableHighlight()
    {
        for (int index = 0; index < _renderers.Length; index++)
        {
            ColorerRenderer.LerpToColor(_renderers[index], _highlightColor, _blinkTime);
        }
    }

    protected override void DisableHighlight()
    {
        for (int index = 0; index < _renderers.Length; index++)
        {
            ColorerRenderer.Stop(_renderers[index], _originalColors[index], _blinkTime);
        }
    }
}
