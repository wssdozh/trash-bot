using UnityEngine;
using System;
using System.Collections;

public class PickupAnimator : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _stopDistance = 0.1f;

    private Transform _targetTransform;
    private Action _onArrived;

    public void PlayAttraction(Transform target, Action onArrived)
    {
        StopAllCoroutines();

        _targetTransform = target;
        _onArrived = onArrived;

        StartCoroutine(AnimateMovement());
    }

    private IEnumerator AnimateMovement()
    {
        while (enabled)
        {
            if (_targetTransform == null)
            {
                yield break;
            }

            transform.position = Vector3.MoveTowards(transform.position, _targetTransform.position, _moveSpeed * Time.deltaTime);

            float distance = Vector3.Distance(transform.position, _targetTransform.position);

            if (distance <= _stopDistance)
            {
                break;
            }

            yield return null;
        }

        _onArrived?.Invoke();
    }
}
