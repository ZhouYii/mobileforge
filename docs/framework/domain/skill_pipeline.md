# Skill Pipeline Module

## Overview

A **condition-outcome composition model** that replaces Tower of Saviors' monolithic 12,000+ line switch statement for skill execution. Instead of hard-coding every skill as a unique code path, MobileForge decomposes skills into reusable **conditions** and **outcomes** wired together by JSON definitions.

ToS ships roughly 119 distinct condition types and 192 distinct outcome types scattered across its codebase. Analysis shows most are minor variations of ~30 condition patterns and ~40 outcome patterns. MobileForge encodes the variations as **parameters in JSON**, not as separate classes, reducing code volume by over 90% while remaining fully extensible.

## Module Files

| File | Purpose | Dependencies |
|---|---|---|
| `skill_types` | SkillDef, SkillContext, SkillResult, ConditionDef, OutcomeDef | None (leaf) |
| `skill_pipeline` | SkillPipeline: orchestrates condition evaluation and outcome execution | skill_types, effect_registry |
| `effect_registry` | EffectRegistry: dictionary of simple stateless effect callables | skill_types |
| `skill_condition_base` | SkillConditionBase: abstract class for complex stateful conditions | skill_types |
| `skill_outcome_base` | SkillOutcomeBase: abstract class for complex outcomes with turn persistence | skill_types |
| `skill_loader` | SkillLoader: deserializes SkillDef from JSON | skill_types |

## Architecture

```
                         SkillDef (JSON)
                              |
                              v
                  SkillPipeline.activate_skill(def, context)
                              |
              +---------------+---------------+
              |                               |
              v                               v
     Evaluate Conditions                Execute Outcomes
     (all must pass)                    (in sequence)
              |                               |
     +--------+--------+            +--------+--------+
     |        |        |            |        |        |
  Cond_1   Cond_2   Cond_N      Out_1    Out_2    Out_N
     |        |        |            |        |        |
     v        v        v            v        v        v
  true/false for each           Each outcome mutates
                                SkillResult / registers
                                combat hooks / modifies
                                board state

     If ALL conditions pass  -->  outcomes execute
     If ANY condition fails  -->  skill activation fails, SkillResult.success = false
```

### Data Flow

```
  JSON file                 Domain                           Combat / Board
  ---------                 ------                           -------------

  skills.json  --->  SkillLoader.load(json)
                           |
                     SkillDef stored
                           |
                     Player activates skill
                           |
                     SkillPipeline.activate_skill(def, ctx)
                           |
                     Evaluate conditions against SkillContext
                           |
                     Execute outcomes:
                       - Simple effects via EffectRegistry
                       - Complex outcomes via SkillOutcomeBase subclasses
                           |
                     SkillResult returned  --->  CombatResolver hooks registered
                                           --->  BoardLogic state modified
                                           --->  EnemyState debuffs applied
```

## Skill Activation Flow

### 6-Step Activation Sequence

```
1. Player taps skill portrait
   │
2. Check cooldown: current_cooldown > 0? → Show "not ready" toast, STOP
   │
3. Build SkillContext: snapshot board, team, enemies, combos, HP, turn_number
   │
4. Evaluate conditions (per SkillRule):
   │   For each rule in skill_def.rules:
   │     For each condition in rule.conditions:
   │       condition.is_valid(context)?
   │     ALL must pass → execute this rule's outcomes
   │     ANY fails → skip this rule
   │
5. Execute outcomes:
   │   For each outcome in matched_rule.outcomes:
   │     Is it a registered simple effect (Tier 1)?
   │       → EffectRegistry.execute(type, params, context, result)
   │     Is it a registered condition type (Tier 2)?
   │       → SkillConditionBase.evaluate(params, context)
   │     Is it a registered outcome type (Tier 3)?
   │       → SkillOutcomeBase.apply(params, context, result)
   │       → If outcome.turns_left > 0: add to persistent_outcomes
   │       → Register hooks on CombatResolver if needed
   │
6. Return SkillResult with accumulated effects
```

### Tier Decision Flowchart

