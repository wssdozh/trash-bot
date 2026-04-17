using UnityEngine;

namespace JunkyardBoss
{
    [CreateAssetMenu(fileName = "BossExcavatorConfig", menuName = "Game/Bosses/BossExcavatorConfig")]
    public sealed class BossExcavatorConfig : ScriptableObject
    {
        [Min(1f)]
        [SerializeField] private float _maxHealth = 280f;

        [Range(0.05f, 0.95f)]
        [SerializeField] private float _phaseTwoRatio = 0.5f;

        [Header("Phase Two")]
        [Min(0.05f)]
        [SerializeField] private float _phaseChangeDuration = 0.9f;

        [Range(1f, 3f)]
        [SerializeField] private float _phaseTwoAttackSpeedMult = 1.25f;

        [Range(1f, 3f)]
        [SerializeField] private float _phaseTwoDamageMult = 1.32f;

        [Range(1f, 3f)]
        [SerializeField] private float _phaseTwoChargeSpeedMult = 1.18f;

        [Range(1f, 3f)]
        [SerializeField] private float _phaseTwoSweepSpinSpeedMult = 1.38f;

        [Range(1f, 3f)]
        [SerializeField] private float _phaseTwoComboSweepSpinSpeedMult = 2.15f;

        [SerializeField] private BossExcavatorState _startState = BossExcavatorState.Idle;

        [Header("Move")]
        [Min(0.1f)]
        [SerializeField] private float _baseMoveSpeed = 4.15f;

        [Min(1f)]
        [SerializeField] private float _baseTurnSpeed = 168f;

        [Range(1f, 45f)]
        [SerializeField] private float _moveStartAngle = 10f;

        [Range(1f, 60f)]
        [SerializeField] private float _moveStopAngle = 36f;

        [Min(0.1f)]
        [SerializeField] private float _stopDistance = 0.55f;

        [Min(0.1f)]
        [SerializeField] private float _mediumDistance = 5.6f;

        [Min(0.1f)]
        [SerializeField] private float _distanceTolerance = 0.7f;

        [Min(0.1f)]
        [SerializeField] private float _distanceHysteresis = 0.35f;

        [Min(0.1f)]
        [SerializeField] private float _retreatDistance = 5.95f;

        [Range(5f, 90f)]
        [SerializeField] private float _flankAngle = 20f;

        [Min(0f)]
        [SerializeField] private float _flankSwitchThreshold = 2f;

        [Min(0f)]
        [SerializeField] private float _flankSwitchCooldown = 1.8f;

        [Min(0f)]
        [SerializeField] private float _targetSwitchCooldown = 0.35f;

        [Min(0f)]
        [SerializeField] private float _desiredPointDeadZone = 0.4f;

        [Min(0.1f)]
        [SerializeField] private float _wallProbeDistance = 2.2f;

        [Min(0.05f)]
        [SerializeField] private float _probeRadius = 0.75f;

        [Min(0f)]
        [SerializeField] private float _probeHeight = 0.8f;

        [Min(0.1f)]
        [SerializeField] private float _forwardProbeDistance = 2f;

        [Min(0.1f)]
        [SerializeField] private float _wallEscapeDistance = 6f;

        [Min(0.1f)]
        [SerializeField] private float _cornerEscapeDistance = 9f;

        [Range(0f, 1f)]
        [SerializeField] private float _escapeCenterWeight = 0.5f;

        [Min(1f)]
        [SerializeField] private float _arenaReturnDistance = 18.9f;

        [Min(0.1f)]
        [SerializeField] private float _chargeAlignDistance = 13.8f;

        [Min(0f)]
        [SerializeField] private float _wallPenalty = 4.5f;

        [Min(0f)]
        [SerializeField] private float _cornerPenalty = 8f;

        [Min(0f)]
        [SerializeField] private float _blockedPenalty = 6f;

        [Header("Attack Decision")]
        [Min(0.1f)]
        [SerializeField] private float _attackChaseDistance = 4.95f;

