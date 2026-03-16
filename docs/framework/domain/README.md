# Domain Layer

## Purpose

The domain layer contains **pure game logic with ZERO engine dependencies**. No nodes, no scenes, no rendering, no input handling. Every class here operates on plain data and returns plain data.

- **Godot**: all domain classes extend `RefCounted` (never `Node`)
- **Unity**: all domain classes are plain C# classes (never `MonoBehaviour`)

Because there are no engine ties, every module is **testable without an editor or running scene**. Tests run from CLI via `gut` (Godot) or `dotnet test` (Unity).

## Modules

| Module | Description |
|---|---|
| **Board** | 6x5 match-3 puzzle board: gem movement, match detection, cascade resolution |
| **Combat** | 5-hook damage pipeline with element chart and combo scaling |
| **Monster** | Definition/instance split, stat curves, leveling, fusion, evolution |
| **Skill** | Skill definitions, activation conditions, and the pipeline that injects hooks into combat |
| **EnemyAI** | Turn-based enemy intent selection and countdown timers |
| **Economy** | Currency, stamina, and resource transaction logic |
| **LootTable** | Weighted random drop tables with pity/guarantee systems |
| **TeamBuilder** | Party composition validation and leader skill resolution |
| **DungeonRunner** | Per-floor orchestrator that wires board, combat, enemies, and loot together |

## Module Dependency Graph

```
DungeonRunner
  |---> BoardLogic
  |---> CombatResolver
  |---> SkillPipeline -----> CombatResolver
  |          |-------------> BoardLogic
  |---> EnemyAI
  |---> Economy
  |---> LootTable

TeamBuilder -----> MonsterManager

BoardLogic          (leaf)
CombatResolver      (leaf)
MonsterManager      (leaf)
EnemyAI             (leaf)
Economy             (leaf)
LootTable           (leaf)
```

Key observations:
- **DungeonRunner** is the only top-level orchestrator. It depends on six other modules.
- **SkillPipeline** is mid-tier: it depends on CombatResolver (to register hooks) and BoardLogic (to read board state for conditional skills).
- **TeamBuilder** depends only on MonsterManager (to validate that monsters exist and meet constraints).
- All other modules are **independent leaf modules** with zero cross-module dependencies.

## File Convention: `*_types` Pattern

Every module follows the same internal structure:

```
module/
  module_types.gd      # Data classes, enums, constants. LEAF file, no deps.
  module_config.gd     # Tuning knobs (optional). Depends only on _types.
  module_logic.gd      # Core algorithms. Depends on _types and _config.
  module_events.gd     # Signal/event definitions (optional). Depends on _types.
```

The `*_types` file is always a **leaf with no dependencies**. Logic files import their own `_types` and possibly `_config`, but never import types from another module directly. Cross-module data flows through orchestrator-level wiring.

## Cross-Module Dependencies

Cross-module dependencies exist **only at the orchestrator level**. When DungeonRunner needs to pass board results into the combat resolver, it does so explicitly:

```gdscript
# DungeonRunner wires modules together -- modules never import each other
var cascade_result := board_logic.resolve_cascade(board)
var damage_results := combat_resolver.resolve_turn(cascade_result.matches, team, enemies)
```

Modules receive data through method parameters, never by reaching into another module's internals.

## Instantiation: Per-Session, Not Singletons

Domain modules are **instantiated per gameplay session**, not registered as global singletons (autoloads).

```gdscript
# Correct: fresh instances per dungeon run
func start_dungeon(dungeon_def: DungeonDef) -> void:
    var config := BoardConfig.new(6, 5)
    var board_logic := BoardLogic.new(config)
    var combat_resolver := CombatResolver.new(ElementChart.new())
    var runner := DungeonRunner.new(board_logic, combat_resolver, ...)

# Wrong: global singleton that leaks state between runs
# var board_logic = BoardLogic  # autoload -- never do this
```

```csharp
// C# equivalent
public DungeonSession StartDungeon(DungeonDef def)
{
    var config = new BoardConfig(6, 5);
    var boardLogic = new BoardLogic(config);
    var combatResolver = new CombatResolver(new ElementChart());
    return new DungeonRunner(boardLogic, combatResolver, ...);
}
```

This ensures:
- No stale state between dungeon runs
- Easy to create isolated instances for unit tests
- No hidden coupling through global mutable state