```
Does the effect need to persist across turns?
  │
  ├─ NO: Can it be expressed as (params, context) → mutation?
  │   │
  │   ├─ YES → Tier 1: EffectRegistry (simple callable)
  │   │         Examples: heal_flat, area_damage, change_gem_element
  │   │
  │   └─ NO  → Tier 2: SkillConditionBase (stateful evaluator)
  │             Examples: combo_above, hp_threshold, elements_matched
  │
  └─ YES → Tier 3: SkillOutcomeBase (persistent outcome)
            Examples: atk_buff (3 turns), damage_over_time, combo_scaling_buff
            │
            Lifecycle:
            activate() → on_turn_start() → on_turn_end() → turns_left-- → deactivate()
```

### Persistent Outcome Lifecycle (expanded)

```
Turn N: Skill activated
  │── outcome.apply(params, context, result)
  │── outcome.turns_left = params.turns
  │── outcome registers hooks on CombatResolver
  │── outcome added to SkillPipeline._active_outcomes
  │
Turn N (end):
  │── SkillPipeline.process_turn_end(context)
  │── outcome.process_turn_end(context)  ← per-turn logic
  │── outcome.turns_left -= 1
  │
Turn N+1 (end):
  │── Same tick logic, turns_left decrements
  │
Turn N+k (end): turns_left reaches 0
  │── outcome.on_expire(context)
  │── CombatResolver.unregister_hook(outcome._hook_name)
  │── outcome removed from _active_outcomes
```

### Hook Lifecycle

```
Hook Registration (during outcome.apply):
  CombatResolver.register_hook(HookType.MAIN, callback, priority, name)

Hook Execution (during resolve_player_attack):
  PRE_ELEMENT → POST_ELEMENT → MAIN → POST_DEFENSE → CAN_ZERO
  Each hook receives mutable DamageContext, can modify damage

Hook Cleanup (during outcome.on_expire):
  CombatResolver.unregister_hook(name)
  Hook no longer fires on subsequent damage calculations
```

## The Hybrid Extension Model

Skills range from trivial (heal 1000 HP) to complex (scaling buff that persists 3 turns and stacks with combos). MobileForge supports both with a three-tier extension model:

### Tier 1: EffectRegistry (Simple Stateless Effects)

For effects that are a single function call with no persistence:

```
EffectRegistry
  key: "heal_flat"
  value: func(params, context, result) -> void
```

The registry maps string keys to callables. The callable receives the JSON parameters, the current `SkillContext`, and the `SkillResult` to mutate. No class needed -- just a function.

**Best for:** flat heals, instant damage, stat snapshots, one-shot element conversions.

### Tier 2: SkillConditionBase (Complex Stateful Conditions)

For conditions that need internal state or multi-step evaluation:

```gdscript
class_name SkillConditionBase extends RefCounted

## Return true if this condition is met.
func evaluate(params: Dictionary, context: SkillContext) -> bool:
    push_error("SkillConditionBase.evaluate() not overridden")
    return false
```

Subclasses override `evaluate()`. The `params` dictionary comes from JSON -- the condition class defines what keys it expects.

**Best for:** board state checks (e.g., "at least 5 heart orbs on board"), team composition requirements, HP threshold checks with hysteresis.

### Tier 3: SkillOutcomeBase (Complex Outcomes with Turn Persistence)

For outcomes that last multiple turns and need per-turn processing:

```gdscript
class_name SkillOutcomeBase extends RefCounted

var turns_left: int = 0

## Called when the skill is first activated.
func apply(params: Dictionary, context: SkillContext, result: SkillResult) -> void:
    push_error("SkillOutcomeBase.apply() not overridden")

## Called at the end of each turn while turns_left > 0.
func process_turn_end(context: SkillContext) -> void:
    pass

## Called when the outcome expires (turns_left reaches 0).
func on_expire(context: SkillContext) -> void:
    pass
```

**Best for:** multi-turn buffs, damage-over-time, shields that absorb N hits, stacking effects.

## SkillDef JSON Format

A `SkillDef` is a JSON object describing one skill. It lists conditions (all must pass) and outcomes (executed in order):

```json
{
  "id": "water_burst_003",
  "name": "Tidal Wave",
  "description": "Water ATK x3 for 2 turns. Requires 5+ Water combos.",
  "icon": "skill_water_burst",
  "max_level": 10,
  "base_cooldown": 12,
  "min_cooldown": 6,
  "skill_type": "active",
  "conditions": [
    {
      "type": "min_combos_element",
      "params": {
        "element": "WATER",
        "min_count": 5
      }
    }
  ],
  "outcomes": [
    {
      "type": "atk_multiplier",
      "params": {
        "element": "WATER",
        "multiplier": 3.0,
        "turns": 2,
        "hook_type": "MAIN",
        "priority": 100
      }
    }
  ]
}
```

