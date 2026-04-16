using System;
using UnityEngine;

namespace JunkyardBoss
{
    [DisallowMultipleComponent]
    public sealed class BossExcavatorDebugRuntime : MonoBehaviour
    {
        [SerializeField] private BossExcavator _boss;
        [SerializeField] private BossExcavatorState _state = BossExcavatorState.Reposition;
        [SerializeField] private bool _chargeAlign;
        [SerializeField] private bool _aimLocked;
        [SerializeField] private bool _armLocked;
        [SerializeField] private BossExcavatorArmPose _selectedArmPose = BossExcavatorArmPose.Neutral;
        [SerializeField] private BossExcavatorArmPose _currentArmPose = BossExcavatorArmPose.Neutral;
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
            _state = _boss.State;
            _chargeAlign = false;
            _aimLocked = false;
            _armLocked = false;
            _currentArmPose = _boss.GetArmPose();
            _selectedArmPose = _currentArmPose;
        }

        private void Update()
        {
            _boss.SetAimLocked(_aimLocked);
            _boss.SetArmLocked(_armLocked);
            _currentArmPose = _boss.GetArmPose();

            if (_resetNow)
            {
                _resetNow = false;
                _boss.ResetBoss();
                _state = _boss.State;
                _currentArmPose = _boss.GetArmPose();
            }

            if (_completePhaseNow)
            {
                _completePhaseNow = false;
                _boss.CompletePhaseChange();
                _state = _boss.State;
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

        private void ValidateDependencies()
        {
            if (_boss == null)
            {
                throw new InvalidOperationException(nameof(_boss));
            }
        }
    }
}
