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
        [SerializeField] private float _baseMoveSpeed = 1.35f;

        [Min(1f)]
        [SerializeField] private float _baseTurnSpeed = 62f;

        [Range(1f, 45f)]
        [SerializeField] private float _moveStartAngle = 8f;

        [Range(1f, 60f)]
        [SerializeField] private float _moveStopAngle = 14f;

        [Min(0.1f)]
        [SerializeField] private float _stopDistance = 0.9f;

        [Min(0.1f)]
        [SerializeField] private float _mediumDistance = 9f;

        [Min(0.1f)]
        [SerializeField] private float _distanceTolerance = 1.4f;

        [Min(0.1f)]
        [SerializeField] private float _distanceHysteresis = 0.6f;

        [Min(0.1f)]
        [SerializeField] private float _retreatDistance = 11.5f;

        [Range(5f, 90f)]
        [SerializeField] private float _flankAngle = 26f;

        [Min(0f)]
        [SerializeField] private float _flankSwitchThreshold = 2f;

        [Min(0f)]
        [SerializeField] private float _flankSwitchCooldown = 1.8f;

        [Min(0f)]
        [SerializeField] private float _targetSwitchCooldown = 0.85f;

        [Min(0f)]
        [SerializeField] private float _desiredPointDeadZone = 1.1f;

        [Min(0.1f)]
        [SerializeField] private float _wallProbeDistance = 2.8f;

        [Min(0.05f)]
        [SerializeField] private float _probeRadius = 1f;

        [Min(0f)]
        [SerializeField] private float _probeHeight = 0.8f;

        [Min(0.1f)]
        [SerializeField] private float _forwardProbeDistance = 2.6f;

        [Min(0.1f)]
        [SerializeField] private float _wallEscapeDistance = 5.2f;

        [Min(0.1f)]
        [SerializeField] private float _cornerEscapeDistance = 7.2f;

        [Range(0f, 1f)]
        [SerializeField] private float _escapeCenterWeight = 0.5f;

        [Min(1f)]
        [SerializeField] private float _arenaReturnDistance = 14f;

        [Min(0.1f)]
        [SerializeField] private float _chargeAlignDistance = 12f;

        [Min(0f)]
        [SerializeField] private float _wallPenalty = 4.5f;

        [Min(0f)]
        [SerializeField] private float _cornerPenalty = 8f;

        [Min(0f)]
        [SerializeField] private float _blockedPenalty = 6f;

        [Header("Aim")]
        [Min(1f)]
        [SerializeField] private float _cabinTurnSpeed = 58f;

        [Range(1f, 3f)]
        [SerializeField] private float _cabinPhaseTwoMult = 1.35f;

        public float MaxHealth => _maxHealth;
        public float PhaseTwoRatio => _phaseTwoRatio;
        public BossExcavatorState StartState => _startState;
        public float BaseMoveSpeed => _baseMoveSpeed;
        public float BaseTurnSpeed => _baseTurnSpeed;
        public float MoveStartAngle => _moveStartAngle;
        public float MoveStopAngle => Mathf.Max(_moveStopAngle, _moveStartAngle);
        public float StopDistance => _stopDistance;
        public float MediumDistance => _mediumDistance;
        public float DistanceTolerance => _distanceTolerance;
        public float DistanceHysteresis => _distanceHysteresis;
        public float RetreatDistance => _retreatDistance;
        public float FlankAngle => _flankAngle;
        public float FlankSwitchThreshold => _flankSwitchThreshold;
        public float FlankSwitchCooldown => _flankSwitchCooldown;
        public float TargetSwitchCooldown => _targetSwitchCooldown;
        public float DesiredPointDeadZone => _desiredPointDeadZone;
        public float WallProbeDistance => _wallProbeDistance;
        public float ProbeRadius => _probeRadius;
        public float ProbeHeight => _probeHeight;
        public float ForwardProbeDistance => _forwardProbeDistance;
        public float WallEscapeDistance => _wallEscapeDistance;
        public float CornerEscapeDistance => _cornerEscapeDistance;
        public float EscapeCenterWeight => _escapeCenterWeight;
        public float ArenaReturnDistance => _arenaReturnDistance;
        public float ChargeAlignDistance => _chargeAlignDistance;
        public float WallPenalty => _wallPenalty;
        public float CornerPenalty => _cornerPenalty;
        public float BlockedPenalty => _blockedPenalty;
        public float CabinTurnSpeed => _cabinTurnSpeed;
        public float CabinPhaseTwoMult => _cabinPhaseTwoMult;
        public float MinMoveDistance => _mediumDistance - _distanceTolerance;
        public float MaxMoveDistance => _mediumDistance + _distanceTolerance;
    }
}
