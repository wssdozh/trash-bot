using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavator : MonoBehaviour
    {
        [SerializeField] private BossExcavatorConfig _config;
        [SerializeField] private BossExcavatorMove _move;
        [SerializeField] private BossExcavatorAim _aim;
        [SerializeField] private Transform _target;
        [SerializeField] private Transform _base;
        [SerializeField] private Rigidbody _baseRigidbody;
        [SerializeField] private Transform _cabin;
        [SerializeField] private Transform _boom;
        [SerializeField] private Transform _stick;
        [SerializeField] private Transform _bucket;

        private BossExcavatorStateMachine _stateMachine;
        private float _currentHealth;
        private BossExcavatorPhase _phase;
        private BossExcavatorState _state;

        public event Action<BossExcavatorPhase, BossExcavatorPhase> PhaseChanged;
        public event Action<BossExcavatorState, BossExcavatorState> StateChanged;
        public event Action Died;

        public BossExcavatorConfig Config => _config;
        public BossExcavatorMove Move => _move;
        public BossExcavatorAim Aim => _aim;
        public Transform Target => _target;
        public Transform Base => _base;
        public Rigidbody BaseRigidbody => _baseRigidbody;
        public Transform Cabin => _cabin;
        public Transform Boom => _boom;
        public Transform Stick => _stick;
        public Transform Bucket => _bucket;
        public float CurrentHealth => _currentHealth;
        public BossExcavatorPhase Phase => _phase;
        public BossExcavatorState State => _state;
        public bool IsDead => _currentHealth <= 0f;

        private void Awake()
        {
            ValidateDependencies();

            if (IsTargetSelf(_target))
            {
                _target = null;
            }

            _stateMachine = new BossExcavatorStateMachine(this);
            _move.Setup(_config, _base, _baseRigidbody, _target);
            _aim.Setup(this, _config, _cabin, _target);
            TryResolveTarget();

            ResetBoss();
        }

        private void Update()
        {
            _stateMachine.Tick();
        }

        private void LateUpdate()
        {
            _aim.Tick();
        }

        private void FixedUpdate()
        {
            if (CanUseTarget() == false)
            {
                TryResolveTarget();
            }

            UpdateMove();
        }

        public void ResetBoss()
        {
            _currentHealth = _config.MaxHealth;
            _phase = BossExcavatorPhase.PhaseOne;

            _stateMachine.Reset();
            ApplyState(_config.StartState);
            _move.SetChargeAlign(false);
            _aim.SetLocked(false);
        }

        public void RequestState(BossExcavatorState state)
        {
            _stateMachine.RequestState(state);
            _stateMachine.Tick();
        }

        public void CompletePhaseChange()
        {
            _stateMachine.CompletePhaseChange();
            _stateMachine.Tick();
        }

        public void SetTarget(Transform target)
        {
            if (target == null)
            {
                throw new InvalidOperationException(nameof(target));
            }

            if (IsTargetSelf(target))
            {
                throw new InvalidOperationException(nameof(target));
            }

            _target = target;
            _move.SetTarget(target);
            _aim.SetTarget(target);
        }

        public void SetChargeAlign(bool isChargeAlign)
        {
            _move.SetChargeAlign(isChargeAlign);
        }

        public void SetAimLocked(bool isLocked)
        {
            _aim.SetLocked(isLocked);
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

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            _stateMachine.Tick();
        }

        public float GetHealthRatio()
        {
            return _currentHealth / _config.MaxHealth;
        }

        internal bool ShouldStartPhaseChange()
        {
            if (_phase != BossExcavatorPhase.PhaseOne)
            {
                return false;
            }

            return GetHealthRatio() <= _config.PhaseTwoRatio;
        }

        internal void ApplyState(BossExcavatorState state)
        {
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
                return;
            }

            if (_state == BossExcavatorState.PhaseChange)
            {
                return;
            }

            if (_state == BossExcavatorState.Idle)
            {
                return;
            }

            if (_state == BossExcavatorState.Reposition)
            {
                _move.SetChargeAlign(false);
                _move.FixedTick();

                return;
            }

            if (_state == BossExcavatorState.Chase)
            {
                _move.SetChargeAlign(false);
                _move.FixedTick();

                return;
            }

            if (_state == BossExcavatorState.Attack)
            {
                _move.FixedTick();
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
        }

        private bool CanUseTarget()
        {
            if (_target == null)
            {
                return false;
            }

            if (IsTargetSelf(_target))
            {
                return false;
            }

            return true;
        }

        private bool TryResolveTarget()
        {
            if (CanUseTarget())
            {
                return true;
            }

            Player player = FindFirstObjectByType<Player>();

            if (player == null)
            {
                return false;
            }

            Transform playerBody = GetPlayerBody(player);

            if (playerBody == null)
            {
                return false;
            }

            if (IsTargetSelf(playerBody))
            {
                return false;
            }

            _target = playerBody;
            _move.SetTarget(playerBody);
            _aim.SetTarget(playerBody);

            return true;
        }

        private Transform GetPlayerBody(Player player)
        {
            if (player == null)
            {
                return null;
            }

            Transform playerTransform = player.transform;
            Transform playerBody = playerTransform.Find("Body");

            if (playerBody == null)
            {
                return null;
            }

            return playerBody;
        }

        private bool IsTargetSelf(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            if (target == transform)
            {
                return true;
            }

            return target.IsChildOf(transform);
        }
    }
}
