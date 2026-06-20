using System;
using UnityEngine;

public class CharacterInteractor : MonoBehaviour
{
    private const int InteractableBufferSize = 32;
    private const string InteractActionName = "Interact";

    [Header("Зависимости")]
    [SerializeField] private Transform _origin;
    [SerializeField] private Texter _texter;

    [Header("Настройки")]
    [SerializeField] private float _interactionSphereRadius = 3f;
    [SerializeField] private LayerMask _interactableMask;
    [SerializeField] private string _promptFormat = "Нажмите [{0}], чтобы {1}";

    private readonly Collider[] _interactableBuffer = new Collider[InteractableBufferSize];

    private Interactable _hovered;
    private PlayerInputActions _promptInputs;

    public event Action<Interactable> Interacted;

    private void Awake()
    {
        if (_origin == null)
        {
            _origin = transform;
        }

        _promptInputs = new PlayerInputActions();
        PlayerInputBindingOverrideStore.Apply(_promptInputs);
    }

    private void OnEnable()
    {
        PlayerInputBindingOverrideStore.Changed += OnBindingOverridesChanged;
    }

    private void OnDisable()
    {
        PlayerInputBindingOverrideStore.Changed -= OnBindingOverridesChanged;
        ClearHovered();
    }

    private void OnDestroy()
    {
        if (_promptInputs == null)
        {
            return;
        }

        _promptInputs.Disable();
        _promptInputs.Dispose();
        _promptInputs = null;
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
        {
            return;
        }

        if (_hovered != null)
        {
            _hovered.Highlight(false);
        }

        _hovered = closest;
        _hovered.Highlight(true);
        ShowHoveredPrompt();
    }

    public void TryInteract(GameObject interactorGameObject)
    {
        if (_hovered == null)
        {
            return;
        }

        _hovered.Interact(interactorGameObject);

        if (Interacted != null)
        {
            Interacted.Invoke(_hovered);
        }

        ShowHoveredPrompt();
    }

    private bool TryFindInteractable(Collider collider, out Interactable interactable)
    {
        if (collider.TryGetComponent(out interactable))
        {
            return true;
        }

        interactable = collider.GetComponentInParent<Interactable>();

        return interactable != null;
    }

    private Interactable FindClosestInteractable()
    {
        int colliderCount = Physics.OverlapSphereNonAlloc(
            _origin.position,
            _interactionSphereRadius,
            _interactableBuffer,
            _interactableMask);

        Interactable closest = null;
        float minSqrDistance = float.MaxValue;

        for (int i = 0; i < colliderCount; i++)
        {
            Collider collider = _interactableBuffer[i];

            if (TryFindInteractable(collider, out Interactable interactable) == false)
            {
                continue;
            }

            if (interactable.isActiveAndEnabled == false || interactable.gameObject.activeInHierarchy == false)
            {
                continue;
            }

            Vector3 nearestPoint = collider.ClosestPoint(_origin.position);
            float sqrDistance = (_origin.position - nearestPoint).sqrMagnitude;

            if (sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
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
        {
            _texter.Hide();
        }
    }

    private void ShowHoveredPrompt()
    {
        if (_hovered == null)
        {
            return;
        }

        if (_texter == null)
        {
            return;
        }

        string prompt = _hovered.GetPrompt();

        if (string.IsNullOrWhiteSpace(prompt))
        {
            _texter.Hide();

            return;
        }

        _texter.Show(BuildPrompt(prompt));
    }

    private string BuildPrompt(string prompt)
    {
        string keyLabel = PlayerInputBindingLabel.Get(_promptInputs, InteractActionName, string.Empty);

        return string.Format(_promptFormat, keyLabel, prompt);
    }

    private void OnBindingOverridesChanged()
    {
        PlayerInputBindingOverrideStore.Apply(_promptInputs);
        ShowHoveredPrompt();
    }

    private void OnDrawGizmosSelected()
    {
        if (_origin == null)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(_origin.position, _interactionSphereRadius);
    }
}
