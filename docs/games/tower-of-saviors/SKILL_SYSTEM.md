# Tower of Saviors Skill System

## The Original Problem

In the original Tower of Saviors codebase, active skill execution is handled by a monolithic `ActiveSkillExecution` class spanning over 12,000 lines of code. The structure looks roughly like this:

```
switch (skillId) {
    case 1001:
        // 50 lines of custom logic for Tidal Wave
        break;
    case 1002:
        // 80 lines of custom logic for Meteor Strike
        break;
    // ... 400+ more cases
}
```

Each skill is a unique code path. When two skills do nearly the same thing (e.g., "deal 10x water damage to all enemies" vs "deal 15x fire damage to all enemies"), the logic is duplicated with minor parameter changes. Analysis of the decompiled code reveals:

- **119 distinct condition types** -- but most are minor variations of ~30 patterns (e.g., `combo >= 5` vs `combo >= 7` are separate condition functions)
- **192 distinct outcome types** -- but most are variations of ~40 patterns (e.g., `water_atk_x2` vs `fire_atk_x3` are separate outcome functions)

The combinatorial explosion comes from encoding **parameters as code** rather than as data.

## The Solution: Condition-Outcome Composition

MobileForge replaces the switch statement with a **composition model**. Each skill is defined in JSON as a list of **rules**, where each rule has:

1. **Conditions** -- all must pass for the rule to execute
2. **Outcomes** -- executed in sequence when conditions pass

```json
{
    "id": 5,
    "name": "Shadow Pact",
    "description": "Boost dark ATK by x2.0 for 2 turns, reduce HP by 25%",
    "type": "active",
    "max_cd": 14,
    "min_cd": 8,
    "max_level": 10,
    "rules": [
        {
            "conditions": [{"type": "always_true", "params": {}}],
            "outcomes": [
                {"type": "atk_buff", "params": {"element": 5, "multiplier": 2.0, "duration_turns": 2}},
                {"type": "self_damage", "params": {"hp_percent": 0.25}}
            ]
        }
    ]
}
```

The same condition and outcome types are reused across many skills. The `atk_buff` outcome works for any element and any multiplier -- the specifics come from `params`.

### Multi-Rule Skills

Some skills have conditional branching. For example, "Combo Mastery" behaves differently depending on the player's combo count:

```json
{
    "id": 8,
    "name": "Combo Mastery",
    "rules": [
        {
            "conditions": [{"type": "combo_gte", "params": {"min_combo": 5}}],
            "outcomes": [{"type": "combo_scaling_atk", "params": {"bonus_per_combo": 0.5, "duration_turns": 1}}]
        },
        {
            "conditions": [{"type": "combo_lt", "params": {"max_combo": 5}}],
            "outcomes": [{"type": "atk_buff", "params": {"element": 0, "multiplier": 1.2, "duration_turns": 1}}]
        }
    ]
}
```

Rules are evaluated in order. The pipeline iterates all rules and executes outcomes for every rule whose conditions pass. This allows a single skill definition to express fallback behavior, conditional bonuses, or independent parallel effects.

## How to Add a New Skill Type

### Step 1: Determine if Existing Types Suffice

Most new skills can be built from existing conditions and outcomes with new parameter values. Check the registered types below before writing code.

### Step 2: Register a New Simple Effect (EffectRegistry)

For stateless, instant effects, register a callable in `TosSkillRegistration.register()`:

```gdscript
# In register_all.gd
pipeline.effect_registry.register("my_new_effect", _my_new_effect)

static func _my_new_effect(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
    var value := int(params.get("amount", 100))
    result.healing += value
```

Then reference it in JSON:
```json
{"type": "my_new_effect", "params": {"amount": 500}}
```

### Step 3: Register a New Condition (MFSkillCondition subclass)

For conditions that need custom logic beyond parameter comparison:

```gdscript
# In register_all.gd, add the class:
class BoardHasElementCondition extends MFSkillCondition:
    func is_valid(ctx: RefCounted) -> bool:
        var elem := int(get_param("element", 0))
        var min_count := int(get_param("min_count", 3))
        if ctx.board == null:
            return false
        var count := 0
        for pos in range(ctx.board.config.total_cells()):
            var gem = ctx.board.get_gem(pos)
            if gem != null and gem.element == elem:
                count += 1
        return count >= min_count

# Register it:
pipeline.condition_registry.register("board_has_element", func(params):
    return BoardHasElementCondition.new(params))
```

### Step 4: Register a Complex Outcome (MFSkillOutcome subclass)

For outcomes that persist across turns or register combat hooks, create a subclass in `skill_defs/outcomes/`:

