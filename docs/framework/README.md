# MobileForge Framework Architecture

MobileForge is a reusable, cross-engine framework for building mobile games. You write game logic once against a shared domain layer, then run it on Godot 4 or Unity with thin engine-specific adapters. Shared JSON schemas and test vectors guarantee both implementations behave identically.

## 3-Layer Architecture

```
┌─────────────────────────────────────────────────┐
│                 PRESENTATION                     │
│  UI scenes/prefabs, input handling, animation    │
│  (Godot: Nodes/Scenes  |  Unity: MonoBehaviours)│
├─────────────────────────────────────────────────┤
│                    DOMAIN                        │
│  Pure game logic, rules, calculations            │
│  NO engine imports — plain GDScript / plain C#   │
├─────────────────────────────────────────────────┤
│                INFRASTRUCTURE                    │
│  Engine singletons: EventBus, GameData,          │
│  PlayerState, SaveManager, NetworkClient,        │
│  AudioManager                                    │
└─────────────────────────────────────────────────┘

         Events flow UP via EventBus
         Calls flow DOWN only
```

## Hard Rules

1. **No upward calls.** Infrastructure never imports domain. Domain never imports presentation. Only downward dependencies.
2. **Domain has zero engine imports.** No `Node`, no `MonoBehaviour`, no `Engine`, no `UnityEngine`. Pure language only.
3. **Events flow up via EventBus.** Lower layers emit events. Upper layers subscribe. No direct callbacks across layer boundaries.
4. **One responsibility per module.** If a class touches two concerns, split it.

## Module List

### Infrastructure Layer

| Module          | Purpose                                      |
|-----------------|----------------------------------------------|
| EventBus        | Pub/sub message broker for decoupled comms   |
| GameData        | Read-only definition storage (loaded from JSON) |
| PlayerState     | Mutable player data organized in sections    |
| SaveManager     | Serialize/deserialize player state to disk   |
| NetworkClient   | HTTP requests with retry and auth headers    |
| AudioManager    | Sound/music playback with pooling            |

### Domain Layer

| Module          | Purpose                                            |
|-----------------|----------------------------------------------------|
| Board           | Gem grid logic, swap, cascade resolution           |
| Combat          | Damage calculation, element modifiers, hook chain  |
| SkillPipeline   | Condition-outcome skill composition and execution  |
| Enemy           | Enemy AI, countdown timers, action decisions       |
| Dungeon         | Dungeon run orchestrator (turns, waves, rewards)   |
| Monster         | Monster instances, leveling, fusion, evolution     |
| Team            | Team slots, validation, stat aggregation           |
| Economy         | Earn, spend, check-afford, stamina                 |
| Gacha           | Banner definitions, weighted pull logic, pity      |
| Loot            | Loot table rolling, drop generation                |

### Presentation Layer

| Module                    | Purpose                                         |
|---------------------------|-------------------------------------------------|
| UIRouter                  | Screen navigation, push/pop, transitions        |
| ScreenRegistry/BaseScreen | Screen registration and base class for screens  |
| PopupStack/BasePopup      | Popup queue with priority, base class for popups|
| ToastLayer                | Transient toast notifications                   |
| OverlayManager            | Persistent overlay panels (e.g., debug, chat)   |
| VirtualList               | Recycling vertical scroll list                  |
| GridView                  | Recycling grid layout                           |
| CurrencyBar               | Top bar currency display with live updates       |
| CardView                  | Monster card rendering (instance or definition) |

## Cross-Engine Parity

Both Godot 4 and Unity implementations are built from the same design documents. Parity is enforced by:

- **Shared JSON schemas**: Definition files, save files, and network payloads use identical JSON structures.
- **Shared test vectors**: JSON files containing input/output pairs that both engines run against.
- **Naming alignment**: Module names, method signatures, and event names map 1:1 (with casing conventions applied per language).

See [CROSS_ENGINE_GUIDE.md](CROSS_ENGINE_GUIDE.md) for the full GDScript-to-C# translation table.

## Extension Model

### Simple Effects: Callable Registry

Register a function by name. The framework calls it by key with a payload dictionary.

```gdscript
# Godot
EffectRegistry.register("grant_gold", func(params):
    CurrencyService.add("gold", params["amount"])
)
```