        [Range(5f, 90f)]
        [SerializeField] private float _repositionBaseAngle = 86f;

        [Min(0.1f)]
        [SerializeField] private float _bucketMaxDistance = 5.1f;

        [Range(1f, 90f)]
        [SerializeField] private float _bucketBaseAngle = 50f;

        [Range(1f, 90f)]
        [SerializeField] private float _bucketCabinAngle = 58f;

        [Min(0.1f)]
        [SerializeField] private float _sweepMaxDistance = 2.9f;

        [Range(1f, 180f)]
        [SerializeField] private float _sweepCabinAngle = 115f;

        [Min(0.1f)]
        [SerializeField] private float _throwMinDistance = 4.6f;

        [Min(0.1f)]
        [SerializeField] private float _throwMaxDistance = 8.3f;

        [Range(1f, 90f)]
        [SerializeField] private float _throwBaseAngle = 52f;

        [Range(1f, 90f)]
        [SerializeField] private float _throwCabinAngle = 46f;

        [Min(0.1f)]
        [SerializeField] private float _chargeMinDistance = 4.85f;

        [Min(0.1f)]
        [SerializeField] private float _chargeMaxDistance = 13f;

        [Range(1f, 90f)]
        [SerializeField] private float _chargeBaseAngle = 36f;

        [Range(0.1f, 1f)]
        [SerializeField] private float _phaseTwoCooldownMult = 0.55f;

        [Header("Attack Cooldowns")]
        [Min(0f)]
        [SerializeField] private float _bucketAttackCooldown = 1.45f;

        [Min(0f)]
        [SerializeField] private float _sweepAttackCooldown = 2.05f;

        [Min(0f)]
        [SerializeField] private float _throwAttackCooldown = 2.45f;

        [Min(0f)]
        [SerializeField] private float _chargeAttackCooldown = 3.8f;

        [Header("Attack Timing")]
        [Min(0.05f)]
        [SerializeField] private float _bucketPrepareTime = 0.48f;

        [Min(0.05f)]
        [SerializeField] private float _bucketStrikeTime = 0.32f;

        [Min(0.05f)]
        [SerializeField] private float _sweepPrepareTime = 0.24f;

        [Min(0.05f)]
        [SerializeField] private float _sweepAttackTime = 1.2f;

        [Min(1)]
        [SerializeField] private int _sweepSpinTurns = 3;

        [Min(0.05f)]
        [SerializeField] private float _throwGrabTime = 0.52f;

        [Min(0.05f)]
        [SerializeField] private float _throwReleaseTime = 0.42f;

        [Min(0.05f)]
        [SerializeField] private float _chargeAlignTime = 0.2f;

        [Min(0.05f)]
        [SerializeField] private float _chargeTelegraphTime = 0.45f;

        [Min(0.05f)]
        [SerializeField] private float _chargeAttackTime = 1.15f;

        [Min(0.05f)]
        [SerializeField] private float _chargeRecoveryTime = 0.7f;

        [Range(0.1f, 3f)]
        [SerializeField] private float _attackPoseSpeedMult = 1.45f;

        [Header("Bucket Attack")]
        [Min(0.1f)]
        [SerializeField] private float _bucketHitDamage = 24f;

        [Min(0.1f)]
        [SerializeField] private float _bucketHitRadius = 2.05f;

        [Range(1f, 180f)]
        [SerializeField] private float _bucketHitAngle = 95f;

        [Min(0f)]
        [SerializeField] private float _bucketHitOffset = 0.95f;

        [Min(0.1f)]
        [SerializeField] private float _bucketShockwaveDamage = 12f;

        [Min(0.1f)]
        [SerializeField] private float _bucketShockwaveRadius = 2.75f;

        [Min(0f)]
        [SerializeField] private float _bucketShockwaveOffset = 0.1f;

        [SerializeField] private LayerMask _bucketHitMask = ~0;

        [Range(0.1f, 3f)]
        [SerializeField] private float _bucketPrepareSpeedMult = 1.35f;

        [Range(0.1f, 3f)]
        [SerializeField] private float _bucketStrikeSpeedMult = 2f;