```gdscript
class_name TosShieldOutcome extends MFSkillOutcome
    var _reduction: float
    var _hook_ref: Callable

    func activate(context: RefCounted, result: RefCounted) -> void:
        _reduction = float(get_param("damage_reduction", 0.5))
        turns_left = int(get_param("duration_turns", 2))
        _hook_ref = func(ctx): ctx.damage *= (1.0 - _reduction)
        context.combat.register_hook(
            MFCombatTypes.DamageHook.POST_DEFENSE, _hook_ref, 50, "shield")
        result.buffs_applied.append({"type": "shield", "turns": turns_left})

    func deactivate(context: RefCounted) -> void:
        context.combat.unregister_hook(
            MFCombatTypes.DamageHook.POST_DEFENSE, _hook_ref)
```

Register via the outcome registry:
```gdscript
pipeline.outcome_registry.register("defense_buff", func(params):
    return TosShieldOutcome.new(params))
```

### Step 5: Define the Skill in JSON

Add the entry to `shared/data/skills.json`:

```json
{
    "id": 11,
    "name": "New Skill Name",
    "type": "active",
    "max_cd": 10,
    "min_cd": 5,
    "max_level": 10,
    "rules": [
        {
            "conditions": [{"type": "board_has_element", "params": {"element": 1, "min_count": 5}}],
            "outcomes": [{"type": "defense_buff", "params": {"damage_reduction": 0.5, "duration_turns": 3}}]
        }
    ]
}
```

### Step 6: Assign to a Monster

Set the monster's `active_skill_id` in `monsters.json` to the new skill's `id`.

## Complete List of Registered Conditions

| Type Key | Parameters | Description |
|---|---|---|
| `always_true` | (none) | Always passes. Used for unconditional skills. |
| `combo_above` | `threshold: int` | Passes when `combo_count >= threshold`. |
| `hp_below` | `percent: float` | Passes when team HP is at or below the given percentage (0.0-1.0). |
| `elements_matched` | `element: int, min_count: int` | Passes when at least `min_count` gems of the given element were matched this turn. |
| `team_has_element` | `element: int` | Passes when at least one team monster has the specified element. |

## Complete List of Registered Effects/Outcomes

### Simple Effects (EffectRegistry)

| Type Key | Parameters | Description |
|---|---|---|
| `area_damage` | `multiplier: float` | Deal `ATK * multiplier` damage to all living enemies. Uses the first team member's ATK stat. |
| `heal_flat` | `amount: int` | Heal the team for a flat HP amount. |
| `heal_percent` | `percent: float` | Heal the team for a percentage of max HP. |
| `change_gem_element` | `from: int, to: int` | Convert all gems of element `from` to element `to` on the board. |
| `delay_enemies` | `turns: int` | Increase the countdown of all living enemies by `turns`. |

### Outcome Types Used in JSON (Awaiting Full Implementation)

These types appear in the skill JSON data and are planned for registration as either simple effects or complex outcome subclasses:

| Type Key | Parameters | Description |
|---|---|---|
| `single_target_damage` | `element: int, multiplier: float, target: string` | Deal damage to a single enemy (e.g., highest HP). |
| `gem_conversion` | `from_element: int, to_element: int` | Convert gems from one element to another. |
| `heal_over_time` | `recovery_multiplier: float, duration_turns: int` | Heal each turn based on team REC. |
| `atk_buff` | `element: int, multiplier: float, duration_turns: int` | Multiply ATK for the given element. |
| `self_damage` | `hp_percent: float` | Reduce own HP by a percentage. |
| `defense_buff` | `damage_reduction: float, duration_turns: int` | Reduce incoming damage for N turns. |
| `combo_scaling_atk` | `bonus_per_combo: float, duration_turns: int` | ATK bonus that scales with combo count. |
| `element_change` | `target: string, to_element: int, duration_turns: int` | Change a monster's element temporarily. |
| `rec_buff` | `multiplier: float, duration_turns: int` | Multiply team REC for N turns. |
| `lifesteal` | `percent_of_damage: float` | Heal for a percentage of damage dealt by the previous outcome. |
| `element_atk_mult` | `element: int, multiplier: float` | Leader skill: multiply ATK for an element (passive). |
| `element_hp_mult` | `element: int, multiplier: float` | Leader skill: multiply HP for an element (passive). |
| `element_rec_mult` | `element: int, multiplier: float` | Leader skill: multiply REC for an element (passive). |

## Architecture: From 12K LoC to Composition

```
BEFORE (original ToS):                    AFTER (MobileForge):

ActiveSkillExecution.cs (12K+ LoC)        skills.json (~150 lines)
  switch(skillId)                            + 5 condition classes (~80 lines)
    case 1001: ... break;                    + 5 effect functions (~60 lines)
    case 1002: ... break;                    = ~290 lines total
    ...400+ cases...
                                           Adding a new skill:
Adding a new skill:                          1. Add JSON entry (3-10 lines)
  1. Add case to switch (~50 lines)          2. Maybe register 1 new type (~20 lines)
  2. Copy-paste from similar skill
  3. Adjust hardcoded values
```

The data-driven approach means game designers can create new skills by editing JSON, without touching GDScript. Only genuinely novel mechanics require new code.
