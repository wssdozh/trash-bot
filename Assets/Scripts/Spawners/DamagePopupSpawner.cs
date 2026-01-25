using UnityEngine;
using DG.Tweening;

public sealed class DamagePopupSpawner : Spawner<DamagePopup>
{
    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < PoolSize; i++)
        {
            DamagePopup popup = Pool.Get();
            Pool.Release(popup);
        }
    }

    public override DamagePopup Spawn(Vector3 position)
    {
        DamagePopup popup = Pool.Get();
        popup.Completed += OnPopupCompleted;
        popup.transform.position = position;

        return popup;
    }

    public void Show(float damage, Vector3 position)
    {
        DamagePopup popup = Spawn(position);
        popup.Setup(damage);
    }

    private void OnPopupCompleted(DamagePopup popup)
    {
        popup.Completed -= OnPopupCompleted;
        Pool.Release(popup);
    }

    protected override void ActionOnRelease(DamagePopup popup)
    {
        popup.gameObject.SetActive(false);
    }

    public override void Despawn(DamagePopup popup)
    {
        Pool.Release(popup);
    }
}