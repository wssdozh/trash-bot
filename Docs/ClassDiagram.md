# Class Diagram

Диаграммы построены по игровому коду из `Assets/Scripts`.
Сторонние пакеты, `Library`, `Plugins` и автогенерируемые Unity-файлы не включены.

## Обзор подсистем

```mermaid
classDiagram
    direction LR

    class PlayerRoot {
        Player
        PlayerDie
        PlayerVictory
        PlayerRoundStats
    }

    class PlayerControl {
        PlayerMovement
        PlayerCombat
        PlayerInteraction
        CursorManager
    }

    class PlayerData {
        Inventory
        PlayerInventory
        BerryWallet
        CurrencyWallet
        PlayerModifierStack
        WeaponModifierStack
    }

    class Combat {
        FireExecutor
        Attacker
        AttackData
        Ammo
        Health
        Stamina
    }

    class Enemies {
        Enemy
        IEnemyBrain
        IEnemyAlert
        EnemyMeleeBrain
        EnemyDroneBrain
        Turret
    }

    class Boss {
        BossExcavator
        BossExcavatorBrain
        BossExcavatorAttack
        BossExcavatorStateMachine
        BossExcavatorConfig
    }

    class LevelGeneration {
        LevelGenerator
        LevelPlanBuilder
        LevelRoomPlacer
        LevelCorridorExecutor
        LevelGenerationProfile
    }

    class RoomGeneration {
        RoomGenerator
        RoomShellBuilder
        RoomDoorPlanner
        RoomContentSpawner
        RoomRuntimeState
    }

    class Economy {
        BasePickup
        Item
        ModifierVendingMachine
        PlayerModifierShop
        IShopOffer
    }

    class UI {
        BaseMenuView
        StatIndicatorBase
        InventoryView
        ModifierVendingMachineMenuView
    }

    PlayerRoot --> PlayerControl
    PlayerRoot --> PlayerData
    PlayerControl --> Combat
    Combat --> Enemies
    Combat --> Boss
    LevelGeneration --> RoomGeneration
    RoomGeneration --> Enemies
    Economy --> PlayerData
    Economy --> Combat
    UI --> PlayerRoot
    UI --> PlayerData
    UI --> Economy
```

## Игрок, бой и инвентарь

```mermaid
classDiagram
    direction TB

    class Player
    class PlayerMovement
    class PlayerMoveApplier
    class PlayerSprint
    class PlayerJumpAction
    class PlayerCombat
    class FireExecutor
    class BulletFireExecutor
    class RocketFireExecutor
    class ShotgunFireExecutor
    class FireExecutorPresenter
    class IShotStrategy
    class BulletShotStrategy
    class RocketShotStrategy
    class ShotgunBurstShotStrategy
    class IDamageCalculator
    class FireDamageCalculator
    class IFireRateProvider
    class FireRateProvider
    class Inventory
    class PlayerInventory
    class InventorySlot
    class InventoryWeaponBinder
    class WeaponHolder
    class WeaponGrip
    class PlayerModifierStack
    class WeaponModifierStack
    class PlayerModifierApplier
    class WeaponModifierApplier

    FireExecutor <|-- BulletFireExecutor
    FireExecutor <|-- RocketFireExecutor
    FireExecutor <|-- ShotgunFireExecutor
    IShotStrategy <|.. BulletShotStrategy
    IShotStrategy <|.. RocketShotStrategy
    IShotStrategy <|.. ShotgunBurstShotStrategy
    IDamageCalculator <|.. FireDamageCalculator
    IFireRateProvider <|.. FireRateProvider

    Player --> PlayerMovement
    PlayerMovement --> PlayerMoveApplier
    PlayerMovement --> PlayerSprint
    PlayerMovement --> PlayerJumpAction
    Player --> PlayerCombat
    PlayerCombat --> FireExecutor
    FireExecutor --> FireExecutorPresenter
    FireExecutor --> IShotStrategy
    FireExecutor --> IDamageCalculator
    FireExecutor --> IFireRateProvider

    Player --> PlayerInventory
    PlayerInventory --> Inventory
    Inventory --> InventorySlot
    InventoryWeaponBinder --> Inventory
    InventoryWeaponBinder --> WeaponHolder
    WeaponHolder --> WeaponGrip

    PlayerModifierApplier --> PlayerModifierStack
    WeaponModifierApplier --> WeaponModifierStack
    PlayerCombat --> WeaponModifierStack
    Player --> PlayerModifierStack
```

## Стрельба, снаряды и урон

