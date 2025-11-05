using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeVisualizer : MonoBehaviour
{
    [SerializeField] private RopeChain _ropeChain;
    [SerializeField] private LineRenderer _lineRenderer;

    private void LateUpdate()
    {
        if (_ropeChain == null)
        {
            return;
        }

        Transform[] links = _ropeChain.Links;
        if (links == null)
        {
            return;
        }

        int count = links.Length + 2;
        _lineRenderer.positionCount = count;

        _lineRenderer.SetPosition(0, _ropeChain.StartAnchor.position);
        for (int i = 0; i < links.Length; i++)
        {
            _lineRenderer.SetPosition(i + 1, links[i].position);
        }
        _lineRenderer.SetPosition(count - 1, _ropeChain.EndAnchor.position);
    }
}
