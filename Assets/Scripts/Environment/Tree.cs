using UnityEngine;
using System.Collections;

public class Tree : DamageableObject
{
    [SerializeField] private Collider _thisCollider;
    [SerializeField] private GameObject _stumpObject;
    [SerializeField] private GameObject _treeObject;
    [SerializeField] private Rigidbody _logPrefab;
    [SerializeField] private Transform _spawnPointLog;

    [Header("Настройки")]
    [SerializeField] private float _minForce;
    [SerializeField] private float _maxForce;

    [Header("Регенерация")]
    [SerializeField] private float _regenerationDelay = 60f;

    protected override void OnDamage()
    {
        PlayShake();
    }

    protected override void OnDeath()
    {
        _stumpObject.SetActive(true);
        _treeObject.SetActive(false);
        _thisCollider.enabled = false;

        Rigidbody spawnedLogRigidbody = Instantiate(_logPrefab, _spawnPointLog.position, transform.rotation);
        spawnedLogRigidbody.AddForce(new Vector3(Random.Range(_minForce, _maxForce), 0f, Random.Range(_minForce, _maxForce)), ForceMode.Impulse);

        StartCoroutine(Regenerate());
    }

    private IEnumerator Regenerate()
    {
        yield return new WaitForSeconds(_regenerationDelay);

        _stumpObject.SetActive(false);
        _treeObject.SetActive(true);

        Health.Increase(Health.MaxValue);

        _thisCollider.enabled = true;
    }
}