        [Range(0.1f, 3f)]
        [SerializeField] private float _bucketRecoverSpeedMult = 1.15f;

        [Header("Sweep Attack")]
        [Min(0.1f)]
        [SerializeField] private float _sweepHitDamage = 4f;

        [Min(0.05f)]
        [SerializeField] private float _sweepDamageInterval = 0.22f;

        [Min(0.1f)]
        [SerializeField] private float _sweepHitRadius = 1.45f;

        [Min(0f)]
        [SerializeField] private float _sweepHitOffset = 0.25f;

        [Min(1f)]
        [SerializeField] private float _sweepSpinSpeed = 270f;

        [Header("Throw Attack")]
        [Min(1)]
        [SerializeField] private int _throwProjectileCount = 3;

        [Range(0f, 90f)]
        [SerializeField] private float _throwProjectileSpreadAngle = 18f;

        [Min(0.1f)]
        [SerializeField] private float _throwProjectileDamage = 6.5f;

        [Min(0.1f)]
        [SerializeField] private float _throwProjectileSpeedMult = 1.15f;

        [Min(0f)]
        [SerializeField] private float _throwSpawnOffset = 0.5f;

        [SerializeField] private LayerMask _throwHitMask = ~0;

        [Header("Charge Attack")]
        [Min(0.1f)]
        [SerializeField] private float _chargeSpeed = 11.2f;

        [Min(0.1f)]
        [SerializeField] private float _chargeHitDamage = 28f;

        [Min(0.1f)]
        [SerializeField] private float _chargeHitRadius = 1.4f;

        [Min(0f)]
        [SerializeField] private float _chargeHitOffset = 1.1f;

        [SerializeField] private LayerMask _chargeHitMask = ~0;

        [Min(0.05f)]
        [SerializeField] private float _attackRecoveryTime = 0.15f;

        [Header("Move Rhythm")]
        [Min(0.1f)]
        [SerializeField] private float _moveOrbitTime = 0.9f;

        [Min(0.1f)]
        [SerializeField] private float _movePressureTime = 2.35f;

        [Min(0.1f)]
        [SerializeField] private float _moveRetreatTime = 0.35f;

        [Min(0f)]
        [SerializeField] private float _moveRepositionCommitTime = 0.18f;

        [Min(0f)]
        [SerializeField] private float _moveChaseCommitTime = 0.1f;

        [Header("Aim")]
        [Min(1f)]
        [SerializeField] private float _cabinTurnSpeed = 88f;

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

        [SerializeField] private Vector3 _armSweepBoomEuler = new Vector3(0f, 0f, 4f);

        [SerializeField] private Vector3 _armSweepStickEuler = new Vector3(0f, 0f, -12f);