```mermaid
classDiagram
    direction TB

    class Stat
    class Health
    class Stamina
    class Attacker
    class AttackData
    class AttackShapeBase
    class SphereForwardAttackShape
    class Ammo
    class Bullet
    class Rocket
    class BossScrapCubeProjectile
    class AmmoLifeListener
    class AmmoLifePath
    class AmmoEnemyAlert
    class AmmoParticleSystem
    class AmmoTrailRenderer
    class AmmoRenderersDisabler
    class AmmoReturner
    class IAmmoSpawner
    class Spawner
    class AmmoSpawner
    class BossScrapCubeSpawner
    class DamagePopupSpawner

    Stat <|-- Health
    Stat <|-- Stamina
    AttackShapeBase <|-- SphereForwardAttackShape
    Ammo <|-- Bullet
    Ammo <|-- Rocket
    Ammo <|-- BossScrapCubeProjectile
    AmmoLifeListener <|-- AmmoLifePath
    AmmoLifeListener <|-- AmmoEnemyAlert
    AmmoLifeListener <|-- AmmoParticleSystem
    AmmoLifeListener <|-- AmmoTrailRenderer
    AmmoLifeListener <|-- AmmoRenderersDisabler
    Spawner <|-- AmmoSpawner
    Spawner <|-- BossScrapCubeSpawner
    Spawner <|-- DamagePopupSpawner
    IAmmoSpawner <|.. AmmoSpawner
    IAmmoSpawner <|.. BossScrapCubeSpawner

    Attacker --> AttackData
    AttackData --> AttackShapeBase
    Ammo --> AttackData
    Ammo --> AmmoReturner
    AmmoLifeListener --> Ammo
    FireExecutor --> IAmmoSpawner
    Health --> DamagePopupSpawner
```

## Враги и босс

```mermaid
classDiagram
    direction TB

    class Enemy
    class EnemyMove
    class EnemyAnimation
    class EnemyRotator
    class EnemyAimCollider
    class EnemyDebugView
    class IEnemyBrain
    class IEnemyAlert
    class EnemyMeleeBrain
    class EnemyDroneBrain
    class EnemySteering
    class EnemyDroneMove
    class EnemySuicideAttack
    class EnemyRoomAlert
    class EnemyRoomLock
    class Turret
    class TargetVision
    class TargetRotator
    class IdleRotator

    IEnemyBrain <|.. EnemyMeleeBrain
    IEnemyBrain <|.. EnemyDroneBrain
    IEnemyAlert <|.. EnemyMeleeBrain
    IEnemyAlert <|.. EnemyDroneBrain
    IEnemyAlert <|.. Turret

    Enemy --> EnemyMove
    Enemy --> EnemyAnimation
    Enemy --> EnemyRotator
    Enemy --> EnemyAimCollider
    Enemy --> IEnemyBrain
    EnemyRoomAlert --> IEnemyAlert
    EnemyRoomLock --> Enemy
    EnemyMeleeBrain --> EnemySteering
    EnemyMeleeBrain --> EnemySuicideAttack
    EnemyDroneBrain --> EnemyDroneMove
    Turret --> TargetVision
    Turret --> TargetRotator
    Turret --> IdleRotator
```

```mermaid
classDiagram
    direction TB

    class BossExcavator
    class BossExcavatorBrain
    class BossExcavatorStateMachine
    class BossExcavatorConfig
    class BossExcavatorMove
    class BossExcavatorAim
    class BossExcavatorArm
    class BossExcavatorAttack
    class BossExcavatorBucketAttack
    class BossExcavatorChargeAttack
    class BossExcavatorScrapTrailAttack
    class BossExcavatorSweepAttack
    class BossExcavatorThrowAttack
    class BossExcavatorPhaseThreeController
    class BossExcavatorPhaseThreeMinion
    class BossScrapTrailBlockSpawner
    class BossScrapCubeSpawner

    BossExcavatorAttack <|-- BossExcavatorBucketAttack
    BossExcavatorAttack <|-- BossExcavatorChargeAttack
    BossExcavatorAttack <|-- BossExcavatorScrapTrailAttack
    BossExcavatorAttack <|-- BossExcavatorSweepAttack
    BossExcavatorAttack <|-- BossExcavatorThrowAttack

    BossExcavator --> BossExcavatorBrain
    BossExcavator --> BossExcavatorStateMachine
    BossExcavator --> BossExcavatorConfig
    BossExcavator --> BossExcavatorMove
    BossExcavator --> BossExcavatorAim
    BossExcavator --> BossExcavatorArm
    BossExcavatorBrain --> BossExcavatorAttack
    BossExcavatorBrain --> BossExcavatorStateMachine
    BossExcavatorScrapTrailAttack --> BossScrapTrailBlockSpawner
    BossExcavatorThrowAttack --> BossScrapCubeSpawner
    BossExcavatorPhaseThreeController --> BossExcavatorPhaseThreeMinion
```

## Уровни и комнаты

