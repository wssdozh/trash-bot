using System;

namespace JunkyardBoss
{
    public sealed class BossExcavatorStateMachine
    {
        private readonly BossExcavator _boss;
        private BossExcavatorState _requestedState;
        private bool _phaseChangeCompleted;

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
        }

        public void RequestState(BossExcavatorState state)
        {
            if (state == BossExcavatorState.PhaseChange || state == BossExcavatorState.Dead)
            {
                throw new InvalidOperationException(nameof(state));
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

            if (_boss.State == _requestedState)
            {
                return;
            }

            _boss.ApplyState(_requestedState);
        }

        private void EnterDead()
        {
            if (_boss.State == BossExcavatorState.Dead)
            {
                return;
            }

            _boss.ApplyState(BossExcavatorState.Dead);
        }

        private void EnterPhaseChange()
        {
            _phaseChangeCompleted = false;
            _boss.ApplyState(BossExcavatorState.PhaseChange);
        }

        private void UpdatePhaseChange()
        {
            if (_phaseChangeCompleted == false)
            {
                return;
            }

            _boss.ApplyPhase(BossExcavatorPhase.PhaseTwo);
            _boss.ApplyState(_requestedState);
            _phaseChangeCompleted = false;
        }
    }
}
