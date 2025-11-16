using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Header("Настройка")]
    [SerializeField] protected string _interactionName = "Взаимодействовать";
    [SerializeField] protected Transform _interactionPoint;

    [Header("Подсветка")]
    [SerializeField] protected HighlighterBase _highlightBase;

    protected virtual void Awake()
    {
        if (_highlightBase == null)
        {
            _highlightBase = GetComponent<HighlightAnimator>();
        }
    }

    public virtual void Highlight(bool state)
    {
        if (_highlightBase != null)
        {
            _highlightBase.Highlight(state);
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
