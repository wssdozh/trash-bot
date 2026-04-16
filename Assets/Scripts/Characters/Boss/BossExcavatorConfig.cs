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

        [Header("Arm")]
        [SerializeField] private Vector3 _armDefaultBoomEuler = new Vector3(0f, 0f, -35f);

        [SerializeField] private Vector3 _armDefaultStickEuler = new Vector3(0f, 0f, 70f);

        [SerializeField] private Vector3 _armDefaultBucketEuler = new Vector3(0f, 0f, -25f);

        [SerializeField] private Vector3 _armBucketPrepareBoomEuler = new Vector3(0f, 0f, -55f);

        [SerializeField] private Vector3 _armBucketPrepareStickEuler = new Vector3(0f, 0f, 95f);

        [SerializeField] private Vector3 _armBucketPrepareBucketEuler = new Vector3(0f, 0f, -45f);

        [SerializeField] private Vector3 _armBucketStrikeBoomEuler = new Vector3(0f, 0f, 10f);

        [SerializeField] private Vector3 _armBucketStrikeStickEuler = new Vector3(0f, 0f, -35f);

        [SerializeField] private Vector3 _armBucketStrikeBucketEuler = new Vector3(0f, 0f, 20f);

        [SerializeField] private Vector3 _armGrabScrapBoomEuler = new Vector3(0f, 0f, 35f);

        [SerializeField] private Vector3 _armGrabScrapStickEuler = new Vector3(0f, 0f, 55f);

        [SerializeField] private Vector3 _armGrabScrapBucketEuler = new Vector3(0f, 0f, -60f);

        [SerializeField] private Vector3 _armThrowScrapBoomEuler = new Vector3(0f, 0f, -15f);

        [SerializeField] private Vector3 _armThrowScrapStickEuler = new Vector3(0f, 0f, -25f);

        [SerializeField] private Vector3 _armThrowScrapBucketEuler = new Vector3(0f, 0f, 55f);

        [SerializeField] private BossExcavatorAxis _armBoomAxis = BossExcavatorAxis.X;

        [SerializeField] private BossExcavatorAxis _armStickAxis = BossExcavatorAxis.X;

        [SerializeField] private BossExcavatorAxis _armBucketAxis = BossExcavatorAxis.X;

        [SerializeField] private bool _armBoomAxisInvert;

        [SerializeField] private bool _armStickAxisInvert;

        [SerializeField] private bool _armBucketAxisInvert;

        [Min(1f)]
        [SerializeField] private float _armBoomSpeed = 38f;

        [Min(1f)]
        [SerializeField] private float _armStickSpeed = 54f;

        [Min(1f)]
        [SerializeField] private float _armBucketSpeed = 76f;

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
        public Vector3 ArmNeutralBoomEuler => _armDefaultBoomEuler;
        public Vector3 ArmNeutralStickEuler => _armDefaultStickEuler;
        public Vector3 ArmNeutralBucketEuler => _armDefaultBucketEuler;
        public Vector3 ArmBucketPrepareBoomEuler => _armBucketPrepareBoomEuler;
        public Vector3 ArmBucketPrepareStickEuler => _armBucketPrepareStickEuler;
        public Vector3 ArmBucketPrepareBucketEuler => _armBucketPrepareBucketEuler;
        public Vector3 ArmBucketStrikeBoomEuler => _armBucketStrikeBoomEuler;
        public Vector3 ArmBucketStrikeStickEuler => _armBucketStrikeStickEuler;
        public Vector3 ArmBucketStrikeBucketEuler => _armBucketStrikeBucketEuler;
        public Vector3 ArmGrabScrapBoomEuler => _armGrabScrapBoomEuler;
        public Vector3 ArmGrabScrapStickEuler => _armGrabScrapStickEuler;
        public Vector3 ArmGrabScrapBucketEuler => _armGrabScrapBucketEuler;
        public Vector3 ArmThrowScrapBoomEuler => _armThrowScrapBoomEuler;
        public Vector3 ArmThrowScrapStickEuler => _armThrowScrapStickEuler;
        public Vector3 ArmThrowScrapBucketEuler => _armThrowScrapBucketEuler;
        public BossExcavatorAxis ArmBoomAxis => _armBoomAxis;
        public BossExcavatorAxis ArmStickAxis => _armStickAxis;
        public BossExcavatorAxis ArmBucketAxis => _armBucketAxis;
        public bool ArmBoomAxisInvert => _armBoomAxisInvert;
        public bool ArmStickAxisInvert => _armStickAxisInvert;
        public bool ArmBucketAxisInvert => _armBucketAxisInvert;
        public Vector3 ArmDefaultBoomEuler => _armDefaultBoomEuler;
        public Vector3 ArmDefaultStickEuler => _armDefaultStickEuler;
        public Vector3 ArmDefaultBucketEuler => _armDefaultBucketEuler;
        public float ArmBoomSpeed => _armBoomSpeed;
        public float ArmStickSpeed => _armStickSpeed;
        public float ArmBucketSpeed => _armBucketSpeed;
        public float MinMoveDistance => _mediumDistance - _distanceTolerance;
        public float MaxMoveDistance => _mediumDistance + _distanceTolerance;
    }
}
