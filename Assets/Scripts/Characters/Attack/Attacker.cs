using UnityEngine;
using System.Collections;

public class Attacker : MonoBehaviour
{
    private const int TargetBufferSize = 16;

    [SerializeField] private AttackData _attackData;
    [SerializeField] private float _hitForce = 6f;
    [SerializeField] private ForceMode _hitForceMode = ForceMode.Impulse;

    private bool _isOnCooldown = false;
    private readonly Collider[] _targetBuffer = new Collider[TargetBufferSize];

    public bool PerformAttack()
    {
        if (_isOnCooldown)
        {
            return false;
        }

        int damage = _attackData.GetDamage();

        int hitCount = _attackData.AttackShape.GetTargets(transform, _attackData.AttackRange, _attackData.HitLayers, _targetBuffer);

        if (hitCount == 0)
        {
            StartCoroutine(StartCooldown());

            return true;
        }

        Collider hit = _targetBuffer[0];
        Rigidbody rigidbody;

        if (hit.TryGetComponent(out rigidbody))
        {
            Vector3 direction = (hit.transform.position - transform.position).normalized;
            rigidbody.AddForce(direction * _hitForce, _hitForceMode);
        }

        Health health;

        if (hit.TryGetComponent(out health))
        {
            health.Decrease(damage);
        }

        StartCoroutine(StartCooldown());

        return true;
    }

    private IEnumerator StartCooldown()
    {
        _isOnCooldown = true;

        yield return new WaitForSeconds(_attackData.AttackCooldown);

        _isOnCooldown = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (_attackData == null)
        {
            return;
        }

        if (_attackData.AttackShape == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        _attackData.AttackShape.DrawGizmos(transform, _attackData.AttackRange);
    }
}
