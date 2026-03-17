# Tower of Saviors - Architecture Document

A match-3 puzzle RPG game with dual implementations in Unity (C#) and Godot (GDScript). Both versions share the same JSON data definitions and core game logic, built on top of the MobileForge framework.

## Table of Contents
- [Overview](#overview)
- [Project Structure](#project-structure)
- [Core Architecture](#core-architecture)
- [Major Systems](#major-systems)
- [Skill System](#skill-system)
- [Battle Flow](#battle-flow)
- [Data Layer](#data-layer)
- [UI/Screen System](#uiscreen-system)
- [Unity vs Godot Comparison](#unity-vs-godot-comparison)
- [Testing](#testing)

---

## Overview

Tower of Saviors is a match-3 puzzle RPG inspired by Puzzle & Dragons. The game features:
- **Match-3 Puzzle Board**: 5x6 grid with drag-to-swap mechanics
- **Card Collection**: Monster collection via gacha system
- **Team Building**: 5-member teams with friend helper support
- **Turn-based Combat**: Element-based damage with combo multipliers
- **Skill System**: Active skills, leader skills, and team skills

Both Unity and Godot implementations follow the same architectural patterns:
- **Framework-dependent**: Built on MobileForge framework modules
- **Data-driven**: All game content defined in shared JSON files
- **Separation of concerns**: Domain logic separate from presentation

---

## Project Structure

```
tower-of-saviors/
├── shared/                    # Platform-agnostic data
│   └── data/
│       ├── monsters.json      # Monster definitions
│       ├── skills.json        # Active skill definitions
│       ├── leader_skills.json # Leader skill definitions
│       ├── team_skills.json   # Passive team skill definitions
│       ├── stages.json        # Dungeon/stage definitions
│       ├── gacha_pools.json   # Gacha pool configurations
│       ├── gem_modifiers.json # Board gem modifiers
│       ├── element_chart.json # Element advantage chart
│       ├── loot_tables.json   # Drop tables for rewards
│       ├── event_shops.json   # Event shop definitions
│       └── monster_exchange.json # Monster exchange configs
│
├── unity/TowerOfSaviors/      # Unity C# implementation
│   ├── TosGame.cs             # Main entry point
│   ├── Screens/               # Screen controllers
│   │   ├── TitleScreen.cs
│   │   ├── BattleScreen.cs
│   │   ├── TeamSelectScreen.cs
│   │   ├── DungeonSelectScreen.cs
│   │   ├── GachaScreen.cs
│   │   ├── MonsterBoxScreen.cs
│   │   ├── ShopScreen.cs
│   │   └── ResultScreen.cs
│   ├── Battle/                # Battle-specific view models
│   │   ├── BoardViewModel.cs
│   │   ├── EnemyViewModel.cs
│   │   ├── ComboTracker.cs
│   │   └── DamageEvent.cs
│   ├── SkillDefs/             # ToS-specific skill implementations
│   │   ├── RegisterAll.cs     # Skill registration
│   │   ├── Conditions/        # Skill conditions
│   │   └── Outcomes/          # Skill outcomes
│   └── Tests/                 # NUnit integration tests
│
└── godot/game/                # Godot GDScript implementation
    ├── tos_game.gd            # Main entry point (autoload)
    ├── tos_theme.gd           # Visual constants and styling
    ├── screens/               # Screen controllers
    │   ├── title_screen.gd
    │   ├── battle_screen.gd
    │   ├── team_select_screen.gd
    │   ├── dungeon_select_screen.gd
    │   ├── gacha_screen.gd
    │   ├── monster_box_screen.gd
    │   ├── shop_screen.gd
    │   └── result_screen.gd
    ├── battle/                # Battle-specific views
    │   ├── board_view.gd      # Interactive puzzle board
    │   ├── enemy_view.gd      # Enemy display component
    │   ├── combo_display.gd   # Combo counter popup
    │   └── damage_label.gd    # Floating damage numbers
    ├── ui/
    │   └── tos_card.gd        # Monster card component
    ├── skill_defs/            # ToS-specific skill implementations
    │   ├── register_all.gd
    │   ├── conditions/
    │   └── outcomes/
    └── tests/                 # GdUnit test scripts
```

---

## Core Architecture

### Entry Point Pattern

Both versions use a central game class that:
1. Initializes framework singletons (EventData, PlayerState, EventBus)
2. Loads all JSON data definitions
3. Creates domain modules (MonsterManager, Economy, SkillPipeline)
4. Registers game-specific skills
5. Sets up screen routing
6. Navigates to the title screen

**Unity** (`TosGame.cs`):
```csharp
public class TosGame
{
    private readonly GameData _gameData;
    private readonly PlayerState _playerState;
    private readonly EventBus _eventBus;
    private readonly Economy _economy;
    private readonly MonsterManager _monsterManager;
    private readonly SkillPipeline _skillPipeline;
    private readonly UIRouter _uiRouter;
    // ...
}
```

**Godot** (`tos_game.gd`):
```gdscript
extends Node
var _game_data: Node
var _player_state: Node
var _event_bus: Node
var _economy: MFEconomy
var _monster_manager: MFMonsterManager
var _skill_pipeline: MFSkillPipeline
var _ui_router: Node
```

### Framework Dependencies

Both implementations depend on MobileForge framework modules:

| Module | Purpose |
|--------|---------|
| `GameData` | JSON definition loading and lookup |
| `PlayerState` | Persistent player data storage |
| `EventBus` | Global event publishing/subscription |
| `Economy` | Currency management (gems, coins, stamina) |
| `MonsterManager` | Monster definitions, instances, stats |
| `SkillPipeline` | Skill activation and effect processing |
| `DungeonRunner` | Battle state machine |
| `BoardLogic` | Match-3 puzzle board logic |
| `CombatResolver` | Damage calculation with element chart |
| `CascadeResolver` | Match detection and cascade resolution |
| `UIRouter` | Screen navigation stack |
| `SaveManager` | Persistent save/load |

---

## Major Systems

### 1. Match-3 Puzzle System

The puzzle board is a 5x6 grid (configurable per dungeon) with 6 elements:
- **Water** (1) - Cyan
- **Fire** (2) - Red
- **Earth** (3) - Green
- **Light** (4) - Yellow
- **Dark** (5) - Magenta
- **Heart** (6) - Pink (healing)

**Board Logic** (`MFBoardLogic`):
- Grid state management with `Gem` objects
- Element ID storage per cell
- Gem swapping and validation
- Board serialization (`to_element_array`, `from_element_array`)

**Cascade Resolution** (`MFCascadeResolver`):
1. Find all 3+ matches (horizontal and vertical)
2. Remove matched gems, record `CascadeStep`
3. Drop gems to fill gaps
4. Spawn new gems at top
5. Repeat until no matches

**Drag Mechanics** (Godot `board_view.gd`):
- PAD-style drag: pick up gem, trace path swapping along the way
- 5-second move time limit
- Trail visualization on visited cells
- Drag indicator follows cursor with 1.06x scale

### 2. Battle System

**Dungeon Runner** (`MFDungeonRunner`):
- Manages dungeon state (waves, enemies, team HP)
- Executes player/enemy turns
- Handles wave progression
- Tracks persistent skill outcomes

**Combat Resolver** (`MFCombatResolver`):
```
Damage Formula:
  base_damage = atk * (1 + 0.25 * (gems_matched - 3))
  combo_mult = 1.0 + 0.25 * (total_combos - 1)
  element_mult = element_chart[attacker][defender]
  final_damage = base_damage * combo_mult * element_mult - defense
```

**Element Chart**:
- Water > Fire (1.5x), Fire > Water (0.5x)
- Fire > Earth (1.5x), Earth > Fire (0.5x)
- Earth > Water (1.5x), Water > Earth (0.5x)
- Light > Dark (1.5x), Dark > Light (1.5x)

**Enemy AI**:
- Countdown timer per enemy
- When countdown reaches 0, enemy attacks
- Attack damage = enemy.atk (modified by team defense)
- Skills: HP regen, poison, bind, element shields

### 3. Card Collection System

**Monster Definition**:
```json
{
  "id": 1,
  "name": "Sea Serpent",
  "element": 1,
  "rarity": 4,
  "max_level": 99,
  "base_hp": 500, "max_hp": 3000,
  "base_atk": 200, "max_atk": 1200,
  "base_rec": 100, "max_rec": 600,
  "cost": 12,
  "active_skill_id": 10,
  "leader_skill_id": 5,
  "evolve_to": 100
}
```

**Monster Instance**:
- Unique instance ID
- Definition ID reference
- Current level and EXP
- Skill level (affects cooldowns)
- Plus stats (bonus HP/ATK/REC)
- Favorite flag

### 4. Gacha System

**Gacha Pool** (`MFGachaTypes.GachaPool`):
- Cost currency and amount
- Entries with monster_id, rarity, weight
- Pity threshold (guarantees top rarity)
- Daily free pull support
- Multi-pull discount (e.g., 10-pull for 9)
- Step-up banner support
- One-time beginner gacha

**Roll Logic** (`MFGachaRoller`):
```gdscript
func roll(pool, pity_count, rng):
  if pity_count >= pool.pity_threshold - 1:
    return _roll_top_rarity(pool, rng, true)  # Pity trigger
  
  total_weight = sum(entry.weight for entry in pool.entries)
  roll = rng.randf() * total_weight
  
  for entry in pool.entries:
    roll -= entry.weight
    if roll <= 0:
      return GachaResult.new(entry.monster_id, entry.rarity, ...)
```

### 5. Economy System

**Currencies**:
- `gems` - Premium currency for gacha
- `coins` - Standard currency for upgrades
- `stamina` - Energy for dungeon entry (refills over time)
- `event_tokens` - Special event currency

**Stamina Timer**:
- 1 stamina per 300 seconds (5 minutes)
- Max 100 stamina
- Server-time based calculation for offline refill

---

## Skill System

The skill system uses a **condition-outcome pattern** with three tiers:

### Skill Types

| Type | Trigger | Duration |
|------|---------|----------|
| **Active Skills** | Manual activation | Instant or N turns |
| **Leader Skills** | Passive (team leader) | Permanent |
| **Team Skills** | Passive (team composition) | Permanent |

### Skill Definition Structure

```json
{
  "id": 100,
  "name": "Ocean's Wrath",
  "type": "active",
  "max_cd": 10,
  "min_cd": 5,
  "rules": [{
    "conditions": [
      {"type": "hp_below", "params": {"threshold": 50}},
      {"type": "combo_above", "params": {"threshold": 3}}
    ],
    "outcomes": [
      {"type": "area_damage", "params": {"multiplier": 3.0}},
      {"type": "heal_percent", "params": {"percent": 0.3}}
    ]
  }]
}
```

### Condition Evaluation

All conditions must pass for outcomes to execute:

| Condition | Description |
|-----------|-------------|
| `always_true` | Always passes |
| `combo_above` | Combo count >= threshold |
| `combo_gte` | Same as combo_above |
| `combo_lt` | Combo count < threshold |
| `hp_below` | Team HP % below threshold |
| `elements_matched` | Specific elements matched this turn |
| `team_has_element` | N+ monsters of element in team |

### Outcome Types

**Instant Effects**:
| Effect | Description |
|--------|-------------|
| `area_damage` | Damage all enemies |
| `single_target_damage` | Damage highest HP enemy |
| `heal_flat` | Fixed HP recovery |
| `heal_percent` | % of max HP recovery |
| `change_gem_element` | Convert all X gems to Y |
| `delay_enemies` | Add N to enemy countdowns |
| `lifesteal` | Heal % of damage dealt |
| `gravity_damage` | % enemy max HP damage |
| `bind_enemy` | Skip enemy turns |
| `stun_enemy` | Delay + prevent counter |

**Persistent Outcomes** (track turns, auto-deactivate):
| Outcome | Description |
|---------|-------------|
| `atk_buff` | Damage multiplier for N turns |
| `defense_buff` | Damage reduction for N turns |
| `heal_over_time` | HP recovery each turn |
| `combo_scaling_atk` | Damage scales with combos |
| `poison_dot` | Damage each turn |
| `counter_attack` | Retaliate when hit |

### Skill Pipeline Flow

```
1. Load skill definition from JSON
2. Create SkillDef object with conditions/outcomes
3. On activation request:
   a. Build SkillContext (team, board, enemies, etc.)
   b. Evaluate all rule conditions
   c. Execute matching outcomes
   d. Return SkillResult (damage, healing, board changes)
4. Persistent outcomes registered with DungeonRunner
5. Outcomes tick each turn, deactivate when turns_left = 0
```

---

## Battle Flow

```
┌─────────────────────────────────────────────────────────────┐
│                     BATTLE SCREEN                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. ON ENTER                                                │
│     ├── Spend stamina                                       │
│     ├── Build team from selected IDs                        │
│     ├── Create board (5x6 or dungeon-specific)              │
│     ├── Create combat resolver with element chart           │
│     ├── Create dungeon runner                               │
│     ├── Apply team skills, leader skills                    │
│     └── Initialize skill cooldowns                          │
│                                                             │
│  2. PLAYER TURN LOOP                                        │
│     ├── Player drags gem (5 second limit)                   │
│     ├── Gem swaps along path                                │
│     ├── On release: resolve cascade                         │
│     │   ├── Find matches                                    │
│     │   ├── Animate match removal                           │
│     │   ├── Drop gems, spawn new                            │
│     │   └── Repeat until no matches                         │
│     ├── Calculate damage per element matched                │
│     ├── Apply damage to enemies                             │
│     ├── Apply healing from hearts                           │
│     ├── Tick skill cooldowns (-1)                           │
│     └── Check win/wave clear                                │
│                                                             │
│  3. ACTIVE SKILL (optional)                                 │
│     ├── Player taps skill button                            │
│     ├── Check cooldown ready                                │
│     ├── Build skill context                                 │
│     ├── Activate via skill pipeline                         │
│     ├── Apply instant effects                               │
│     ├── Register persistent outcomes                        │
│     └── Reset cooldown to max                               │
│                                                             │
│  4. ENEMY TURN (if battle continues)                        │
│     ├── For each alive enemy:                               │
│     │   ├── Decrement countdown                             │
│     │   └── If countdown = 0:                               │
│     │       ├── Calculate attack damage                     │
│     │       ├── Apply to team HP                            │
│     │       └── Reset countdown to base                     │
│     ├── Check loss (team HP <= 0)                           │
│     └── Tick persistent outcomes                            │
│                                                             │
│  5. WAVE CLEAR (if all enemies dead)                        │
│     ├── Advance to next wave                                │
│     ├── Spawn new enemies                                   │
│     └── Continue player turn loop                           │
│                                                             │
│  6. BATTLE END                                              │
│     ├── Win: Navigate to result (rewards)                   │
│     └── Lose: Navigate to result (no rewards)               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Data Layer

### Shared JSON Definitions

All game content is defined in JSON files under `shared/data/`:

| File | Purpose |
|------|---------|
| `monsters.json` | Monster stats, skills, evolution |
| `skills.json` | Active skill definitions |
| `leader_skills.json` | Leader skill definitions |
| `team_skills.json` | Passive team skills |
| `stages.json` | Dungeon waves, enemies, rewards |
| `gacha_pools.json` | Gacha configurations |
| `element_chart.json` | Element advantage matrix |
| `loot_tables.json` | Drop rate tables |
| `gem_modifiers.json` | Board modifier definitions |

### Schema Validation (Godot)

The Godot version validates data on load using `MFSchemaValidator`:
```gdscript
func _register_schemas() -> void:
    var sv = MFSchemaValidator.new()
    sv.register_schema(&"monsters", [
        MFSchemaValidator.field("id", "int", true),
        MFSchemaValidator.field("name", "string", true),
        MFSchemaValidator.field("element", "int", true),
        # ...
    ])
    _game_data.set_schema_validator(sv)
```

---

## UI/Screen System

### Screen Registration

Both versions register screens with the router on startup:

**Unity**:
```csharp
_uiRouter.Register("battle", parameters =>
{
    var screen = new BattleScreen();
    screen.Setup(_gameData, _monsterManager, _skillPipeline, ...);
    return screen;
});
```

**Godot**:
```gdscript
_ui_router.register(&"battle", func(params):
    var screen = preload("...battle_screen.gd").new()
    screen.setup(_game_data, _monster_manager, _skill_pipeline, ...)
    return screen
)
```

### Screen Lifecycle

All screens implement `IScreen` (Unity) or extend `MFBaseScreen` (Godot):

| Method | Purpose |
|--------|---------|
| `setup()` | Inject dependencies |
| `on_enter()` | Called when screen becomes active |
| `on_pause()` | Called when another screen pushed on top |
| `on_resume()` | Called when returning from child screen |
| `on_exit()` | Called when screen is removed |

### Visual Theming (Godot)

`TosTheme` centralizes all visual constants:

```gdscript
# Element Colors
const ELEMENT_COLORS := {
    1: Color(0.251, 1.0, 1.0),     # Water - cyan
    2: Color(1.0, 0.251, 0.251),   # Fire - red
    # ...
}

# Animation Timings (from decompiled ToS source)
const ANIM_PANEL := 0.5           # Top bar entrance
const ANIM_HP_BAR := 0.4          # HP bar animation
const ANIM_ENEMY_ENTER := 1.0     # Enemy entrance
const ANIM_GEM_MATCH := 0.33      # Gem match animation
const ANIM_DAMAGE := 0.46         # Damage number float

# Gem Drag Constants
const GEM_DRAG_SCALE := 1.06
const GEM_DRAG_ALPHA := 0.65
const GEM_CELL_SIZE := 62.0
```

---

## Unity vs Godot Comparison

### Architectural Alignment

| Aspect | Unity | Godot |
|--------|-------|-------|
| Entry Point | `TosGame.cs` class | `tos_game.gd` Node autoload |
| Singletons | `EventBus.Instance` | `get_node("/root/EventBus")` |
| Screens | Classes implementing `IScreen` | Scripts extending `MFBaseScreen` |
| View Models | Plain C# classes | RefCounted objects |
| UI | Separate Unity UI prefabs | Built procedurally in `_build_ui()` |
| Animation | Unity animator/tweens | Godot Tween API |
| Tests | NUnit | GdUnit |

### Implementation Differences

**Unity** (lean, framework-dependent):
- No MonoBehaviour in domain logic
- Screens are pure C# classes (IScreen interface)
- View models for UI state (BoardViewModel, EnemyViewModel)
- UI rendering handled by separate Unity layer

**Godot** (self-contained, full UI):
- Screens extend Control nodes
- UI built procedurally in GDScript
- TosCard, BoardView, EnemyView are full controls
- Includes MFButtonFeedback, MFUIAnim helpers
- Complete ToS-accurate visual styling via TosTheme

### Feature Parity

| Feature | Unity | Godot |
|---------|-------|-------|
| Board cascade resolution | ✅ | ✅ |
| Combat with element chart | ✅ | ✅ |
| Skill system (conditions/outcomes) | ✅ | ✅ |
| Team skills & leader skills | ✅ | ✅ |
| Gacha with pity | ✅ | ✅ |
| Step-up banners | ❌ | ✅ |
| Monster exchange | ❌ | ✅ |
| Friend helper system | ❌ | ✅ |
| Daily login bonus | ❌ | ✅ |
| Animated UI | Partial | Full (ToS-accurate) |
| Gem drag visual effects | ❌ | ✅ |

---

## Testing

Both implementations include comprehensive test suites.

### Unity Tests (`TosIntegrationTests.cs`)

```csharp
[Test]
public void FullBattleFlow_WinCondition()
{
    var dungeonDef = CreateTestDungeon();
    var runner = new DungeonRunner(_board, _combat, _skillPipeline, null);
    runner.Start(dungeonDef, 10000, 10000);
    
    // Auto-resolve turns until battle ends
    while (state.IsActive && turns < 20)
    {
        var cascadeSteps = CascadeResolver.Resolve(_board);
        runner.ExecutePlayerTurn(cascadeSteps, team, teamStats);
        if (state.IsActive) runner.ExecuteEnemyTurn();
    }
    
    Assert.IsFalse(state.IsActive, "Dungeon should end within 20 turns");
}
```

### Godot Tests (`test_tos_battle.gd`)

```gdscript
func test_full_battle_flow() -> void:
    var dungeon_def = _make_dungeon_def()
    _dungeon_runner.start_dungeon(dungeon_def, 10000, 10000)
    
    var turns := 0
    while _dungeon_runner.state.is_active and turns < 20:
        var cascade_steps = MFCascadeResolver.resolve(_board)
        var result = _dungeon_runner.execute_player_turn(cascade_steps, team, team_stats)
        if _dungeon_runner.state.is_active:
            _dungeon_runner.execute_enemy_turn()
        turns += 1
    
    assert_false(_dungeon_runner.state.is_active, "dungeon should end")
```

### Test Categories

| Category | Tests |
|----------|-------|
| Battle Flow | Win/loss conditions, wave progression |
| Skill System | Registration, activation, effects |
| Combat | Element advantage, damage calculation |
| Board | Cascade resolution, forced matches |
| Gacha | Currency deduction, pity triggers |
| Team Selection | Slot management, stat calculation |
| Enemy AI | Attack timing, special behaviors |

---

## Key Design Patterns

### 1. Dependency Injection
Both versions inject framework modules into screens:
```csharp
// Unity
screen.Setup(_gameData, _monsterManager, _skillPipeline, _economy, ...);
```
```gdscript
# Godot
screen.setup(_game_data, _monster_manager, _skill_pipeline, _economy, ...)
```

### 2. Registry Pattern
Skill conditions and outcomes are registered by type string:
```csharp
pipeline.ConditionRegistry.Register("combo_above", 
    parameters => new ComboAboveCondition(parameters));
pipeline.EffectRegistry.Register("area_damage", AreaDamage);
```

### 3. Strategy Pattern
Combat hooks allow skill effects to modify damage:
```gdscript
func activate(context, result):
    _hook_ref = func(dmg_ctx):
        dmg_ctx.damage *= _multiplier
    context.combat.register_hook(MFCombatTypes.DamageHook.MAIN, _hook_ref)
```

### 4. View Model Pattern
Domain state is projected into display-friendly models:
- `BoardViewModel` → gem positions, drag state, animation state
- `EnemyViewModel` → HP ratio, countdown, alive status

### 5. Event-Driven Architecture
Global events for cross-cutting concerns:
```gdscript
_event_bus.subscribe(EventNames.CURRENCY_CHANGED, func(_p): _save_manager.mark_dirty())
_event_bus.subscribe(EventNames.BATTLE_WON, func(_p): _save_manager.mark_dirty())
```

---

## Summary

Tower of Saviors demonstrates a well-architected match-3 RPG with:

1. **Clean separation** between domain logic and presentation
2. **Data-driven design** with shared JSON definitions
3. **Extensible skill system** using condition-outcome pattern
4. **Dual implementation** maintaining architectural parity
5. **Comprehensive testing** at the integration level

The Godot version is more feature-complete with ToS-accurate visuals and additional systems (step-up gacha, monster exchange, friend helpers), while the Unity version provides a lean reference implementation focused on core mechanics.
