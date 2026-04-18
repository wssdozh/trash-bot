using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorAim : MonoBehaviour
    {
        private const float MinSqrMagnitude = 0.0001f;

        private BossExcavator _boss;
        private BossExcavatorConfig _config;
        private Transform _pivot;
        private Transform _target;
        private Vector3 _basePivotLocalPosition;
        private float _currentTurnSpeed;
        private bool _isLocked;

        public bool IsLocked => _isLocked;

        public void Setup(BossExcavator boss, BossExcavatorConfig config, Transform pivot, Transform target)
        {
            if (boss == null)
            {
                throw new InvalidOperationException(nameof(boss));
            }

            if (config == null)
            {
                throw new InvalidOperationException(nameof(config));
            }

            if (pivot == null)
            {
                throw new InvalidOperationException(nameof(pivot));
            }

            _boss = boss;
            _config = config;
            _pivot = pivot;
            _target = target;

            Transform baseTransform = _boss.Base;

            if (baseTransform == null)
            {
                throw new InvalidOperationException(nameof(baseTransform));
            }

            _basePivotLocalPosition = baseTransform.InverseTransformPoint(_pivot.position);
            _currentTurnSpeed = 0f;
            _isLocked = false;
        }

        public void SetTarget(Transform target)
        {
            if (target == null)
            {
                throw new InvalidOperationException(nameof(target));
            }

            _target = target;
        }

        public void SetLocked(bool isLocked)
        {
            _isLocked = isLocked;

            if (isLocked)
            {
                _currentTurnSpeed = 0f;
            }
        }

        public void Tick()
        {
            ValidateDependencies();
            SyncPivotPosition();

            if (_isLocked)
            {
                _currentTurnSpeed = 0f;

                return;
            }

            if (_boss.IsDead)
            {
                _currentTurnSpeed = 0f;

                return;
            }

            if (_target == null)
            {
                _currentTurnSpeed = 0f;

                return;
            }

            Vector3 pivotPosition = _pivot.position;
            Vector3 targetPosition = _target.position;
            targetPosition.y = pivotPosition.y;
            Vector3 lookDirection = targetPosition - pivotPosition;

            if (lookDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                _currentTurnSpeed = 0f;

                return;
            }

            Quaternion currentRotation = _pivot.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            Quaternion nextRotation = BossExcavatorMotionProfile.StepRotation(
                currentRotation,
                targetRotation,
                ref _currentTurnSpeed,
                GetTurnSpeed(),
                _config.CabinTurnAcceleration,
                _config.CabinTurnDeceleration,
                _config.CabinTurnSlowAngle,
                _config.CabinTurnMinSpeedFactor,
                Time.deltaTime);

            _pivot.rotation = nextRotation;
        }

        private float GetTurnSpeed()
        {
            float turnSpeed = _config.CabinTurnSpeed;

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                turnSpeed *= _config.CabinPhaseTwoMult;
            }

            return turnSpeed;
        }

        private void ValidateDependencies()
        {
            if (_boss == null)
            {
                throw new InvalidOperationException(nameof(_boss));
            }

            if (_config == null)
            {
                throw new InvalidOperationException(nameof(_config));
            }

            if (_pivot == null)
            {
                throw new InvalidOperationException(nameof(_pivot));
            }
        }

        private void SyncPivotPosition()
        {
            Transform baseTransform = _boss.Base;

            if (baseTransform == null)
            {
                throw new InvalidOperationException(nameof(baseTransform));
            }

            Vector3 pivotPosition = baseTransform.TransformPoint(_basePivotLocalPosition);
            _pivot.position = pivotPosition;
        }
    }
}
