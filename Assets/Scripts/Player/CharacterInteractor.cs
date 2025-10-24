using UnityEngine;

public class CharacterInteractor : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Transform _origin;
    [SerializeField] private Camera _camera;
    [SerializeField] private CursorManager _cursor;

    [Header("Настройки")]
    [SerializeField] private float _interactionSphereRadius = 3f;
    [SerializeField] private LayerMask _interactableMask;

    private Interactable _hovered;

    private bool TryFindInteractableOnCollider(Collider collider, out Interactable interactable)
    {
        if (collider.TryGetComponent<Interactable>(out interactable) == false)
        {
            interactable = collider.GetComponentInParent<Interactable>();
        }

        return interactable != null;
    }

    public void TickHover()
    {
        Ray ray = _camera.ScreenPointToRay(_cursor.MouseScreenPos);
        RaycastHit hitInfo;

        bool hasHit = Physics.Raycast(ray, out hitInfo, 100f, _interactableMask);

        if (hasHit == true)
        {
            Interactable target;

            if (TryFindInteractableOnCollider(hitInfo.collider, out target) == true)
            {
                if (_hovered != target)
                {
                    if (_hovered != null)
                        _hovered.Highlight(false);

                    _hovered = target;
                    _hovered.Highlight(true);
                }

                return;
            }
        }

        if (_hovered != null)
        {
            _hovered.Highlight(false);
            _hovered = null;
        }
    }

    public void TryInteract(GameObject interactorGameObject)
    {
        Collider[] colliders = Physics.OverlapSphere(
            _origin.position,
            _interactionSphereRadius,
            _interactableMask
        );

        bool isInRange = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Interactable interactable;

            if (TryFindInteractableOnCollider(colliders[i], out interactable) == false)
                continue;

            if (interactable == _hovered)
            {
                isInRange = true;
                break;
            }
        }

        if (isInRange == true)
        {
            _hovered.Interact(interactorGameObject);
        }
        else
        {
            Debug.Log("ОБЪЕКТ ДАЛЕКО");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_origin == null)
            return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(_origin.position, _interactionSphereRadius);
    }
}
