using UnityEngine;

public class RopeChain : MonoBehaviour
{
    [SerializeField] private int _segments = 8;
    [SerializeField] private float _segmentLength = 0.25f;
    [SerializeField] private float _spring = 500f;
    [SerializeField] private float _damper = 40f;
    [SerializeField] private float _maxDistance = 0.02f;
    [SerializeField] private float _linkMass = 0.1f;
    [SerializeField] private float _linkLinearDamping = 0.08f;
    [SerializeField] private float _linkAngularDamping = 0.15f;

    private Transform[] _links;
    private SpringJoint _endJoint;
    private Transform _startAnchor;
    private Transform _endAnchor;

    public Transform[] Links => _links;
    public Transform StartAnchor => _startAnchor;
    public Transform EndAnchor => _endAnchor;

    public void Build(Rigidbody startBody, Transform startPoint, Rigidbody endBody, Transform endPoint)
    {
        _startAnchor = startPoint;
        _endAnchor = endPoint;

        _links = new Transform[_segments];

        Rigidbody previous = startBody;
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        Vector3 dir = (endPos - startPos).normalized;
        Vector3 pos = startPos;

        for (int i = 0; i < _segments; i++)
        {
            GameObject link = new GameObject("RopeLink_" + i.ToString());
            link.transform.SetParent(transform);
            link.transform.position = pos;

            Rigidbody rb = link.AddComponent<Rigidbody>();
            rb.mass = _linkMass;
            rb.linearDamping = _linkLinearDamping;
            rb.angularDamping = _linkAngularDamping;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            SpringJoint joint = link.AddComponent<SpringJoint>();
            joint.connectedBody = previous;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = previous.transform.InverseTransformPoint(pos);
            joint.spring = _spring;
            joint.damper = _damper;
            joint.minDistance = 0f;
            joint.maxDistance = _maxDistance;

            _links[i] = link.transform;
            previous = rb;
            pos += dir * _segmentLength;
        }

        _endJoint = endBody.gameObject.AddComponent<SpringJoint>();
        _endJoint.connectedBody = previous;
        _endJoint.autoConfigureConnectedAnchor = false;
        _endJoint.anchor = endBody.transform.InverseTransformPoint(endPoint.position);
        _endJoint.connectedAnchor = Vector3.zero;
        _endJoint.spring = _spring;
        _endJoint.damper = _damper;
        _endJoint.minDistance = 0f;
        _endJoint.maxDistance = _maxDistance;
    }

    public void DestroyAll()
    {
        if (_endJoint != null)
        {
            Object.Destroy(_endJoint);
        }
        if (_links != null)
        {
            for (int i = 0; i < _links.Length; i++)
            {
                if (_links[i] != null)
                {
                    Object.Destroy(_links[i].gameObject);
                }
            }
        }
        _links = null;
        _endJoint = null;
        _startAnchor = null;
        _endAnchor = null;
    }

    private void OnDestroy()
    {
        DestroyAll();
    }
}