        [SerializeField] private Vector3 _armSweepBucketEuler = new Vector3(0f, 0f, 18f);

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
        public float PhaseChangeDuration => _phaseChangeDuration;
        public float PhaseTwoAttackSpeedMult => _phaseTwoAttackSpeedMult;
        public float PhaseTwoDamageMult => _phaseTwoDamageMult;
        public float PhaseTwoChargeSpeedMult => _phaseTwoChargeSpeedMult;
        public float PhaseTwoSweepSpinSpeedMult => _phaseTwoSweepSpinSpeedMult;
        public float PhaseTwoComboSweepSpinSpeedMult => _phaseTwoComboSweepSpinSpeedMult;
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
        public float AttackChaseDistance => _attackChaseDistance;
        public float RepositionBaseAngle => _repositionBaseAngle;
        public float BucketMaxDistance => _bucketMaxDistance;
        public float BucketBaseAngle => _bucketBaseAngle;
        public float BucketCabinAngle => _bucketCabinAngle;
        public float SweepMaxDistance => _sweepMaxDistance;
        public float SweepCabinAngle => _sweepCabinAngle;
        public float ThrowMinDistance => _throwMinDistance;
        public float ThrowMaxDistance => Mathf.Max(_throwMaxDistance, _throwMinDistance + 0.1f);
        public float ThrowBaseAngle => _throwBaseAngle;
        public float ThrowCabinAngle => _throwCabinAngle;
        public float ChargeMinDistance => _chargeMinDistance;
        public float ChargeMaxDistance => Mathf.Max(_chargeMaxDistance, _chargeMinDistance + 0.1f);
        public float ChargeBaseAngle => _chargeBaseAngle;
        public float PhaseTwoCooldownMult => _phaseTwoCooldownMult;
        public float BucketAttackCooldown => _bucketAttackCooldown;
        public float SweepAttackCooldown => _sweepAttackCooldown;
        public float ThrowAttackCooldown => _throwAttackCooldown;
        public float ChargeAttackCooldown => _chargeAttackCooldown;
        public float BucketPrepareTime => _bucketPrepareTime;
        public float BucketStrikeTime => _bucketStrikeTime;
        public float SweepPrepareTime => _sweepPrepareTime;
        public float SweepAttackTime => _sweepAttackTime;
        public int SweepSpinTurns => Mathf.Max(_sweepSpinTurns, 1);
        public float ThrowGrabTime => _throwGrabTime;
        public float ThrowReleaseTime => _throwReleaseTime;
        public float ChargeAlignTime => _chargeAlignTime;
        public float ChargeTelegraphTime => _chargeTelegraphTime;
        public float ChargeAttackTime => _chargeAttackTime;
        public float ChargeRecoveryTime => _chargeRecoveryTime;
        public float AttackPoseSpeedMult => _attackPoseSpeedMult;
        public float BucketHitDamage => _bucketHitDamage;
        public float BucketHitRadius => _bucketHitRadius;
        public float BucketHitAngle => _bucketHitAngle;
        public float BucketHitOffset => _bucketHitOffset;
        public float BucketShockwaveDamage => _bucketShockwaveDamage;
        public float BucketShockwaveRadius => _bucketShockwaveRadius;
        public float BucketShockwaveOffset => _bucketShockwaveOffset;
        public LayerMask BucketHitMask => _bucketHitMask;
        public float BucketPrepareSpeedMult => _bucketPrepareSpeedMult;
        public float BucketStrikeSpeedMult => _bucketStrikeSpeedMult;
        public float BucketRecoverSpeedMult => _bucketRecoverSpeedMult;
        public float SweepHitDamage => _sweepHitDamage;
        public float SweepDamageInterval => _sweepDamageInterval;
        public float SweepHitRadius => _sweepHitRadius;
        public float SweepHitOffset => _sweepHitOffset;
        public float SweepSpinSpeed => _sweepSpinSpeed;
        public int ThrowProjectileCount => Mathf.Max(_throwProjectileCount, 1);
        public float ThrowProjectileSpreadAngle => _throwProjectileSpreadAngle;
        public float ThrowProjectileDamage => _throwProjectileDamage;
        public float ThrowProjectileSpeedMult => _throwProjectileSpeedMult;
        public float ThrowSpawnOffset => _throwSpawnOffset;
        public LayerMask ThrowHitMask => _throwHitMask;
        public float ChargeSpeed => _chargeSpeed;
        public float ChargeHitDamage => _chargeHitDamage;
        public float ChargeHitRadius => _chargeHitRadius;
        public float ChargeHitOffset => _chargeHitOffset;
        public LayerMask ChargeHitMask => _chargeHitMask;
        public float AttackRecoveryTime => _attackRecoveryTime;
        public float MoveOrbitTime => _moveOrbitTime;
        public float MovePressureTime => _movePressureTime;
        public float MoveRetreatTime => _moveRetreatTime;
        public float MoveRepositionCommitTime => _moveRepositionCommitTime;
        public float MoveChaseCommitTime => _moveChaseCommitTime;
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
        public Vector3 ArmSweepBoomEuler => _armSweepBoomEuler;
        public Vector3 ArmSweepStickEuler => _armSweepStickEuler;
        public Vector3 ArmSweepBucketEuler => _armSweepBucketEuler;
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
