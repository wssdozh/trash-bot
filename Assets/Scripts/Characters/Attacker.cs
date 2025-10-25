using UnityEngine;
using System.Collections;

public class Attacker : MonoBehaviour
{
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private int minDamage = 6;
    [SerializeField] private int maxDamage = 10;
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private LayerMask hitLayers;

    private bool isOnCooldown = false;

    public bool PerformAttack()
    {
        if (isOnCooldown == true)
        {
            return false;
        }

        int damage = Random.Range(minDamage, maxDamage + 1);

        Vector3 center = transform.position + transform.forward * (attackRange * 0.5f);
        Collider[] hits = Physics.OverlapSphere(center, attackRange, hitLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            Health health = hits[i].GetComponent<Health>();
            if (health != null)
            {
                health.Decrease(damage);
                break;
            }
        }

        StartCoroutine(StartCooldown());
        return true;
    }

    private IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isOnCooldown = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * (attackRange * 0.5f), attackRange);
    }
}
