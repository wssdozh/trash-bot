using System;
using System.Collections;
using UnityEngine;

public class PickupAnimator : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _stopDistance = 0.1f;
    [SerializeField] private float _minDuration = 0.12f;
    [SerializeField] private float _arcHeight = 0.25f;

    private Transform _targetTransform;
    private Action _onArrived;
    private Vector3 _startPosition;
    private float _duration;

    public void PlayAttraction(Transform target, Action onArrived)
    {
        StopAnimation();

        _targetTransform = target;
        _onArrived = onArrived;
        _startPosition = transform.position;
        _duration = GetDuration();

        StartCoroutine(AnimateMovement());
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private IEnumerator AnimateMovement()
    {
        float elapsedTime = 0f;

        while (true)
        {
            if (_targetTransform == null)
            {
                yield break;
            }

            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / _duration);
            Vector3 targetPosition = _targetTransform.position;
            Vector3 nextPosition = Vector3.Lerp(_startPosition, targetPosition, progress);
            nextPosition.y += Mathf.Sin(progress * Mathf.PI) * _arcHeight;
            transform.position = nextPosition;

            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance <= _stopDistance)
            {
                transform.position = targetPosition;
                break;
            }

            if (progress >= 1f)
            {
                transform.position = targetPosition;
                break;
            }

            yield return null;
        }

        Action onArrived = _onArrived;

        StopAnimation();

        if (onArrived != null)
        {
            onArrived.Invoke();
        }
    }

    private float GetDuration()
    {
        if (_targetTransform == null)
        {
            return _minDuration;
        }

        float distance = Vector3.Distance(_startPosition, _targetTransform.position);
        float duration = distance / _moveSpeed;

        if (duration < _minDuration)
        {
            duration = _minDuration;
        }

        return duration;
    }

    private void StopAnimation()
    {
        StopAllCoroutines();
        _targetTransform = null;
        _onArrived = null;
    }
}
