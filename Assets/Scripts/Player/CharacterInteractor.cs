using UnityEngine;

public class CharacterInteractor : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Transform _origin;
    [SerializeField] private Texter _texter;

    [Header("Настройки")]
    [SerializeField] private float _interactionSphereRadius = 3f;
    [SerializeField] private LayerMask _interactableMask;

    private Interactable _hovered;

    private void Awake()
    {
        if (_origin == null)
            _origin = transform;
    }

    private bool TryFindInteractable(Collider collider, out Interactable interactable)
    {
        if (collider.TryGetComponent(out interactable))
            return true;

        interactable = collider.GetComponentInParent<Interactable>();
        return interactable != null;
    }

    public void TickHover()
    {
        Interactable closest = FindClosestInteractable();

        if (closest == null)
        {
            ClearHovered();
            return;
        }

        if (_hovered == closest)
            return;

        if (_hovered != null)
            _hovered.Highlight(false);

        _hovered = closest;
        _hovered.Highlight(true);

        _texter.Show(_hovered.GetPrompt());
    }

    public void TryInteract(GameObject interactorGameObject)
    {
        if (_hovered == null)
        {
            Debug.Log("Нет объектов поблизости");

            return;
        }

        _hovered.Interact(interactorGameObject);

        _texter.Show(_hovered.GetPrompt());
    }

    private Interactable FindClosestInteractable()
    {
        Collider[] colliders = Physics.OverlapSphere(_origin.position, _interactionSphereRadius, _interactableMask);

        Interactable closest = null;
        float minSqrDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            if (TryFindInteractable(collider, out Interactable interactable) == false)
                continue;

            if (interactable.isActiveAndEnabled == false || interactable.gameObject.activeInHierarchy == false)
                continue;

            Vector3 nearestPoint = collider.ClosestPoint(_origin.position);
            float sqr = (_origin.position - nearestPoint).sqrMagnitude;

            if (sqr < minSqrDistance)
            {
                minSqrDistance = sqr;
                closest = interactable;
            }
        }

        return closest;
    }

    private void ClearHovered()
    {
        if (_hovered != null)
        {
            _hovered.Highlight(false);
            _hovered = null;
        }

        if (_texter != null)
            _texter.Hide();
    }

    private void OnDisable()
    {
        ClearHovered();
    }

    private void OnDrawGizmosSelected()
    {
        if (_origin == null)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(_origin.position, _interactionSphereRadius);
    }
}