```mermaid
classDiagram
    direction TB

    class LevelGenerator
    class LevelGenerationContext
    class LevelGenerationProfile
    class LevelSequenceProfile
    class LevelRoomPrefabLibrary
    class LevelPlanBuilder
    class LevelRoomPlacer
    class LevelRoomShellInstantiator
    class LevelRoomFinalizer
    class LevelCorridorExecutor
    class LevelCorridorBoundsBuilder
    class LevelRoomStreamer
    class LevelRuntimeNavMesh
    class RoomGenerator
    class RoomShellBuilder
    class RoomInteriorBlockFiller
    class RoomDoorPlanner
    class RoomPassagePlanner
    class RoomContentSpawner
    class RoomNookSpawner
    class RoomRuntimeState
    class RoomCombatLock
    class RoomEnterTrigger
    class RoomDoorGate
    class RoomDoorMarker

    LevelGenerator --> LevelGenerationProfile
    LevelGenerator --> LevelGenerationContext
    LevelGenerator --> LevelPlanBuilder
    LevelGenerator --> LevelRoomPlacer
    LevelGenerator --> LevelRoomShellInstantiator
    LevelGenerator --> LevelRoomFinalizer
    LevelGenerator --> LevelCorridorExecutor
    LevelGenerator --> LevelRuntimeNavMesh
    LevelGenerationProfile --> LevelSequenceProfile
    LevelGenerationProfile --> LevelRoomPrefabLibrary
    LevelCorridorExecutor --> LevelCorridorBoundsBuilder
    LevelRoomStreamer --> RoomRuntimeState

    RoomGenerator --> RoomShellBuilder
    RoomGenerator --> RoomDoorPlanner
    RoomGenerator --> RoomPassagePlanner
    RoomGenerator --> RoomContentSpawner
    RoomGenerator --> RoomNookSpawner
    RoomGenerator --> RoomRuntimeState
    RoomShellBuilder --> RoomInteriorBlockFiller
    RoomRuntimeState --> RoomCombatLock
    RoomEnterTrigger --> RoomRuntimeState
    RoomDoorGate --> RoomDoorMarker
```

## Предметы, модификаторы, магазин и UI

```mermaid
classDiagram
    direction TB

    class BasePickup
    class BaseAnimatedPickup
    class BerryPickup
    class CurrencyPickup
    class ItemPickup
    class PickupSpawner
    class PickupReturner
    class Item
    class ItemEffect
    class FoodEffect
    class WeaponModifier
    class WeaponTypeFilteredModifier
    class DamageMultiplierModifier
    class DamageMultiplierFilteredModifier
    class CriticalHitModifier
    class ProjectileSpeedMultiplierModifier
    class PlayerModifier
    class PlayerMultiplierModifier
    class PlayerHealthRegenUnlock
    class IShopOffer
    class ModifierOffer
    class PlayerModifierOffer
    class ModifierOfferPool
    class PlayerModifierPool
    class ModifierVendingMachine
    class PlayerModifierShop
    class ModifierVendingMachinePurchase
    class PlayerModifierPurchase
    class ModifierVendingMachineMenuView
    class PlayerModifierMenuView
    class ModifierOfferCardView
    class BaseMenuView
    class PauseMenuView
    class ExitMenuView
    class SettingsMenuView
    class StatIndicatorBase
    class HealthSmoothSliderIndicator
    class StaminaSmoothSliderIndicator
    class HealthTextIndicator

    BasePickup <|-- BaseAnimatedPickup
    BaseAnimatedPickup <|-- BerryPickup
    BaseAnimatedPickup <|-- CurrencyPickup
    BaseAnimatedPickup <|-- ItemPickup
    ItemEffect <|-- FoodEffect
    WeaponModifier <|-- WeaponTypeFilteredModifier
    WeaponModifier <|-- DamageMultiplierModifier
    WeaponTypeFilteredModifier <|-- DamageMultiplierFilteredModifier
    WeaponTypeFilteredModifier <|-- CriticalHitModifier
    WeaponTypeFilteredModifier <|-- ProjectileSpeedMultiplierModifier
    PlayerModifier <|-- PlayerMultiplierModifier
    PlayerModifier <|-- PlayerHealthRegenUnlock
    IShopOffer <|.. ModifierOffer
    IShopOffer <|.. PlayerModifierOffer
    BaseMenuView <|-- PauseMenuView
    BaseMenuView <|-- ExitMenuView
    BaseMenuView <|-- SettingsMenuView
    StatIndicatorBase <|-- HealthSmoothSliderIndicator
    StatIndicatorBase <|-- StaminaSmoothSliderIndicator
    StatIndicatorBase <|-- HealthTextIndicator

    ItemPickup --> Item
    Item --> ItemEffect
    PickupSpawner --> BasePickup
    BasePickup --> PickupReturner
    ModifierOffer --> WeaponModifier
    PlayerModifierOffer --> PlayerModifier
    ModifierOfferPool --> ModifierOffer
    PlayerModifierPool --> PlayerModifierOffer
    ModifierVendingMachine --> ModifierVendingMachinePurchase
    PlayerModifierShop --> PlayerModifierPurchase
    ModifierVendingMachineMenuView --> ModifierOfferCardView
    PlayerModifierMenuView --> ModifierOfferCardView
```
