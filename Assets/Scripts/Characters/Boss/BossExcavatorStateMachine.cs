using System;

namespace JunkyardBoss
{
    public sealed class BossExcavatorStateMachine
    {
        private readonly BossExcavator _boss;
        private BossExcavatorState _requestedState;
        private bool _phaseChangeCompleted;
        private bool _hasManualRequest;

        public BossExcavatorStateMachine(BossExcavator boss)
        {
            if (boss == null)
            {
                throw new InvalidOperationException(nameof(boss));
            }

            _boss = boss;
            _requestedState = BossExcavatorState.Idle;
        }

        public void Reset()
        {
            _requestedState = _boss.Config.StartState;
            _phaseChangeCompleted = false;
            _hasManualRequest = false;
        }

        public void RequestState(BossExcavatorState state)
        {
            if (state == BossExcavatorState.PhaseChange || state == BossExcavatorState.Dead)
            {
                throw new InvalidOperationException(nameof(state));
            }

            _requestedState = state;
            _hasManualRequest = true;
        }

        public void RequestAutoState(BossExcavatorState state)
        {
            if (state == BossExcavatorState.PhaseChange || state == BossExcavatorState.Dead)
            {
                throw new InvalidOperationException(nameof(state));
            }

            if (_hasManualRequest)
            {
                return;
            }

            _requestedState = state;
        }

        public void CompletePhaseChange()
        {
            _phaseChangeCompleted = true;
        }

        public void Tick()
        {
            if (_boss.IsDead)
            {
                EnterDead();

                return;
            }

            if (_boss.State == BossExcavatorState.PhaseChange)
            {
                UpdatePhaseChange();

                return;
            }

            if (_boss.ShouldStartPhaseChange())
            {
                EnterPhaseChange();

                return;
            }

            ApplyRequestedState();
        }

        private void EnterDead()
        {
            if (_boss.State == BossExcavatorState.Dead)
            {
                return;
            }

            _hasManualRequest = false;
            _boss.ApplyState(BossExcavatorState.Dead);
        }

        private void EnterPhaseChange()
        {
            _phaseChangeCompleted = false;
            _hasManualRequest = false;
            _requestedState = BossExcavatorState.Reposition;
            _boss.ApplyState(BossExcavatorState.PhaseChange);
        }

        private void UpdatePhaseChange()
        {
            if (_phaseChangeCompleted == false)
            {
                return;
            }

            _boss.ApplyPhase(GetNextPhase());
            _hasManualRequest = false;
            _requestedState = BossExcavatorState.Reposition;
            _boss.ApplyState(_requestedState);
            _phaseChangeCompleted = false;
        }

        private BossExcavatorPhase GetNextPhase()
        {
            if (_boss.Phase == BossExcavatorPhase.PhaseOne)
            {
                return BossExcavatorPhase.PhaseTwo;
            }

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                return BossExcavatorPhase.PhaseThree;
            }

            return BossExcavatorPhase.PhaseThree;
        }

        private void ApplyRequestedState()
        {
            if (_boss.State != _requestedState)
            {
                _boss.ApplyState(_requestedState);
            }

            if (_hasManualRequest)
            {
                _hasManualRequest = false;
            }
        }
    }
}