### SkillDef Fields

| Field | Type | Description |
|---|---|---|
| `id` | `String` | Unique skill identifier |
| `name` | `String` | Display name |
| `description` | `String` | Player-facing description text |
| `icon` | `String` | Icon asset key |
| `max_level` | `int` | Maximum skill level (affects cooldown) |
| `base_cooldown` | `int` | Cooldown at skill level 1 |
| `min_cooldown` | `int` | Minimum cooldown floor (at max skill level) |
| `skill_type` | `String` | `"active"`, `"leader"`, or `"passive"` |
| `conditions` | `Array[ConditionDef]` | All must pass for activation |
| `outcomes` | `Array[OutcomeDef]` | Executed in order on activation |

### ConditionDef Fields

| Field | Type | Description |
|---|---|---|
| `type` | `String` | Registry key for the condition evaluator |
| `params` | `Dictionary` | Arbitrary parameters passed to the condition |

### OutcomeDef Fields

| Field | Type | Description |
|---|---|---|
| `type` | `String` | Registry key for the outcome executor |
| `params` | `Dictionary` | Arbitrary parameters passed to the outcome |

## SkillContext Fields

`SkillContext` captures the complete game state snapshot at the moment of skill activation:

| Field | Type | Description |
|---|---|---|
| `caster` | `MonsterInstance` | The monster activating the skill |
| `team` | `Array[MonsterInstance]` | The player's current team |
| `enemies` | `Array[EnemyState]` | All enemies in the current wave |
| `board_snapshot` | `Array[GemData]` | Current board state (flat array) |
| `total_combos` | `int` | Combo count from the current turn's cascade |
| `matches_by_element` | `Dictionary[Element, int]` | Number of matches per element this turn |
| `current_hp` | `int` | Team's current HP |
| `max_hp` | `int` | Team's max HP |
| `turn_number` | `int` | Current turn count in this dungeon run |
| `combat_resolver` | `CombatResolver` | Reference for registering hooks (outcomes only) |
| `flags` | `Dictionary` | Arbitrary key-value store for cross-outcome communication |

## SkillResult Fields

`SkillResult` is returned by `activate_skill()` and accumulates the effects of all outcomes:

| Field | Type | Description |
|---|---|---|
| `success` | `bool` | True if all conditions passed and outcomes executed |
| `failure_reason` | `String` | Reason string if activation failed (empty on success) |
| `hooks_registered` | `Array[String]` | Names of combat hooks that were registered |
| `hp_healed` | `int` | Total HP healed by this activation |
| `damage_dealt` | `int` | Total direct damage dealt by this activation |
| `buffs_applied` | `Array[Dictionary]` | List of buff descriptors applied |
| `debuffs_applied` | `Array[Dictionary]` | List of debuff descriptors applied to enemies |
| `board_modified` | `bool` | True if any outcome changed the board state |
| `persistent_outcomes` | `Array[SkillOutcomeBase]` | Outcomes that persist beyond this turn |

## Persistent Outcomes and Turn Lifecycle

Outcomes that extend `SkillOutcomeBase` can persist across turns. The `turns_left` field controls duration:

```
Turn N: Skill activated
  |-- outcome.apply() called
  |-- outcome.turns_left = params.turns (e.g., 3)
  |-- outcome added to SkillResult.persistent_outcomes
  |
Turn N end:
  |-- SkillPipeline.process_turn_end() called
  |-- For each persistent outcome:
  |     outcome.process_turn_end(context)
  |     outcome.turns_left -= 1
  |
Turn N+1 end:
  |-- Same tick logic
  |-- turns_left = 1
  |
Turn N+2 end:
  |-- turns_left reaches 0
  |-- outcome.on_expire(context) called
  |-- outcome removed from active list
  |-- Any registered combat hooks are unregistered
```

### process_turn_end Flow

