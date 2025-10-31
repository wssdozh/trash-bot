using Unity.VisualScripting;
using UnityEngine;

public class Tree : DamageableObject
{
    [SerializeField] private float _destroyDelay = 0f;
    [SerializeField] private Collider _thisCollider;
    [SerializeField] private GameObject _stumpObject;
    [SerializeField] private Rigidbody _logPrefab;
    [SerializeField] private Transform _spawnPointLog;

    [Header("Настройки")]
    [SerializeField] private float _minForce;
    [SerializeField] private float _maxForce;


    protected override void OnDamage()
    {
        PlayShake();
    }

    protected override void OnDeath()
    {
        _stumpObject.SetActive(true);

        _thisCollider.enabled = false;

        Rigidbody _stumpRigidBody = Instantiate(_logPrefab, _spawnPointLog.position, transform.rotation);
        _stumpRigidBody.AddForce(new Vector3(Random.Range(_minForce, _maxForce), 0f, Random.Range(_minForce, _maxForce)), ForceMode.Impulse);

        Destroy(gameObject, _destroyDelay);
    }
}