```csharp
// Unity
EffectRegistry.Register("grant_gold", (params) => {
    CurrencyService.Add("gold", (int)params["amount"]);
});
```

### Complex Behaviors: Abstract Base Class

Subclass `BaseEffect` (GDScript) / `BaseEffect` (C#) for multi-step logic with setup/teardown.

```gdscript
# Godot
class_name BuffEffect extends BaseEffect

func apply(target, params: Dictionary) -> void:
    # add stat modifier
func remove(target) -> void:
    # remove stat modifier
func tick(delta: float) -> void:
    # count down duration
```

## Initialization Order

Infrastructure modules initialize in this fixed order:

```
1. EventBus          (no dependencies)
2. GameData          (depends on EventBus)
3. PlayerState       (depends on EventBus)
4. SaveManager       (depends on PlayerState, EventBus)
5. NetworkClient     (depends on EventBus)
6. AudioManager      (depends on EventBus, GameData)
```

Domain services initialize after all infrastructure is ready. They subscribe to events in their own `initialize()` method.

## 3-Layer Dependency Graph

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                              │
│                                                                         │
│  ┌──────────┐  ┌───────────┐  ┌───────────┐  ┌───────────────────┐    │
│  │ UIRouter  │  │PopupStack │  │ToastLayer │  │OverlayManager    │    │
│  │ uses:     │  │ uses:     │  │ uses:     │  │ uses:             │    │
│  │  EventBus │  │  EventBus │  │  EventBus │  │  EventBus         │    │
│  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘  └────────┬──────────┘    │
│        │              │              │                   │              │
│  ┌─────┴──────┐  ┌────┴──────┐  ┌───┴───────┐  ┌──────┴──────────┐   │
│  │VirtualList │  │ GridView  │  │CurrencyBar│  │    CardView     │   │
│  │ uses:      │  │ uses:     │  │ uses:     │  │ uses:           │   │
│  │  (none)    │  │  (none)   │  │  EventBus │  │  GameData       │   │
│  │            │  │           │  │  PlayerSt │  │                 │   │
│  └────────────┘  └───────────┘  └───────────┘  └─────────────────┘   │
│                                                                         │
│  ┌──────────────────┐  ┌──────────────────┐                            │
│  │   BaseScreen     │  │   BasePopup      │                            │
│  │ uses: UIRouter   │  │ uses: PopupStack │                            │
│  └──────────────────┘  └──────────────────┘                            │
└─────────────────────────────────┬───────────────────────────────────────┘
                              │ subscribes to EventBus
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           DOMAIN LAYER                                  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────┐      │
│  │                    DungeonRunner (ORCHESTRATOR)                │      │
│  │  calls → BoardLogic, CombatResolver, SkillPipeline,          │      │
│  │          EnemyAI, Economy, LootTable                          │      │
│  └──────────────────────────┬────────────────────────────────────┘      │
│     ┌───────────┬───────────┼───────┬──────────┬────────────┐          │
│     ▼           ▼           ▼       ▼          ▼            ▼          │
│  BoardLogic  CombatRes.  SkillPipe EnemyAI  Economy    LootTable      │
│  MonsterManager          TeamBuilder          GachaRoller              │
└─────────────────────────────┬───────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        INFRASTRUCTURE LAYER                             │
│  EventBus   GameData   PlayerState   SaveManager   NetworkClient       │
│  AudioManager                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

## End-to-End Data Flow Example

**"User drags gem" flow:**

1. User drags gem -- Presentation (BoardView) translates touch to grid index
2. BoardView calls Domain (BoardLogic.swap_gems, BoardLogic.resolve_cascade)
3. Domain returns Array[CascadeStep] -- pure data, no waiting
4. BoardView animates the CascadeSteps sequentially
5. BattleScreen calls DungeonRunner.execute_player_turn(cascade_steps)
6. DungeonRunner calls CombatResolver, SkillPipeline, EnemyAI internally
7. DungeonRunner emits EventBus signals (damage_dealt, enemy_killed, etc.)
8. Presentation subscribes and animates damage numbers, HP bars, etc.

## Module Interface Contracts

### Infrastructure

| Module | Public Methods | Inputs | Outputs | Side Effects |
|---|---|---|---|---|
| EventBus | subscribe, unsubscribe, emit, clear_all | StringName + Callable + Dictionary | void | Stores callback references |
| GameData | load_definitions, get_definition, get_all_definitions, has_definition | StringName + int | Dictionary/Array | Loads JSON once, read-only after |
| PlayerState | set_value, get_value, get_section, to_save_dict, from_save_dict | StringName + Variant | Variant/Dictionary | Mutates state, emits events |
| SaveManager | save, load, has_save, register_migrator, register_saveable | int + Callable | Error | Disk I/O |
| NetworkClient | request, set_base_url, set_auth_token | String + Dictionary | async Response | HTTP I/O |
| AudioManager | play_sfx, play_bgm, stop_bgm, preload_audio | StringName + float | void | Audio playback |

### Domain

| Module | Public Methods | Inputs | Outputs | Side Effects |
|---|---|---|---|---|
| BoardLogic | init_board, swap_gems, move_gem_path, resolve_cascade, detect_matches, change_gem_element | BoardConfig, positions | CascadeStep[], MatchResult[] | Mutates internal grid |
| CombatResolver | resolve_player_attack, resolve_enemy_attack, register_hook, unregister_hook | DamageContext, DamageHook | DamageResult | Hook registration |
| SkillPipeline | register_condition_type, register_outcome_type, activate_skill, process_turn_end | SkillDef, SkillContext | SkillResult | Registers hooks |
| EnemyAI | tick_countdowns, decide_actions | Array[EnemyState] | Array[EnemyAction] | None (pure) |
| DungeonRunner | start, execute_player_turn, execute_enemy_turn, activate_skill | DungeonDef, CascadeStep[] | TurnResult | Mutates state, emits events |
| GachaRoller | roll, roll_multi, get_displayed_rates | GachaPool, pity, rng | GachaResult | None (pure) |
| MonsterManager | create_instance, level_up, fuse, evolve, get_stats | MonsterDef/Instance | MonsterInstance | Mutates instance |
| TeamBuilder | set_slot, validate, get_team_stats | int, MonsterInstance | bool, TeamStats | Mutates team |
| Economy | can_afford, spend, earn, check_stamina | StringName, int | bool | Mutates PlayerState |
| LootTable | roll_drops | LootTableDef, rng | Array[LootDrop] | None (pure) |

### Presentation

| Module | Public Methods | Inputs | Outputs | Side Effects |
|---|---|---|---|---|
| UIRouter | register, navigate, push, pop, replace | StringName, Callable, Dictionary | void | Creates/destroys screens |
| PopupStack | show, dismiss, dismiss_all | StringName, Callable, priority | BasePopup | Creates popups |
| ToastLayer | show | text, icon, duration, type | void | Creates toast nodes |
| OverlayManager | show, hide, hide_all | StringName, Callable | void | Creates/destroys overlays |
| VirtualList | set_data | Array, Callable, float | void | Recycles children |
| GridView | set_data | Array, Callable, int | void | Recycles children |
| CurrencyBar | bind | StringName | void | Subscribes to EventBus |
| CardView | bind, bind_def | MonsterInstance/MonsterDef | void | Updates visuals |

## Intra-Module Dependency Pattern

Every module folder contains a `*_types.gd` file that serves as the leaf node with zero dependencies. This types file defines the data structures (classes, enums, constants) used throughout the module.

Logic files within a module import only their own `_types` file. For example, `board_logic.gd` imports `board_types.gd`, and `combat_resolver.gd` imports `combat_types.gd`. This keeps each module self-contained.

Cross-module dependencies exist only at the **orchestrator level**. `DungeonRunner` is the primary orchestrator -- it imports `BoardLogic`, `CombatResolver`, `SkillPipeline`, `EnemyAI`, `Economy`, and `LootTable` to coordinate a dungeon run. `SkillPipeline` is a secondary orchestrator that may reference `CombatResolver` (for hook registration) and `BoardLogic` (for board mutations). No other domain module imports from another domain module.

## Further Reading

- [GETTING_STARTED.md](GETTING_STARTED.md) — Setup guide for both engines
- [CROSS_ENGINE_GUIDE.md](CROSS_ENGINE_GUIDE.md) — GDScript / C# translation reference
- [infrastructure/README.md](infrastructure/README.md) — Infrastructure layer deep dive
- [domain/README.md](domain/README.md) — Domain layer deep dive
- [presentation/README.md](presentation/README.md) — Presentation layer deep dive
