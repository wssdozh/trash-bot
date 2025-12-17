using UnityEngine;
using System.Collections;

public class Attacker : MonoBehaviour
{
    [SerializeField] private AttackData _attackData;
    [SerializeField] private float _hitForce = 6f;
    [SerializeField] private ForceMode _hitForceMode = ForceMode.Impulse;

    private bool _isOnCooldown = false;

    public bool PerformAttack()
    {
        if (_isOnCooldown == true)
        {
            return false;
        }

        int damage = _attackData.GetDamage();

        Collider[] hits = _attackData.AttackShape.GetTargets(transform, _attackData.AttackRange, _attackData.HitLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].TryGetComponent(out Rigidbody rigidbody) == true)
            {
                Vector3 direction = (hits[i].transform.position - transform.position).normalized;
                rigidbody.AddForce(direction * _hitForce, _hitForceMode);
            }

            if (hits[i].TryGetComponent(out Health health) == true)
            {
                health.Decrease(damage);
            }

            break;
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
