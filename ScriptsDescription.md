# Описание скриптов Assets/Scripts

Всего .cs файлов: 385

## Assets/Scripts\Animators\Character\AnimatorSwitcher.cs
- Типы: class AnimatorSwitcher : MonoBehaviour
- SerializeField: _animator: Animator; _baseController: RuntimeAnimatorController; _weaponAnimators: List<WeaponAnimatorEntry>; _defaultWeaponType: WeaponType; _weaponLayerIndex: int; _switchBlendTime: float; _battleLayerWeight: float
- Публичный API: class AnimatorSwitcher : MonoBehaviour {; bool IsBattleMode {; void SetBattleMode(bool isBattleMode); void SetBattleModeInstant(bool isBattleMode); void SetWeaponType(WeaponType weaponType); void SetWeaponTypeInstant(WeaponType weaponType)

## Assets/Scripts\Animators\Character\PlayerAnimator.cs
- Типы: class PlayerAnimator : MonoBehaviour
- SerializeField: _animator: Animator; _moveLerpSpeed: float; _walkMoveValue: float; _runMoveValue: float; _moveDirectionDeadZone: float; _walkStepInterval: float; _runStepInterval: float; _attackVariantsCount: int
- Публичный API: class PlayerAnimator : MonoBehaviour {; void SetMoveState(bool isMoving); void SetSprintState(bool isSprinting); void SetStepSoundAllowed(bool isAllowed); void SetWorldMoveDirection(Vector3 worldMoveDirection); void TriggerJump(); void TriggerPoint(); void TriggerAttack(); void TriggerTakeDamage(); void SetFight(bool isFight); void SetController(RuntimeAnimatorController controller); void SetLayerWeight(int layerIndex, float layerWeight)

## Assets/Scripts\Animators\Character\StepAnimator.cs
- Типы: class StepAnimator
- SerializeField: нет
- Публичный API: class StepAnimator {; void UpdateStepFromMoveDirection( bool isMoving, bool isSprinting, bool isStepSoundAllowed, Vector3 worldMoveDirection, float deltaTime); void StopStep()

## Assets/Scripts\Animators\Character\StepDirection.cs
- Типы: enum StepDirection
- SerializeField: нет
- Публичный API: enum StepDirection {

## Assets/Scripts\Animators\Character\WeaponAnimatorEntry.cs
- Типы: class WeaponAnimatorEntry
- SerializeField: нет
- Публичный API: class WeaponAnimatorEntry {

## Assets/Scripts\Animators\Character\WeaponType.cs
- Типы: enum WeaponType
- SerializeField: нет
- Публичный API: enum WeaponType {

## Assets/Scripts\Animators\Cursor\CursorAnimator.cs
- Типы: class CursorAnimator : MonoBehaviour
- SerializeField: _visualRectTransform: RectTransform; _useUnscaledTime: bool; _clickDownScale: float; _clickDownDuration: float; _clickOvershootScale: float; _clickOvershootDuration: float; _clickReturnDuration: float; _clickEaseDown: Ease; _clickEaseOvershoot: Ease; _clickEaseReturn: Ease; _holdScale: float; _holdReleaseDuration: float; _holdJitterPositionStrength: float; _holdJitterRotationStrength: float; _holdJitterCycleDuration: float; _holdJitterVibrato: int; _holdJitterRandomness: float; _secondaryClickRotationPunch: float; _secondaryClickDuration: float; _scrollScalePunch: float; _scrollDuration: float
- Публичный API: class CursorAnimator : MonoBehaviour {; void PlayClick(); void BeginHoldCandidate(float thresholdSeconds); void CancelHoldCandidate(); void ConfirmHold(); void EndHold(); void ResetToBase(); void PlaySecondaryClick(); void PlayScroll(float direction)

## Assets/Scripts\Animators\Effects\DamageShakeAnimator.cs
- Типы: class DamageShakeAnimator : MonoBehaviour
- SerializeField: _targetTransform: Transform; _shakePositionStrength: float; _shakePositionVibrato: int; _shakePositionDuration: float; _shakeRotationStrength: float; _shakeRotationVibrato: int; _shakeRotationDuration: float
- Публичный API: class DamageShakeAnimator : MonoBehaviour {; void Shake()

## Assets/Scripts\Animators\Effects\PickupAnimator.cs
- Типы: class PickupAnimator : MonoBehaviour
- SerializeField: _moveSpeed: float; _stopDistance: float; _minDuration: float; _arcHeight: float
- Публичный API: class PickupAnimator : MonoBehaviour {; void PlayAttraction(Transform target, Action onArrived)

## Assets/Scripts\Animators\Effects\PooledParticleEffect.cs
- Типы: class ParticleEffect : MonoBehaviour
- SerializeField: _particleSystem: ParticleSystem
- Публичный API: class ParticleEffect : MonoBehaviour {; void Initialize(ParticleEffectSpawner particleEffectSpawner); void Play(); void StopAndClear()

## Assets/Scripts\Animators\Feedbacks\ColorEmissionFeedback.cs
- Типы: class ColorEmissionFeedback : Feedback
- SerializeField: _renderer: Renderer; _flashColor: Color; _flashDuration: float; _returnDuration: float; _intensity: float
- Публичный API: class ColorEmissionFeedback : Feedback {; void Initialize(Renderer renderer); void Play(); void Stop()

## Assets/Scripts\Animators\Feedbacks\ColorFlashFeedback.cs
- Типы: class ColorFlashFeedback : Feedback
- SerializeField: _renderer: Renderer; _flashColor: Color; _flashDuration: float; _returnDuration: float
- Публичный API: class ColorFlashFeedback : Feedback {; void Play(); void Stop()

## Assets/Scripts\Animators\Feedbacks\ColorFlashImageFeedback.cs
- Типы: class ColorFlashImageFeedback : Feedback
- SerializeField: _image: Image; _flashColor: Color; _flashDuration: float; _returnDuration: float
- Публичный API: class ColorFlashImageFeedback : Feedback {; void Play(); void Stop()

## Assets/Scripts\Animators\Feedbacks\Feedback.cs
- Типы: class Feedback : MonoBehaviour
- SerializeField: нет
- Публичный API: class Feedback : MonoBehaviour {; bool IsPlaying =>; void Play(); void Stop()

## Assets/Scripts\Animators\Feedbacks\FeedbackGroup.cs
- Типы: class FeedbackGroup : MonoBehaviour
- SerializeField: _feedbacks: Feedback[]
- Публичный API: class FeedbackGroup : MonoBehaviour {; bool IsPlaying {; void Initialize(Feedback[] feedbacks); void Play(); void Stop()

## Assets/Scripts\Animators\Feedbacks\HealthFeedbackTrigger.cs
- Типы: class HealthFeedbackTrigger : MonoBehaviour
- SerializeField: _health: Health; _feedbackGroup: FeedbackGroup
- Публичный API: class HealthFeedbackTrigger : MonoBehaviour {; void Initialize(Health health, FeedbackGroup feedbackGroup)

## Assets/Scripts\Animators\Feedbacks\ShakeFeedback.cs
- Типы: class ShakeFeedback : Feedback
- SerializeField: _rootTransform: Transform; _shakeTransform: Transform; _shakePositionStrength: float; _shakePositionVibrato: int; _shakePositionDuration: float; _shakeRotationStrength: float; _shakeRotationVibrato: int; _shakeRotationDuration: float
- Публичный API: class ShakeFeedback : Feedback {; bool IsPlaying =>; void Play(); void Stop(); void Initialize(Transform rootTransform, Transform shakeTransform); void SetStrengthMultiplier(float strengthMultiplier)

## Assets/Scripts\Animators\Highlighting\HighlightAnimator.cs
- Типы: class HighlightAnimator : HighlighterBase
- SerializeField: _highlightColor: Color; _blinkTime: float
- Публичный API: class HighlightAnimator : HighlighterBase {

## Assets/Scripts\Animators\Highlighting\HighlighterBase.cs
- Типы: class HighlighterBase : MonoBehaviour
- SerializeField: нет
- Публичный API: class HighlighterBase : MonoBehaviour {; void Highlight(bool state)

## Assets/Scripts\Animators\Highlighting\HighlightLayerSwitcher.cs
- Типы: class HighlightLayerSwitcher : HighlighterBase
- SerializeField: _highlightLayer: RenderingLayerMask
- Публичный API: class HighlightLayerSwitcher : HighlighterBase {

## Assets/Scripts\Animators\Highlighting\InventorySlotHighlight.cs
- Типы: class InventorySlotHighlight : MonoBehaviour
- SerializeField: _targetTransform: RectTransform; _activeScale: float; _duration: float
- Публичный API: class InventorySlotHighlight : MonoBehaviour {; void SetActive(bool isActive)

## Assets/Scripts\Audio\AmmoImpactAudio.cs
- Типы: class AmmoImpactAudio : AmmoLifeListener
- SerializeField: _audioSource: AudioSource; _impactClips: AudioClip[]; _targetImpactClips: AudioClip[]
- Публичный API: class AmmoImpactAudio : AmmoLifeListener {; bool IsLifeEndComplete {

## Assets/Scripts\Audio\AudioOneShotCategory.cs
- Типы: enum AudioOneShotCategory
- SerializeField: нет
- Публичный API: enum AudioOneShotCategory {

## Assets/Scripts\Audio\AudioOneShotGate.cs
- Типы: class AudioOneShotGate
- SerializeField: нет
- Публичный API: class AudioOneShotGate {; bool TryPlay( AudioSource audioSource, AudioClip clip, float volumeScale, AudioOneShotCategory category)

## Assets/Scripts\Audio\BossExcavatorAudio.cs
- Типы: class BossExcavatorAudio : MonoBehaviour
- SerializeField: _boss: BossExcavator; _oneShotSource: AudioSource; _loopSource: AudioSource; _engineLoopClip: AudioClip; _bucketPrepareClip: AudioClip; _bucketImpactClip: AudioClip; _sweepClip: AudioClip; _throwPrepareClip: AudioClip; _throwReleaseClip: AudioClip; _chargePrepareClip: AudioClip; _chargeDashClip: AudioClip
- Публичный API: class BossExcavatorAudio : MonoBehaviour {

## Assets/Scripts\Audio\CombatMusicAudio.cs
- Типы: class CombatMusicAudio : MonoBehaviour
- SerializeField: _audioSource: AudioSource
- Публичный API: class CombatMusicAudio : MonoBehaviour {

## Assets/Scripts\Audio\FootstepAudio.cs
- Типы: class FootstepAudio : MonoBehaviour
- SerializeField: _audioSource: AudioSource; _animator: PlayerAnimator; _profile: FootstepAudioProfile
- Публичный API: class FootstepAudio : MonoBehaviour {

## Assets/Scripts\Audio\FootstepAudioProfile.cs
- Типы: class FootstepAudioProfile : ScriptableObject
- SerializeField: _walkClips: AudioClip[]; _runClips: AudioClip[]
- Публичный API: class FootstepAudioProfile : ScriptableObject {; bool HasWalkClips(); AudioClip GetClip(bool isRunning)

## Assets/Scripts\Audio\HealthAudio.cs
- Типы: class HealthAudio : MonoBehaviour
- SerializeField: _audioSource: AudioSource; _health: Health; _damagedClips: AudioClip[]; _endedClips: AudioClip[]
- Публичный API: class HealthAudio : MonoBehaviour {

## Assets/Scripts\Audio\MeleeAttackAudio.cs
- Типы: class MeleeAttackAudio : MonoBehaviour
- SerializeField: _audioSource: AudioSource; _attacker: Attacker; _weaponHolder: WeaponHolder; _profile: MeleeAttackAudioProfile
- Публичный API: class MeleeAttackAudio : MonoBehaviour {

## Assets/Scripts\Audio\MeleeAttackAudioProfile.cs
- Типы: class MeleeAttackAudioProfile : ScriptableObject
- SerializeField: _fistClips: AudioClip[]; _batonClips: AudioClip[]
- Публичный API: class MeleeAttackAudioProfile : ScriptableObject {; bool HasClips(); AudioClip GetClip(Item item)

## Assets/Scripts\Audio\PickupAudio.cs
- Типы: class PickupAudio : MonoBehaviour
- SerializeField: _audioSource: AudioSource; _pickup: BasePickup; _clips: AudioClip[]; _category: AudioOneShotCategory
- Публичный API: class PickupAudio : MonoBehaviour {

## Assets/Scripts\Audio\RangedFireAudio.cs
- Типы: class RangedFireAudio : MonoBehaviour
- SerializeField: _audioSource: AudioSource; _fireExecutor: FireExecutor; _turret: Turret; _droneBrain: EnemyDroneBrain; _clip: AudioClip
- Публичный API: class RangedFireAudio : MonoBehaviour {

## Assets/Scripts\Audio\UIButtonAudio.cs
- Типы: class UIButtonAudio : MonoBehaviour
- SerializeField: _button: Button; _audioSource: AudioSource; _clickClip: AudioClip
- Публичный API: class UIButtonAudio : MonoBehaviour {

## Assets/Scripts\Audio\WeaponHolderAudio.cs
- Типы: class WeaponHolderAudio : MonoBehaviour
- SerializeField: _audioSource: AudioSource; _weaponHolder: WeaponHolder; _equipClips: AudioClip[]
- Публичный API: class WeaponHolderAudio : MonoBehaviour {

## Assets/Scripts\Characters\AimRotationSolver.cs
- Типы: class AimRotationSolver
- SerializeField: нет
- Публичный API: class AimRotationSolver {; bool TryGetRotation(Vector3 originPoint, Vector3 upAxis, Vector3 aimPoint, out Quaternion targetRotation)

## Assets/Scripts\Characters\Attack\AttackData.cs
- Типы: class AttackData : ScriptableObject
- SerializeField: _attackRange: float; _minDamage: int; _maxDamage: int; _attackCooldown: float; _isMultiHit: bool; _hitLayers: LayerMask; _attackShape: AttackShapeBase
- Публичный API: class AttackData : ScriptableObject {; float AttackRange =>; int MinDamage =>; int MaxDamage =>; float AttackCooldown =>; bool IsMultiHit =>; LayerMask HitLayers =>; AttackShapeBase AttackShape =>; int GetDamage()

## Assets/Scripts\Characters\Attack\Attacker.cs
- Типы: class Attacker : MonoBehaviour
- SerializeField: _attackData: AttackData; _weaponHolder: WeaponHolder; _hitForce: float; _hitForceMode: ForceMode; _isGizmoVisible: bool; _gizmoPointSize: float
- Публичный API: class Attacker : MonoBehaviour {; AttackData AttackData =>; bool CanStartAttack(); bool PerformAttack(); bool PerformAttack(WeaponModifierContext weaponModifierContext); bool PerformAttack(Vector3 attackDirection); bool PerformAttack(Vector3 attackDirection, WeaponModifierContext weaponModifierContext); bool CanHitTarget(Transform targetTransform, Vector3 attackDirection)

## Assets/Scripts\Characters\Attack\AttackShapeBase.cs
- Типы: class AttackShapeBase : ScriptableObject
- SerializeField: нет
- Публичный API: class AttackShapeBase : ScriptableObject {; int GetTargets(Transform originTransform, float range, LayerMask hitLayers, Collider[] resultBuffer); void DrawGizmos(Transform originTransform, float range); int GetTargets(Vector3 originPoint, Vector3 attackDirection, float range, LayerMask hitLayers, Collider[] resultBuffer); void DrawGizmos(Vector3 originPoint, Vector3 attackDirection, float range)

## Assets/Scripts\Characters\Attack\SphereForwardAttackShape.cs
- Типы: class SphereForwardAttackShape : AttackShapeBase
- SerializeField: _hitRadiusFactor: float; _forwardOffsetFactor: float
- Публичный API: class SphereForwardAttackShape : AttackShapeBase {; int GetTargets(Vector3 originPoint, Vector3 attackDirection, float range, LayerMask hitLayers, Collider[] resultBuffer); void DrawGizmos(Vector3 originPoint, Vector3 attackDirection, float range)

## Assets/Scripts\Characters\Boss\AI\BossExcavatorBrain.Attack.cs
- Типы: class BossExcavatorBrain
- SerializeField: нет
- Публичный API: partial class BossExcavatorBrain {

## Assets/Scripts\Characters\Boss\AI\BossExcavatorBrain.AttackQueue.cs
- Типы: class BossExcavatorBrain
- SerializeField: нет
- Публичный API: partial class BossExcavatorBrain {

## Assets/Scripts\Characters\Boss\AI\BossExcavatorBrain.AttackRules.cs
- Типы: class BossExcavatorBrain
- SerializeField: нет
- Публичный API: partial class BossExcavatorBrain {

## Assets/Scripts\Characters\Boss\AI\BossExcavatorBrain.AttackSelection.cs
- Типы: class BossExcavatorBrain
- SerializeField: нет
- Публичный API: partial class BossExcavatorBrain {

## Assets/Scripts\Characters\Boss\AI\BossExcavatorBrain.cs
- Типы: class BossExcavatorBrain
- SerializeField: нет
- Публичный API: partial class BossExcavatorBrain {; BossExcavatorAttack CurrentAttack =>; BossExcavatorAttack TargetAttack =>; void Reset(); void Tick(); void FixedTick()

## Assets/Scripts\Characters\Boss\AI\BossExcavatorBrain.Stall.cs
- Типы: class BossExcavatorBrain
- SerializeField: нет
- Публичный API: partial class BossExcavatorBrain {

## Assets/Scripts\Characters\Boss\AI\BossExcavatorBrain.State.cs
- Типы: class BossExcavatorBrain
- SerializeField: нет
- Публичный API: partial class BossExcavatorBrain {

## Assets/Scripts\Characters\Boss\Attacks\BossExcavatorBucketAttack.Combat.cs
- Типы: class BossExcavatorBucketAttack
- SerializeField: нет
- Публичный API: partial class BossExcavatorBucketAttack {

## Assets/Scripts\Characters\Boss\Attacks\BossExcavatorBucketAttack.cs
- Типы: class BossExcavatorBucketAttack
- SerializeField: нет
- Публичный API: partial class BossExcavatorBucketAttack {; bool IsRunning =>; float Duration =>; void Reset(); void StartAttack(); bool Tick(); void Cancel(bool restoreNeutralPose)

## Assets/Scripts\Characters\Boss\Attacks\BossExcavatorChargeAttack.Combat.cs
- Типы: class BossExcavatorChargeAttack
- SerializeField: нет
- Публичный API: partial class BossExcavatorChargeAttack {

## Assets/Scripts\Characters\Boss\Attacks\BossExcavatorChargeAttack.cs
- Типы: class BossExcavatorChargeAttack
- SerializeField: нет
- Публичный API: partial class BossExcavatorChargeAttack {; bool IsRunning =>; void Reset(); void StartAttack(bool isComboSweep); bool Tick(); void FixedTick(); void Cancel(bool restoreNeutralPose)

## Assets/Scripts\Characters\Boss\Attacks\BossExcavatorScrapTrailAttack.cs
- Типы: class BossExcavatorScrapTrailAttack
- SerializeField: нет
- Публичный API: class BossExcavatorScrapTrailAttack {; bool IsRunning =>; void Reset(); void StartAttack(); bool Tick(); void Cancel(bool restoreNeutralPose)

## Assets/Scripts\Characters\Boss\Attacks\BossExcavatorSweepAttack.cs
- Типы: class BossExcavatorSweepAttack
- SerializeField: нет
- Публичный API: class BossExcavatorSweepAttack {; bool IsRunning =>; void Reset(); void StartAttack(); bool Tick(); void Cancel(bool restoreNeutralPose)

## Assets/Scripts\Characters\Boss\Attacks\BossExcavatorThrowAttack.cs
- Типы: class BossExcavatorThrowAttack
- SerializeField: нет
- Публичный API: class BossExcavatorThrowAttack {; bool IsRunning =>; void Reset(); void StartAttack(); bool Tick(); void Cancel(bool restoreNeutralPose)

## Assets/Scripts\Characters\Boss\Components\BossExcavatorAim.cs
- Типы: class BossExcavatorAim : MonoBehaviour
- SerializeField: нет
- Публичный API: class BossExcavatorAim : MonoBehaviour {; bool IsLocked =>; void Setup(BossExcavator boss, BossExcavatorConfig config, Transform pivot, Transform target); void SetTarget(Transform target); void SetLocked(bool isLocked); void Tick()

## Assets/Scripts\Characters\Boss\Components\BossExcavatorArm.cs
- Типы: class BossExcavatorArm : MonoBehaviour
- SerializeField: нет
- Публичный API: class BossExcavatorArm : MonoBehaviour {; bool IsLocked =>; BossExcavatorArmPose CurrentPose =>; void Setup(BossExcavator boss, BossExcavatorConfig config, Transform boom, Transform stick, Transform bucket); void Tick(); void SetLocked(bool isLocked); void SetDefaultPose(); void SetDefaultPoseImmediate(); void SetNeutralPose(); void SetNeutralPoseImmediate(); void SetBucketPreparePose(); void SetBucketStrikePose()

## Assets/Scripts\Characters\Boss\Components\BossExcavatorMove.Agent.cs
- Типы: class BossExcavatorMove
- SerializeField: нет
- Публичный API: partial class BossExcavatorMove {; void InvalidatePath()

## Assets/Scripts\Characters\Boss\Components\BossExcavatorMove.Collision.cs
- Типы: class BossExcavatorMove
- SerializeField: нет
- Публичный API: partial class BossExcavatorMove {

## Assets/Scripts\Characters\Boss\Components\BossExcavatorMove.cs
- Типы: class BossExcavatorMove : MonoBehaviour
- SerializeField: _arenaCenter: Transform; _obstacleMask: LayerMask; _navMeshAgent: NavMeshAgent
- Публичный API: partial class BossExcavatorMove : MonoBehaviour {; BossExcavatorTargetPoint TargetPoint =>; Vector3 DesiredPoint =>; float MediumDistance =>; float RetreatDistance =>; float MinMoveDistance =>; float AttackChaseDistance =>; LayerMask ObstacleMask =>; Collider[] BodyColliders =>; float CurrentPlanarSpeed =>; Vector3 CurrentMoveDirection =>; void Setup(BossExcavatorConfig config, Transform baseTransform, Rigidbody baseRigidbody, Transform target)

## Assets/Scripts\Characters\Boss\Components\BossExcavatorMove.Gizmo.cs
- Типы: class BossExcavatorMove
- SerializeField: нет
- Публичный API: partial class BossExcavatorMove {

## Assets/Scripts\Characters\Boss\Components\BossExcavatorMove.Motion.cs
- Типы: class BossExcavatorMove
- SerializeField: нет
- Публичный API: partial class BossExcavatorMove {

## Assets/Scripts\Characters\Boss\Components\BossExcavatorMove.Navigation.cs
- Типы: class BossExcavatorMove
- SerializeField: нет
- Публичный API: partial class BossExcavatorMove {

## Assets/Scripts\Characters\Boss\Components\BossExcavatorMove.Points.cs
- Типы: class BossExcavatorMove
- SerializeField: нет
- Публичный API: partial class BossExcavatorMove {

## Assets/Scripts\Characters\Boss\Components\BossExcavatorMove.Runtime.cs
- Типы: class BossExcavatorMove
- SerializeField: нет
- Публичный API: partial class BossExcavatorMove {

## Assets/Scripts\Characters\Boss\Components\BossExcavatorMove.Targeting.cs
- Типы: class BossExcavatorMove
- SerializeField: нет
- Публичный API: partial class BossExcavatorMove {

## Assets/Scripts\Characters\Boss\Core\BossExcavator.cs
- Типы: class BossExcavator : MonoBehaviour
- SerializeField: _config: BossExcavatorConfig; _move: BossExcavatorMove; _aim: BossExcavatorAim; _arm: BossExcavatorArm; _target: Transform; _base: Transform; _baseRigidbody: Rigidbody; _cabin: Transform; _boom: Transform; _stick: Transform; _bucket: Transform; _health: Health
- Публичный API: partial class BossExcavator : MonoBehaviour {; BossExcavatorConfig Config =>; BossExcavatorMove Move =>; BossExcavatorAim Aim =>; BossExcavatorArm Arm =>; Transform Target =>; Transform Base =>; Rigidbody BaseRigidbody =>; Transform Cabin =>; Transform Boom =>; Transform Stick =>; Transform Bucket =>

## Assets/Scripts\Characters\Boss\Core\BossExcavator.Facade.cs
- Типы: class BossExcavator
- SerializeField: нет
- Публичный API: partial class BossExcavator {; void SetTarget(Transform target); void SetChargeAlign(bool isChargeAlign); void SetMoveAttackIntent(BossExcavatorAttack attackIntent); void SetAimLocked(bool isLocked); void SetArmLocked(bool isLocked); void SetArmDefaultPose(); BossExcavatorArmPose GetArmPose(); void SetArmNeutralPose(); void SetArmBucketPreparePose(); void SetArmBucketStrikePose(); void SetArmGrabScrapPose()

## Assets/Scripts\Characters\Boss\Core\BossExcavator.Gizmo.cs
- Типы: class BossExcavator
- SerializeField: нет
- Публичный API: partial class BossExcavator {

## Assets/Scripts\Characters\Boss\Core\BossExcavator.Targeting.cs
- Типы: class BossExcavator
- SerializeField: нет
- Публичный API: partial class BossExcavator {

## Assets/Scripts\Characters\Boss\Core\BossExcavatorArmPose.cs
- Типы: enum BossExcavatorArmPose
- SerializeField: нет
- Публичный API: enum BossExcavatorArmPose {

## Assets/Scripts\Characters\Boss\Core\BossExcavatorAttack.cs
- Типы: enum BossExcavatorAttack
- SerializeField: нет
- Публичный API: enum BossExcavatorAttack {

## Assets/Scripts\Characters\Boss\Core\BossExcavatorAxis.cs
- Типы: enum BossExcavatorAxis
- SerializeField: нет
- Публичный API: enum BossExcavatorAxis {

## Assets/Scripts\Characters\Boss\Core\BossExcavatorConfig.cs
- Типы: class BossExcavatorConfig : ScriptableObject
- SerializeField: _maxHealth: float; _phaseTwoRatio: float; _phaseThreeRatio: float; _phaseChangeDuration: float; _phaseTwoAttackSpeedMult: float; _phaseTwoDamageMult: float; _phaseTwoChargeSpeedMult: float; _phaseTwoSweepSpinSpeedMult: float; _phaseTwoComboSweepSpinSpeedMult: float; _phaseThreeDecisionSpeedMult: float; _phaseThreeAttackSpeedMult: float; _phaseThreeDamageMult: float; _phaseThreeChargeSpeedMult: float; _phaseThreeCooldownMult: float; _phaseThreeStallRecoverTimeMult: float; _phaseThreeInitialWaveSize: int; _phaseThreeWaveSizeStep: int; _phaseThreeEnemyPrefabs: List<EnemySpawnConfig>; _startState: BossExcavatorState; _baseMoveSpeed: float; _baseTurnSpeed: float; _baseTurnAcceleration: float; _baseTurnDeceleration: float; _baseTurnSlowAngle: float; _baseTurnMinSpeedFactor: float; _moveStartAngle: float; _moveStopAngle: float; _stopDistance: float; _mediumDistance: float; _distanceTolerance: float; _distanceHysteresis: float; _retreatDistance: float; _flankAngle: float; _flankSwitchThreshold: float; _flankSwitchCooldown: float; _targetSwitchCooldown: float; _desiredPointDeadZone: float; _wallProbeDistance: float; _probeRadius: float; _probeHeight: float; _forwardProbeDistance: float; _wallEscapeDistance: float; _cornerEscapeDistance: float; _escapeCenterWeight: float; _arenaReturnDistance: float; _chargeAlignDistance: float; _wallPenalty: float; _cornerPenalty: float; _blockedPenalty: float; _attackChaseDistance: float; _repositionBaseAngle: float; _bucketMaxDistance: float; _bucketBaseAngle: float; _bucketCabinAngle: float; _sweepMaxDistance: float; _sweepCabinAngle: float; _throwMinDistance: float; _throwMaxDistance: float; _throwBaseAngle: float; _throwCabinAngle: float; _chargeMinDistance: float; _chargeMaxDistance: float; _chargeBaseAngle: float; _phaseTwoCooldownMult: float; _bucketAttackCooldown: float; _sweepAttackCooldown: float; _throwAttackCooldown: float; _chargeAttackCooldown: float; _scrapTrailCooldown: float; _bucketPrepareTime: float; _bucketStrikeTime: float; _sweepPrepareTime: float; _sweepAttackTime: float; _sweepSpinTurns: int; _throwGrabTime: float; _throwReleaseTime: float; _chargeAlignTime: float; _chargeTelegraphTime: float; _chargeAttackTime: float; _chargeRecoveryTime: float; _attackPoseSpeedMult: float; _scrapTrailMinDistance: float; _scrapTrailMaxDistance: float; _scrapTrailBaseAngle: float; _scrapTrailDuration: float; _scrapTrailMinMoveSpeed: float; _scrapTrailSpawnSpacing: float; _scrapTrailSpawnBackOffset: float; _scrapTrailGroundProbeHeight: float; _scrapTrailGroundProbeDistance: float; _scrapTrailBlockLifetime: float; _scrapTrailBlockSize: Vector3; _scrapTrailGroundMask: LayerMask; _bucketHitDamage: float; _bucketHitRadius: float; _bucketHitAngle: float; _bucketHitOffset: float; _bucketShockwaveDamage: float; _bucketShockwaveRadius: float; _bucketShockwaveOffset: float; _bucketHitMask: LayerMask; _bucketPrepareSpeedMult: float; _bucketStrikeSpeedMult: float; _bucketRecoverSpeedMult: float; _sweepHitDamage: float; _sweepDamageInterval: float; _sweepHitRadius: float; _sweepHitOffset: float; _sweepPushForce: float; _sweepPushLift: float; _sweepSpinSpeed: float; _throwProjectileCount: int; _phaseTwoThrowProjectileCount: int; _throwProjectileSpreadAngle: float; _phaseTwoThrowProjectileSpreadAngle: float; _throwProjectileDamage: float; _throwProjectileSpeedMult: float; _throwSpawnOffset: float; _throwHitMask: LayerMask; _chargeSpeed: float; _chargeHitDamage: float; _chargeHitRadius: float; _chargeHitOffset: float; _chargeHitMask: LayerMask; _attackRecoveryTime: float; _moveOrbitTime: float; _movePressureTime: float; _moveRetreatTime: float; _moveRepositionCommitTime: float; _moveChaseCommitTime: float; _cabinTurnSpeed: float; _cabinTurnAcceleration: float; _cabinTurnDeceleration: float; _cabinTurnSlowAngle: float; _cabinTurnMinSpeedFactor: float; _cabinPhaseTwoMult: float; _cabinPhaseThreeMult: float; _armDefaultBoomEuler: Vector3; _armDefaultStickEuler: Vector3; _armDefaultBucketEuler: Vector3; _armBucketPrepareBoomEuler: Vector3; _armBucketPrepareStickEuler: Vector3; _armBucketPrepareBucketEuler: Vector3; _armBucketStrikeBoomEuler: Vector3; _armBucketStrikeStickEuler: Vector3; _armBucketStrikeBucketEuler: Vector3; _armSweepBoomEuler: Vector3; _armSweepStickEuler: Vector3; _armSweepBucketEuler: Vector3; _armGrabScrapBoomEuler: Vector3; _armGrabScrapStickEuler: Vector3; _armGrabScrapBucketEuler: Vector3; _armThrowScrapBoomEuler: Vector3; _armThrowScrapStickEuler: Vector3; _armThrowScrapBucketEuler: Vector3; _armTrailScrapeBoomEuler: Vector3; _armTrailScrapeStickEuler: Vector3; _armTrailScrapeBucketEuler: Vector3; _armChargeBraceBoomEuler: Vector3; _armChargeBraceStickEuler: Vector3; _armChargeBraceBucketEuler: Vector3; _armBoomAxis: BossExcavatorAxis; _armStickAxis: BossExcavatorAxis; _armBucketAxis: BossExcavatorAxis; _armBoomAxisInvert: bool; _armStickAxisInvert: bool; _armBucketAxisInvert: bool; _armBoomSpeed: float; _armStickSpeed: float; _armBucketSpeed: float; _armTurnAcceleration: float; _armTurnDeceleration: float; _armTurnSlowAngle: float; _armTurnMinSpeedFactor: float
- Публичный API: partial class BossExcavatorConfig : ScriptableObject {

## Assets/Scripts\Characters\Boss\Core\BossExcavatorConfig.Properties.cs
- Типы: class BossExcavatorConfig
- SerializeField: нет
- Публичный API: partial class BossExcavatorConfig {; float MaxHealth =>; float PhaseTwoRatio =>; float PhaseThreeRatio =>; float PhaseChangeDuration =>; float PhaseThreeDecisionSpeedMult =>; float PhaseTwoAttackSpeedMult =>; float PhaseTwoDamageMult =>; float PhaseTwoChargeSpeedMult =>; float PhaseTwoSweepSpinSpeedMult =>; float PhaseTwoComboSweepSpinSpeedMult =>; float PhaseThreeAttackSpeedMult =>

## Assets/Scripts\Characters\Boss\Core\BossExcavatorDebugRuntime.cs
- Типы: class BossExcavatorDebugRuntime : MonoBehaviour
- SerializeField: _boss: BossExcavator; _currentState: BossExcavatorState; _currentPhase: BossExcavatorPhase; _currentAttack: BossExcavatorAttack; _targetAttack: BossExcavatorAttack; _currentArmPose: BossExcavatorArmPose; _targetPoint: BossExcavatorTargetPoint; _targetDistance: float; _baseAngleToTarget: float; _cabinAngleToTarget: float; _currentMoveSpeed: float; _state: BossExcavatorState; _chargeAlign: bool; _aimLocked: bool; _armLocked: bool; _selectedArmPose: BossExcavatorArmPose; _applyNow: bool; _resetNow: bool; _completePhaseNow: bool; _applyArmPoseNow: bool; _copyCurrentArmPoseNow: bool
- Публичный API: class BossExcavatorDebugRuntime : MonoBehaviour {

## Assets/Scripts\Characters\Boss\Core\BossExcavatorMotionProfile.cs
- Типы: class BossExcavatorMotionProfile
- SerializeField: нет
- Публичный API: class BossExcavatorMotionProfile {; Quaternion StepRotation( Quaternion currentRotation, Quaternion targetRotation, ref float currentSpeed, float maxSpeed, float acceleration, float deceleration, float slowAngle, float minSpeedFactor, float deltaTime); float EstimateTravelTime( Quaternion currentRotation, Quaternion targetRotation, float currentSpeed, float maxSpeed, float acceleration, float deceleration, float slowAngle, float minSpeedFactor)

## Assets/Scripts\Characters\Boss\Core\BossExcavatorPhase.cs
- Типы: enum BossExcavatorPhase
- SerializeField: нет
- Публичный API: enum BossExcavatorPhase {

## Assets/Scripts\Characters\Boss\Core\BossExcavatorState.cs
- Типы: enum BossExcavatorState
- SerializeField: нет
- Публичный API: enum BossExcavatorState {

## Assets/Scripts\Characters\Boss\Core\BossExcavatorStateMachine.cs
- Типы: class BossExcavatorStateMachine
- SerializeField: нет
- Публичный API: class BossExcavatorStateMachine {; void Reset(); void RequestState(BossExcavatorState state); void RequestAutoState(BossExcavatorState state); void CompletePhaseChange(); void Tick()

## Assets/Scripts\Characters\Boss\Core\BossExcavatorTargetPoint.cs
- Типы: enum BossExcavatorTargetPoint
- SerializeField: нет
- Публичный API: enum BossExcavatorTargetPoint {

## Assets/Scripts\Characters\Boss\PhaseThree\BossExcavatorPhaseThreeController.cs
- Типы: class BossExcavatorPhaseThreeController
- SerializeField: нет
- Публичный API: class BossExcavatorPhaseThreeController {; void Reset(); void Tick()

## Assets/Scripts\Characters\Boss\PhaseThree\BossExcavatorPhaseThreeMinion.cs
- Типы: class BossExcavatorPhaseThreeMinion
- SerializeField: нет
- Публичный API: class BossExcavatorPhaseThreeMinion {; BossRoomEnemySpawnPoint SpawnPoint =>; GameObject RootObject =>; bool IsAlive {; void Alert(Vector3 point); void Kill(); void Dispose()

## Assets/Scripts\Characters\Boss\PhaseThree\BossRoomEnemySpawnPoint.cs
- Типы: class BossRoomEnemySpawnPoint : MonoBehaviour
- SerializeField: _riseDuration: float; _riseDepthInBlocks: float; _spawnHeightOffsetInBlocks: float
- Публичный API: class BossRoomEnemySpawnPoint : MonoBehaviour {; bool IsBusy {; void SetBlockSize(float blockSize); BossExcavatorPhaseThreeMinion TrySpawn(EnemySpawnConfig enemySpawn, Transform spawnRoot, RoomRuntimeState roomRuntimeState); void ForceClear()

## Assets/Scripts\Characters\Boss\Scrap\BossScrapCubeProjectile.cs
- Типы: class BossScrapCubeProjectile : Ammo
- SerializeField: нет
- Публичный API: class BossScrapCubeProjectile : Ammo {

## Assets/Scripts\Characters\Boss\Scrap\BossScrapCubeSpawner.cs
- Типы: class BossScrapCubeSpawner : Spawner<BossScrapCubeProjectile>, IAmmoSpawner
- SerializeField: нет
- Публичный API: class BossScrapCubeSpawner : Spawner<BossScrapCubeProjectile>, IAmmoSpawner {; BossScrapCubeProjectile Spawn( Vector3 position, Vector3 moveDirection, float damage, float speedMultiplier, LayerMask hitMask, Transform ignoredRoot); BossScrapCubeProjectile Spawn(Vector3 position); void Despawn(BossScrapCubeProjectile projectile)

## Assets/Scripts\Characters\Boss\Scrap\BossScrapTrailBlock.cs
- Типы: class BossScrapTrailBlock : MonoBehaviour
- SerializeField: _boxCollider: BoxCollider
- Публичный API: class BossScrapTrailBlock : MonoBehaviour {; void BindSpawner(BossScrapTrailBlockSpawner spawner); void Activate(Vector3 position, Quaternion rotation, Vector3 size, float lifetime, Collider[] ignoredColliders)

## Assets/Scripts\Characters\Boss\Scrap\BossScrapTrailBlockSpawner.cs
- Типы: class BossScrapTrailBlockSpawner : Spawner<BossScrapTrailBlock>
- SerializeField: нет
- Публичный API: BossScrapTrailBlock Spawn(Vector3 position, Quaternion rotation, Vector3 size, float lifetime, Collider[] ignoredColliders); BossScrapTrailBlock Spawn(Vector3 position); void Despawn(BossScrapTrailBlock block)

## Assets/Scripts\Characters\CharacterDied.cs
- Типы: class CharacterDied : MonoBehaviour
- SerializeField: _mainCollider: Collider; _mainRigidbody: Rigidbody; _animator: Animator; _rigidbodies: List<Rigidbody>; _colliders: List<Collider>
- Публичный API: void EnableRegdoll(); void DisableRegdoll(); void GetRigidbodiesAndColliders()

## Assets/Scripts\Characters\CharacterEffects.cs
- Типы: class CharacterEffects : MonoBehaviour
- SerializeField: _health: Health; _stamina: Stamina
- Публичный API: class CharacterEffects : MonoBehaviour {; void Heal(float amount); void RestoreStamina(float amount)

## Assets/Scripts\Characters\Enemy\Core\CurrencyDropOnDeath.cs
- Типы: class CurrencyDropOnDeath : MonoBehaviour
- SerializeField: _health: Health; _pickupPrefab: CurrencyPickup; _dropCountMin: int; _dropCountMax: int; _amountPerCoin: int; _spawnOffset: Vector3; _forceXMin: float; _forceXMax: float; _forceYMin: float; _forceYMax: float; _forceZMin: float; _forceZMax: float
- Публичный API: class CurrencyDropOnDeath : MonoBehaviour {

## Assets/Scripts\Characters\Enemy\Core\Enemy.cs
- Типы: class Enemy : MonoBehaviour
- SerializeField: _health: Health; _characterDied: CharacterDied
- Публичный API: class Enemy : MonoBehaviour {; bool IsDead {; Health Health =>

## Assets/Scripts\Characters\Enemy\Core\EnemyAimCollider.cs
- Типы: class EnemyAimCollider : MonoBehaviour
- SerializeField: _collider: Collider; _enemy: Enemy; _turret: Turret
- Публичный API: class EnemyAimCollider : MonoBehaviour {; Vector3 AimPoint =>

## Assets/Scripts\Characters\Enemy\Core\EnemyAlertPulse.cs
- Типы: class EnemyAlertPulse : MonoBehaviour
- SerializeField: нет
- Публичный API: class EnemyAlertPulse : MonoBehaviour {; void Setup(float moveSpeed, float size, float trailTime, float trailWidth, Color color); void Play(Vector3 startPoint, Transform target)

## Assets/Scripts\Characters\Enemy\Core\EnemyAnimation.cs
- Типы: class EnemyAnimation : MonoBehaviour
- SerializeField: _animator: PlayerAnimator; _animatorSwitcher: AnimatorSwitcher; _enemyMove: EnemyMove; _enemyBrain: MonoBehaviour; _health: Health; _enemy: Enemy; _weaponHolder: WeaponHolder; _weaponPrefab: BasePickup; _weaponType: WeaponType
- Публичный API: class EnemyAnimation : MonoBehaviour {; void TriggerAttack(); void SetWeapon(BasePickup weaponPrefab); WeaponModifierContext BuildAttackContext()

## Assets/Scripts\Characters\Enemy\Core\EnemyDebugView.cs
- Типы: class EnemyDebugView : MonoBehaviour
- SerializeField: _brain: EnemyMeleeBrain; _enemyMove: EnemyMove; _enemyRotator: EnemyRotator; _targetVision: TargetVision; _state: EnemyState; _isTargetVisible: bool; _hasTarget: bool; _targetDistance: float; _isSafeMove: bool; _isIdleWalking: bool; _isSearchIdle: bool; _isCombatClockwise: bool; _hasLastSeenPoint: bool; _hasSearchPoint: bool; _searchStep: int; _rangeMode: int; _isAttackWindup: bool; _attackWindupTimer: float; _isAttackInProgress: bool; _isHitPending: bool; _moveAmount: float; _isRunRequested: bool; _isRunning: bool; _idleStuckTimer: float; _steeringStuckTimer: float; _steeringStatus: string; _hasNavAgent: bool; _isNavAgentEnabled: bool; _isOnNavMesh: bool; _isPathPending: bool; _hasPath: bool; _hasPathTarget: bool; _hasLastNavPoint: bool; _pathStatus: string; _pathStopDistance: float; _forwardDirection: Vector3; _moveDirection: Vector3; _steerDirection: Vector3; _attackDirection: Vector3; _hasRequestedTargetPoint: bool; _hasResolvedTargetPoint: bool; _hasLookPoint: bool; _targetPoint: Vector3; _requestedTargetPoint: Vector3; _resolvedTargetPoint: Vector3; _lookPoint: Vector3; _pathTargetPoint: Vector3; _lastSeenPoint: Vector3; _searchTargetPoint: Vector3; _idleTargetPoint: Vector3; _idleLookPoint: Vector3; _lastNavPoint: Vector3
- Публичный API: class EnemyDebugView : MonoBehaviour {; void RefreshDebugData(); string BuildDebugSnapshot()

## Assets/Scripts\Characters\Enemy\Core\EnemyMove.cs
- Типы: class EnemyMove : MonoBehaviour
- SerializeField: _characterMover: CharacterMover; _steerSpeed: float; _moveScale: float; _runScaleFactor: float; _moveGainSpeed: float; _moveDropSpeed: float
- Публичный API: class EnemyMove : MonoBehaviour {; Vector3 MoveDirection =>; float MoveAmount =>; bool IsRunning =>; bool IsRunRequested =>; bool IsWallBlocked =>; void SetDirection(Vector3 moveDirection); void SetRun(bool isRunning); void SetSpeedScale(float speedScale); void StopMove(); void ForceStop()

## Assets/Scripts\Characters\Enemy\Core\EnemyPatrolPicker.cs
- Типы: class EnemyPatrolPicker
- SerializeField: нет
- Публичный API: class EnemyPatrolPicker {; void Clear(); bool TryPickPoint(EnemyRoomLock enemyRoomLock, Vector3 currentPoint, Vector3 forwardDirection, Vector3 fallbackPoint, float height, float minDistance, int maxTryCount, Func<int> getFallbackDirection, out Vector3 patrolPoint); bool TryPickNextPoint(EnemyRoomLock enemyRoomLock, Vector3 currentPoint, Vector3 forwardDirection, float height, int maxTryCount, Func<int> getFallbackDirection, Func<Vector3, bool> isPointAccepted)

## Assets/Scripts\Characters\Enemy\Core\EnemyRotator.cs
- Типы: class EnemyRotator : MonoBehaviour
- SerializeField: _rotationRoot: Transform; _rigidbody: Rigidbody; _rotationSpeed: float
- Публичный API: class EnemyRotator : MonoBehaviour {; Vector3 ForwardDirection {; void RotateToPoint(Vector3 targetPoint); void RotateToDirection(Vector3 direction); void RotateToDirection(Vector3 direction, float deltaTime); void SnapToDirection(Vector3 direction)

## Assets/Scripts\Characters\Enemy\Core\EnemyState.cs
- Типы: enum EnemyState
- SerializeField: нет
- Публичный API: enum EnemyState {

## Assets/Scripts\Characters\Enemy\Core\IEnemyAlert.cs
- Типы: interface IEnemyAlert
- SerializeField: нет
- Публичный API: interface IEnemyAlert {

## Assets/Scripts\Characters\Enemy\Core\IEnemyBrain.cs
- Типы: interface IEnemyBrain
- SerializeField: нет
- Публичный API: interface IEnemyBrain {

## Assets/Scripts\Characters\Enemy\Drone\EnemyDroneBrain.cs
- Типы: class EnemyDroneBrain : MonoBehaviour, IEnemyBrain, IEnemyAlert
- SerializeField: _enemy: Enemy; _targetVision: TargetVision; _enemyMove: EnemyDroneMove; _enemyCrash: EnemyDroneCrash; _targetRotator: TargetRotator; _idleRotator: IdleRotator; _fireExecutor: FireExecutor; _fightMinDistance: float; _fightMaxDistance: float; _pursuitDistance: float; _fireDistance: float; _strafeDistance: float; _strafeTimeMin: float; _strafeTimeMax: float; _strafeWaitMin: float; _strafeWaitMax: float; _searchTime: float; _searchReachDistance: float; _idleRadius: float; _idleWaitMin: float; _idleWaitMax: float; _idleReachDistance: float
- Публичный API: class EnemyDroneBrain : MonoBehaviour, IEnemyBrain, IEnemyAlert {; EnemyState State =>; bool ApplyAlert(Vector3 point)

## Assets/Scripts\Characters\Enemy\Drone\EnemyDroneCrash.cs
- Типы: class EnemyDroneCrash : MonoBehaviour
- SerializeField: _moveRoot: Transform; _rigidbody: Rigidbody; _force: float; _up: float; _back: float; _down: float
- Публичный API: class EnemyDroneCrash : MonoBehaviour {; void Crash(Vector3 moveVelocity, Vector3 moveDirection)

## Assets/Scripts\Characters\Enemy\Drone\EnemyDroneMove.cs
- Типы: class EnemyDroneMove : MonoBehaviour
- SerializeField: _moveRoot: Transform; _rigidbody: Rigidbody; _moveSpeed: float; _heightSpeed: float; _turnSpeed: float; _moveGainSpeed: float; _moveDropSpeed: float; _moveReachDistance: float; _slowDistance: float; _verticalSmoothTime: float; _hoverAmplitude: float; _hoverSpeed: float; _ceilingGap: float; _floorGap: float; _combatFloorGap: float; _heightReturnSpeed: float; _heightReturnThreshold: float; _obstacleMask: LayerMask; _probeRadius: float; _probeDistance: float; _avoidAngleStep: float; _liftMaxHeight: float; _liftStepHeight: float; _liftGainSpeed: float; _liftDropSpeed: float
- Публичный API: class EnemyDroneMove : MonoBehaviour {; Vector3 MoveVelocity =>; Vector3 ForwardDirection =>; void ResetAnchor(); Vector3 GetAnchorPoint(); void SetTargetHeight(float targetHeight); void ClearTargetHeight(); void SetMovePoint(Vector3 movePoint); void StopMove(); void ForceStop()

## Assets/Scripts\Characters\Enemy\Melee\EnemyMeleeBrain.Combat.cs
- Типы: class EnemyMeleeBrain
- SerializeField: нет
- Публичный API: partial class EnemyMeleeBrain {

## Assets/Scripts\Characters\Enemy\Melee\EnemyMeleeBrain.cs
- Типы: class EnemyMeleeBrain : MonoBehaviour, IEnemyBrain, IEnemyAlert
- SerializeField: _enemy: Enemy; _targetVision: TargetVision; _enemyMove: EnemyMove; _enemyRotator: EnemyRotator; _attacker: Attacker; _animation: EnemyAnimation; _animationEvents: PlayerAnimationEvents; _weaponHolder: WeaponHolder; _suicideAttack: EnemySuicideAttack; _runStartDistance: float; _runStopDistance: float; _fightMinDistance: float; _fightMaxDistance: float; _fireMaxDistance: float; _fightGapDistance: float; _rangeRunStart: float; _rangeRunStop: float; _orbitWeight: float; _ringWeight: float; _slotWeight: float; _slotAngle: float; _slotRadius: float; _slotCount: int; _recoverBack: float; _recoverSide: float; _idleMoveMin: float; _idleMoveMax: float; _idleWaitMin: float; _idleWaitMax: float; _idleWaitScale: float; _idleTurnMin: float; _idleTurnMax: float; _idleLookAngle: float; _idleReachDistance: float; _idleWallGap: float; _lostChaseDistance: float; _lostStopDistance: float; _searchPointDistance: float; _obstacleMask: LayerMask; _allyMask: LayerMask; _probeRadius: float; _probeHeight: float; _probeDistance: float; _probeAngle: float; _avoidWeight: float; _separationRadius: float; _separationWeight: float; _isMoveGizmoVisible: bool
- Публичный API: partial class EnemyMeleeBrain : MonoBehaviour, IEnemyBrain, IEnemyAlert {; EnemyState State =>; EnemySteering Steering =>; bool DebugHasLastSeenPoint =>; bool DebugHasSearchPoint =>; bool DebugIsSafeMove =>; bool DebugIsIdleWalking =>; bool DebugIsSearchIdle =>; bool DebugIsAttackInProgress =>; bool DebugIsAttackWindup =>; bool DebugIsCombatClockwise =>; bool DebugIsHitPending =>

## Assets/Scripts\Characters\Enemy\Melee\EnemyMeleeBrain.Gizmo.cs
- Типы: class EnemyMeleeBrain
- SerializeField: нет
- Публичный API: partial class EnemyMeleeBrain {

## Assets/Scripts\Characters\Enemy\Melee\EnemyMeleeBrain.Idle.cs
- Типы: class EnemyMeleeBrain
- SerializeField: нет
- Публичный API: partial class EnemyMeleeBrain {

## Assets/Scripts\Characters\Enemy\Melee\EnemyMeleeBrain.State.cs
- Типы: class EnemyMeleeBrain
- SerializeField: нет
- Публичный API: partial class EnemyMeleeBrain {

## Assets/Scripts\Characters\Enemy\Melee\EnemyMeleeBrain.Utility.cs
- Типы: class EnemyMeleeBrain
- SerializeField: нет
- Публичный API: partial class EnemyMeleeBrain {

## Assets/Scripts\Characters\Enemy\Melee\EnemySuicideAttack.cs
- Типы: class EnemySuicideAttack : MonoBehaviour
- SerializeField: _health: Health; _fxPrefab: GameObject; _startDistance: float; _delay: float; _damageMask: LayerMask; _damage: float; _radius: float; _impulse: float; _up: float; _fxLife: float; _explosionClip: AudioClip; _explosionMixerGroup: AudioMixerGroup
- Публичный API: class EnemySuicideAttack : MonoBehaviour {; bool IsActive =>; bool IsStartNeeded(float distance); void StartFuse(); void Tick(); void ResetState()

## Assets/Scripts\Characters\Enemy\Room\EnemyRoomAlert.cs
- Типы: class EnemyRoomAlert : MonoBehaviour
- SerializeField: нет
- Публичный API: class EnemyRoomAlert : MonoBehaviour {; void AlertPoint(Vector3 point, MonoBehaviour sender); void AlertPoint(Vector3 point, MonoBehaviour sender, int maxCount, System.Random random)

## Assets/Scripts\Characters\Enemy\Room\EnemyRoomLock.cs
- Типы: class EnemyRoomLock : MonoBehaviour
- SerializeField: нет
- Публичный API: class EnemyRoomLock : MonoBehaviour {; void Setup(RoomRuntimeState roomRuntimeState); bool ContainsRoomPoint(Vector3 point); bool ContainsMovePoint(Vector3 point); Vector3 ClampMovePoint(Vector3 point); float GetMoveTop(); float GetMoveBottom(); int GetPatrolCount(); int GetNearestPatrolIndex(Vector3 point); int GetNextPatrolIndex(int patrolIndex, int patrolDirection); Vector3 GetPatrolPoint(int patrolIndex, float height); void AlertPoint(Vector3 point, MonoBehaviour sender)

## Assets/Scripts\Characters\Enemy\Steering\EnemySteering.Combat.cs
- Типы: class EnemySteering
- SerializeField: нет
- Публичный API: partial class EnemySteering {; bool ChaseTarget(Vector3 targetPoint, float ringDistance, float ringTolerance, float lookBlend); bool OrbitTarget(Transform target, float ringDistance, float ringTolerance, bool isClockwise); bool RecoverTarget(Transform target, bool isClockwise)

## Assets/Scripts\Characters\Enemy\Steering\EnemySteering.cs
- Типы: class EnemySteering
- SerializeField: нет
- Публичный API: partial class EnemySteering {; string DebugStatus =>; Vector3 DebugRequestedTargetPoint =>; Vector3 DebugResolvedTargetPoint =>; Vector3 DebugLookPoint =>; Vector3 DebugMoveDirection =>; Vector3 DebugSteerDirection =>; Vector3 DebugPathTargetPoint =>; Vector3 DebugLastNavPoint =>; float DebugPathStopDistance =>; float DebugMoveStuckTimer =>; bool HasDebugRequestedTargetPoint =>

## Assets/Scripts\Characters\Enemy\Steering\EnemySteering.Nav.cs
- Типы: class EnemySteering
- SerializeField: нет
- Публичный API: partial class EnemySteering {; Vector3 GetReachPoint(Vector3 targetPoint, float wallGap); bool IsLineBlocked(Vector3 targetPoint)

## Assets/Scripts\Characters\Enemy\Steering\EnemySteering.Point.cs
- Типы: class EnemySteering
- SerializeField: нет
- Публичный API: partial class EnemySteering {; bool HasPointClearance(Vector3 point); bool HasWallGap(Vector3 point, float wallGap); Vector3 GetSafePoint(Vector3 point, float wallGap); bool TryPickNavPoint(Vector3 currentPoint, Vector3 forwardDirection, float minDistance, float maxDistance, float wallGap, int tryCount, Func<float> getProgress, out Vector3 navPoint)

## Assets/Scripts\Characters\Enemy\Steering\EnemySteering.Probe.cs
- Типы: class EnemySteering
- SerializeField: нет
- Публичный API: partial class EnemySteering {; bool ResolveOverlap()

## Assets/Scripts\Characters\Fire\Ammo\Core\Ammo.Collision.cs
- Типы: class Ammo
- SerializeField: нет
- Публичный API: partial class Ammo {

## Assets/Scripts\Characters\Fire\Ammo\Core\Ammo.cs
- Типы: class Ammo : MonoBehaviour
- SerializeField: _rigidbody: Rigidbody; _collisionCollider: Collider; _targetLayers: LayerMask; _minSpeed: float; _maxSpeed: float; _lifetimeSeconds: float
- Публичный API: partial class Ammo : MonoBehaviour {; Transform IgnoredRoot =>; LayerMask TargetLayers =>; void SetLayers(LayerMask targetLayers); void SetDamage(float damage); void SetSpeedMultiplier(float speedMultiplier); void SetIgnoredRoot(Transform ignoredRoot)

## Assets/Scripts\Characters\Fire\Ammo\Core\Ammo.Motion.cs
- Типы: class Ammo
- SerializeField: нет
- Публичный API: partial class Ammo {

## Assets/Scripts\Characters\Fire\Ammo\Core\AmmoReturner.cs
- Типы: class AmmoReturner : MonoBehaviour
- SerializeField: _ammo: Ammo
- Публичный API: class AmmoReturner : MonoBehaviour {; Ammo Ammo =>; void Initialize(IAmmoSpawner spawner); void ReturnToPool()

## Assets/Scripts\Characters\Fire\Ammo\Core\IAmmoSpawner.cs
- Типы: interface IAmmoSpawner
- SerializeField: нет
- Публичный API: interface IAmmoSpawner {

## Assets/Scripts\Characters\Fire\Ammo\Effect\AmmoEffect.cs
- Типы: class AmmoEffect : MonoBehaviour
- SerializeField: _bulletReturner: AmmoReturner; _particlePrefab: ParticleEffect
- Публичный API: class AmmoEffect : MonoBehaviour {

## Assets/Scripts\Characters\Fire\Ammo\Effect\AmmoParticleSystem.cs
- Типы: class AmmoParticleSystem : AmmoLifeListener
- SerializeField: _particleSystem: ParticleSystem
- Публичный API: class AmmoParticleSystem : AmmoLifeListener {; bool IsLifeEndComplete {

## Assets/Scripts\Characters\Fire\Ammo\Effect\AmmoRendererDisabler.cs
- Типы: class AmmoRenderersDisabler : AmmoLifeListener
- SerializeField: _renderers: Renderer[]
- Публичный API: class AmmoRenderersDisabler : AmmoLifeListener {

## Assets/Scripts\Characters\Fire\Ammo\Effect\AmmoTrailRenderer.cs
- Типы: class AmmoTrailRenderer : AmmoLifeListener
- SerializeField: _trailRenderer: TrailRenderer
- Публичный API: class AmmoTrailRenderer : AmmoLifeListener {; bool IsLifeEndComplete {

## Assets/Scripts\Characters\Fire\Ammo\Lifecycle\AmmoEnemyAlert.cs
- Типы: class AmmoEnemyAlert : AmmoLifeListener
- SerializeField: _alertRadius: float
- Публичный API: class AmmoEnemyAlert : AmmoLifeListener {

## Assets/Scripts\Characters\Fire\Ammo\Lifecycle\AmmoLifeListener.cs
- Типы: class AmmoLifeListener : MonoBehaviour
- SerializeField: _ammo: Ammo
- Публичный API: class AmmoLifeListener : MonoBehaviour {; bool IsLifeEndComplete =>

## Assets/Scripts\Characters\Fire\Ammo\Lifecycle\AmmoLifePath.cs
- Типы: class AmmoLifePath : AmmoLifeListener
- SerializeField: _ammoReturner: AmmoReturner
- Публичный API: class AmmoLifePath : AmmoLifeListener {

## Assets/Scripts\Characters\Fire\Ammo\Projectiles\Bullet.cs
- Типы: class Bullet : Ammo
- SerializeField: _impulseStrength: float
- Публичный API: class Bullet : Ammo {

## Assets/Scripts\Characters\Fire\Ammo\Projectiles\Rocket.cs
- Типы: class Rocket : Ammo
- SerializeField: _impulseStrength: float; _radiusImpulse: float; _upwardsModifier: float
- Публичный API: class Rocket : Ammo {; void SetExplosionRadiusMultiplier(float radiusMultiplier)

## Assets/Scripts\Characters\Fire\BulletFireExecutor.cs
- Типы: class BulletFireExecutor : FireExecutor
- SerializeField: _muzzle: Transform; _bulletPrefab: Ammo; _minDamage: float; _maxDamage: float; _rocketRadiusMultiplier: float
- Публичный API: class BulletFireExecutor : FireExecutor {

## Assets/Scripts\Characters\Fire\FireExecutor.cs
- Типы: class FireExecutor : MonoBehaviour
- SerializeField: _fireRatePerSecond: float; _targetLayers: LayerMask; _maxAimAngleDegrees: float; _readyAngleDegrees: float
- Публичный API: class FireExecutor : MonoBehaviour {; void ApplyModifierContext(WeaponModifierContext context); void SetAimPoint(Vector3 aimPoint); void ClearAimPoint(); void StartFiring(); void StopFiring(); bool TryStartFiring(); bool TryFire(); bool IsAimReady(); float GetFireCooldown01(); void SetTargetLayers(LayerMask targetLayers); void SetIgnoredRoot(Transform ignoredRoot)

## Assets/Scripts\Characters\Fire\FireExecutrer\Calculator\FireDamageCalculator.cs
- Типы: class FireDamageCalculator : IDamageCalculator
- SerializeField: нет
- Публичный API: class FireDamageCalculator : IDamageCalculator {; float CalculateScaledDamage(float minDamage, float maxDamage)

## Assets/Scripts\Characters\Fire\FireExecutrer\Calculator\FireModifierState.cs
- Типы: class FireModifierState
- SerializeField: нет
- Публичный API: class FireModifierState {; float FireRateMultiplier =>; float DamageMultiplier =>; float SpreadMultiplier =>; int PelletBonus =>; float ProjectileSpeedMultiplier =>; float ExplosionRadiusMultiplier =>; float CriticalChance01 =>; float CriticalDamageMultiplier =>; void SetContext(WeaponModifierContext context)

## Assets/Scripts\Characters\Fire\FireExecutrer\Calculator\FireRateProvider.cs
- Типы: class FireRateProvider : IFireRateProvider
- SerializeField: нет
- Публичный API: class FireRateProvider : IFireRateProvider {; float GetEffectiveFireRatePerSecond()

## Assets/Scripts\Characters\Fire\FireExecutrer\Calculator\IDamageCalculator.cs
- Типы: interface IDamageCalculator
- SerializeField: нет
- Публичный API: interface IDamageCalculator {

## Assets/Scripts\Characters\Fire\FireExecutrer\Calculator\IFireRateProvider.cs
- Типы: interface IFireRateProvider
- SerializeField: нет
- Публичный API: interface IFireRateProvider {

## Assets/Scripts\Characters\Fire\FireExecutrer\FireExecutorPresenter.cs
- Типы: class FireExecutorPresenter
- SerializeField: нет
- Публичный API: class FireExecutorPresenter {; bool HasAimPoint =>; Vector3 AimPoint =>; void SetIgnoredRoot(Transform ignoredRoot); void OnEnable(); void OnDisable(); void Tick(float timeSeconds); float GetFireCooldown01(float timeSeconds); void SetAimPoint(Vector3 aimPoint); void ClearAimPoint(); bool IsAimReady(); bool TryStartFiring()

## Assets/Scripts\Characters\Fire\FireExecutrer\FireShotContext.cs
- Типы: struct FireShotContext
- SerializeField: нет
- Публичный API: struct FireShotContext {

## Assets/Scripts\Characters\Fire\FireExecutrer\Strategy\BulletShotStrategy.cs
- Типы: class BulletShotStrategy : IShotStrategy
- SerializeField: нет
- Публичный API: class BulletShotStrategy : IShotStrategy {; bool IsBusy =>; bool TryStartShot(FireShotContext context); void Tick(FireShotContext context); void Stop()

## Assets/Scripts\Characters\Fire\FireExecutrer\Strategy\IShotStrategy.cs
- Типы: interface IShotStrategy
- SerializeField: нет
- Публичный API: interface IShotStrategy {

## Assets/Scripts\Characters\Fire\FireExecutrer\Strategy\RocketShotStrategy.cs
- Типы: class RocketShotStrategy : IShotStrategy
- SerializeField: нет
- Публичный API: class RocketShotStrategy : IShotStrategy {; bool IsBusy =>; bool TryStartShot(FireShotContext context); void Tick(FireShotContext context); void Stop()

## Assets/Scripts\Characters\Fire\FireExecutrer\Strategy\ShotgunBurstShotStrategy.cs
- Типы: class ShotgunBurstShotStrategy : IShotStrategy
- SerializeField: нет
- Публичный API: class ShotgunBurstShotStrategy : IShotStrategy {; bool IsBusy =>; bool TryStartShot(FireShotContext context); void Tick(FireShotContext context); void Stop()

## Assets/Scripts\Characters\Fire\RocketFireExecutor.cs
- Типы: class RocketFireExecutor : FireExecutor
- SerializeField: _muzzle: Transform; _rocketPrefab: Ammo; _minDamage: float; _maxDamage: float
- Публичный API: class RocketFireExecutor : FireExecutor {

## Assets/Scripts\Characters\Fire\ShotgunFireExecutor.cs
- Типы: class ShotgunFireExecutor : FireExecutor
- SerializeField: _muzzle: Transform; _pelletPrefab: Ammo; _minPelletsPerShot: int; _maxPelletsPerShot: int; _spreadAngleDegrees: float; _pelletIntervalSeconds: float; _minPelletDamage: float; _maxPelletDamage: float
- Публичный API: class ShotgunFireExecutor : FireExecutor {

## Assets/Scripts\Characters\Turret\IdleRotator.cs
- Типы: class IdleRotator : MonoBehaviour
- SerializeField: _rotationPivot: Transform; _idleRotationSpeed: float; _idleMinAngle: float; _idleMaxAngle: float
- Публичный API: class IdleRotator : MonoBehaviour {; void ResetBaseRotation(); void CaptureBaseRotation()

## Assets/Scripts\Characters\Turret\TargetRotator.cs
- Типы: class TargetRotator : MonoBehaviour
- SerializeField: _targetVision: TargetVision; _rotationPivot: Transform; _rotationSpeed: float; _maxPitchAngle: float
- Публичный API: class TargetRotator : MonoBehaviour {; void SetAimPoint(Vector3 aimPoint); void ClearAimPoint()

## Assets/Scripts\Characters\Turret\TargetVision.cs
- Типы: class TargetVision : MonoBehaviour
- SerializeField: _origin: Transform; _currentTarget: Transform; _viewDistance: float; _scanInterval: float; _targetLayerMask: LayerMask; _obstacleLayerMask: LayerMask; _targetTag: string
- Публичный API: class TargetVision : MonoBehaviour {; bool IsTargetVisible {; float DistanceToTarget {; Vector3 CurrentTargetPoint {; Transform CurrentTarget {; bool IsPointVisible(Vector3 targetPoint); void Refresh()

## Assets/Scripts\Characters\Turret\Turret.cs
- Типы: class Turret : MonoBehaviour, IEnemyAlert
- SerializeField: _health: Health; _targetVision: TargetVision; _targetRotator: TargetRotator; _idleRotator: IdleRotator; _fireExecutor: FireExecutor; _headCrash: TurretHeadCrash; _fireDelaySeconds: float
- Публичный API: class Turret : MonoBehaviour, IEnemyAlert {; bool IsDead =>; bool ApplyAlert(Vector3 point)

## Assets/Scripts\Characters\Turret\TurretHeadCrash.cs
- Типы: class TurretHeadCrash : MonoBehaviour
- SerializeField: _moveRoot: Transform; _rigidbody: Rigidbody; _collider: Collider; _ignoredCollider: Collider; _force: float; _up: float; _forward: float; _spin: float
- Публичный API: class TurretHeadCrash : MonoBehaviour {; void Crash(); void BeginSink(float sinkDelay, float sinkDuration, float sinkDistance)

## Assets/Scripts\Developer\DeveloperCheatSave.cs
- Типы: class DeveloperCheatSave
- SerializeField: нет
- Публичный API: class DeveloperCheatSave {; bool LoadInfiniteHealth(); bool LoadInfiniteDamage(); void SaveInfiniteHealth(bool isEnabled); void SaveInfiniteDamage(bool isEnabled)

## Assets/Scripts\Environment\DamageableObject.cs
- Типы: class DamageableObject : MonoBehaviour
- SerializeField: нет
- Публичный API: class DamageableObject : MonoBehaviour {

## Assets/Scripts\Environment\FadableObstacle.cs
- Типы: class FadableObstacle : MonoBehaviour, IFadable
- SerializeField: _renderer: Renderer; _settings: FadableSettings
- Публичный API: class FadableObstacle : MonoBehaviour, IFadable {; void OnOccluded(); void OnVisible()

## Assets/Scripts\Environment\Harvestable.cs
- Типы: class Harvestable : DamageableObject
- SerializeField: _spawnHeightOffset: float; _pickupPrefab: BasePickup; _forceXMin: float; _forceXMax: float; _forceYMin: float; _forceYMax: float; _forceZMin: float; _forceZMax: float
- Публичный API: class Harvestable : DamageableObject {

## Assets/Scripts\Environment\IFadable.cs
- Типы: interface IFadable
- SerializeField: нет
- Публичный API: interface IFadable {

## Assets/Scripts\Environment\Light\StaticRotation.cs
- Типы: class StaticRotation : MonoBehaviour
- SerializeField: _light: Light; _rotationAxis: Vector3; _degreesPerSecond: float; _isActive: bool
- Публичный API: class StaticRotation : MonoBehaviour {; void SetActive(bool isActive); void SetDegreesPerSecond(float degreesPerSecond); void SetRotationAxis(Vector3 rotationAxis)

## Assets/Scripts\Interactables\DoorInteractable.cs
- Типы: class DoorInteractable : Interactable
- SerializeField: _doorTransform: Transform; _openAngle: float; _openDuration: float; _openEase: Ease
- Публичный API: class DoorInteractable : Interactable {; string GetPrompt(); void Interact(GameObject interactor)

## Assets/Scripts\Interactables\FractureFx.cs
- Типы: class FractureFx : MonoBehaviour
- SerializeField: _explosionForce: float; _explosionRadius: float; _upwardsModifier: float; _fadeDelay: float; _fadeDuration: float; _targetTransparency: float; _destroyDelayAfterFade: float; _rigidbodies: List<Rigidbody>; _renderers: List<Renderer>
- Публичный API: class FractureFx : MonoBehaviour {; void CollectChildren(); void Play()

## Assets/Scripts\Interactables\Interactable.cs
- Типы: class Interactable : MonoBehaviour
- SerializeField: нет
- Публичный API: class Interactable : MonoBehaviour {; void Highlight(bool state); string GetPrompt(); void Interact(GameObject interactor)

## Assets/Scripts\Interactables\PickupOnDeath.cs
- Типы: class PickupOnDeath : MonoBehaviour
- SerializeField: _health: Health; _intactObject: GameObject; _pickupPrefab: BasePickup; _spawnOffset: Vector3; _useSinkAnimation: bool
- Публичный API: class PickupOnDeath : MonoBehaviour {

## Assets/Scripts\Level\Core\Corridors\LevelCorridorBoundsBuilder.cs
- Типы: class LevelCorridorBoundsBuilder
- SerializeField: нет
- Публичный API: bool TryBuildCorridorCollisionBounds(Vector3 fromDoor, Vector3 toDoor, int corridorWidthInBlocks, LevelCorridorBuilder corridorBuilder, LevelPlacementSettings placementSettings, out Bounds bounds); bool IsStrictlyStraight(Vector3 from, Vector3 to)

## Assets/Scripts\Level\Core\Corridors\LevelCorridorExecutor.cs
- Типы: class LevelCorridorExecutor
- SerializeField: нет
- Публичный API: bool BuildCorridors(LevelGenerationContext generationContext, Transform corridorsRoot, LevelCorridorBuilder corridorBuilder); void ClearCorridors(Transform corridorsRoot); bool BuildCorridor(LevelEdge edge, Transform corridorsRoot, LevelCorridorBuilder corridorBuilder)

## Assets/Scripts\Level\Core\Generation\LevelDoorAlignmentUtility.cs
- Типы: class LevelDoorAlignmentUtility
- SerializeField: нет
- Публичный API: void RotateRoomToMatchEntrance(RoomGenerator room, RoomDoorMarker entrance, Vector3 desiredOut); Vector3 GetWorldSideDirection(Transform roomTransform, DoorSide side)

## Assets/Scripts\Level\Core\Generation\LevelGenerationContext.cs
- Типы: class LevelGenerationContext
- SerializeField: нет
- Публичный API: void ClearData()

## Assets/Scripts\Level\Core\Generation\LevelGenerator.cs
- Типы: class LevelGenerator : MonoBehaviour
- SerializeField: _roomsRoot: Transform; _corridorsRoot: Transform; _levelSequenceProfile: LevelSequenceProfile; _roomPrefabLibrary: LevelRoomPrefabLibrary; _corridorBuilder: LevelCorridorBuilder; _levelSeed: int; _generateOnAwake: bool; _randomSeedOnAwake: bool; _removeLegacyEnvironment: bool; _pauseGameplayDuringRuntimeGeneration: bool; _streamRooms: bool; _player: Transform; _disallowCorridorIntersections: bool; _randomizeRootRotation: bool; _snapRoomsToGrid: bool
- Публичный API: class LevelGenerator : MonoBehaviour {; bool HasGeneratedLevel =>; bool IsGenerating =>; void StartRuntimeGeneration(); void Generate(); void Clear(); Vector3 GetStartRoomCenter(); Vector3 GetBossRoomCenter(); Vector3 GetBossRoomEntry(); bool TryGetSchematicLayout( int gridWidth, int gridHeight, List<Vector2Int> roomCells, List<Vector2Int> corridorCells, out Vector2Int startCell, out Vector2Int exitCell )

## Assets/Scripts\Level\Core\Generation\LevelGeneratorTypes.cs
- Типы: class LevelNode; class PlacedRoomInfo; struct LevelEdge
- SerializeField: нет
- Публичный API: нет

## Assets/Scripts\Level\Core\Generation\LevelGeneratorUtility.cs
- Типы: class LevelGeneratorUtility
- SerializeField: нет
- Публичный API: int RandomRangeInclusive(System.Random random, Vector2Int range); int NextAnyInt(System.Random random); void DestroyChildren(Transform parent)

## Assets/Scripts\Level\Core\Generation\LevelPlanBuilder.cs
- Типы: class LevelPlanBuilder
- SerializeField: нет
- Публичный API: LevelNode BuildPlan(LevelGenerationContext generationContext, System.Random random, LevelSequenceProfile levelSequenceProfile, LevelTreasureRatio treasureRatio)

## Assets/Scripts\Level\Core\Rooms\LevelRoomBoundsCalculator.cs
- Типы: class LevelRoomBoundsCalculator
- SerializeField: нет
- Публичный API: Bounds CalculateRoomBounds(RoomGenerator room, float padding)

## Assets/Scripts\Level\Core\Rooms\LevelRoomFinalizer.cs
- Типы: class LevelRoomFinalizer
- SerializeField: нет
- Публичный API: void FinalizeInteriors(LevelGenerationContext generationContext, float enemyBorderGap); void FinalizeRoom(LevelGenerationContext generationContext, int roomIndex, float enemyBorderGap)

## Assets/Scripts\Level\Core\Rooms\LevelRoomPlacer.cs
- Типы: class LevelRoomPlacer
- SerializeField: нет
- Публичный API: bool PlaceAllRooms(LevelGenerationContext generationContext, LevelNode root, System.Random random, LevelCorridorBuilder corridorBuilder, LevelPlacementSettings placementSettings)

## Assets/Scripts\Level\Core\Rooms\LevelRoomShellInstantiator.cs
- Типы: class LevelRoomShellInstantiator
- SerializeField: нет
- Публичный API: bool InstantiateRoomsShellOnly( LevelGenerationContext generationContext, System.Random random, Transform roomsRoot, LevelRoomPrefabLibrary roomPrefabLibrary, LevelSequenceProfile levelSequenceProfile, int maximumRoomRegenerateAttempts ); bool InstantiateRoomShell( LevelGenerationContext generationContext, int nodeIndex, System.Random random, Transform roomsRoot, LevelRoomPrefabLibrary roomPrefabLibrary, LevelSequenceProfile levelSequenceProfile, int maximumRoomRegenerateAttempts )

## Assets/Scripts\Level\Core\Rooms\StartRoomCenterer.cs
- Типы: class StartRoomCenterer : MonoBehaviour
- SerializeField: _levelGenerator: LevelGenerator; _playerRoot: Transform; _playerBody: Transform; _heightOffset: float
- Публичный API: class StartRoomCenterer : MonoBehaviour {

## Assets/Scripts\Level\Core\Runtime\LevelRoomStreamer.cs
- Типы: class LevelRoomStreamer : MonoBehaviour
- SerializeField: _player: Transform
- Публичный API: class LevelRoomStreamer : MonoBehaviour {; void ClearRooms()

## Assets/Scripts\Level\Core\Runtime\LevelRuntimeNavMesh.cs
- Типы: class LevelRuntimeNavMesh : MonoBehaviour
- SerializeField: _agentTypeId: int; _editorGeometry: NavMeshCollectGeometry; _runtimeGeometry: NavMeshCollectGeometry
- Публичный API: class LevelRuntimeNavMesh : MonoBehaviour {; bool IsBusy {; void RequestBuild(); void BuildNow(); void RequestUpdate(); void ClearData()

## Assets/Scripts\Level\Corridors\LevelCorridorBuilder.cs
- Типы: class LevelCorridorBuilder : MonoBehaviour; struct WallVisualMetrics; enum WallPostPlacement
- SerializeField: _floorPrefab: GameObject; _autoDetectFloorPrefabSize: bool; _floorPrefabSizeInUnits: Vector2; _floorLocalOffset: Vector3; _wallPrefab: GameObject; _postPrefab: GameObject; _wallPostPlacement: WallPostPlacement; _wallPostVisualPrefab: GameObject; _wallPostVisualResourcePath: string; _wallPostVisualYawOffset: float; _wallLocalOffset: Vector3; _enableColliders: bool; _wallVisualPrefabs: List<GameObject>; _wallVisualResourcePaths: List<string>; _hideWallBlockRenderersWhenVisualsAssigned: bool; _wallVisualYawOffset: float; _wallVisualFloorOffsetInUnits: float
- Публичный API: class LevelCorridorBuilder : MonoBehaviour {; float BlockSize =>; void BuildBetweenDoors(Transform parent, RoomDoorMarker fromDoor, RoomDoorMarker toDoor, int corridorWidthInBlocks)

## Assets/Scripts\Level\Profiles\LevelGenerationProfile.cs
- Типы: class LevelGenerationProfile : ScriptableObject
- SerializeField: _mainPathRoomTypes: List<RoomType>; _branchCountRange: Vector2Int; _branchLengthRange: Vector2Int; _branchRoomType: RoomType; _branchTerminalRoomType: RoomType; _allowBranchesFromStart: bool; _allowBranchesFromBoss: bool
- Публичный API: class LevelGenerationProfile : ScriptableObject {; IReadOnlyList<RoomType> MainPathRoomTypes =>; Vector2Int BranchCountRange =>; Vector2Int BranchLengthRange =>; RoomType BranchRoomType =>; RoomType BranchTerminalRoomType =>; bool AllowBranchesFromStart =>; bool AllowBranchesFromBoss =>

## Assets/Scripts\Level\Profiles\LevelRoomPrefabLibrary.cs
- Типы: class LevelRoomPrefabLibrary : ScriptableObject
- SerializeField: _startRooms: List<WeightedRoomGeneratorPrefab>; _combatRooms: List<WeightedRoomGeneratorPrefab>; _treasureRooms: List<WeightedRoomGeneratorPrefab>; _bossRooms: List<WeightedRoomGeneratorPrefab>
- Публичный API: class LevelRoomPrefabLibrary : ScriptableObject {; RoomGenerator Pick(RoomType roomType, System.Random random)

## Assets/Scripts\Level\Profiles\LevelSequenceProfile.cs
- Типы: class LevelSequenceProfile : ScriptableObject
- SerializeField: _firstCombatProfile: RoomTypeProfile; _mainCombatCountRange: Vector2Int; _allowTreasureOnMainPath: bool; _mainTreasureCountClamp: Vector2Int; _totalBranchCountRange: Vector2Int; _branchLengthRange: Vector2Int; _allowBranchesFromStart: bool; _allowBranchesFromBoss: bool; _allowTreasureInsideBranches: bool; _branchTerminalIsTreasure: bool
- Публичный API: class LevelSequenceProfile : ScriptableObject {; RoomTypeProfile FirstCombatProfile =>; Vector2Int MainCombatCountRange =>; bool AllowTreasureOnMainPath =>; float MainTreasurePerCombatRatio =>; Vector2Int MainTreasureCountClamp =>; int MainTreasureMinimumSpacing =>; Vector2Int TotalBranchCountRange =>; Vector2Int BranchLengthRange =>; int MaximumBranchDepth =>; float NestedBranchChance =>; bool AllowBranchesFromStart =>

## Assets/Scripts\Level\Profiles\WeightedRoomGeneratorPrefab.cs
- Типы: class WeightedRoomGeneratorPrefab
- SerializeField: _prefab: RoomGenerator
- Публичный API: class WeightedRoomGeneratorPrefab {; RoomGenerator Prefab =>; int Weight =>

## Assets/Scripts\Pickups\Core\BaseAnimatedPickup.cs
- Типы: class BaseAnimatedPickup : BasePickup
- SerializeField: _rigidbody: Rigidbody; _returner: PickupReturner; _pickupIdle: PickupIdle
- Публичный API: class BaseAnimatedPickup : BasePickup {

## Assets/Scripts\Pickups\Core\BasePickup.cs
- Типы: class BasePickup : MonoBehaviour
- SerializeField: _item: Item; _amount: int
- Публичный API: class BasePickup : MonoBehaviour {; Item Item {; int Amount {; bool TryCollect(GameObject player, Inventory inventory); void Pickup(GameObject player); void SetAmount(int amount)

## Assets/Scripts\Pickups\Core\PickupReturner.cs
- Типы: class PickupReturner : MonoBehaviour
- SerializeField: _pickup: BasePickup
- Публичный API: class PickupReturner : MonoBehaviour {; void SetSpawner(Spawner spawner); void SetCanReturn(bool canReturn); void ReturnToPool()

## Assets/Scripts\Pickups\Core\ScaleCompensator.cs
- Типы: class ParentScaleCompensator : MonoBehaviour
- SerializeField: _targetWorldScale: Vector3
- Публичный API: class ParentScaleCompensator : MonoBehaviour {

## Assets/Scripts\Pickups\Idle\PickupIdle.cs
- Типы: class PickupIdle : MonoBehaviour
- SerializeField: _idleBehaviours: PickupIdleBehaviour[]
- Публичный API: class PickupIdle : MonoBehaviour {; void SetIdleActive(bool isIdleActive)

## Assets/Scripts\Pickups\Idle\PickupIdleBehaviour.cs
- Типы: class PickupIdleBehaviour : MonoBehaviour
- SerializeField: нет
- Публичный API: class PickupIdleBehaviour : MonoBehaviour {; void SetIdleActive(bool isIdleActive)

## Assets/Scripts\Pickups\Idle\PickupIdleMotion.cs
- Типы: class PickupIdleMotion : PickupIdleBehaviour
- SerializeField: _moveAmplitude: float; _moveDurationSeconds: float; _rotationDegrees: Vector3; _rotationDurationSeconds: float; _randomStartDelaySeconds: float; _baseHeightOffset: float; _baseOffsetTransitionSeconds: float
- Публичный API: class PickupIdleMotion : PickupIdleBehaviour {

## Assets/Scripts\Pickups\Idle\PickupIdleParticle.cs
- Типы: class PickupIdleParticles : PickupIdleBehaviour
- SerializeField: _particleSystem: ParticleSystem; _randomStartDelaySeconds: float; _clearOnStop: bool
- Публичный API: class PickupIdleParticles : PickupIdleBehaviour {

## Assets/Scripts\Pickups\Items\BerryPickup.cs
- Типы: class BerryPickup : BaseAnimatedPickup
- SerializeField: нет
- Публичный API: class BerryPickup : BaseAnimatedPickup {; bool TryCollect(GameObject player, Inventory inventory)

## Assets/Scripts\Pickups\Items\CurrencyPickup.cs
- Типы: class CurrencyPickup : BaseAnimatedPickup
- SerializeField: нет
- Публичный API: class CurrencyPickup : BaseAnimatedPickup {; bool TryCollect(GameObject player, Inventory inventory)

## Assets/Scripts\Pickups\Items\ItemPickup.cs
- Типы: class ItemPickup : BaseAnimatedPickup
- SerializeField: нет
- Публичный API: class ItemPickup : BaseAnimatedPickup {

## Assets/Scripts\Player\Animation\AttackEndedStateBehaviour.cs
- Типы: class AttackEndedStateBehaviour : StateMachineBehaviour
- SerializeField: нет
- Публичный API: class AttackEndedStateBehaviour : StateMachineBehaviour {; void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)

## Assets/Scripts\Player\Animation\PlayerAnimationEvents.cs
- Типы: class PlayerAnimationEvents : MonoBehaviour
- SerializeField: нет
- Публичный API: class PlayerAnimationEvents : MonoBehaviour {; void InvokeAttackingEvent(); void InvokeAttackEndedEvent(); void InvokeStepEvent()

## Assets/Scripts\Player\BerryWallet.cs
- Типы: class BerryWallet : MonoBehaviour
- SerializeField: _item: Item; _berries: int
- Публичный API: class BerryWallet : MonoBehaviour {; int Berries =>; bool TryConsume(CharacterEffects effects); void Add(int amount)

## Assets/Scripts\Player\Combat\Attacks\PlayerMeleeAttack.cs
- Типы: class PlayerMeleeAttack
- SerializeField: нет
- Публичный API: class PlayerMeleeAttack {; float AttackStaminaCost =>; void SetAttackStaminaCost(float attackStaminaCost); bool StartAttack(); void CancelOnly(); void StartHolding(); void StopHolding()

## Assets/Scripts\Player\Combat\Attacks\PlayerRangedFire.cs
- Типы: class PlayerRangedFire
- SerializeField: нет
- Публичный API: class PlayerRangedFire {; bool IsFiring {; float AttackStaminaCost =>; void SetAttackStaminaCost(float attackStaminaCost); void Tick(); bool StartFiring(); void StopFiringOnly(); void SetAimPoint(Vector3 aimPoint)

## Assets/Scripts\Player\Combat\Core\PlayerActiveWeaponType.cs
- Типы: class PlayerActiveWeaponType
- SerializeField: нет
- Публичный API: class PlayerActiveWeaponType {; WeaponType Value {

## Assets/Scripts\Player\Combat\Core\PlayerBattleState.cs
- Типы: class PlayerBattleState
- SerializeField: нет
- Публичный API: class PlayerBattleState {; bool IsInBattle {; bool Tick(float deltaTime); void Touch(); void Exit(); void SyncWeaponAnimatorIfNeeded(); void UnlockWeaponSwitchAndRefreshAnimator(); void SetSwitchLocked(bool isLocked); void SetHoldAllowed(bool isAllowed)

## Assets/Scripts\Player\Combat\Core\PlayerBattleTimer.cs
- Типы: class PlayerBattleTimer
- SerializeField: нет
- Публичный API: class PlayerBattleTimer {; bool IsRunning {; void Restart(float seconds); void Stop(); bool Tick(float deltaTime)

## Assets/Scripts\Player\Combat\Core\PlayerCombat.cs
- Типы: class PlayerCombat : MonoBehaviour
- SerializeField: _attacker: Attacker; _stamina: Stamina; _weaponHolder: WeaponHolder; _weaponModifierApplier: WeaponModifierApplier; _animator: PlayerAnimator; _animatorSwitcher: AnimatorSwitcher; _inventory: Inventory; _animationEvents: PlayerAnimationEvents; _movementGate: PlayerMovementGate; _timeBattle: float; _attackStaminaCost: float; _attackStartDelaySeconds: float; _meleeAttackStartDelaySeconds: float
- Публичный API: class PlayerCombat : MonoBehaviour {; float AttackStaminaCost =>; bool IsInBattle {; bool AttackStart(); void AttackCancel(); void SetAimPoint(Vector3 aimPoint); void ExitBattle(); void ApplyModifier(float attackStaminaCost)

## Assets/Scripts\Player\Combat\Core\PlayerCombatCore.cs
- Типы: class PlayerCombatCore
- SerializeField: нет
- Публичный API: class PlayerCombatCore {; bool IsInBattle =>; void OnEnabled(); void OnDisabled(); void Tick(float deltaTime); void EnterBattle(); bool AttackStart(); void AttackCancel(); void SetAimPoint(Vector3 aimPoint); bool StartHoldingAttack(); void StopHoldingAttack(); void ExitBattle()

## Assets/Scripts\Player\CurrencyWallet.cs
- Типы: class CurrencyWallet : MonoBehaviour
- SerializeField: _coins: int
- Публичный API: class CurrencyWallet : MonoBehaviour {; int Coins =>; bool CanSpend(int amount); bool TrySpend(int amount); void Add(int amount)

## Assets/Scripts\Player\Input\PlayerInputActions.cs
- Типы: struct PlayerActions; struct UIActions; struct CameraActions; interface IPlayerActions; interface IUIActions; interface ICameraActions
- SerializeField: нет
- Публичный API: partial class @PlayerInputActions: IInputActionCollection2, IDisposable {; InputActionAsset asset {; void Dispose(); InputBinding? bindingMask {; ReadOnlyArray<InputDevice>? devices {; ReadOnlyArray<InputControlScheme> controlSchemes =>; bool Contains(InputAction action); IEnumerator<InputAction> GetEnumerator(); void Enable(); void Disable(); IEnumerable<InputBinding> bindings =>; InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)

## Assets/Scripts\Player\Interact\CarryAttachment.cs
- Типы: class CarryAttachment : MonoBehaviour
- SerializeField: _carryPoint: Transform
- Публичный API: class CarryAttachment : MonoBehaviour {; Transform CarryPoint =>

## Assets/Scripts\Player\Interact\CharacterInteractor.cs
- Типы: class CharacterInteractor : MonoBehaviour
- SerializeField: _origin: Transform; _texter: Texter; _interactionSphereRadius: float; _interactableMask: LayerMask
- Публичный API: class CharacterInteractor : MonoBehaviour {; void TickHover(); void TryInteract(GameObject interactorGameObject)

## Assets/Scripts\Player\Interact\PickupTrigger.cs
- Типы: class PickupTrigger : MonoBehaviour
- SerializeField: _triggerCollider: Collider; _pickupMask: LayerMask; _inventory: Inventory; _pickupTarget: Transform
- Публичный API: class PickupTrigger : MonoBehaviour {

## Assets/Scripts\Player\Interact\PlayerInteraction.cs
- Типы: class PlayerInteraction : MonoBehaviour
- SerializeField: _interactor: CharacterInteractor; _movement: CharacterMover; _animator: PlayerAnimator; _cursor: CursorManager; _combat: PlayerCombat; _modifierVendingMachineMenuView: ModifierVendingMachineMenuView; _playerModifierMenuView: PlayerModifierMenuView; _hoverTickSeconds: float
- Публичный API: class PlayerInteraction : MonoBehaviour {; void Interact()

## Assets/Scripts\Player\Inventory\Core\Inventory.cs
- Типы: class Inventory : MonoBehaviour
- SerializeField: _capacity: int; _slots: List<InventorySlot>; _activeIndex: int
- Публичный API: class Inventory : MonoBehaviour {; IReadOnlyList<InventorySlot> Slots {; int ActiveIndex {; bool TryAddItem(Item item, int amount); bool TryUseItem(int slotIndex); void SetActiveIndex(int index); bool TryRemoveFromSlot(int slotIndex, int amount); void NextActiveSlot(); void PreviousActiveSlot()

## Assets/Scripts\Player\Inventory\Core\InventoryDropper.cs
- Типы: class InventoryDropper : MonoBehaviour
- SerializeField: _inventory: Inventory; _dropOrigin: Transform; _dropDistance: float
- Публичный API: class InventoryDropper : MonoBehaviour {; void DropOneFromActiveSlot(); void DropAllFromActiveSlot()

## Assets/Scripts\Player\Inventory\Core\InventorySlot.cs
- Типы: class InventorySlot
- SerializeField: нет
- Публичный API: class InventorySlot {; bool IsEmpty(); bool CanStack(Item item); void SetItem(Item item, int amount); void Clear()

## Assets/Scripts\Player\Inventory\Core\PlayerInventory.cs
- Типы: class PlayerInventory : MonoBehaviour
- SerializeField: _inventory: Inventory; _inventoryDropper: InventoryDropper; _effects: CharacterEffects; _berryWallet: BerryWallet
- Публичный API: class PlayerInventory : MonoBehaviour {; void Scroll(Vector2 scrollValue); void SetActiveSlot(int slotIndex); void DropOne(); void DropAll(); void TryUseActiveItem()

## Assets/Scripts\Player\Inventory\UI\InventorySlotView.cs
- Типы: class InventorySlotView : MonoBehaviour
- SerializeField: _icon: Image; _amountText: TMP_Text; _highlight: InventorySlotHighlight
- Публичный API: class InventorySlotView : MonoBehaviour {; void SetSlot(InventorySlot slot); void Refresh(); void SetActive(bool isActive)

## Assets/Scripts\Player\Inventory\UI\InventoryView.cs
- Типы: class InventoryView : MonoBehaviour
- SerializeField: _inventory: Inventory; _slotViewPrefab: InventorySlotView; _slotsParent: Transform
- Публичный API: class InventoryView : MonoBehaviour {

## Assets/Scripts\Player\Modifier\PlayerHealthRegenUnlock.cs
- Типы: class PlayerHealthRegenUnlock : PlayerModifier
- SerializeField: нет
- Публичный API: class PlayerHealthRegenUnlock : PlayerModifier {; void Apply(ref PlayerModifierContext context)

## Assets/Scripts\Player\Modifier\PlayerModifier.cs
- Типы: class PlayerModifier : ScriptableObject
- SerializeField: нет
- Публичный API: class PlayerModifier : ScriptableObject {; void Apply(ref PlayerModifierContext context)

## Assets/Scripts\Player\Modifier\PlayerModifierApplier.cs
- Типы: class PlayerModifierApplier : MonoBehaviour
- SerializeField: _health: Health; _stamina: Stamina; _characterMover: CharacterMover; _playerMovement: PlayerMovement; _playerCombat: PlayerCombat; _playerModifierStack: PlayerModifierStack
- Публичный API: class PlayerModifierApplier : MonoBehaviour {

## Assets/Scripts\Player\Modifier\PlayerModifierContext.cs
- Типы: struct PlayerModifierContext
- SerializeField: нет
- Публичный API: struct PlayerModifierContext {

## Assets/Scripts\Player\Modifier\PlayerModifierStack.cs
- Типы: class PlayerModifierStack : MonoBehaviour
- SerializeField: _modifiers: List<PlayerModifier>
- Публичный API: class PlayerModifierStack : MonoBehaviour {; IReadOnlyList<PlayerModifier> Modifiers {; void Add(PlayerModifier modifier); void ClearAll()

## Assets/Scripts\Player\Modifier\PlayerModifierStat.cs
- Типы: enum PlayerModifierStat
- SerializeField: нет
- Публичный API: enum PlayerModifierStat {

## Assets/Scripts\Player\Modifier\PlayerMultiplierModifier.cs
- Типы: class PlayerMultiplierModifier : PlayerModifier
- SerializeField: _stat: PlayerModifierStat; _multiplier: float
- Публичный API: class PlayerMultiplierModifier : PlayerModifier {; PlayerModifierStat Stat =>; float Multiplier =>; void Apply(ref PlayerModifierContext context)

## Assets/Scripts\Player\Movement\CameraMover.cs
- Типы: class CameraMover : MonoBehaviour
- SerializeField: _player: Transform; _cursorManager: CursorManager; _camera: Camera; _height: float; _smoothSpeed: float; _positionOffset: Vector3
- Публичный API: class CameraMover : MonoBehaviour {

## Assets/Scripts\Player\Movement\CharacterJumper.cs
- Типы: class CharacterJump : MonoBehaviour
- SerializeField: _rigidbody: Rigidbody; _jumpForce: float; _rayLengthGround: float; _groundLayer: LayerMask
- Публичный API: class CharacterJump : MonoBehaviour {; void OnJump()

## Assets/Scripts\Player\Movement\CharacterMover.cs
- Типы: class CharacterMover : MonoBehaviour
- SerializeField: _rigidbody: Rigidbody; _speed: float; _speedSprint: float; _isWallSlideEnabled: bool; _wallNormalMaxY: float
- Публичный API: class CharacterMover : MonoBehaviour {; float Speed =>; float SprintSpeed =>; bool IsKnockbackActive =>; bool IsWallBlocked =>; void OnMove(Vector2 input); void OnSprint(bool sprinting); void StopMove(); void ApplyKnockback(Vector3 direction, float speed, float duration, float lift); void ForceStop(); void ApplySpeed(float speed, float sprintSpeed)

## Assets/Scripts\Player\Movement\CharacterRotator.cs
- Типы: class CharacterRotator : MonoBehaviour
- SerializeField: _rotationSpeed: float; _cursorManager: CursorManager
- Публичный API: void Rotate(); void RotateTowardsMovement(Vector2 moveInput)

## Assets/Scripts\Player\Movement\PlayerMovement\PlayerAttackMovementBlend.cs
- Типы: class PlayerAttackMovementBlend
- SerializeField: нет
- Публичный API: class PlayerAttackMovementBlend {; bool IsRecoverActive {; void StartRecover(); void CancelRecover(); void TickSlowdown(float deltaTime); void TickRecover(float deltaTime)

## Assets/Scripts\Player\Movement\PlayerMovement\PlayerJumpAction.cs
- Типы: class PlayerJumpAction
- SerializeField: нет
- Публичный API: class PlayerJumpAction {; float StaminaCost =>; void SetCost(float jumpStaminaCost); void TryJump(bool isMovementAllowed)

## Assets/Scripts\Player\Movement\PlayerMovement\PlayerMoveApplier.cs
- Типы: class PlayerMoveApplier
- SerializeField: нет
- Публичный API: class PlayerMoveApplier {; Vector2 InputMoveVector {; Vector2 AppliedMoveVector {; Vector3 LastWorldMoveDirection {; void SetInput(Vector2 moveVector); void Apply(Vector2 moveVector); void SetMoveState(bool isMoving); void SetStepSoundAllowed(bool isAllowed); void SetWorldMoveDirection(Vector3 worldMoveDirection)

## Assets/Scripts\Player\Movement\PlayerMovement\PlayerMovement.cs
- Типы: class PlayerMovement : MonoBehaviour
- SerializeField: _movement: CharacterMover; _jump: CharacterJump; _rotator: CharacterRotator; _animator: PlayerAnimator; _stamina: Stamina; _movementGate: PlayerMovementGate; _jumpStaminaCost: float; _sprintStaminaCostPerSecond: float; _moveStopDelaySeconds: float; _attackEnterSeconds: float; _attackExitSeconds: float; _attackMovementMultiplier: float
- Публичный API: class PlayerMovement : MonoBehaviour {; bool IsSprinting =>; float JumpStaminaCost =>; float SprintStaminaCostPerSecond =>; void SetMove(Vector2 moveVector); void TryJump(); void ApplyKnockback(Vector3 direction, float speed, float duration, float lift); void TryStartSprinting(); void StopSprinting(); void ApplyModifier(float jumpStaminaCost, float sprintStaminaCostPerSecond); void TickFixed(bool isInBattle)

## Assets/Scripts\Player\Movement\PlayerMovement\PlayerMoveStopDelay.cs
- Типы: class PlayerMoveStopDelay
- SerializeField: нет
- Публичный API: class PlayerMoveStopDelay {; void OnInputMove(Vector2 moveVector); void Tick(float deltaTime); void Cancel()

## Assets/Scripts\Player\Movement\PlayerMovement\PlayerRotationByState.cs
- Типы: class PlayerRotationByState
- SerializeField: нет
- Публичный API: void TickFixed(bool isInBattle, bool isMovementAllowed)

## Assets/Scripts\Player\Movement\PlayerMovement\PlayerSprint.cs
- Типы: class PlayerSprint
- SerializeField: нет
- Публичный API: class PlayerSprint {; bool IsSprinting {; float StaminaCostPerSecond =>; void SetCostPerSecond(float sprintStaminaCostPerSecond); bool TryStart(); void Stop(); void Tick(float deltaTime)

## Assets/Scripts\Player\Movement\PlayerMovementGate.cs
- Типы: class PlayerMovementGate : MonoBehaviour
- SerializeField: нет
- Публичный API: class PlayerMovementGate : MonoBehaviour {; bool IsMovementAllowed {; void BlockMovement(); void AllowMovement()

## Assets/Scripts\Player\Root\LineSightFader.cs
- Типы: class LineSightFader : MonoBehaviour
- SerializeField: _cameraTransform: Transform; _target: Transform; _obstacleMask: LayerMask; _sphereRadius: float; _checkInterval: float; _minDistanceFromCamera: float; _minDistanceToTarget: float
- Публичный API: class LineSightFader : MonoBehaviour {

## Assets/Scripts\Player\Root\Player.cs
- Типы: class Player : MonoBehaviour
- SerializeField: _health: Health; _uiCanvas: RectTransform; _bossHealthIndicatorTemplate: RectTransform; _movement: PlayerMovement; _combat: PlayerCombat; _inventory: PlayerInventory; _interaction: PlayerInteraction; _pause: PlayerPause
- Публичный API: class Player : MonoBehaviour {; Player Instance {; PlayerMovement Movement =>

## Assets/Scripts\Player\Root\PlayerDie.cs
- Типы: class PlayerDie : MonoBehaviour
- SerializeField: _characterDied: CharacterDied; _timeScaleSettings: TimeScaleSettings; _heldMode: HeldMode; _player: Player; _health: Health; _blurOverlay: BlurOverlay; _exitMenuView: ExitMenuView; _buttonRevival: Button
- Публичный API: нет

## Assets/Scripts\Player\Root\PlayerRoundStats.cs
- Типы: class PlayerRoundStats : MonoBehaviour
- SerializeField: _currencyWallet: CurrencyWallet
- Публичный API: class PlayerRoundStats : MonoBehaviour {; PlayerRoundStatsSnapshot CreateSnapshot()

## Assets/Scripts\Player\Root\PlayerRoundStatsSnapshot.cs
- Типы: class PlayerRoundStatsSnapshot
- SerializeField: нет
- Публичный API: class PlayerRoundStatsSnapshot {; float DurationSeconds {; int DefeatedEnemies {; int DefeatedBosses {; int CollectedCoins {

## Assets/Scripts\Player\Root\PlayerVictory.cs
- Типы: class PlayerVictory : MonoBehaviour
- SerializeField: _timeScaleSettings: TimeScaleSettings; _heldMode: HeldMode; _player: Player; _roundStats: PlayerRoundStats; _blurOverlay: BlurOverlay; _victoryMenuView: BaseMenuView; _roundStatsView: VictoryRoundStatsView; _restartButton: Button; _mainMenuButton: Button; _mainMenuSceneName: string; _transitionDelaySeconds: float
- Публичный API: class PlayerVictory : MonoBehaviour {

## Assets/Scripts\Player\UI\Cursor\CursorInputHandler.cs
- Типы: class CursorInputHandler : MonoBehaviour
- SerializeField: _cursorAnimator: CursorAnimator; _holdThresholdSeconds: float
- Публичный API: class CursorInputHandler : MonoBehaviour {

## Assets/Scripts\Player\UI\Cursor\CursorManager.cs
- Типы: class CursorManager : MonoBehaviour
- SerializeField: _camera: Camera; _player: Transform; _rectTransform: RectTransform; _interactableMask: LayerMask
- Публичный API: class CursorManager : MonoBehaviour {; Vector3 MouseWorldPos {; Vector3 MouseGroundPos {; Vector3 MouseHitPos {; bool HasHit {; Vector2 MouseScreenPos =>; bool TryGetHitObject(out RaycastHit hitInfo); void Enable(); void Disable()

## Assets/Scripts\Player\UI\Cursor\CursorRadialIndicator.cs
- Типы: class CursorRadialIndicator : MonoBehaviour
- SerializeField: _cooldownImage: Image
- Публичный API: class CursorRadialIndicator : MonoBehaviour {; void SetCooldown01(float cooldown01); void SetReady()

## Assets/Scripts\Player\UI\Cursor\FireCooldownCursorIndicator.cs
- Типы: class FireCooldownCursorIndicator : MonoBehaviour
- SerializeField: _weaponHolder: WeaponHolder; _cooldownView: CursorRadialIndicator
- Публичный API: class FireCooldownCursorIndicator : MonoBehaviour {

## Assets/Scripts\Player\UI\Hud\BerryWalletView.cs
- Типы: class BerryWalletView : MonoBehaviour
- SerializeField: _wallet: BerryWallet; _berriesText: TMP_Text
- Публичный API: class BerryWalletView : MonoBehaviour {

## Assets/Scripts\Player\UI\Hud\CurrencyWalletView.cs
- Типы: class CurrencyWalletView : MonoBehaviour
- SerializeField: _wallet: CurrencyWallet; _coinsText: TMP_Text
- Публичный API: class CurrencyWalletView : MonoBehaviour {

## Assets/Scripts\Player\UI\Hud\RemainingEnemyIndicatorView.cs
- Типы: class RemainingEnemyIndicatorView : MonoBehaviour
- SerializeField: нет
- Публичный API: class RemainingEnemyIndicatorView : MonoBehaviour {; void Initialize( RectTransform parent, Sprite ringSprite, Sprite dotSprite, Color fillColor, Color ringColor, Color dotColor); void ShowOnScreen(Vector2 anchoredPosition); void ShowOffScreen(Vector2 anchoredPosition); void SetVisible(bool isVisible)

## Assets/Scripts\Player\UI\Hud\RemainingEnemyOverlay.cs
- Типы: class RemainingEnemyOverlay : MonoBehaviour
- SerializeField: нет
- Публичный API: class RemainingEnemyOverlay : MonoBehaviour {; void Initialize(Transform playerTransform, RectTransform uiRoot)

## Assets/Scripts\Player\UI\Pause\PlayerPause.cs
- Типы: class PlayerPause : MonoBehaviour
- SerializeField: _pauseController: PauseController
- Публичный API: class PlayerPause : MonoBehaviour {; void Toggle()

## Assets/Scripts\Player\Weapon\HeldMode.cs
- Типы: class HeldMode : MonoBehaviour
- SerializeField: _colliders: Collider[]; _componentsToDisable: MonoBehaviour[]; _rigidbodies: Rigidbody[]
- Публичный API: class HeldMode : MonoBehaviour {; void SetHeld(bool isHeld)

## Assets/Scripts\Player\Weapon\InventoryWeaponBinder.cs
- Типы: class InventoryWeaponBinder : MonoBehaviour
- SerializeField: _inventory: Inventory; _weaponHolder: WeaponHolder
- Публичный API: class InventoryWeaponBinder : MonoBehaviour {

## Assets/Scripts\Player\Weapon\Modificator\WeaponModifier.cs
- Типы: class WeaponModifier : ScriptableObject
- SerializeField: нет
- Публичный API: class WeaponModifier : ScriptableObject {; void Apply(ref WeaponModifierContext context)

## Assets/Scripts\Player\Weapon\Modificator\WeaponModifierApplier.cs
- Типы: class WeaponModifierApplier : MonoBehaviour
- SerializeField: _weaponHolder: WeaponHolder; _weaponModifierStack: WeaponModifierStack; _inventory: Inventory
- Публичный API: class WeaponModifierApplier : MonoBehaviour {; WeaponModifierContext BuildCurrentContext()

## Assets/Scripts\Player\Weapon\Modificator\WeaponModifierContext.cs
- Типы: struct WeaponModifierContext
- SerializeField: нет
- Публичный API: struct WeaponModifierContext {; void SetDefaults()

## Assets/Scripts\Player\Weapon\Modificator\WeaponModifierStack.cs
- Типы: class WeaponModifierStack : MonoBehaviour
- SerializeField: _modifiers: List<WeaponModifier>
- Публичный API: class WeaponModifierStack : MonoBehaviour {; IReadOnlyList<WeaponModifier> Modifiers {; void Add(WeaponModifier modifier); void ClearAll()

## Assets/Scripts\Player\Weapon\Modificator\WeaponTypeFilteredModifier.cs
- Типы: class WeaponTypeFilteredModifier : WeaponModifier
- SerializeField: _allowedWeaponTypes: List<WeaponType>
- Публичный API: class WeaponTypeFilteredModifier : WeaponModifier {; void Apply(ref WeaponModifierContext context)

## Assets/Scripts\Player\Weapon\WeaponGrid.cs
- Типы: class WeaponGrip : MonoBehaviour
- SerializeField: _localPositionOffset: Vector3; _localRotationOffsetEuler: Vector3
- Публичный API: class WeaponGrip : MonoBehaviour {; Vector3 LocalPositionOffset {; Vector3 LocalRotationOffsetEuler {

## Assets/Scripts\Player\Weapon\WeaponHolder.cs
- Типы: class WeaponHolder : MonoBehaviour
- SerializeField: _weaponSocket: Transform; _ownerHealth: Health; _targetLayers: LayerMask
- Публичный API: class WeaponHolder : MonoBehaviour {; FireExecutor FireExecutor {; bool IsHoldAllowed {; bool IsSwitchLocked {; Item CurrentItem =>; void SetHoldAllowed(bool isHoldAllowed); void SetSwitchLocked(bool isSwitchLocked); void Equip(BasePickup pickupPrefab); void Clear()

## Assets/Scripts\Room\Build\RoomFloorOccupancy.cs
- Типы: class RoomFloorOccupancy
- SerializeField: нет
- Публичный API: class RoomFloorOccupancy {; int RoomWidthInBlocks {; int RoomDepthInBlocks {; HashSet<Vector2Int> OccupiedFloorCells {; bool IsFree(Vector2Int floorCell)

## Assets/Scripts\Room\Build\RoomInteriorBlockFiller.cs
- Типы: class RoomInteriorBlockFiller : MonoBehaviour; struct NoiseCell; struct SmallPrefabSelection
- SerializeField: _interiorBlocksRoot: Transform; _fallbackInteriorCubePrefab: GameObject; _interiorSmallPrefabs: List<WeightedPrefab>; _interiorLargePrefabs: List<WeightedPrefab>; _blockSize: float; _spawnOnlyExposedSmallCubes: bool
- Публичный API: class RoomInteriorBlockFiller : MonoBehaviour {; void Clear(); RoomFloorOccupancy Fill(Vector3Int roomSizeInBlocks, IReadOnlyCollection<Vector2Int> reservedFloorCells, RoomTypeProfile roomTypeProfile, System.Random random)

## Assets/Scripts\Room\Build\RoomShellBuilder.cs
- Типы: class RoomShellBuilder : MonoBehaviour; struct FenceVisualMetrics; enum FloorBuildMode; enum FencePostPlacement
- SerializeField: _floorBlocksRoot: Transform; _floorBlockPrefab: GameObject; _floorBuildMode: FloorBuildMode; _floorPrefab: GameObject; _autoScaleFloorPrefab: bool; _floorPrefabBaseSizeInUnits: Vector2; _floorPrefabLocalOffset: Vector3; _floorPrefabScaleMultiplier: Vector3; _fencePostsRoot: Transform; _fencePostPrefab: GameObject; _postPlacement: FencePostPlacement; _fencePostVisualPrefab: GameObject; _fencePostVisualResourcePath: string; _fencePostVisualYawOffset: float; _fenceSegmentsRoot: Transform; _fenceSegmentPrefab: GameObject; _fenceSegmentVisualPrefabs: List<GameObject>; _fenceSegmentVisualResourcePaths: List<string>; _hideFenceBlockRenderersWhenVisualsAssigned: bool; _fenceSegmentVisualYawOffset: float; _doorMarkersRoot: Transform; _doorMarkerPrefab: GameObject; _blockSize: float; _ceilingEnabled: bool; _postPivotAtBase: bool; _segmentPivotAtBase: bool; _floorSurfaceYOffset: float
- Публичный API: class RoomShellBuilder : MonoBehaviour {; float BlockSize =>; int PostHeightInBlocks =>; Material FenceMaterial =>; void BuildShell(Vector3Int roomSizeInBlocks, IReadOnlyList<RoomDoorPlan> doorPlans); void Clear()

## Assets/Scripts\Room\Config\EnemySpawnConfig.cs
- Типы: class EnemySpawnConfig
- SerializeField: _prefab: GameObject; _weight: int; _guaranteed: bool; _weaponPrefab: BasePickup
- Публичный API: class EnemySpawnConfig {; GameObject Prefab =>; int Weight =>; float SpawnHeight =>; bool Guaranteed =>; BasePickup WeaponPrefab =>; void SetSpawnHeight(float spawnHeight)

## Assets/Scripts\Room\Config\EnemySpawnHeight.cs
- Типы: class EnemySpawnHeight
- SerializeField: _prefab: GameObject
- Публичный API: class EnemySpawnHeight {; GameObject Prefab =>; float SpawnHeight =>

## Assets/Scripts\Room\Config\EnemySpawnPicker.cs
- Типы: class EnemySpawnPicker
- SerializeField: нет
- Публичный API: class EnemySpawnPicker {; EnemySpawnConfig PickSpawn(IReadOnlyList<EnemySpawnConfig> enemySpawns, System.Random random); EnemySpawnConfig PickSpawn( IReadOnlyList<EnemySpawnConfig> enemySpawns, IReadOnlyList<EnemySpawnConfig> spawnedEnemySpawns, System.Random random )

## Assets/Scripts\Room\Config\Enums.cs
- Типы: enum RoomType; enum DoorSide; enum DoorRole
- SerializeField: нет
- Публичный API: enum RoomType {; enum DoorSide {; enum DoorRole {

## Assets/Scripts\Room\Config\NookPrefabConfig.cs
- Типы: class NookPrefabConfig
- SerializeField: _prefab: GameObject; _guaranteed: bool; _countRange: Vector2Int
- Публичный API: class NookPrefabConfig {; GameObject Prefab =>; int Weight =>; bool Guaranteed =>; Vector2Int CountRange =>; int MinimumDistanceFromCorridorInCells =>; int WallMarginInCells =>; int FootprintRadiusInCells =>; int MinimumDistanceToAnyNookInCells =>; int MinimumDistanceToSameTypeInCells =>; int SameTypeNeighborRadiusInCells =>; int MaximumSameTypeWithinNeighborRadius =>

## Assets/Scripts\Room\Config\WeightedPrefab.cs
- Типы: class WeightedPrefab
- SerializeField: _prefab: GameObject; _weight: int; _guaranteed: bool
- Публичный API: class WeightedPrefab {; GameObject Prefab =>; int Weight =>; float SpawnHeight =>; bool Guaranteed =>; void SetSpawnHeight(float spawnHeight)

## Assets/Scripts\Room\Config\WeightedPrefabPicker.cs
- Типы: class WeightedPrefabPicker
- SerializeField: нет
- Публичный API: class WeightedPrefabPicker {; GameObject PickPrefab(IReadOnlyList<WeightedPrefab> weightedPrefabs, System.Random random)

## Assets/Scripts\Room\Core\RoomCombatLock.cs
- Типы: class RoomCombatLock : MonoBehaviour
- SerializeField: _gatesRoot: Transform; _enterTriggersRoot: Transform
- Публичный API: class RoomCombatLock : MonoBehaviour {; bool IsLocked =>; RoomRuntimeState RoomRuntimeState =>; IReadOnlyList<RoomCombatLock> Instances =>; void Setup(RoomRuntimeState roomRuntimeState, float blockSize); int FillAliveThreatTransforms(List<Transform> threatTransforms, int maxCount)

## Assets/Scripts\Room\Core\RoomEnterTrigger.cs
- Типы: class RoomEnterTrigger : MonoBehaviour
- SerializeField: нет
- Публичный API: class RoomEnterTrigger : MonoBehaviour {; void Setup(RoomDoorMarker roomDoorMarker, float blockSize)

## Assets/Scripts\Room\Core\RoomGenerator.cs
- Типы: class RoomGenerator : MonoBehaviour
- SerializeField: _roomSizeInBlocks: Vector3Int; _randomSeed: int; _entranceDoorEnabled: bool; _exitDoorEnabled: bool; _roomTypeProfile: RoomTypeProfile; _roomDoorPlanner: RoomDoorPlanner; _roomShellBuilder: RoomShellBuilder; _roomPassagePlanner: RoomPassagePlanner; _roomInteriorBlockFiller: RoomInteriorBlockFiller; _roomContentSpawner: RoomContentSpawner; _roomNookSpawner: RoomNookSpawner; _combineInteriorChunks: bool; _roomInteriorChunkCombiner: RoomInteriorChunkCombiner
- Публичный API: class RoomGenerator : MonoBehaviour {; IReadOnlyList<RoomDoorMarker> DoorMarkers =>; bool HasGeneratedShell =>; void SetRuntimeSeed(int seed); void ClearRuntimeSeed(); void SetDoorRolesEnabled(bool entranceDoorEnabled, bool exitDoorEnabled); void SetRuntimeDoorCountRange(int minimumDoorCount, int maximumDoorCount); void ClearRuntimeDoorCountRange(); void SetRuntimeCombatRoomIndex(int combatRoomIndex); void SetRuntimeProfile(RoomTypeProfile roomTypeProfile); void ClearRuntimeProfile(); void Generate()

## Assets/Scripts\Room\Core\RoomRuntimeState.cs
- Типы: class RoomRuntimeState : MonoBehaviour
- SerializeField: нет
- Публичный API: class RoomRuntimeState : MonoBehaviour {; void Setup(Bounds roomBounds, float enemyBorderGap); bool ContainsRoomPoint(Vector3 point); bool ContainsMovePoint(Vector3 point); Vector3 ClampMovePoint(Vector3 point); Vector3 ClampSnapPoint(Vector3 point); float GetMoveTop(); float GetMoveBottom(); int GetPatrolCount(); Vector3 GetPatrolPoint(int patrolIndex, float height); int GetGroundPatrolCount(); Vector3 GetGroundPatrolPoint(int patrolIndex, float height)

## Assets/Scripts\Room\Doors\RoomDoorGate.cs
- Типы: class RoomDoorGate : MonoBehaviour
- SerializeField: _moveTime: float; _visualPrefab: GameObject; _visualResourcePath: string; _fallbackVisualResourcePath: string; _visualYawOffset: float
- Публичный API: class RoomDoorGate : MonoBehaviour {; void Setup(RoomDoorMarker roomDoorMarker, float blockSize); void SetClosed(bool isClosed, bool isInstant)

## Assets/Scripts\Room\Doors\RoomDoorMarker.cs
- Типы: class RoomDoorMarker : MonoBehaviour
- SerializeField: _side: DoorSide; _role: DoorRole
- Публичный API: class RoomDoorMarker : MonoBehaviour {; DoorSide Side =>; DoorRole Role =>; int WidthInBlocks =>; int HeightInBlocks =>; void Initialize(DoorSide side, DoorRole role, int widthInBlocks, int heightInBlocks)

## Assets/Scripts\Room\Doors\RoomDoorPlan.cs
- Типы: class RoomDoorPlan
- SerializeField: _side: DoorSide; _role: DoorRole; _openingOffset: int; _openingWidthInBlocks: int; _openingHeightInBlocks: int
- Публичный API: class RoomDoorPlan {; DoorSide Side =>; DoorRole Role =>; int OpeningOffset =>; int OpeningWidthInBlocks =>; int OpeningHeightInBlocks =>

## Assets/Scripts\Room\Planning\RoomDoorPlanner.cs
- Типы: class RoomDoorPlanner : MonoBehaviour; struct DoorCandidate
- SerializeField: _minimumDoorCount: int; _maximumDoorCount: int; _openingWidthInBlocks: int; _openingHeightInBlocks: int
- Публичный API: class RoomDoorPlanner : MonoBehaviour {; List<RoomDoorPlan> CreateDoorPlans( Vector3Int roomSizeInBlocks, RoomTypeProfile roomTypeProfile, bool entranceDoorEnabled, bool exitDoorEnabled, System.Random random ); List<RoomDoorPlan> CreateDoorPlans( Vector3Int roomSizeInBlocks, RoomTypeProfile roomTypeProfile, bool entranceDoorEnabled, bool exitDoorEnabled, int minimumDoorCount, int maximumDoorCount, System.Random random )

## Assets/Scripts\Room\Planning\RoomPassagePlanner.cs
- Типы: class RoomPassagePlanner : MonoBehaviour; enum CornerId; struct PocketRect
- SerializeField: _doorClearDepthInCells: int; _additionalPassageWidthPaddingInCells: int
- Публичный API: class RoomPassagePlanner : MonoBehaviour {; IReadOnlyCollection<Vector2Int> AdditionalNoFillCells =>; bool TryGetGuaranteedNookCell(out Vector2Int guaranteedNookCell); HashSet<Vector2Int> CreateReservedFloorCells(Vector3Int roomSizeInBlocks, IReadOnlyList<RoomDoorPlan> doorPlans, RoomTypeProfile roomTypeProfile)

## Assets/Scripts\Room\Spawn\RoomContentSpawner.cs
- Типы: class RoomContentSpawner : MonoBehaviour; struct SpawnCandidate
- SerializeField: _objectsRoot: Transform; _enemiesRoot: Transform; _blockSize: float; _objectSpawnHeight: float; _enemySpawnHeight: float
- Публичный API: class RoomContentSpawner : MonoBehaviour {; void Spawn( RoomTypeProfile roomTypeProfile, Vector3Int roomSizeInBlocks, RoomFloorOccupancy floorOccupancy, IReadOnlyCollection<Vector2Int> reservedFloorCells, IReadOnlyList<RoomDoorPlan> doorPlans, int combatRoomIndex, System.Random random ); void SetBlockSize(float blockSize); void Clear()

## Assets/Scripts\Room\Spawn\RoomNookSpawner.cs
- Типы: class RoomNookSpawner : MonoBehaviour; struct NookComponent; struct PlacedNook
- SerializeField: _nooksRoot: Transform; _blockSize: float
- Публичный API: class RoomNookSpawner : MonoBehaviour {; void Spawn( RoomTypeProfile roomTypeProfile, Vector3Int roomSizeInBlocks, RoomFloorOccupancy floorOccupancy, IReadOnlyCollection<Vector2Int> corridorReservedFloorCells, bool hasGuaranteedNookCell, Vector2Int guaranteedNookCell, System.Random random ); void SetBlockSize(float blockSize); void Clear()

## Assets/Scripts\Room\Utils\Chunk\ChunkRootContext.cs
- Типы: struct ChunkRootContext
- SerializeField: нет
- Публичный API: struct ChunkRootContext {

## Assets/Scripts\Room\Utils\Chunk\ChunkRootInstaller.cs
- Типы: class ChunkRootInstaller : ScriptableObject
- SerializeField: нет
- Публичный API: class ChunkRootInstaller : ScriptableObject {; void Install(ref ChunkRootContext context)

## Assets/Scripts\Room\Utils\Chunk\ChunkVariantRootComposer.cs
- Типы: class ChunkVariantRootComposer : MonoBehaviour
- SerializeField: нет
- Публичный API: class ChunkVariantRootComposer : MonoBehaviour {; ChunkVariantSwitcherBase Compose( GameObject rootObject, GameObject staticVisualObject, GameObject notStaticVisualObject, bool startWithStaticActive )

## Assets/Scripts\Room\Utils\Chunk\ChunkVariantSwitcherBase.cs
- Типы: class ChunkVariantSwitcherBase : MonoBehaviour
- SerializeField: нет
- Публичный API: class ChunkVariantSwitcherBase : MonoBehaviour {; void Initialize(GameObject staticObject, GameObject notStaticObject); void SetUseStatic(bool useStatic)

## Assets/Scripts\Room\Utils\Interior\RoomInteriorBlockRangeCollector.cs
- Типы: class RoomInteriorBlockRangeCollector; struct RoomInteriorBlockRange
- SerializeField: нет
- Публичный API: class RoomInteriorBlockRangeCollector {; void Collect( Transform interiorBlocksRoot, Transform combinedRoot, float blockSize, int chunkSizeInCells, bool includeInactive, List<RoomInteriorBlockRange> blockRanges ); struct RoomInteriorBlockRange {

## Assets/Scripts\Room\Utils\Interior\RoomInteriorChunkCombiner.cs
- Типы: class RoomInteriorChunkCombiner : MonoBehaviour
- SerializeField: _interiorBlocksRoot: Transform; _combinedRoot: Transform; _chunkRootCompositionProfile: RoomInteriorChunkRootCompositionProfile; _blockSize: float; _includeInactive: bool; _disableSourceRenderers: bool; _disableSourceColliders: bool; _createMeshCollider: bool; _meshColliderConvex: bool; _meshColliderIsTrigger: bool
- Публичный API: class RoomInteriorChunkCombiner : MonoBehaviour {; void Combine(); void ClearCombined()

## Assets/Scripts\Room\Utils\Interior\RoomInteriorChunkDynamicOnDamage.cs
- Типы: class RoomInteriorChunkDynamicOnDamage : MonoBehaviour
- SerializeField: нет
- Публичный API: class RoomInteriorChunkDynamicOnDamage : MonoBehaviour {; void Initialize(ChunkVariantSwitcherBase chunkVariantSwitcher); void Initialize(ChunkVariantSwitcherBase chunkVariantSwitcher, GameObject staticObject, GameObject notStaticObject, Health health); void ApplyDamage(); void ApplyDamage(int damage); void ApplyDamage(float damage)

## Assets/Scripts\Room\Utils\Interior\RoomInteriorChunkDynamicOnDamageInstaller.cs
- Типы: class RoomInteriorChunkDynamicOnDamageInstaller : ChunkRootInstaller
- SerializeField: _feedbackPrefab: GameObject; _popupPrefab: GameObject
- Публичный API: class RoomInteriorChunkDynamicOnDamageInstaller : ChunkRootInstaller {; void Install(ref ChunkRootContext context)

## Assets/Scripts\Room\Utils\Interior\RoomInteriorChunkRootCompositionProfile.cs
- Типы: class RoomInteriorChunkRootCompositionProfile : ScriptableObject
- SerializeField: _installers: ChunkRootInstaller[]
- Публичный API: class RoomInteriorChunkRootCompositionProfile : ScriptableObject {; void Compose( GameObject rootObject, GameObject staticVisualObject, GameObject notStaticVisualObject, bool startWithStaticActive )

## Assets/Scripts\Room\Utils\Interior\RoomInteriorChunkVariantRootComposer.cs
- Типы: class RoomInteriorChunkVariantRootComposer : ChunkVariantRootComposer
- SerializeField: нет
- Публичный API: class RoomInteriorChunkVariantRootComposer : ChunkVariantRootComposer {

## Assets/Scripts\Room\Utils\Interior\RoomInteriorChunkVariantSwitcher.cs
- Типы: class RoomInteriorChunkVariantSwitcher : ChunkVariantSwitcherBase
- SerializeField: нет
- Публичный API: class RoomInteriorChunkVariantSwitcher : ChunkVariantSwitcherBase {; void SetUseStatic(bool useStatic)

## Assets/Scripts\Room\Utils\Interior\RoomInteriorChunkVariantSwitcherInstaller.cs
- Типы: class RoomInteriorChunkVariantSwitcherInstaller : ChunkRootInstaller
- SerializeField: нет
- Публичный API: class RoomInteriorChunkVariantSwitcherInstaller : ChunkRootInstaller {; void Install(ref ChunkRootContext context)

## Assets/Scripts\Room\Utils\Interior\RoomInteriorClusterMeshCombiner.cs
- Типы: class RoomInteriorClusterMeshCombiner; struct SubMeshSource
- SerializeField: нет
- Публичный API: class RoomInteriorClusterMeshCombiner {; void CreateClusterRoot( int clusterIndex, HashSet<int> objectIndices, List<RoomInteriorBlockRange> blockRanges, HashSet<int> combinedSourceObjectIds )

## Assets/Scripts\Room\Utils\Interior\RoomInteriorCombinedMeshOwner.cs
- Типы: class RoomInteriorCombinedMeshOwner : MonoBehaviour
- SerializeField: нет
- Публичный API: class RoomInteriorCombinedMeshOwner : MonoBehaviour {; void Initialize(Mesh mesh, MeshCollider meshCollider, MeshFilter staticMeshFilter, MeshFilter notStaticMeshFilter)

## Assets/Scripts\Room\Utils\Interior\RoomInteriorHiddenBlockCuller.cs
- Типы: class RoomInteriorHiddenBlockCuller : MonoBehaviour; struct BlockVoxelRange
- SerializeField: _interiorBlocksRoot: Transform; _blockSize: float; _destroyHiddenObjects: bool; _disableHiddenObjects: bool; _treatFloorAsSolid: bool; _floorSolidYIndex: int; _includeInactive: bool; _ignoreRoot: Transform
- Публичный API: class RoomInteriorHiddenBlockCuller : MonoBehaviour {; void CullHidden()

## Assets/Scripts\Room\Utils\Interior\RoomInteriorSourceDisabler.cs
- Типы: class RoomInteriorSourceDisabler
- SerializeField: нет
- Публичный API: class RoomInteriorSourceDisabler {; void DisableRenderers(MeshFilter[] meshFilters, Transform combinedRoot, HashSet<int> combinedSourceObjectIds); void DisableColliders(MeshFilter[] meshFilters, Transform combinedRoot, HashSet<int> combinedSourceObjectIds)

## Assets/Scripts\Room\Utils\Interior\RoomInteriorVoxelClusterer.cs
- Типы: class RoomInteriorVoxelClusterer
- SerializeField: нет
- Публичный API: class RoomInteriorVoxelClusterer {; List<HashSet<int>> BuildClusters(List<RoomInteriorBlockRange> blockRanges)

## Assets/Scripts\ScriptableObject\FadableSettings.cs
- Типы: class FadableSettings : ScriptableObject
- SerializeField: нет
- Публичный API: class FadableSettings : ScriptableObject {

## Assets/Scripts\ScriptableObject\FireRateAddToMultiplierWeaponModifier.cs
- Типы: class FireRateWeaponFilteredModifier : WeaponTypeFilteredModifier
- SerializeField: _addToMultiplier: float
- Публичный API: class FireRateWeaponFilteredModifier : WeaponTypeFilteredModifier {; float AddToMultiplier =>

## Assets/Scripts\ScriptableObject\FireRateWeaponModifier.cs
- Типы: class FireRateWeaponModifier : WeaponModifier
- SerializeField: _multiplier: float
- Публичный API: class FireRateWeaponModifier : WeaponModifier {; float Multiplier =>; void Apply(ref WeaponModifierContext context)

## Assets/Scripts\ScriptableObject\Item\FoodEffect.cs
- Типы: class FoodEffect : ItemEffect
- SerializeField: _healAmount: float; _staminaAmount: float
- Публичный API: class FoodEffect : ItemEffect {; void Apply(CharacterEffects target)

## Assets/Scripts\ScriptableObject\Item\Item.cs
- Типы: class Item : ScriptableObject
- SerializeField: _id: string; _displayName: string; _icon: Sprite; _isStackable: bool; _maxStack: int; _weaponType: WeaponType; _isMultiHit: bool; _effects: List<ItemEffect>; _weaponModifiers: List<WeaponModifier>; _audioProfile: ItemAudioProfile; _prefab: BasePickup
- Публичный API: class Item : ScriptableObject {; string Id =>; string DisplayName =>; Sprite Icon =>; bool IsStackable =>; int MaxStack =>; WeaponType WeaponType =>; bool IsMultiHit =>; IReadOnlyList<ItemEffect> Effects =>; IReadOnlyList<WeaponModifier> WeaponModifiers =>; ItemAudioProfile AudioProfile =>; BasePickup Prefab =>

## Assets/Scripts\ScriptableObject\Item\ItemAudioProfile.cs
- Типы: class ItemAudioProfile : ScriptableObject
- SerializeField: _useClip: AudioClip
- Публичный API: class ItemAudioProfile : ScriptableObject {; AudioClip UseClip {

## Assets/Scripts\ScriptableObject\Item\ItemEffect.cs
- Типы: class ItemEffect : ScriptableObject
- SerializeField: нет
- Публичный API: class ItemEffect : ScriptableObject {; void Apply(CharacterEffects target)

## Assets/Scripts\ScriptableObject\Modifier\CriticalHitModifier.cs
- Типы: class CriticalHitModifier : WeaponTypeFilteredModifier
- SerializeField: _chanceAdd01: float; _damageMultiplier: float
- Публичный API: class CriticalHitModifier : WeaponTypeFilteredModifier {; float ChanceAdd01 =>; float DamageMultiplier =>

## Assets/Scripts\ScriptableObject\Modifier\DamageMultiplierFilteredModifier.cs
- Типы: class DamageMultiplierFilteredModifier : WeaponTypeFilteredModifier
- SerializeField: _multiplier: float
- Публичный API: class DamageMultiplierFilteredModifier : WeaponTypeFilteredModifier {; float Multiplier =>

## Assets/Scripts\ScriptableObject\Modifier\DamageMultiplierModifier.cs
- Типы: class DamageMultiplierModifier : WeaponModifier
- SerializeField: _multiplier: float
- Публичный API: class DamageMultiplierModifier : WeaponModifier {; float Multiplier =>; void Apply(ref WeaponModifierContext context)

## Assets/Scripts\ScriptableObject\Modifier\ExplosionRadiusMultiplierModifier.cs
- Типы: class ExplosionRadiusMultiplierModifier : WeaponTypeFilteredModifier
- SerializeField: _multiplier: float
- Публичный API: class ExplosionRadiusMultiplierModifier : WeaponTypeFilteredModifier {; float Multiplier =>

## Assets/Scripts\ScriptableObject\Modifier\PelletBonusModifier.cs
- Типы: class PelletBonusModifier : WeaponTypeFilteredModifier
- SerializeField: _bonus: int
- Публичный API: class PelletBonusModifier : WeaponTypeFilteredModifier {; int Bonus =>

## Assets/Scripts\ScriptableObject\Modifier\ProjectileSpeedMultiplierModifier.cs
- Типы: class ProjectileSpeedMultiplierModifier : WeaponTypeFilteredModifier
- SerializeField: _multiplier: float
- Публичный API: class ProjectileSpeedMultiplierModifier : WeaponTypeFilteredModifier {; float Multiplier =>

## Assets/Scripts\ScriptableObject\Modifier\SpreadMultiplierModifier.cs
- Типы: class SpreadMultiplierModifier : WeaponTypeFilteredModifier
- SerializeField: _multiplier: float
- Публичный API: class SpreadMultiplierModifier : WeaponTypeFilteredModifier {; float Multiplier =>

## Assets/Scripts\ScriptableObjects\RoomNoiseProfile.cs
- Типы: class RoomNoiseProfile : ScriptableObject; class SimplexNoise2D
- SerializeField: _seed: int; _offset: Vector2; _domainWarpEnabled: bool; _clearingEnabled: bool; _clearingCenter01: Vector2; _invert: bool; _applySmoothstep: bool
- Публичный API: class RoomNoiseProfile : ScriptableObject {; int PreviewResolution =>; void SetRuntimeSeed(int seed); void ClearRuntimeSeed(); float[,] GenerateNoiseMap(int width, int height); float Evaluate(float x, float y)

## Assets/Scripts\ScriptableObjects\RoomTypeProfile.cs
- Типы: class RoomTypeProfile : ScriptableObject
- SerializeField: _roomType: RoomType; _noiseProfile: RoomNoiseProfile; _largeCubeStackHeightRange: Vector2Int; _randomYawRotation: bool; _enemySpawnCountRange: Vector2Int; _enemyPrefabs: List<EnemySpawnConfig>; _objectSpawnCountRange: Vector2Int; _objectPrefabs: List<WeightedPrefab>; _nookPrefabs: List<NookPrefabConfig>
- Публичный API: class RoomTypeProfile : ScriptableObject {; RoomType RoomType =>; RoomNoiseProfile NoiseProfile =>; float BlockFillPercent =>; float LargeCubeAreaPercent =>; int MinimumStackHeightInBlocks =>; int MaximumStackHeightInBlocks =>; float HeightExponent =>; Vector2Int LargeCubeStackHeightRange =>; bool RandomYawRotation =>; Vector2Int EnemySpawnCountRange =>; IReadOnlyList<EnemySpawnConfig> EnemyPrefabs =>

## Assets/Scripts\Shop\VendingMachine\Data\IShopOffer.cs
- Типы: interface IShopOffer
- SerializeField: нет
- Публичный API: interface IShopOffer {

## Assets/Scripts\Shop\VendingMachine\Data\ModifierOffer.cs
- Типы: class ModifierOffer : ScriptableObject, IShopOffer
- SerializeField: _title: string; _icon: Sprite; _requiredItem: Item; _rarity: ModifierOfferRarity; _price: int; _modifiers: WeaponModifier[]
- Публичный API: class ModifierOffer : ScriptableObject, IShopOffer {; string Title =>; Sprite Icon =>; Item RequiredItem =>; ModifierOfferRarity Rarity =>; string RarityText =>; int Price =>; string Description =>; WeaponModifier[] Modifiers =>; bool IsCompatible(Inventory inventory)

## Assets/Scripts\Shop\VendingMachine\Data\ModifierOfferPool.cs
- Типы: class ModifierOfferPool : ScriptableObject
- SerializeField: _offers: ModifierOffer[]
- Публичный API: class ModifierOfferPool : ScriptableObject {; ModifierOffer[] Offers =>

## Assets/Scripts\Shop\VendingMachine\Data\ModifierOfferRarity.cs
- Типы: enum ModifierOfferRarity
- SerializeField: нет
- Публичный API: enum ModifierOfferRarity {

## Assets/Scripts\Shop\VendingMachine\Data\PlayerModifierOffer.cs
- Типы: class PlayerModifierOffer : ScriptableObject, IShopOffer
- SerializeField: _title: string; _description: string; _icon: Sprite; _rarity: ModifierOfferRarity; _price: int; _modifiers: PlayerModifier[]
- Публичный API: class PlayerModifierOffer : ScriptableObject, IShopOffer {; string Title =>; string Description =>; Sprite Icon =>; ModifierOfferRarity Rarity =>; int Price =>; PlayerModifier[] Modifiers =>

## Assets/Scripts\Shop\VendingMachine\Data\PlayerModifierPool.cs
- Типы: class PlayerModifierPool : ScriptableObject
- SerializeField: _offers: PlayerModifierOffer[]
- Публичный API: class PlayerModifierPool : ScriptableObject {; PlayerModifierOffer[] Offers =>

## Assets/Scripts\Shop\VendingMachine\Runtime\ModifierVendingMachine.cs
- Типы: class ModifierVendingMachine : Interactable
- SerializeField: _offerPool: ModifierOfferPool; _cardCount: int; _commonWeight: int; _rareWeight: int; _legendaryWeight: int; _disableColliderOnPurchase: bool
- Публичный API: class ModifierVendingMachine : Interactable {; int OfferCount =>; bool IsPurchased =>; ModifierOffer GetOffer(int index); string GetPrompt(); void Interact(GameObject interactor); void MarkPurchased()

## Assets/Scripts\Shop\VendingMachine\Runtime\ModifierVendingMachinePurchase.cs
- Типы: class ModifierVendingMachinePurchase : MonoBehaviour
- SerializeField: _vendingMachine: ModifierVendingMachine
- Публичный API: class ModifierVendingMachinePurchase : MonoBehaviour {; bool TryPurchase(int offerIndex, GameObject buyer)

## Assets/Scripts\Shop\VendingMachine\Runtime\PlayerModifierPurchase.cs
- Типы: class PlayerModifierPurchase : MonoBehaviour
- SerializeField: _shop: PlayerModifierShop
- Публичный API: class PlayerModifierPurchase : MonoBehaviour {; bool TryPurchase(int offerIndex, GameObject buyer)

## Assets/Scripts\Shop\VendingMachine\Runtime\PlayerModifierShop.cs
- Типы: class PlayerModifierShop : Interactable
- SerializeField: _offerPool: PlayerModifierPool; _cardCount: int; _commonWeight: int; _rareWeight: int; _legendaryWeight: int; _disableColliderOnPurchase: bool
- Публичный API: class PlayerModifierShop : Interactable {; int OfferCount =>; bool IsPurchased =>; PlayerModifierOffer GetOffer(int index); string GetPrompt(); void Interact(GameObject interactor); void MarkPurchased()

## Assets/Scripts\Shop\VendingMachine\UI\Cards\ModifierOfferCardAnimator.cs
- Типы: class ModifierOfferCardAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
- SerializeField: _cardTransform: RectTransform; _tiltTransform: RectTransform; _canvasGroup: CanvasGroup; _buyButton: Button; _revealDurationSeconds: float; _revealOffsetY: float; _revealScale: float; _hideDurationSeconds: float; _hideOffsetY: float; _hideScale: float; _hoverScale: float; _scaleSmoothSpeed: float; _tiltMaxDegrees: float; _tiltSmoothTimeSeconds: float; _clickPunchScale: float; _clickDurationSeconds: float; _ignoreTimeScale: bool
- Публичный API: class ModifierOfferCardAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler {; void SetHiddenInstant(); void CaptureBasePosition(); void PlayReveal(float delaySeconds); void PlayHide(float delaySeconds); void PlayPurchased(); void OnPointerEnter(PointerEventData eventData); void OnPointerExit(PointerEventData eventData); void OnPointerMove(PointerEventData eventData)

## Assets/Scripts\Shop\VendingMachine\UI\Cards\ModifierOfferCardView.cs
- Типы: class ModifierOfferCardView : MonoBehaviour
- SerializeField: _background: Image; _icon: Image; _titleText: Text; _priceText: Text; _descriptionText: Text; _buyButton: Button; _buyButtonText: Text; _rareCardSprite: Sprite; _epicCardSprite: Sprite; _legendaryCardSprite: Sprite
- Публичный API: class ModifierOfferCardView : MonoBehaviour {; void SetIndex(int index); void Render(IShopOffer offer, bool canBuy)

## Assets/Scripts\Shop\VendingMachine\UI\Menu\ModifierVendingMachineMenuAnimator.cs
- Типы: class ModifierVendingMachineMenuAnimator : MonoBehaviour
- SerializeField: _root: GameObject; _canvasGroup: CanvasGroup; _panelTransform: RectTransform; _cardAnimators: ModifierOfferCardAnimator[]; _panelFadeDurationSeconds: float; _panelScaleFrom: float; _panelScaleDurationSeconds: float; _cardStaggerSeconds: float; _closeFadeDurationSeconds: float; _closeCardStaggerSeconds: float; _ignoreTimeScale: bool
- Публичный API: class ModifierVendingMachineMenuAnimator : MonoBehaviour {; void PlayOpen(); void PlayClose(Action onClosed)

## Assets/Scripts\Shop\VendingMachine\UI\Menu\ModifierVendingMachineMenuOpener.cs
- Типы: class ModifierVendingMachineMenuOpener : MonoBehaviour
- SerializeField: _vendingMachine: ModifierVendingMachine; _menuView: ModifierVendingMachineMenuView
- Публичный API: class ModifierVendingMachineMenuOpener : MonoBehaviour {

## Assets/Scripts\Shop\VendingMachine\UI\Menu\ModifierVendingMachineMenuView.cs
- Типы: class ModifierVendingMachineMenuView : MonoBehaviour
- SerializeField: _root: GameObject; _blurOverlay: BlurOverlay; _pauseController: PauseController; _coinsText: TMP_Text; _cardViews: ModifierOfferCardView[]; _purchase: ModifierVendingMachinePurchase; _animator: ModifierVendingMachineMenuAnimator
- Публичный API: class ModifierVendingMachineMenuView : MonoBehaviour {; bool IsOpen =>; void HandleInteractionRequest(ModifierVendingMachine machine, GameObject buyer); void Show(ModifierVendingMachine machine, GameObject buyer); bool TryClose(); void Hide()

## Assets/Scripts\Shop\VendingMachine\UI\Menu\PlayerModifierMenuOpener.cs
- Типы: class PlayerModifierMenuOpener : MonoBehaviour
- SerializeField: _shop: PlayerModifierShop; _menuView: PlayerModifierMenuView
- Публичный API: class PlayerModifierMenuOpener : MonoBehaviour {

## Assets/Scripts\Shop\VendingMachine\UI\Menu\PlayerModifierMenuView.cs
- Типы: class PlayerModifierMenuView : MonoBehaviour
- SerializeField: _root: GameObject; _blurOverlay: BlurOverlay; _pauseController: PauseController; _coinsText: TMP_Text; _cardViews: ModifierOfferCardView[]; _purchase: PlayerModifierPurchase; _animator: ModifierVendingMachineMenuAnimator
- Публичный API: class PlayerModifierMenuView : MonoBehaviour {; bool IsOpen =>; void HandleInteractionRequest(PlayerModifierShop shop, GameObject buyer); void Show(PlayerModifierShop shop, GameObject buyer); bool TryClose(); void Hide()

## Assets/Scripts\Spawners\Core\InitialSpawnMode.cs
- Типы: enum InitialSpawnMode
- SerializeField: нет
- Публичный API: enum InitialSpawnMode {

## Assets/Scripts\Spawners\Core\ServiceLocator.cs
- Типы: class SpawnerServiceLocator
- SerializeField: нет
- Публичный API: class SpawnerServiceLocator {; void Register<T>(string key, Spawner<T> spawner) where T : MonoBehaviour {; Spawner<T> Get<T>(string key) where T : MonoBehaviour {; Spawner<T> Find<T>(string key) where T : MonoBehaviour {; void Unregister<T>(string key) where T : MonoBehaviour {

## Assets/Scripts\Spawners\Core\Spawner.cs
- Типы: class Spawner : MonoBehaviour
- SerializeField: нет
- Публичный API: class Spawner : MonoBehaviour {

## Assets/Scripts\Spawners\Core\SpawnerGeneric.cs
- Типы: class Spawner
- SerializeField: нет
- Публичный API: class Spawner<T> : Spawner where T : MonoBehaviour {; int CountActiveObjects {; T Spawn(Vector3 position); T Spawn(Vector3 position, Transform parent); void Despawn(T instance)

## Assets/Scripts\Spawners\Effects\DamagePopupSpawner.cs
- Типы: class DamagePopupSpawner : Spawner<DamagePopup>
- SerializeField: нет
- Публичный API: DamagePopup Spawn(Vector3 position); void Show(float damage, Vector3 position); void Despawn(DamagePopup popup)

## Assets/Scripts\Spawners\Effects\ParticleEffectSpawner.cs
- Типы: class ParticleEffectSpawner : Spawner<ParticleEffect>
- SerializeField: нет
- Публичный API: ParticleEffect Spawn(Vector3 position); void Despawn(ParticleEffect effect)

## Assets/Scripts\Spawners\Pickups\AmmoSpawner.cs
- Типы: class AmmoSpawner : Spawner<Ammo>, IAmmoSpawner
- SerializeField: нет
- Публичный API: class AmmoSpawner : Spawner<Ammo>, IAmmoSpawner {; Ammo Spawn(Vector3 position, Quaternion rotation, LayerMask targetLayers, Transform ignoredRoot); Ammo Spawn(Vector3 position); void Despawn(Ammo ammo)

## Assets/Scripts\Spawners\Pickups\PickupSpawner.cs
- Типы: class PickupSpawner : Spawner<BasePickup>
- SerializeField: нет
- Публичный API: BasePickup Spawn(Vector3 position); void Despawn(BasePickup pickup)

## Assets/Scripts\Spawners\Pickups\PickupSpawnPoint.cs
- Типы: class PickupSpawnPoint : MonoBehaviour
- SerializeField: _spawner: PickupSpawner; _spawnTransform: Transform; _initialSpawnMode: InitialSpawnMode; _initialDelaySeconds: float; _enableRegularSpawn: bool; _regularIntervalSeconds: float
- Публичный API: class PickupSpawnPoint : MonoBehaviour {; BasePickup SpawnNow(); void StartRegularSpawn(); void StopRegularSpawn()

## Assets/Scripts\Stats\Health.cs
- Типы: class Health : Stat
- SerializeField: _autoRegen: bool; _regenPerSecond: float; _regenDelay: float
- Публичный API: class Health : Stat {; bool AutoRegen =>; float RegenPerSecond =>; float RegenDelay =>; void Decrease(float amount); void SetAutoRegen(bool isRegen); void SetRegenPerSecond(float regenPerSecond); void SetRegenDelay(float regenDelay); void ApplyModifier(float maxValue, bool autoRegen, float regenPerSecond, float regenDelay)

## Assets/Scripts\Stats\Stamina.cs
- Типы: class Stamina : Stat
- SerializeField: _autoRegen: bool; _regenPerSecond: float; _regenDelay: float
- Публичный API: class Stamina : Stat {; bool AutoRegen =>; float RegenPerSecond =>; float RegenDelay =>; void Decrease(float amount); void SetAutoRegen(bool autoRegen); void SetRegenPerSecond(float regenPerSecond); void SetRegenDelay(float regenDelay); void ApplyModifier(float maxValue, bool autoRegen, float regenPerSecond, float regenDelay)

## Assets/Scripts\Stats\Stat.cs
- Типы: class Stat : MonoBehaviour
- SerializeField: нет
- Публичный API: class Stat : MonoBehaviour {; float MinValue =>; float MaxValue =>; float Value =>; float Normalized =>; void Increase(float amount); void Decrease(float amount); void SetValue(float newValue); void SetMaxValue(float newMaxValue); void Fill()

## Assets/Scripts\UI\Effects\DamagePopup.cs
- Типы: class DamagePopup : MonoBehaviour
- SerializeField: _text: TMP_Text; _moveYDistance: float; _moveXDistance: float; _duration: float; _scaleUpValue: float; _fadeDuration: float; _randomOffsetX: float; _randomOffsetY: float
- Публичный API: class DamagePopup : MonoBehaviour {; void Setup(float damage)

## Assets/Scripts\UI\Effects\DamagePopupOnHealth.cs
- Типы: class DamagePopupOnHealth : MonoBehaviour
- SerializeField: _health: Health; _spawnPoint: Transform; _prefab: GameObject
- Публичный API: class DamagePopupOnHealth : MonoBehaviour {; void Initialize(Health health, Transform spawnPoint)

## Assets/Scripts\UI\Effects\LookAtMainCameraAssigner.cs
- Типы: class LookAtMainCameraAssigner : MonoBehaviour
- SerializeField: нет
- Публичный API: class LookAtMainCameraAssigner : MonoBehaviour {

## Assets/Scripts\UI\Health\BossHealthOverlay.cs
- Типы: class BossHealthOverlay : MonoBehaviour
- SerializeField: нет
- Публичный API: class BossHealthOverlay : MonoBehaviour {; void Initialize(RectTransform uiRoot, RectTransform indicatorTemplate)

## Assets/Scripts\UI\Health\BossSegmentedHealthIndicator.cs
- Типы: class BossSegmentedHealthIndicator : MonoBehaviour
- SerializeField: _phaseOneFill: RectTransform; _phaseTwoFill: RectTransform; _phaseThreeFill: RectTransform
- Публичный API: class BossSegmentedHealthIndicator : MonoBehaviour {; void SetBoss(BossExcavator boss); void ClearBoss()

## Assets/Scripts\UI\Health\HealthSmoothSliderIndicator.cs
- Типы: class HealthSmoothSliderIndicator : StatIndicatorBase<Health>
- SerializeField: _slider: Slider; _duration: float
- Публичный API: нет

## Assets/Scripts\UI\Health\HealthTextIndicator.cs
- Типы: class HealthTextIndicator : StatIndicatorBase<Health>
- SerializeField: _text: TextMeshProUGUI
- Публичный API: нет

## Assets/Scripts\UI\Health\StaminaSmoothSliderIndicator.cs
- Типы: class StaminaSmoothSliderIndicator : StatIndicatorBase<Stamina>
- SerializeField: _slider: Slider; _duration: float
- Публичный API: нет

## Assets/Scripts\UI\Health\StatIndicatorBase.cs
- Типы: class StatIndicatorBase
- SerializeField: нет
- Публичный API: class StatIndicatorBase<T> : MonoBehaviour where T : Stat {; void SetStat(T stat); void ClearStat()

## Assets/Scripts\UI\Menu\Core\BaseMenuView.cs
- Типы: class BaseMenuView : MonoBehaviour
- SerializeField: нет
- Публичный API: class BaseMenuView : MonoBehaviour {; bool IsOpen {; bool IsAnimating {; void Show(); void Hide()

## Assets/Scripts\UI\Menu\Core\BlurOverlay.cs
- Типы: class BlurOverlay : MonoBehaviour
- SerializeField: _canvasGroup: CanvasGroup; _image: Image; _showDurationSeconds: float; _hideDurationSeconds: float; _showEaseCurve: AnimationCurve; _hideEaseCurve: AnimationCurve; _overrideMaterialProperties: bool; _iterations: float; _escapeRadius: float; _glow: float; _alpha: float; _scale: float; _pixelResolution: float
- Публичный API: class BlurOverlay : MonoBehaviour {; void Show(); void ShowImmediate(); void Hide(); void HideImmediate()

## Assets/Scripts\UI\Menu\Core\ButtonMenu.cs
- Типы: struct ButtonMenu
- SerializeField: нет
- Публичный API: struct ButtonMenu {

## Assets/Scripts\UI\Menu\Core\DungeonSchematicView.cs
- Типы: class DungeonSchematicView : MonoBehaviour
- SerializeField: _viewRoot: RectTransform; _spriteSource: Image; _frameColor: Color; _gridColor: Color; _roomColor: Color; _corridorColor: Color; _startColor: Color; _exitColor: Color; _cursorColor: Color
- Публичный API: class DungeonSchematicView : MonoBehaviour {; int GridWidth {; int GridHeight {; void ApplyLoadingPalette(); void PlayLoadingAnimation(); void StopAnimation(); void ClearLayout(); void ShowLayout( IReadOnlyCollection<Vector2Int> roomCells, IReadOnlyCollection<Vector2Int> corridorCells, Vector2Int startCell, Vector2Int exitCell ); void SetCurrentCell(Vector2Int currentCell)

## Assets/Scripts\UI\Menu\Core\MenuButtonScale.cs
- Типы: class MenuButtonScale : MonoBehaviour,
- SerializeField: _rect: RectTransform; _button: Button; _scaleIdle: Vector2; _scaleHover: Vector2; _scalePressed: Vector2; _durationHover: float; _durationPress: float; _durationRelease: float
- Публичный API: class MenuButtonScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {; void OnPointerEnter(PointerEventData eventData); void OnPointerExit(PointerEventData eventData); void OnPointerDown(PointerEventData eventData); void OnPointerUp(PointerEventData eventData)

## Assets/Scripts\UI\Menu\Core\SceneLoadingScreen.cs
- Типы: class SceneLoadingScreen : MonoBehaviour
- SerializeField: _blurOverlay: BlurOverlay; _schematicView: DungeonSchematicView; _canvas: Canvas; _panelCanvasGroup: CanvasGroup; _panelTransform: RectTransform; _subtitleText: TMP_Text; _enterDelaySeconds: float; _panelFadeDurationSeconds: float; _panelShowDurationSeconds: float; _panelHideDurationSeconds: float; _panelHiddenOffsetY: float; _panelHiddenScale: float; _panelShownScale: float; _subtitleDotIntervalSeconds: float; _postLoadDelaySeconds: float; _blurHideWaitSeconds: float
- Публичный API: class SceneLoadingScreen : MonoBehaviour {; void LoadScene(string sceneName); void ReloadCurrentScene()

## Assets/Scripts\UI\Menu\Exit\ExitButton.cs
- Типы: class ExitButton : MonoBehaviour
- SerializeField: _button: Button; _sceneName: string
- Публичный API: class ExitButton : MonoBehaviour {; void ExitGame()

## Assets/Scripts\UI\Menu\Exit\ExitMenuView.cs
- Типы: class ExitMenuView : BaseMenuView
- SerializeField: _canvasGroup: CanvasGroup; _panelTransform: RectTransform; _slideDistance: float; _fadeInDurationSeconds: float; _slideInDurationSeconds: float; _settleDurationSeconds: float; _shownScale: float; _fadeOutDurationSeconds: float; _slideOutDurationSeconds: float; _hiddenScale: float
- Публичный API: bool IsOpen {; bool IsAnimating {; void Show(); void Hide()

## Assets/Scripts\UI\Menu\Exit\VictoryRoundStatsView.cs
- Типы: class VictoryRoundStatsView : MonoBehaviour
- SerializeField: _statsText: TMP_Text
- Публичный API: class VictoryRoundStatsView : MonoBehaviour {; void Render(PlayerRoundStatsSnapshot snapshot)

## Assets/Scripts\UI\Menu\Main\MainMenuCursorFollower.cs
- Типы: class MainMenuCursorFollower : MonoBehaviour
- SerializeField: _cursorRectTransform: RectTransform
- Публичный API: class MainMenuCursorFollower : MonoBehaviour {

## Assets/Scripts\UI\Menu\Main\MainMenuSceneController.cs
- Типы: class MainMenuSceneController : MonoBehaviour
- SerializeField: _mainMenuView: PauseMenuView; _settingsMenuView: SettingsMenuView; _startButton: Button; _settingsButton: Button; _exitButton: Button; _backButton: Button; _levelSceneName: string
- Публичный API: class MainMenuSceneController : MonoBehaviour {

## Assets/Scripts\UI\Menu\Main\MainMenuScrapBackgroundGenerator.cs
- Типы: class MainMenuScrapBackgroundGenerator : MonoBehaviour; struct NoiseCell; struct SmallPrefabSelection
- SerializeField: _noiseProfile: RoomNoiseProfile; _floorPrefab: GameObject; _floorPrefabBaseSizeInUnits: Vector2; _floorLocalOffset: Vector3; _fallbackJunkCubePrefab: GameObject; _smallPrefabs: List<WeightedPrefab>; _largePrefabs: List<WeightedPrefab>; _sizeInBlocks: Vector2Int; _spawnOnlyExposedSmallCubes: bool; _randomYawRotation: bool; _seed: int; _generateOnAwake: bool
- Публичный API: class MainMenuScrapBackgroundGenerator : MonoBehaviour {; void Generate(); void ClearGenerated()

## Assets/Scripts\UI\Menu\Main\MainMenuSettingsPanelView.cs
- Типы: class MainMenuSettingsPanelView : MonoBehaviour
- SerializeField: _audioMixer: AudioMixer; _masterSlider: Slider; _masterValue: TMP_Text; _musicSlider: Slider; _musicValue: TMP_Text; _effectsSlider: Slider; _effectsValue: TMP_Text; _windowButton: Button; _windowButtonImage: Image; _windowButtonText: TMP_Text; _screenButton: Button; _screenButtonImage: Image; _screenButtonText: TMP_Text; _vSyncOffButton: Button; _vSyncOffButtonImage: Image; _vSyncOffButtonText: TMP_Text; _vSyncOnButton: Button; _vSyncOnButtonImage: Image; _vSyncOnButtonText: TMP_Text; _lowQualityButton: Button; _lowQualityButtonImage: Image; _lowQualityButtonText: TMP_Text; _mediumQualityButton: Button; _mediumQualityButtonImage: Image; _mediumQualityButtonText: TMP_Text; _highQualityButton: Button; _highQualityButtonImage: Image; _highQualityButtonText: TMP_Text
- Публичный API: class MainMenuSettingsPanelView : MonoBehaviour {

## Assets/Scripts\UI\Menu\Pause\PauseCameraFov.cs
- Типы: class PauseCameraFov : MonoBehaviour
- SerializeField: _camera: Camera; _pausedFovMultiplier: float; _pauseDurationSeconds: float; _resumeDurationSeconds: float; _pauseEase: Ease; _resumeEase: Ease
- Публичный API: class PauseCameraFov : MonoBehaviour {; void EnterPause(); void ExitPause()

## Assets/Scripts\UI\Menu\Pause\PauseController.cs
- Типы: class PauseController : MonoBehaviour
- SerializeField: _pauseMenuView: PauseMenuView; _baseMenuViews: List<BaseMenuView>; _pauseCameraFov: PauseCameraFov; _blurOverlay: BlurOverlay; _timeScaleSettings: TimeScaleSettings
- Публичный API: class PauseController : MonoBehaviour {; PauseController Instance {; bool IsPaused =>; void Pause(); void PauseTimeOnly(); void Resume(); void ResumeTimeOnly()

## Assets/Scripts\UI\Menu\Pause\PauseMenuButtonListener.cs
- Типы: class PauseMenuButtonListener
- SerializeField: нет
- Публичный API: class PauseMenuButtonListener {; void OnClicked()

## Assets/Scripts\UI\Menu\Pause\PauseMenuNavigation.cs
- Типы: class PauseMenuNavigation : MonoBehaviour
- SerializeField: _buttonMenus: List<ButtonMenu>
- Публичный API: class PauseMenuNavigation : MonoBehaviour {

## Assets/Scripts\UI\Menu\Pause\PauseMenuView.cs
- Типы: class PauseMenuView : BaseMenuView
- SerializeField: _canvasGroup: CanvasGroup; _panelTransform: RectTransform; _slideDirection: PauseSlideDirection; _slideDistance: float; _fadeInDurationSeconds: float; _slideInDurationSeconds: float; _settleDurationSeconds: float; _fadeOutDurationSeconds: float; _slideOutDurationSeconds: float; _hiddenScale: float; _shownScale: float
- Публичный API: class PauseMenuView : BaseMenuView {; bool IsAnimating {; bool IsOpen {; void Show(); void Hide()

## Assets/Scripts\UI\Menu\Pause\PauseSlideDirection.cs
- Типы: enum PauseSlideDirection
- SerializeField: нет
- Публичный API: enum PauseSlideDirection {

## Assets/Scripts\UI\Menu\Settings\SettingsData.cs
- Типы: struct SettingsData
- SerializeField: нет
- Публичный API: нет

## Assets/Scripts\UI\Menu\Settings\SettingsMenuView.cs
- Типы: class SettingsMenuView : BaseMenuView
- SerializeField: _canvasGroup: CanvasGroup; _panel: RectTransform; _direction: SettingsSlideDirection; _distance: float; _fadeInDuration: float; _fadeOutDuration: float; _showDuration: float; _hideDuration: float; _scaleHidden: float; _scaleShown: float
- Публичный API: class SettingsMenuView : BaseMenuView {; bool IsAnimating {; bool IsOpen {; void Show(); void Hide()

## Assets/Scripts\UI\Menu\Settings\SettingsPanelView.cs
- Типы: class SettingsPanelView : MonoBehaviour
- SerializeField: _audioMixer: AudioMixer; _masterSlider: Slider; _masterValue: TMP_Text; _musicSlider: Slider; _musicValue: TMP_Text; _effectsSlider: Slider; _effectsValue: TMP_Text; _windowButton: Button; _screenButton: Button; _vSyncOffButton: Button; _vSyncOnButton: Button; _lowQualityButton: Button; _mediumQualityButton: Button; _highQualityButton: Button; _healthOffButton: Button; _healthOnButton: Button; _damageOffButton: Button; _damageOnButton: Button; _teleportBossButton: Button; _resetButton: Button; _playerRoot: Transform; _playerBody: Transform; _levelRoot: GameObject
- Публичный API: class SettingsPanelView : MonoBehaviour {

## Assets/Scripts\UI\Menu\Settings\SettingsPresenter.cs
- Типы: class SettingsPresenter
- SerializeField: нет
- Публичный API: void Initialize(); void Dispose()

## Assets/Scripts\UI\Menu\Settings\SettingsSave.cs
- Типы: class SettingsSave
- SerializeField: нет
- Публичный API: SettingsData Load(int defaultQualityLevel, bool defaultFullScreen, bool defaultVSyncEnabled); void Save(SettingsData settingsData)

## Assets/Scripts\UI\Menu\Settings\SettingsSlideDirection.cs
- Типы: enum SettingsSlideDirection
- SerializeField: нет
- Публичный API: enum SettingsSlideDirection {

## Assets/Scripts\Utils\Colorer\ColorerEmissionRenderer.cs
- Типы: class ToonRendererEmissiveColorer
- SerializeField: нет
- Публичный API: class ToonRendererEmissiveColorer {; void LerpToEmission(Renderer renderer, Color targetColor, float duration, float intensity = 1.0f, bool useUnscaledTime = true); Color ReadBaseEmission(Renderer renderer); void Stop(Renderer renderer)

## Assets/Scripts\Utils\Colorer\ColorerGraphic.cs
- Типы: class ColorerGraphic
- SerializeField: нет
- Публичный API: class ColorerGraphic {; void LerpToColor(Graphic graphic, Color targetColor, float duration, bool useUnscaledTime); void Stop(Graphic graphic, Color baseColor, float duration, bool useUnscaledTime); void Flash(Graphic graphic, Color flashColor, Color baseColor, float flashDuration, float returnDuration, bool useUnscaledTime); void FadeToTransparency(Graphic graphic, float targetTransparency, float duration, bool useUnscaledTime); void StopFade(Graphic graphic)

## Assets/Scripts\Utils\Colorer\ColorerRenderer.cs
- Типы: class ColorerRenderer
- SerializeField: нет
- Публичный API: class ColorerRenderer {; void LerpToColor(Renderer renderer, Color targetColor, float duration, bool useUnscaledTime = true); void Stop(Renderer renderer, Color baseColor, float duration, bool useUnscaledTime = true); void FadeToTransparency(Renderer renderer, float targetTransparency, float duration, bool useUnscaledTime = true); void StopFade(Renderer renderer)

## Assets/Scripts\Utils\Colorer\IColorer.cs
- Типы: не найдены
- SerializeField: нет
- Публичный API: нет

## Assets/Scripts\Utils\FramesPerSecondDisplay.cs
- Типы: class FramesPerSecondDisplay : MonoBehaviour
- SerializeField: нет
- Публичный API: class FramesPerSecondDisplay : MonoBehaviour {

## Assets/Scripts\Utils\Texter.cs
- Типы: class Texter : MonoBehaviour
- SerializeField: _text: Text; _roomClearCanvasGroup: CanvasGroup; _roomClearTransform: RectTransform; _roomClearText: Text; _fadeDuration: float; _roomClearMessage: string; _roomClearEnterOffset: float; _roomClearHideOffset: float; _roomClearShowDuration: float; _roomClearHoldDuration: float; _roomClearHideDuration: float; _roomClearHiddenScale: float
- Публичный API: class Texter : MonoBehaviour {; void Show(string message); void ShowRoomClear(); bool CanShowRoomClear(); void Hide(); void Clear()

## Assets/Scripts\Utils\TimeScale\TImeScale.cs
- Типы: class TimeScale : TimeScaleBase
- SerializeField: нет
- Публичный API: class TimeScale : TimeScaleBase {; bool IsAnimating {; void Animate(float targetTimeScale, float durationSeconds, AnimationCurve easeCurve, bool isUnscaledUpdate); void Kill(bool complete); void ResetToDefault(); void Dispose()

## Assets/Scripts\Utils\TimeScale\TimeScaleBase.cs
- Типы: class TimeScaleBase
- SerializeField: нет
- Публичный API: class TimeScaleBase {; bool IsAnimating {; void Animate(float targetTimeScale, float durationSeconds, AnimationCurve easeCurve, bool isUnscaledUpdate); void Kill(bool complete); void ResetToDefault(); void Dispose()

## Assets/Scripts\Utils\TimeScale\TimeScaleSettings.cs
- Типы: class TimeScaleSettings : MonoBehaviour
- SerializeField: _pauseDurationSeconds: float; _resumeDurationSeconds: float; _pausedTimeScale: float; _pauseEaseCurve: AnimationCurve; _resumeEaseCurve: AnimationCurve; _baseFixedDeltaTime: float; _minPhysicsTimeScale: float
- Публичный API: class TimeScaleSettings : MonoBehaviour {; float PauseDurationSeconds =>; float ResumeDurationSeconds =>; float PausedTimeScale =>; AnimationCurve PauseEaseCurve =>; AnimationCurve ResumeEaseCurve =>; float BaseFixedDeltaTime {; float MinPhysicsTimeScale =>

## Assets/Scripts\Utils\TMPWarmup.cs
- Типы: class TMPWarmup : MonoBehaviour
- SerializeField: fontAsset: TMP_FontAsset
- Публичный API: class TMPWarmup : MonoBehaviour {