```gdscript
func process_turn_end(context: SkillContext) -> void:
    var expired: Array[SkillOutcomeBase] = []
    for outcome in _active_outcomes:
        outcome.process_turn_end(context)
        outcome.turns_left -= 1
        if outcome.turns_left <= 0:
            outcome.on_expire(context)
            expired.append(outcome)
    for outcome in expired:
        _active_outcomes.erase(outcome)
```

## Cooldown Formula

A skill's cooldown decreases as the monster's `skill_level` increases:

```
cooldown = max(base_cooldown + 1 - skill_level, min_cooldown)
```

| Skill Level | base_cooldown=12, min_cooldown=6 |
|---|---|
| 1 | max(12 + 1 - 1, 6) = 12 |
| 2 | max(12 + 1 - 2, 6) = 11 |
| 3 | max(12 + 1 - 3, 6) = 10 |
| 4 | max(12 + 1 - 4, 6) = 9 |
| 5 | max(12 + 1 - 5, 6) = 8 |
| 6 | max(12 + 1 - 6, 6) = 7 |
| 7 | max(12 + 1 - 7, 6) = 6 |
| 8+ | max(12 + 1 - 8, 6) = 6 (floored at min) |

The formula guarantees cooldown never drops below `min_cooldown`, even at absurdly high skill levels.

## Registering Custom Conditions and Outcomes

### Registering a Simple Effect (Tier 1)

```gdscript
# GDScript
var registry := EffectRegistry.new()

# Register a flat heal effect
registry.register_effect("heal_flat", func(params: Dictionary,
        context: SkillContext, result: SkillResult) -> void:
    var amount: int = params.get("amount", 0)
    result.hp_healed += amount
)

# Register an element conversion effect
registry.register_effect("convert_element", func(params: Dictionary,
        context: SkillContext, result: SkillResult) -> void:
    var from_elem: String = params.get("from", "")
    var to_elem: String = params.get("to", "")
    # Board modification logic would go here
    result.board_modified = true
)
```

```csharp
// C#
var registry = new EffectRegistry();

registry.RegisterEffect("heal_flat", (params, context, result) =>
{
    int amount = params.GetValueOrDefault("amount", 0);
    result.HpHealed += amount;
});

registry.RegisterEffect("convert_element", (params, context, result) =>
{
    string fromElem = params.GetValueOrDefault("from", "");
    string toElem = params.GetValueOrDefault("to", "");
    result.BoardModified = true;
});
```

### Registering a Custom Condition (Tier 2)

```gdscript
# GDScript
class_name MinHpPercentCondition extends SkillConditionBase

## Condition: team HP must be at or below a percentage threshold.
## Params: { "threshold": 0.5 }  -- e.g., below 50% HP
func evaluate(params: Dictionary, context: SkillContext) -> bool:
    var threshold: float = params.get("threshold", 1.0)
    var hp_ratio: float = float(context.current_hp) / float(context.max_hp)
    return hp_ratio <= threshold

# Registration:
pipeline.register_condition("hp_below_percent", MinHpPercentCondition.new())
```

```csharp
// C#
public class MinHpPercentCondition : SkillConditionBase
{
    public override bool Evaluate(Dictionary<string, object> parms, SkillContext ctx)
    {
        float threshold = Convert.ToSingle(parms.GetValueOrDefault("threshold", 1.0f));
        float hpRatio = (float)ctx.CurrentHp / ctx.MaxHp;
        return hpRatio <= threshold;
    }
}

// Registration:
pipeline.RegisterCondition("hp_below_percent", new MinHpPercentCondition());
```

### Registering a Custom Outcome (Tier 3)

```gdscript
# GDScript
class_name ComboScalingBuffOutcome extends SkillOutcomeBase

var _base_multiplier: float = 1.0
var _per_combo_bonus: float = 0.0
var _element: int = -1
var _hook_name: String = ""

func apply(params: Dictionary, context: SkillContext, result: SkillResult) -> void:
    _base_multiplier = params.get("base_multiplier", 2.0)
    _per_combo_bonus = params.get("per_combo_bonus", 0.5)
    _element = params.get("element", Element.WATER)
    turns_left = params.get("turns", 3)
    _hook_name = "combo_scaling_buff_%s" % context.caster.instance_id

    # Register a combat hook that scales with combo count
    context.combat_resolver.register_hook(
        HookType.MAIN,
        _apply_buff,
        100,
        _hook_name
    )
    result.hooks_registered.append(_hook_name)
    result.buffs_applied.append({
        "name": _hook_name,
        "turns": turns_left,
        "base_mult": _base_multiplier
    })

func _apply_buff(ctx: DamageContext) -> DamageContext:
    if ctx.element == _element:
        var mult := _base_multiplier + ctx.total_combos * _per_combo_bonus
        ctx.damage *= mult
    return ctx

func process_turn_end(context: SkillContext) -> void:
    pass  # No per-turn logic needed; hook stays registered

func on_expire(context: SkillContext) -> void:
    context.combat_resolver.unregister_hook(_hook_name)

# Registration:
pipeline.register_outcome("combo_scaling_buff", ComboScalingBuffOutcome.new())
```

