using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Health _health;
    [SerializeField] private CharacterDied _characterDied;

    [Header("Death")]
    [SerializeField, Min(0f)] private float _sinkDelay = 5.2f;
    [SerializeField, Min(0.01f)] private float _sinkDuration = 6.5f;
    [SerializeField, Min(0f)] private float _sinkDistance = 2.2f;

    private Collider[] _corpseColliders;
    private Rigidbody[] _corpseRigidbodies;
    private bool _isSinkStarted;

    public static event Action<Enemy> AnyDied;

    public event Action Died;

    public bool IsDead { get; private set; }
    public Health Health => _health;

    private void Awake()
    {
        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (_sinkDelay < 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDelay));
        }

        if (_sinkDuration <= 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDuration));
        }

        if (_sinkDistance < 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDistance));
        }

        _corpseColliders = GetComponentsInChildren<Collider>(true);
        _corpseRigidbodies = GetComponentsInChildren<Rigidbody>(true);
    }

    private void OnEnable()
    {
        _health.Ended += Die;
    }

    private void OnDisable()
    {
        _health.Ended -= Die;
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        if (_characterDied != null)
        {
            _characterDied.EnableRegdoll();
        }

        Action died = Died;

        if (died != null)
        {
            died.Invoke();
        }

        Action<Enemy> anyDied = AnyDied;

        if (anyDied != null)
        {
            anyDied.Invoke(this);
        }

        StartSink();
    }

    private void StartSink()
    {
        if (_isSinkStarted)
        {
            return;
        }

        _isSinkStarted = true;
        StartCoroutine(SinkCoroutine());
    }

    private IEnumerator SinkCoroutine()
    {
        float delayTimer = 0f;

        while (delayTimer < _sinkDelay)
        {
            delayTimer += Time.deltaTime;

            yield return null;
        }

        DisableCorpsePhysics();

        Vector3 startPoint = transform.position;
        Vector3 endPoint = startPoint + (Vector3.down * _sinkDistance);
        float sinkTimer = 0f;

        while (sinkTimer < _sinkDuration)
        {
            sinkTimer += Time.deltaTime;
            float sinkProgress = Mathf.Clamp01(sinkTimer / _sinkDuration);
            transform.position = Vector3.Lerp(startPoint, endPoint, sinkProgress);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void DisableCorpsePhysics()
    {
        int rigidbodyIndex = 0;

        while (rigidbodyIndex < _corpseRigidbodies.Length)
        {
            Rigidbody corpseRigidbody = _corpseRigidbodies[rigidbodyIndex];

            if (corpseRigidbody != null)
            {
                corpseRigidbody.linearVelocity = Vector3.zero;
                corpseRigidbody.angularVelocity = Vector3.zero;
                corpseRigidbody.useGravity = false;
                corpseRigidbody.isKinematic = true;
                corpseRigidbody.detectCollisions = false;
            }

            rigidbodyIndex += 1;
        }

        int colliderIndex = 0;

        while (colliderIndex < _corpseColliders.Length)
        {
            Collider corpseCollider = _corpseColliders[colliderIndex];

            if (corpseCollider != null)
            {
                corpseCollider.enabled = false;
            }

            colliderIndex += 1;
        }
    }
}
