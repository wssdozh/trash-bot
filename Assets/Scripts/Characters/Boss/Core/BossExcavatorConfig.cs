using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    [CreateAssetMenu(fileName = "BossExcavatorConfig", menuName = "Game/Bosses/BossExcavatorConfig")]
    public sealed partial class BossExcavatorConfig : ScriptableObject
    {
        [Min(1f)]
        [SerializeField] private float _maxHealth = 670f;

        [Range(0.05f, 0.95f)]
        [SerializeField] private float _phaseTwoRatio = 0.5f;

        [Range(0.01f, 0.9f)]
        [SerializeField] private float _phaseThreeRatio = 0.22f;

        [Header("Phase Two")]
        [Min(0.05f)]
        [SerializeField] private float _phaseChangeDuration = 0.9f;

        [Range(1f, 3f)]
        [SerializeField] private float _phaseTwoAttackSpeedMult = 1.25f;

        [Range(1f, 3f)]
        [SerializeField] private float _phaseTwoDamageMult = 1.32f;

        [Range(0.1f, 3f)]
        [SerializeField] private float _phaseTwoChargeSpeedMult = 1f;

        [Range(1f, 3f)]
        [SerializeField] private float _phaseTwoSweepSpinSpeedMult = 1.38f;

        [Range(1f, 3f)]
        [SerializeField] private float _phaseTwoComboSweepSpinSpeedMult = 2.15f;

        [Range(0.2f, 10f)]
        [SerializeField] private float _phaseThreeDecisionSpeedMult = 7f;

        [Range(1f, 5f)]
        [SerializeField] private float _phaseThreeAttackSpeedMult = 1.75f;

        [Range(1f, 5f)]
        [SerializeField] private float _phaseThreeDamageMult = 1.85f;

        [Range(0.1f, 3f)]
        [SerializeField] private float _phaseThreeChargeSpeedMult = 1.42f;

        [Range(0.1f, 1f)]
        [SerializeField] private float _phaseThreeCooldownMult = 0.35f;

        [Range(0.1f, 1f)]
        [SerializeField] private float _phaseThreeStallRecoverTimeMult = 0.25f;

        [Header("Phase Three Summons")]
        [SerializeField] private int _phaseThreeInitialWaveSize = 3;

        [SerializeField] private int _phaseThreeWaveSizeStep = 1;

        [SerializeField] private List<EnemySpawnConfig> _phaseThreeEnemyPrefabs = new List<EnemySpawnConfig>();

        [SerializeField] private BossExcavatorState _startState = BossExcavatorState.Idle;

        [Header("Move")]
        [Min(0.1f)]
        [SerializeField] private float _baseMoveSpeed = 4.15f;

        [Min(1f)]
        [SerializeField] private float _baseTurnSpeed = 168f;

        [Min(1f)]
        [SerializeField] private float _baseTurnAcceleration = 560f;

        [Min(1f)]
        [SerializeField] private float _baseTurnDeceleration = 760f;

        [Range(1f, 90f)]
        [SerializeField] private float _baseTurnSlowAngle = 42f;

        [Range(0.05f, 1f)]
        [SerializeField] private float _baseTurnMinSpeedFactor = 0.24f;

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

        [Min(0f)]
        [SerializeField] private float _scrapTrailCooldown = 4.8f;

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

        [Header("Scrap Trail")]
        [Min(0.1f)]
        [SerializeField] private float _scrapTrailMinDistance = 3.2f;

        [Min(0.1f)]
        [SerializeField] private float _scrapTrailMaxDistance = 10.8f;

        [Range(1f, 120f)]
        [SerializeField] private float _scrapTrailBaseAngle = 72f;

        [Min(0.1f)]
        [SerializeField] private float _scrapTrailDuration = 2.15f;

        [Min(0.1f)]
        [SerializeField] private float _scrapTrailMinMoveSpeed = 1.15f;

        [Min(0.1f)]
        [SerializeField] private float _scrapTrailSpawnSpacing = 0.9f;

        [Min(0f)]
        [SerializeField] private float _scrapTrailSpawnBackOffset = 0.85f;

        [Min(0.1f)]
        [SerializeField] private float _scrapTrailGroundProbeHeight = 2.4f;

        [Min(0.1f)]
        [SerializeField] private float _scrapTrailGroundProbeDistance = 4.2f;

        [Min(0.1f)]
        [SerializeField] private float _scrapTrailBlockLifetime = 9f;

        [SerializeField] private Vector3 _scrapTrailBlockSize = new Vector3(0.475f, 0.45f, 0.475f);

        [SerializeField] private LayerMask _scrapTrailGroundMask = ~0;

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

        [Min(0f)]
        [SerializeField] private float _sweepPushForce = 11.35f;

        [Min(0f)]
        [SerializeField] private float _sweepPushLift = 0.47f;

        [Min(1f)]
        [SerializeField] private float _sweepSpinSpeed = 270f;

        [Header("Throw Attack")]
        [Min(1)]
        [SerializeField] private int _throwProjectileCount = 5;

        [Min(1)]
        [SerializeField] private int _phaseTwoThrowProjectileCount = 9;

        [Range(0f, 90f)]
        [SerializeField] private float _throwProjectileSpreadAngle = 18f;

        [Range(0f, 180f)]
        [SerializeField] private float _phaseTwoThrowProjectileSpreadAngle = 34f;

        [Min(0.1f)]
        [SerializeField] private float _throwProjectileDamage = 6.5f;

        [Min(0.1f)]
        [SerializeField] private float _throwProjectileSpeedMult = 0.58f;

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

        [Min(1f)]
        [SerializeField] private float _cabinTurnAcceleration = 260f;

        [Min(1f)]
        [SerializeField] private float _cabinTurnDeceleration = 360f;

        [Range(1f, 90f)]
        [SerializeField] private float _cabinTurnSlowAngle = 28f;

        [Range(0.05f, 1f)]
        [SerializeField] private float _cabinTurnMinSpeedFactor = 0.18f;

        [Range(0.1f, 3f)]
        [SerializeField] private float _cabinPhaseTwoMult = 0.82f;

        [Range(0.1f, 3f)]
        [SerializeField] private float _cabinPhaseThreeMult = 1.8f;

        [Header("Arm")]
        [SerializeField] private Vector3 _armDefaultBoomEuler = new Vector3(0f, 0f, -35f);

        [SerializeField] private Vector3 _armDefaultStickEuler = new Vector3(0f, 0f, 70f);

        [SerializeField] private Vector3 _armDefaultBucketEuler = new Vector3(0f, 0f, -25f);

        [SerializeField] private Vector3 _armBucketPrepareBoomEuler = new Vector3(0f, 0f, -62f);

        [SerializeField] private Vector3 _armBucketPrepareStickEuler = new Vector3(0f, 0f, 110f);

        [SerializeField] private Vector3 _armBucketPrepareBucketEuler = new Vector3(0f, 0f, -56f);

        [SerializeField] private Vector3 _armBucketStrikeBoomEuler = new Vector3(0f, 0f, 12f);

        [SerializeField] private Vector3 _armBucketStrikeStickEuler = new Vector3(0f, 0f, 18f);

        [SerializeField] private Vector3 _armBucketStrikeBucketEuler = new Vector3(0f, 0f, 12f);

        [SerializeField] private Vector3 _armSweepBoomEuler = new Vector3(0f, 0f, 6f);

        [SerializeField] private Vector3 _armSweepStickEuler = new Vector3(0f, 0f, 14f);

        [SerializeField] private Vector3 _armSweepBucketEuler = new Vector3(0f, 0f, 12f);

        [SerializeField] private Vector3 _armGrabScrapBoomEuler = new Vector3(0f, 0f, 36f);

        [SerializeField] private Vector3 _armGrabScrapStickEuler = new Vector3(0f, 0f, 92f);

        [SerializeField] private Vector3 _armGrabScrapBucketEuler = new Vector3(0f, 0f, -62f);

        [SerializeField] private Vector3 _armThrowScrapBoomEuler = new Vector3(0f, 0f, -18f);

        [SerializeField] private Vector3 _armThrowScrapStickEuler = new Vector3(0f, 0f, 12f);

        [SerializeField] private Vector3 _armThrowScrapBucketEuler = new Vector3(0f, 0f, 42f);

        [SerializeField] private Vector3 _armTrailScrapeBoomEuler = new Vector3(0f, 0f, 18f);

        [SerializeField] private Vector3 _armTrailScrapeStickEuler = new Vector3(0f, 0f, 36f);

        [SerializeField] private Vector3 _armTrailScrapeBucketEuler = new Vector3(0f, 0f, -42f);

        [SerializeField] private Vector3 _armChargeBraceBoomEuler = new Vector3(0f, 0f, -38f);

        [SerializeField] private Vector3 _armChargeBraceStickEuler = new Vector3(0f, 0f, 102f);

        [SerializeField] private Vector3 _armChargeBraceBucketEuler = new Vector3(0f, 0f, -34f);

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

        [Min(1f)]
        [SerializeField] private float _armTurnAcceleration = 220f;

        [Min(1f)]
        [SerializeField] private float _armTurnDeceleration = 320f;

        [Range(1f, 90f)]
        [SerializeField] private float _armTurnSlowAngle = 18f;

        [Range(0.05f, 1f)]
        [SerializeField] private float _armTurnMinSpeedFactor = 0.16f;

    }
}
