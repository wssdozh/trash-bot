using UnityEngine;

namespace JunkyardBoss
{
    [CreateAssetMenu(fileName = "BossExcavatorConfig", menuName = "Game/Bosses/BossExcavatorConfig")]
    public sealed class BossExcavatorConfig : ScriptableObject
    {
        [Min(1f)]
        [SerializeField] private float _maxHealth = 300f;

        [Range(0.05f, 0.95f)]
        [SerializeField] private float _phaseTwoRatio = 0.5f;

        [SerializeField] private BossExcavatorState _startState = BossExcavatorState.Idle;

        [Header("Move")]
        [Min(0.1f)]
        [SerializeField] private float _baseMoveSpeed = 4f;

        [Min(1f)]
        [SerializeField] private float _baseTurnSpeed = 180f;

        [Range(1f, 45f)]
        [SerializeField] private float _moveStartAngle = 14f;

        [Min(0.1f)]
        [SerializeField] private float _stopDistance = 0.35f;

        [Min(0.1f)]
        [SerializeField] private float _mediumDistance = 8f;

        [Min(0.1f)]
        [SerializeField] private float _distanceTolerance = 1.5f;

        [Range(5f, 90f)]
        [SerializeField] private float _flankAngle = 40f;

        [Min(0f)]
        [SerializeField] private float _flankSwitchThreshold = 0.5f;

        [Min(0.1f)]
        [SerializeField] private float _wallProbeDistance = 2.5f;

        [Min(0.05f)]
        [SerializeField] private float _probeRadius = 0.8f;

        [Min(0f)]
        [SerializeField] private float _probeHeight = 0.6f;

        [Min(0.1f)]
        [SerializeField] private float _forwardProbeDistance = 1.6f;

        [Min(0.1f)]
        [SerializeField] private float _wallEscapeDistance = 4f;

        [Min(1f)]
        [SerializeField] private float _arenaReturnDistance = 14f;

        [Min(0.1f)]
        [SerializeField] private float _chargeAlignDistance = 10f;

        [Min(0f)]
        [SerializeField] private float _wallPenalty = 3f;

        [Min(0f)]
        [SerializeField] private float _cornerPenalty = 6f;

        public float MaxHealth => _maxHealth;

        public float PhaseTwoRatio => _phaseTwoRatio;

        public BossExcavatorState StartState => _startState;

        public float BaseMoveSpeed => _baseMoveSpeed;

        public float BaseTurnSpeed => _baseTurnSpeed;

        public float MoveStartAngle => _moveStartAngle;

        public float StopDistance => _stopDistance;

        public float MediumDistance => _mediumDistance;

        public float DistanceTolerance => _distanceTolerance;

        public float FlankAngle => _flankAngle;

        public float FlankSwitchThreshold => _flankSwitchThreshold;

        public float WallProbeDistance => _wallProbeDistance;

        public float ProbeRadius => _probeRadius;

        public float ProbeHeight => _probeHeight;

        public float ForwardProbeDistance => _forwardProbeDistance;

        public float WallEscapeDistance => _wallEscapeDistance;

        public float ArenaReturnDistance => _arenaReturnDistance;

        public float ChargeAlignDistance => _chargeAlignDistance;

        public float WallPenalty => _wallPenalty;

        public float CornerPenalty => _cornerPenalty;

        public float MinMoveDistance => _mediumDistance - _distanceTolerance;

        public float MaxMoveDistance => _mediumDistance + _distanceTolerance;
    }
}