```csharp
// C#
public class ComboScalingBuffOutcome : SkillOutcomeBase
{
    private float _baseMultiplier;
    private float _perComboBonus;
    private Element _element;
    private string _hookName = "";

    public override void Apply(Dictionary<string, object> parms,
                               SkillContext ctx, SkillResult result)
    {
        _baseMultiplier = Convert.ToSingle(parms.GetValueOrDefault("base_multiplier", 2.0f));
        _perComboBonus = Convert.ToSingle(parms.GetValueOrDefault("per_combo_bonus", 0.5f));
        _element = (Element)Convert.ToInt32(parms["element"]);
        TurnsLeft = Convert.ToInt32(parms.GetValueOrDefault("turns", 3));
        _hookName = $"combo_scaling_buff_{ctx.Caster.InstanceId}";

        ctx.CombatResolver.RegisterHook(
            HookType.Main,
            ApplyBuff,
            priority: 100,
            name: _hookName
        );
        result.HooksRegistered.Add(_hookName);
        result.BuffsApplied.Add(new BuffDescriptor
        {
            Name = _hookName,
            Turns = TurnsLeft,
            BaseMultiplier = _baseMultiplier
        });
    }

    private DamageContext ApplyBuff(DamageContext ctx)
    {
        if (ctx.Element == _element)
        {
            float mult = _baseMultiplier + ctx.TotalCombos * _perComboBonus;
            ctx.Damage *= mult;
        }
        return ctx;
    }

    public override void OnExpire(SkillContext ctx)
    {
        ctx.CombatResolver.UnregisterHook(_hookName);
    }
}

// Registration:
pipeline.RegisterOutcome("combo_scaling_buff", new ComboScalingBuffOutcome());
```

## SkillPipeline API

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `new` | `registry: EffectRegistry` | `SkillPipeline` | Construct with an effect registry |
| `register_condition` | `type: String, condition: SkillConditionBase` | `void` | Register a custom condition evaluator |
| `register_outcome` | `type: String, outcome: SkillOutcomeBase` | `void` | Register a custom outcome executor |
| `activate_skill` | `def: SkillDef, context: SkillContext` | `SkillResult` | Evaluate conditions and execute outcomes |
| `can_activate` | `def: SkillDef, context: SkillContext` | `bool` | Check if all conditions pass (without executing) |
| `process_turn_end` | `context: SkillContext` | `void` | Tick all persistent outcomes, expire finished ones |
| `get_cooldown` | `def: SkillDef, skill_level: int` | `int` | Calculate cooldown for a given skill level |
| `get_active_outcomes` | | `Array[SkillOutcomeBase]` | List currently active persistent outcomes |
| `clear_all` | | `void` | Remove all persistent outcomes and unregister their hooks |
| `has_active_outcome` | `name: String` | `bool` | Check if a named outcome is currently active |

## Code Examples

### Registering a Simple Heal Effect

```gdscript
# GDScript
var registry := EffectRegistry.new()

registry.register_effect("heal_flat", func(params: Dictionary,
        context: SkillContext, result: SkillResult) -> void:
    var amount: int = params.get("amount", 0)
    # Scale heal by team's total REC
    var rec_bonus: float = float(context.team.reduce(
        func(acc, m): return acc + m.rec, 0)) * 0.1
    result.hp_healed += int(amount + rec_bonus)
)
```

