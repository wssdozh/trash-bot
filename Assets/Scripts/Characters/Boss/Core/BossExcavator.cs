using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavator : MonoBehaviour
    {
        [SerializeField] private BossExcavatorConfig _config;
        [SerializeField] private BossExcavatorMove _move;
        [SerializeField] private BossExcavatorAim _aim;
        [SerializeField] private BossExcavatorArm _arm;
        [SerializeField] private Transform _target;
        [SerializeField] private Transform _base;
        [SerializeField] private Rigidbody _baseRigidbody;
        [SerializeField] private Transform _cabin;
        [SerializeField] private Transform _boom;
        [SerializeField] private Transform _stick;
        [SerializeField] private Transform _bucket;
        [SerializeField] private Health _health;

        private BossExcavatorBrain _brain;
        private BossExcavatorPhaseThreeController _phaseThreeController;
        private BossExcavatorStateMachine _stateMachine;
        private RoomCombatLock _roomCombatLock;
        private BossExcavatorPhase _phase;
        private BossExcavatorState _state;

        public event Action<BossExcavatorPhase, BossExcavatorPhase> PhaseChanged;
        public event Action<BossExcavatorState, BossExcavatorState> StateChanged;
        public event Action Died;

        public BossExcavatorConfig Config => _config;
        public BossExcavatorMove Move => _move;
        public BossExcavatorAim Aim => _aim;
        public BossExcavatorArm Arm => _arm;
        public Transform Target => _target;
        public Transform Base => _base;
        public Rigidbody BaseRigidbody => _baseRigidbody;
        public Transform Cabin => _cabin;
        public Transform Boom => _boom;
        public Transform Stick => _stick;
        public Transform Bucket => _bucket;
        public Health Health => _health;
        public float CurrentHealth => _health.Value;
        public BossExcavatorPhase Phase => _phase;
        public BossExcavatorState State => _state;
        public BossExcavatorAttack CurrentAttack => _brain.CurrentAttack;
        public BossExcavatorAttack TargetAttack => _brain.TargetAttack;
        public bool IsDead => CurrentHealth <= _health.MinValue;

        private void Awake()
        {
            ValidateDependencies();

            if (IsTargetSelf(_target))
            {
                _target = null;
            }

            _brain = new BossExcavatorBrain(this);
            _phaseThreeController = new BossExcavatorPhaseThreeController(this);
            _stateMachine = new BossExcavatorStateMachine(this);
            _roomCombatLock = ResolveRoomCombatLock();
            _move.Setup(_config, _base, _baseRigidbody, _target);
            _aim.Setup(this, _config, _cabin, _target);
            _arm.Setup(this, _config, _boom, _stick, _bucket);
            TryResolveTarget();

            ResetBoss();
        }

        private void OnEnable()
        {
            _health.Ended += OnHealthEnded;
        }

        private void OnDisable()
        {
            _health.Ended -= OnHealthEnded;
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null)
            {
                return;
            }

            DrawBucketAttackGizmos();
            DrawSweepAttackGizmos();
            DrawChargeAttackGizmos();
            DrawThrowAttackGizmos();
        }

        private void Update()
        {
            if (ShouldHoldPreCombatIdle())
            {
                HoldPreCombatIdle();

                return;
            }

            _brain.Tick();
            _stateMachine.Tick();
            _phaseThreeController.Tick();
        }

        private void LateUpdate()
        {
            _aim.Tick();
            _arm.Tick();
        }

        private void FixedUpdate()
        {
            if (CanUseTarget() == false)
            {
                TryResolveTarget();
            }

            if (ShouldHoldPreCombatIdle())
            {
                HoldPreCombatIdle();

                return;
            }

            _brain.FixedTick();
            UpdateMove();
        }

        public void ResetBoss()
        {
            ResetHealth();
            _phase = BossExcavatorPhase.PhaseOne;

            _stateMachine.Reset();
            ApplyState(_config.StartState);
            _move.ResetRuntime();
            _move.SetChargeAlign(false);
            _move.SetAttackIntent(BossExcavatorAttack.None);
            _aim.SetLocked(false);
            _arm.SetLocked(false);
            _arm.SetDefaultPoseImmediate();
            _brain.Reset();
            _phaseThreeController.Reset();
        }

        public void RequestState(BossExcavatorState state)
        {
            _stateMachine.RequestState(state);
            _stateMachine.Tick();
        }

        internal void RequestAutoState(BossExcavatorState state)
        {
            _stateMachine.RequestAutoState(state);
        }

        public void CompletePhaseChange()
        {
            _stateMachine.CompletePhaseChange();
            _stateMachine.Tick();
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            if (_state == BossExcavatorState.Dead)
            {
                return;
            }

            _health.Decrease(damage);
            _stateMachine.Tick();
        }

        public float GetHealthRatio()
        {
            return CurrentHealth / _config.MaxHealth;
        }

        internal bool ShouldStartPhaseChange()
        {
            if (_phase == BossExcavatorPhase.PhaseOne)
            {
                return GetHealthRatio() <= _config.PhaseTwoRatio;
            }

            if (_phase == BossExcavatorPhase.PhaseTwo)
            {
                return GetHealthRatio() <= _config.PhaseThreeRatio;
            }

            return false;
        }

        public bool IsAdvancedPhase
        {
            get
            {
                return _phase != BossExcavatorPhase.PhaseOne;
            }
        }

        public bool IsFinalPhase
        {
            get
            {
                return _phase == BossExcavatorPhase.PhaseThree;
            }
        }

        public float GetPhaseAttackSpeedMult()
        {
            if (_phase == BossExcavatorPhase.PhaseThree)
            {
                return _config.PhaseThreeAttackSpeedMult;
            }

            if (_phase == BossExcavatorPhase.PhaseTwo)
            {
                return _config.PhaseTwoAttackSpeedMult;
            }

            return 1f;
        }

        public float GetPhaseDamageMult()
        {
            if (_phase == BossExcavatorPhase.PhaseThree)
            {
                return _config.PhaseThreeDamageMult;
            }

            if (_phase == BossExcavatorPhase.PhaseTwo)
            {
                return _config.PhaseTwoDamageMult;
            }

            return 1f;
        }

        public bool IsFriendlyMinion(Health health)
        {
            if (health == null)
            {
                return false;
            }

            Transform current = health.transform;

            while (current != null)
            {
                if (current.name == BossExcavatorPhaseThreeController.MinionsRootName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        public float GetPhaseChargeSpeedMult()
        {
            if (_phase == BossExcavatorPhase.PhaseThree)
            {
                return _config.PhaseThreeChargeSpeedMult;
            }

            if (_phase == BossExcavatorPhase.PhaseTwo)
            {
                return _config.PhaseTwoChargeSpeedMult;
            }

            return 1f;
        }

        public float GetPhaseCooldownMult()
        {
            if (_phase == BossExcavatorPhase.PhaseThree)
            {
                return _config.PhaseThreeCooldownMult;
            }

            if (_phase == BossExcavatorPhase.PhaseTwo)
            {
                return _config.PhaseTwoCooldownMult;
            }

            return 1f;
        }

        public float GetPhaseCabinTurnSpeedMult()
        {
            if (_phase == BossExcavatorPhase.PhaseThree)
            {
                return _config.CabinPhaseThreeMult;
            }

            if (_phase == BossExcavatorPhase.PhaseTwo)
            {
                return _config.CabinPhaseTwoMult;
            }

            return 1f;
        }

        public float GetPhaseSweepSpinSpeedMult()
        {
            if (IsAdvancedPhase == false)
            {
                return 1f;
            }

            return _config.PhaseTwoSweepSpinSpeedMult;
        }

        public float GetPhaseComboSweepSpinSpeedMult()
        {
            if (IsAdvancedPhase == false)
            {
                return 1f;
            }

            return _config.PhaseTwoComboSweepSpinSpeedMult;
        }

        public int GetPhaseThrowProjectileCount()
        {
            if (IsAdvancedPhase)
            {
                return _config.PhaseTwoThrowProjectileCount;
            }

            return _config.ThrowProjectileCount;
        }

        public float GetPhaseThrowProjectileSpreadAngle()
        {
            if (IsAdvancedPhase)
            {
                return _config.PhaseTwoThrowProjectileSpreadAngle;
            }

            return _config.ThrowProjectileSpreadAngle;
        }

        internal void ApplyState(BossExcavatorState state)
        {
            state = NormalizeState(state);

            if (_state == state)
            {
                return;
            }

            BossExcavatorState previousState = _state;
            _state = state;

            StateChanged?.Invoke(previousState, _state);

            if (_state == BossExcavatorState.Dead)
            {
                Died?.Invoke();
            }
        }

        internal void ApplyPhase(BossExcavatorPhase phase)
        {
            if (_phase == phase)
            {
                return;
            }

            BossExcavatorPhase previousPhase = _phase;
            _phase = phase;

            PhaseChanged?.Invoke(previousPhase, _phase);
        }

        private void UpdateMove()
        {
            if (_state == BossExcavatorState.Dead)
            {
                _move.Stop();

                return;
            }

            if (_state == BossExcavatorState.PhaseChange)
            {
                _move.Stop();

                return;
            }

            if (_state == BossExcavatorState.Idle)
            {
                _move.Stop();

                return;
            }

            if (_state == BossExcavatorState.Move)
            {
                _move.FixedTick();

                return;
            }

            if (_state == BossExcavatorState.Attack)
            {
                if (CurrentAttack == BossExcavatorAttack.ThrowScrap)
                {
                    _move.FixedTick();

                    return;
                }

                _move.Stop();
            }
        }

        private void ValidateDependencies()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(nameof(_config));
            }

            if (_move == null)
            {
                throw new InvalidOperationException(nameof(_move));
            }

            if (_aim == null)
            {
                throw new InvalidOperationException(nameof(_aim));
            }

            if (_arm == null)
            {
                throw new InvalidOperationException(nameof(_arm));
            }

            if (_base == null)
            {
                throw new InvalidOperationException(nameof(_base));
            }

            if (_baseRigidbody == null)
            {
                throw new InvalidOperationException(nameof(_baseRigidbody));
            }

            if (_cabin == null)
            {
                throw new InvalidOperationException(nameof(_cabin));
            }

            if (_boom == null)
            {
                throw new InvalidOperationException(nameof(_boom));
            }

            if (_stick == null)
            {
                throw new InvalidOperationException(nameof(_stick));
            }

            if (_bucket == null)
            {
                throw new InvalidOperationException(nameof(_bucket));
            }

            if (_health == null)
            {
                throw new InvalidOperationException(nameof(_health));
            }
        }

        private bool ShouldHoldPreCombatIdle()
        {
            if (IsDead)
            {
                return false;
            }

            RoomCombatLock roomCombatLock = ResolveRoomCombatLock();

            if (roomCombatLock == null)
            {
                return false;
            }

            return roomCombatLock.IsLocked == false;
        }

        private void HoldPreCombatIdle()
        {
            _move.SetChargeAlign(false);
            _move.SetAttackIntent(BossExcavatorAttack.None);
            _move.Stop();
            _aim.SetLocked(false);
            _arm.SetLocked(false);
            _stateMachine.RequestAutoState(BossExcavatorState.Idle);
            _stateMachine.Tick();
        }

        private RoomCombatLock ResolveRoomCombatLock()
        {
            if (_roomCombatLock != null)
            {
                return _roomCombatLock;
            }

            if (_base != null)
            {
                _roomCombatLock = _base.GetComponentInParent<RoomCombatLock>();

                if (_roomCombatLock != null)
                {
                    return _roomCombatLock;
                }
            }

            _roomCombatLock = GetComponentInParent<RoomCombatLock>();

            return _roomCombatLock;
        }

        private BossExcavatorState NormalizeState(BossExcavatorState state)
        {
            if (state == BossExcavatorState.Reposition)
            {
                return BossExcavatorState.Move;
            }

            if (state == BossExcavatorState.Chase)
            {
                return BossExcavatorState.Move;
            }

            return state;
        }

        private void ResetHealth()
        {
            _health.SetAutoRegen(false);
            _health.SetMaxValue(_config.MaxHealth);
            _health.Fill();
        }

        private void OnHealthEnded()
        {
            if (_stateMachine == null)
            {
                return;
            }

            _stateMachine.Tick();
        }

    }
}
