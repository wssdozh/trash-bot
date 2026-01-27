using System;
using System.Collections;
using UnityEngine;

public sealed class AmmoLifePath : AmmoLifeListener
{
    [Header("Зависимости")]
    [SerializeField] private AmmoReturner _ammoReturner;

    private AmmoLifeListener[] _lifeListeners;
    private Coroutine _returnRoutine;

    protected override void Awake()
    {
        base.Awake();

        if (_ammoReturner == null)
        {
            throw new InvalidOperationException(nameof(_ammoReturner));
        }

        _lifeListeners = GetComponents<AmmoLifeListener>();
    }

    protected override void OnAmmoEnabled()
    {
        StopReturnRoutineIfRunning();
    }

    protected override void OnAmmoLifeEnded()
    {
        StopReturnRoutineIfRunning();

        _returnRoutine = StartCoroutine(ReturnAfterLifePathCompleted());
    }

    private IEnumerator ReturnAfterLifePathCompleted()
    {
        yield return null;

        while (IsAllLifeListenersComplete() == false)
        {
            yield return null;
        }

        _returnRoutine = null;

        _ammoReturner.ReturnToPool();
    }

    private bool IsAllLifeListenersComplete()
    {
        int index = 0;

        while (index < _lifeListeners.Length)
        {
            AmmoLifeListener lifeListener = _lifeListeners[index];

            if (lifeListener != this)
            {
                if (lifeListener.IsLifeEndComplete == false)
                {
                    return false;
                }
            }

            index++;
        }

        return true;
    }

    private void StopReturnRoutineIfRunning()
    {
        if (_returnRoutine == null)
        {
            return;
        }

        StopCoroutine(_returnRoutine);
        _returnRoutine = null;
    }
}
