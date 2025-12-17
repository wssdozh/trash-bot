using UnityEngine;

public class Log : DamageableObject
{
    [SerializeField] private Rigidbody _leftPart;
    [SerializeField] private Rigidbody _rightPart;
    [SerializeField] private Transform _leftSpawnPoint;
    [SerializeField] private Transform _rightSpawnPoint;
    [SerializeField] private float _splitForce = 3f;
    [SerializeField] private float _torqueForce = 2f;

    private bool _isBroken = false;

    protected override void OnDamage()
    {
    }

    protected override void OnDeath()
    {
        if (_isBroken == false)
        {
            _isBroken = true;
            SplitLog();
        }
    }

    private void SplitLog()
    {
        Rigidbody left = Instantiate(_leftPart, _leftSpawnPoint.position, _leftSpawnPoint.rotation);
        Rigidbody right = Instantiate(_rightPart, _rightSpawnPoint.position, _rightSpawnPoint.rotation);

        left.AddForce(_leftSpawnPoint.right * -_splitForce, ForceMode.Impulse);
        left.AddTorque(Vector3.forward * _torqueForce, ForceMode.Impulse);

        right.AddForce(_rightSpawnPoint.right * _splitForce, ForceMode.Impulse);
        right.AddTorque(Vector3.back * _torqueForce, ForceMode.Impulse);

        gameObject.SetActive(false);
    }
}
