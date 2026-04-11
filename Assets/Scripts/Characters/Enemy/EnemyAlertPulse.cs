using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAlertPulse : MonoBehaviour
{
    private const float ReachDistance = 0.08f;
    private const float ReachDistanceSqr = ReachDistance * ReachDistance;

    private TrailRenderer _trailRenderer;
    private Transform _target;
    private Transform _visualRoot;
    private float _moveSpeed;
    private bool _isReady;
    private bool _isPlaying;

    public void Setup(float moveSpeed, float size, float trailTime, float trailWidth, Color color)
    {
        if (_isReady == false)
        {
            BuildView();
        }

        _moveSpeed = moveSpeed;
        _visualRoot.localScale = Vector3.one * size;
        _trailRenderer.time = trailTime;
        _trailRenderer.startWidth = trailWidth;
        _trailRenderer.endWidth = trailWidth * 0.1f;
        _trailRenderer.startColor = color;
        _trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);

        Renderer visualRenderer = _visualRoot.GetComponent<Renderer>();

        if (visualRenderer != null)
        {
            visualRenderer.material.color = color;
        }
    }

    public void Play(Vector3 startPoint, Transform target)
    {
        if (_isReady == false)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        transform.position = startPoint;
        _target = target;
        _isPlaying = true;
        gameObject.SetActive(true);
        _trailRenderer.Clear();
    }

    private void Update()
    {
        if (_isPlaying == false)
        {
            return;
        }

        if (_target == null)
        {
            StopPulse();

            return;
        }

        Vector3 targetPoint = _target.position;
        Vector3 nextPoint = Vector3.MoveTowards(transform.position, targetPoint, _moveSpeed * Time.deltaTime);
        Vector3 moveDirection = nextPoint - transform.position;
        transform.position = nextPoint;

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            transform.forward = moveDirection.normalized;
        }

        Vector3 reachDelta = transform.position - targetPoint;

        if (reachDelta.sqrMagnitude <= ReachDistanceSqr)
        {
            StopPulse();
        }
    }

    private void BuildView()
    {
        GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualObject.name = "Visual";
        visualObject.transform.SetParent(transform, false);

        Collider visualCollider = visualObject.GetComponent<Collider>();

        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        _visualRoot = visualObject.transform;
        _trailRenderer = gameObject.AddComponent<TrailRenderer>();
        _trailRenderer.autodestruct = false;
        _trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trailRenderer.receiveShadows = false;
        _trailRenderer.minVertexDistance = 0.02f;
        _trailRenderer.material = visualObject.GetComponent<Renderer>().material;
        _isReady = true;
    }

    private void StopPulse()
    {
        _isPlaying = false;
        _target = null;
        _trailRenderer.Clear();
        gameObject.SetActive(false);
    }
}
