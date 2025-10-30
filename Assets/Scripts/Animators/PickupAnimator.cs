using UnityEngine;
using System;
using System.Collections;

public class PickupAnimator : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _stopDistance = 0.3f;

    private Transform _targetTransform;
    private bool _isAnimating = false;
    private Action _onArrived;

    public void PlayAttraction(Transform target, Action onArrived)
    {
        if (_isAnimating == false)
        {
            _targetTransform = target;
            _onArrived = onArrived;
            StartCoroutine(AnimateMovement());
        }
    }

    private IEnumerator AnimateMovement()
    {
        _isAnimating = true;

        while (true)
        {
            if (_targetTransform == null)
            {
                _isAnimating = false;
                
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

        _isAnimating = false;
        _onArrived?.Invoke();
    }
}
