# Architecture Diagrams

Simplified architecture view. These diagrams intentionally do not mirror every class 1:1.
They show the main gameplay systems, data flow, ownership, and runtime dependencies.

## Overview

```mermaid
flowchart LR
    Input[Input] --> Player[Player]
    Player --> Combat[Combat]
    Player --> Inventory[Inventory and Wallets]
    Player --> Interaction[Interaction]
    Camera[Camera] --> Player

    Profiles[ScriptableObject Profiles] --> Level[Level Generation]
    Profiles --> Room[Room Runtime]
    Profiles --> Combat
    Profiles --> Shops[Shops and Modifiers]

    Level --> Room
    Room --> Enemies[Enemies and Boss]
    Room --> Pickups[Pickups and Spawners]
    Room --> Combat

    Combat --> Health[Health and Stats]
    Enemies --> Combat
    Pickups --> Inventory
    Shops --> Inventory

    Health --> Feedback[Feedback and Effects]
    Inventory --> UI[UI]
    Combat --> UI
    Room --> UI
    Settings[Settings and Save] --> UI
```

## Runtime Flow

```mermaid
flowchart TD
    Menu[Main Menu] --> Settings[Settings Save]
    Menu --> Load[Scene Loading Screen]
    Load --> Generate[Generate Level]
    Generate --> BuildRooms[Build Rooms and Corridors]
    BuildRooms --> SpawnContent[Spawn Content]
    SpawnContent --> StartRoom[Place Player]
    StartRoom --> Explore[Explore Rooms]
    Explore --> EnterRoom[Enter Combat Room]
    EnterRoom --> LockRoom[Room Combat Lock]
    LockRoom --> Fight[Player vs Enemies]
    Fight --> ClearRoom[Room Cleared]
    ClearRoom --> Rewards[Pickups, Currency, Modifiers]
    Rewards --> Explore
    Fight --> Death[Player Death]
    Explore --> Victory[Victory]
    Death --> RoundStats[Round Stats UI]
    Victory --> RoundStats
```

## Level And Rooms

```mermaid
flowchart LR
    LevelProfiles[Level Profiles] --> LevelGenerator[Level Generator]
    LevelGenerator --> LevelPlan[Level Plan Builder]
    LevelPlan --> RoomPlacement[Room Placement]
    RoomPlacement --> CorridorBuild[Corridor Build]
    RoomPlacement --> RoomGenerator[Room Generator]

    RoomProfiles[Room Profiles] --> RoomGenerator
    RoomGenerator --> DoorPlan[Door and Passage Planning]
    RoomGenerator --> Shell[Room Shell]
    RoomGenerator --> Interior[Interior Blocks and Chunks]
    RoomGenerator --> Content[Room Content Spawner]
    RoomGenerator --> Runtime[Room Runtime State]

    Content --> Enemies[Enemy Spawn Points]
    Content --> Pickups[Pickups and Props]
    Runtime --> CombatLock[Combat Lock and Gates]
    Runtime --> Streamer[Room Streaming]
    Runtime --> NavMesh[Runtime NavMesh]
```

## Combat

```mermaid
flowchart LR
    PlayerCombat[Player Combat] --> Weapon[Weapon Holder]
    Weapon --> Fire[Fire Executors]
    Fire --> Strategy[Shot Strategies]
    Strategy --> Ammo[Ammo and Projectiles]
    Strategy --> AttackShape[Melee Attack Shape]

    Enemies[Enemy Brains] --> EnemyMove[Enemy Movement]
    Enemies --> EnemyAttack[Enemy Attacks]
    Boss[Boss Brain] --> BossAttacks[Boss Attacks]
    Boss --> BossMovement[Boss Movement and Aim]

    Ammo --> Damage[Damage Calculation]
    AttackShape --> Damage
    EnemyAttack --> Damage
    BossAttacks --> Damage
    Damage --> Health[Health and Stats]
    Health --> Death[Death, Drops, Room State]
    Health --> Feedback[Feedback, Particles, UI]
```

## Player

```mermaid
flowchart TD
    Player[Player Facade] --> Movement[Movement]
    Player --> Combat[Combat]
    Player --> Inventory[Inventory]
    Player --> Interaction[Interaction]
    Player --> Pause[Pause]
    Player --> Victory[Victory]

    Movement --> CharacterMover[Character Mover]
    Movement --> JumpSprint[Jump and Sprint]
    Movement --> Rotation[Rotation and Animation Blend]

    Combat --> Ranged[Ranged Fire]
    Combat --> Melee[Melee Attack]
    Combat --> BattleState[Battle State and Timer]

    Inventory --> WeaponBind[Weapon Binding]
    Inventory --> Wallets[Currency and Berry Wallets]
    Interaction --> Interactables[Doors, Pickups, Shops]

    Wallets --> UI[HUD and Views]
    BattleState --> UI
    Pause --> UI
    Victory --> UI
```

## UI And Economy

```mermaid
flowchart LR
    UI[UI Layer] --> HUD[HUD Overlays]
    UI --> Menus[Menu Views]
    UI --> SettingsPanel[Settings Panel]
    UI --> ShopViews[Shop Views]

    HUD --> HealthViews[Health and Stamina Indicators]
    HUD --> WalletViews[Wallet Views]
    HUD --> InventoryViews[Inventory Views]
    HUD --> RoomViews[Room and Enemy Indicators]

    SettingsPanel --> SettingsPresenter[Settings Presenter]
    SettingsPresenter --> SettingsSave[Settings Save]

    ShopViews --> Vending[Modifier Vending Machine]
    ShopViews --> PlayerShop[Player Modifier Shop]
    Vending --> ModifierStack[Weapon Modifier Stack]
    PlayerShop --> PlayerModifierStack[Player Modifier Stack]

    Pickups[Pickups] --> Wallets[Wallets]
    Wallets --> WalletViews
    ModifierStack --> Combat[Combat]
    PlayerModifierStack --> Player[Player]
```

## Boss

```mermaid
flowchart TD
    Boss[Boss Excavator] --> Brain[Boss Brain]
    Boss --> StateMachine[State Machine]
    Boss --> Move[Move]
    Boss --> Aim[Aim]
    Boss --> Arm[Arm]
    Boss --> Health[Health]

    Brain --> AttackSelection[Attack Selection]
    Brain --> AttackQueue[Attack Queue]
    Brain --> PhaseRules[Phase Rules]
    Brain --> StateMachine

    AttackSelection --> Bucket[Bucket Attack]
    AttackSelection --> Charge[Charge Attack]
    AttackSelection --> Sweep[Sweep Attack]
    AttackSelection --> Throw[Throw Attack]
    AttackSelection --> ScrapTrail[Scrap Trail]

    Health --> PhaseController[Phase Three Controller]
    PhaseController --> Minions[Minion Spawn Points]
    Throw --> ScrapCubes[Scrap Cube Spawner]
    ScrapTrail --> TrailBlocks[Scrap Trail Blocks]
    Boss --> RoomLock[Room Combat Lock]
```
