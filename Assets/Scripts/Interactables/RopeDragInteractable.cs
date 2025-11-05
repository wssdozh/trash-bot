using UnityEngine;

public class RopeDragInteractable : Interactable
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private LineRenderer _lineRendererPrefab;

    private bool _isAttached = false;
    private GameObject _ropeObject;

    public override string GetPrompt()
    {
        if (_isAttached == false)
        {
            return "Прицепить верёвку";
        }
        return "Отцепить верёвку";
    }

    public override void Interact(GameObject interactor)
    {
        if (_isAttached == false)
        {
            Attach(interactor);
            return;
        }
        Detach();
    }

    private void Attach(GameObject interactor)
    {
        RopeAnchor ropeAnchor = interactor.GetComponent<RopeAnchor>();

        Transform localPoint = _interactionPoint != null ? _interactionPoint : transform;
        Transform anchorPoint = ropeAnchor.AnchorPoint != null ? ropeAnchor.AnchorPoint : ropeAnchor.transform;

        _ropeObject = new GameObject("Rope");
        RopeChain chain = _ropeObject.AddComponent<RopeChain>();
        chain.Build(ropeAnchor.Rigidbody, anchorPoint, _rigidbody, localPoint);

        if (_lineRendererPrefab != null)
        {
            LineRenderer lr = Object.Instantiate(_lineRendererPrefab, _ropeObject.transform);
            RopeVisualizer vis = _ropeObject.AddComponent<RopeVisualizer>();
            typeof(RopeVisualizer).GetField("_ropeChain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(vis, chain);
            typeof(RopeVisualizer).GetField("_lineRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(vis, lr);
        }

        _isAttached = true;
    }

    private void Detach()
    {
        if (_ropeObject != null)
        {
            RopeChain chain = _ropeObject.GetComponent<RopeChain>();
            chain.DestroyAll();
            Object.Destroy(_ropeObject);
        }
        _ropeObject = null;
        _isAttached = false;
    }

    private void OnDisable()
    {
        if (_isAttached == false)
        {
            return;
        }
        Detach();
    }
}
