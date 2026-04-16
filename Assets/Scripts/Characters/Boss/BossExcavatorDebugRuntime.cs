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
        [SerializeField] private bool _applyNow;
        [SerializeField] private bool _resetNow;
        [SerializeField] private bool _completePhaseNow;

        private void Awake()
        {
            ValidateDependencies();
        }

        private void Start()
        {
            _state = _boss.State;
            _chargeAlign = false;
        }

        private void Update()
        {
            if (_resetNow)
            {
                _resetNow = false;
                _boss.ResetBoss();
                _state = _boss.State;
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
