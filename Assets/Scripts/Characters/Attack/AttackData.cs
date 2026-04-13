using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Combat/Attack Data")]
public class AttackData : ScriptableObject
{
    [SerializeField] private float _attackRange = 1.6f;
    [SerializeField] private int _minDamage = 6;
    [SerializeField] private int _maxDamage = 10;
    [SerializeField] private float _attackCooldown = 0.6f;
    [SerializeField] private bool _isMultiHit;
    [SerializeField] private LayerMask _hitLayers;
    [SerializeField] private AttackShapeBase _attackShape;

    public float AttackRange => _attackRange;
    public int MinDamage => _minDamage;
    public int MaxDamage => _maxDamage;
    public float AttackCooldown => _attackCooldown;
    public bool IsMultiHit => _isMultiHit;
    public LayerMask HitLayers => _hitLayers;
    public AttackShapeBase AttackShape => _attackShape;

    public int GetDamage()
    {
        int damage = Random.Range(_minDamage, _maxDamage + 1);

        return damage;
    }
}
