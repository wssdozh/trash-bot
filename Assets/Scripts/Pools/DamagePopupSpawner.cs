using UnityEngine;
using DG.Tweening;

public class DamagePopupSpawner : Spawner<DamagePopup>
{
    [SerializeField] private DamagePopupSpawnerRef _link;

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < PoolSize; i++)
        {
            DamagePopup damagePopup = Pool.Get();
            Pool.Release(damagePopup);
        }

        if (_link == null == false)
        {
            _link.Set(this);
        }
    }

    private void OnDestroy()
    {
        if (_link == null == false)
        {
            if (_link.Value == this)
            {
                _link.Clear();
            }
        }
    }

    public DamagePopup Show(float damage, Vector3 position)
    {
        DamagePopup damagePopup = Pool.Get();
        damagePopup.Completed += OnPopupCompleted;
        damagePopup.transform.position = position;
        damagePopup.Setup(damage);
        return damagePopup;
    }

    private void OnPopupCompleted(DamagePopup damagePopup)
    {
        damagePopup.Completed -= OnPopupCompleted;
        Pool.Release(damagePopup);
    }
}