```csharp
// C#
var registry = new EffectRegistry();

registry.RegisterEffect("heal_flat", (params, context, result) =>
{
    int amount = Convert.ToInt32(params.GetValueOrDefault("amount", 0));
    float recBonus = context.Team.Sum(m => m.Rec) * 0.1f;
    result.HpHealed += (int)(amount + recBonus);
});
```

### Loading and Activating a Skill from JSON

```gdscript
# GDScript
# 1. Load skill definitions
var json_text := FileAccess.get_file_as_string("res://data/skills.json")
var skill_defs: Dictionary = SkillLoader.load_all(json_text)

# 2. Set up the pipeline
var registry := EffectRegistry.new()
# ... register effects ...
var pipeline := SkillPipeline.new(registry)
# ... register custom conditions and outcomes ...

# 3. Build the context
var context := SkillContext.new()
context.caster = team[0]
context.team = team
context.enemies = current_wave_enemies
context.board_snapshot = board_logic.get_board_snapshot()
context.total_combos = cascade_result.total_combo
context.matches_by_element = cascade_result.matches_by_element
context.current_hp = party_hp
context.max_hp = party_max_hp
context.turn_number = current_turn
context.combat_resolver = combat_resolver

# 4. Activate the skill
var skill_def: SkillDef = skill_defs["water_burst_003"]
var result: SkillResult = pipeline.activate_skill(skill_def, context)

if result.success:
    print("Skill activated! Healed: ", result.hp_healed)
    print("Hooks registered: ", result.hooks_registered)
    for buff in result.buffs_applied:
        print("Buff: ", buff.name, " for ", buff.turns, " turns")
else:
    print("Skill failed: ", result.failure_reason)
```

```csharp
// C#
// 1. Load skill definitions
string json = File.ReadAllText("data/skills.json");
var skillDefs = SkillLoader.LoadAll(json);

// 2. Set up the pipeline
var registry = new EffectRegistry();
// ... register effects ...
var pipeline = new SkillPipeline(registry);
// ... register custom conditions and outcomes ...

// 3. Build the context
var context = new SkillContext
{
    Caster = team[0],
    Team = team,
    Enemies = currentWaveEnemies,
    BoardSnapshot = boardLogic.GetBoardSnapshot(),
    TotalCombos = cascadeResult.TotalCombo,
    MatchesByElement = cascadeResult.MatchesByElement,
    CurrentHp = partyHp,
    MaxHp = partyMaxHp,
    TurnNumber = currentTurn,
    CombatResolver = combatResolver
};

// 4. Activate the skill
var skillDef = skillDefs["water_burst_003"];
SkillResult result = pipeline.ActivateSkill(skillDef, context);

if (result.Success)
{
    Console.WriteLine($"Skill activated! Healed: {result.HpHealed}");
    Console.WriteLine($"Hooks: {string.Join(", ", result.HooksRegistered)}");
}
else
{
    Console.WriteLine($"Skill failed: {result.FailureReason}");
}
```

### Processing Turn Lifecycle

```gdscript
# GDScript
# At the end of each turn, tick all persistent outcomes
func _on_turn_end() -> void:
    var context := _build_skill_context()
    pipeline.process_turn_end(context)

    # Check what's still active
    var active := pipeline.get_active_outcomes()
    for outcome in active:
        print("Active: ", outcome.turns_left, " turns remaining")

    # When battle ends, clean up everything
    if battle_over:
        pipeline.clear_all()
```

```csharp
// C#
void OnTurnEnd()
{
    var context = BuildSkillContext();
    pipeline.ProcessTurnEnd(context);

    var active = pipeline.GetActiveOutcomes();
    foreach (var outcome in active)
        Console.WriteLine($"Active: {outcome.TurnsLeft} turns remaining");

    if (battleOver)
        pipeline.ClearAll();
}
```

### Checking Cooldown Before Activation

```gdscript
# GDScript
var skill_def: SkillDef = skill_defs["water_burst_003"]
var monster: MonsterInstance = team[0]

var cd: int = pipeline.get_cooldown(skill_def, monster.skill_level)
print("Cooldown: ", cd, " turns")
# skill_level=1 -> 12, skill_level=5 -> 8, skill_level=7+ -> 6
```

```csharp
// C#
var skillDef = skillDefs["water_burst_003"];
int cd = pipeline.GetCooldown(skillDef, monster.SkillLevel);
Console.WriteLine($"Cooldown: {cd} turns");
```
