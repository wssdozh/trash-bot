using System;
using UnityEngine;

namespace JunkyardBoss
{
    [DisallowMultipleComponent]
    public sealed class BossExcavatorDebugRuntime : MonoBehaviour
    {
        [Header("Links")]
        [SerializeField] private BossExcavator _boss;

        [Header("Runtime")]
        [SerializeField] private BossExcavatorState _currentState = BossExcavatorState.Idle;
        [SerializeField] private BossExcavatorPhase _currentPhase = BossExcavatorPhase.PhaseOne;
        [SerializeField] private BossExcavatorAttack _currentAttack = BossExcavatorAttack.None;
        [SerializeField] private BossExcavatorAttack _targetAttack = BossExcavatorAttack.None;
        [SerializeField] private BossExcavatorArmPose _currentArmPose = BossExcavatorArmPose.Neutral;
        [SerializeField] private BossExcavatorTargetPoint _targetPoint = BossExcavatorTargetPoint.ArenaCenter;
        [SerializeField] private float _targetDistance;
        [SerializeField] private float _baseAngleToTarget;
        [SerializeField] private float _cabinAngleToTarget;
        [SerializeField] private float _currentMoveSpeed;

        [Header("State Command")]
        [SerializeField] private BossExcavatorState _state = BossExcavatorState.Reposition;
        [SerializeField] private bool _chargeAlign;
        [SerializeField] private bool _aimLocked;
        [SerializeField] private bool _armLocked;

        [Header("Arm Command")]
        [SerializeField] private BossExcavatorArmPose _selectedArmPose = BossExcavatorArmPose.Neutral;
        [SerializeField] private bool _applyNow;
        [SerializeField] private bool _resetNow;
        [SerializeField] private bool _completePhaseNow;
        [SerializeField] private bool _applyArmPoseNow;
        [SerializeField] private bool _copyCurrentArmPoseNow;

        private void Awake()
        {
            ValidateDependencies();
        }

        private void Start()
        {
            _currentState = _boss.State;
            _currentPhase = _boss.Phase;
            _state = _boss.State;
            _chargeAlign = false;
            _aimLocked = false;
            _armLocked = false;
            _currentArmPose = _boss.GetArmPose();
            _selectedArmPose = _currentArmPose;
            _currentAttack = _boss.CurrentAttack;
            _targetAttack = _boss.TargetAttack;
            UpdateRuntimeMetrics();
        }

        private void Update()
        {
            _boss.SetAimLocked(_aimLocked);
            _boss.SetArmLocked(_armLocked);
            _currentState = _boss.State;
            _currentPhase = _boss.Phase;
            _currentArmPose = _boss.GetArmPose();
            _currentAttack = _boss.CurrentAttack;
            _targetAttack = _boss.TargetAttack;
            UpdateRuntimeMetrics();

            if (_resetNow)
            {
                _resetNow = false;
                _boss.ResetBoss();
                _state = _boss.State;
                _currentState = _boss.State;
                _currentPhase = _boss.Phase;
                _currentArmPose = _boss.GetArmPose();
                _currentAttack = _boss.CurrentAttack;
                _targetAttack = _boss.TargetAttack;
                UpdateRuntimeMetrics();
            }

            if (_completePhaseNow)
            {
                _completePhaseNow = false;
                _boss.CompletePhaseChange();
                _state = _boss.State;
                _currentState = _boss.State;
                _currentPhase = _boss.Phase;
            }

            if (_applyNow)
            {
                _applyNow = false;
                ApplyState();
            }

            if (_applyArmPoseNow)
            {
                _applyArmPoseNow = false;
                _boss.SetArmPose(_selectedArmPose);
                _currentArmPose = _boss.GetArmPose();
            }

            if (_copyCurrentArmPoseNow)
            {
                _copyCurrentArmPoseNow = false;
                _selectedArmPose = _currentArmPose;
            }
        }

        private void ApplyState()
        {
            _boss.SetChargeAlign(_chargeAlign);
            _boss.RequestState(_state);
        }

        private void UpdateRuntimeMetrics()
        {
            _targetPoint = _boss.Move.TargetPoint;
            _currentMoveSpeed = _boss.Move.CurrentPlanarSpeed;
            _targetDistance = GetTargetDistance();
            _baseAngleToTarget = GetTargetAngle(_boss.Base);
            _cabinAngleToTarget = GetTargetAngle(_boss.Cabin);
        }

        private float GetTargetDistance()
        {
            if (_boss.Target == null)
            {
                return 0f;
            }

            if (_boss.Base == null)
            {
                return 0f;
            }

            Vector3 basePosition = _boss.Base.position;
            Vector3 targetPosition = _boss.Target.position;
            basePosition.y = 0f;
            targetPosition.y = 0f;

            return Vector3.Distance(basePosition, targetPosition);
        }

        private float GetTargetAngle(Transform pivot)
        {
            if (pivot == null)
            {
                return 0f;
            }

            if (_boss.Target == null)
            {
                return 0f;
            }

            Vector3 pivotPosition = pivot.position;
            Vector3 targetPosition = _boss.Target.position;
            targetPosition.y = pivotPosition.y;
            Vector3 lookDirection = targetPosition - pivotPosition;

            if (lookDirection.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            return Vector3.Angle(pivot.forward, lookDirection.normalized);
        }

        private void ValidateDependencies()
        {
            if (_boss == null)
            {
                throw new InvalidOperationException(nameof(_boss));
            }
        }
    }
}
