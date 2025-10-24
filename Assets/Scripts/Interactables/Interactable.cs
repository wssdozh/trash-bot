using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Header("Настройка")]
    [SerializeField] protected string _interactionName = "Взаимодействовать";
    [SerializeField] protected Transform _interactionPoint;

    [Header("Подсветка")]
    [SerializeField] protected Color _highlightColor = Color.yellow;
    [SerializeField] protected float _blinkTime = 0.7f;

    protected Color[] _originalColors;
    protected Renderer[] _renderers;

    protected virtual void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        { 
            _originalColors[i] = _renderers[i].material.color;
        }
    }

    public virtual void Highlight(bool state)
    {
        if (_renderers == null)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (state)
                Colorer.LerpToColor(_renderers[i], _highlightColor, _blinkTime);
            else
                Colorer.Stop(_renderers[i], _originalColors[i], _blinkTime);
        }
    }

    public abstract string GetPrompt();

    public abstract void Interact(GameObject interactor);

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 point = _interactionPoint != null ? _interactionPoint.position : transform.position;
        Gizmos.DrawWireSphere(point, 0.25f);
    }
}
