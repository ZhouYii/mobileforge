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
# In skill_defs/conditions/my_condition.gd
class_name MyCondition extends MFSkillCondition

func is_valid(ctx: RefCounted) -> bool:
    return some_check(ctx)
```

Register it in `register_all.gd`:
```gdscript
pipeline.condition_registry.register("my_condition", func(params):
    return MyCondition.new(params))
```

### Step 4: Register a Complex Outcome (MFSkillOutcome subclass)

For outcomes that persist across turns or register combat hooks, create a subclass in `skill_defs/outcomes/`:

```gdscript
class_name TosShieldOutcome extends MFSkillOutcome

func activate(context: RefCounted, result: RefCounted) -> void:
    turns_left = int(get_param("duration_turns", 2))
    result.buffs_applied.append({"type": "shield", "turns": turns_left})

func deactivate(context: RefCounted) -> void:
    pass  # cleanup
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
            "conditions": [{"type": "always_true", "params": {}}],
            "outcomes": [{"type": "defense_buff", "params": {"damage_reduction": 0.5, "duration_turns": 3}}]
        }
    ]
}
```

### Step 6: Assign to a Monster

Set the monster's `active_skill_id` in `monsters.json` to the new skill's `id`.

## Complete List of Registered Conditions (7 total)

| Type Key | Parameters | Description |
|---|---|---|
| `always_true` | (none) | Always passes. Used for unconditional skills. |
| `combo_above` | `threshold: int` | Passes when `combo_count >= threshold`. |
| `combo_gte` | `min_combo: int` | Passes when `combo_count >= min_combo`. Alias for combo_above with different param name. |
| `combo_lt` | `max_combo: int` | Passes when `combo_count < max_combo`. |
| `hp_below` | `percent: float` | Passes when team HP is at or below the given percentage (0.0-1.0). |
| `elements_matched` | `element: int, min_count: int` | Passes when at least `min_count` gems of the given element were matched this turn. |
| `team_has_element` | `element: int` | Passes when at least one team monster has the specified element. |

## Complete List of Registered Effects/Outcomes (15 total)

### Simple Effects (EffectRegistry) — 10

| Type Key | Parameters | Description |
|---|---|---|
| `area_damage` | `multiplier: float` | Deal `ATK * multiplier` damage to all living enemies. Uses the first team member's ATK stat. |
| `heal_flat` | `amount: int` | Heal the team for a flat HP amount. |
| `heal_percent` | `percent: float` | Heal the team for a percentage of max HP. |
| `change_gem_element` | `from: int, to: int` | Convert all gems of element `from` to element `to` on the board. |
| `delay_enemies` | `turns: int` | Increase the countdown of all living enemies by `turns`. |
| `single_target_damage` | `multiplier: float, target: string` | Deal `ATK * multiplier` to one enemy (e.g., `"highest_hp"`). |
| `gem_conversion` | `from_element: int, to_element: int` | Alias for `change_gem_element` using `from_element`/`to_element` param names. |
| `self_damage` | `hp_percent: float` | Reduce own HP by a percentage. HP floor is 1 (can't self-kill). |
| `element_change` | `to_element: int, duration_turns: int` | Change a monster's element temporarily. Tracked via `buffs_applied`. |
| `rec_buff` | `multiplier: float, duration_turns: int` | Multiply team REC for healing calculations. Tracked via `buffs_applied`. |
| `lifesteal` | `percent_of_damage: float` | Heal for a percentage of damage dealt by previous outcomes in this activation. |

### Persistent Outcomes (OutcomeRegistry, SkillOutcomeBase subclasses) — 4

| Type Key | Class | Parameters | Description |
|---|---|---|---|
| `heal_over_time` | `TosHealOverTimeOutcome` | `recovery_multiplier: float, duration_turns: int` | Heals each turn based on `team_REC * multiplier`. |
| `atk_buff` | `TosAtkBuffOutcome` | `element: int, multiplier: float, duration_turns: int` | Registers a MAIN hook that multiplies damage. Element 0 = all. |
| `defense_buff` | `TosDefenseBuffOutcome` | `damage_reduction: float, duration_turns: int` | Registers a POST_DEFENSE hook that reduces incoming damage. |
| `combo_scaling_atk` | `TosComboScalingAtkOutcome` | `bonus_per_combo: float, duration_turns: int` | Registers a MAIN hook: `damage *= (1 + (combos - 1) * bonus)`. |

### Team Skill Effects (EffectRegistry) — 3

| Type Key | Parameters | Description |
|---|---|---|
| `element_atk_mult` | `element: int, multiplier: float` | Passive: multiply ATK for an element. |
| `element_hp_mult` | `element: int, multiplier: float` | Passive: multiply HP for an element. |
| `element_rec_mult` | `element: int, multiplier: float` | Passive: multiply REC for an element. |

## Architecture: From 12K LoC to Composition

```
BEFORE (original ToS):                    AFTER (MobileForge):

ActiveSkillExecution.cs (12K+ LoC)        skills.json (~165 lines)
  switch(skillId)                            + 7 condition classes (~100 lines)
    case 1001: ... break;                    + 10 simple effects (~200 lines)
    case 1002: ... break;                    + 4 persistent outcomes (~180 lines)
    ...400+ cases...                         + 3 team skill effects (~30 lines)
                                           = ~675 lines total
Adding a new skill:
  1. Add case to switch (~50 lines)        Adding a new skill:
  2. Copy-paste from similar skill           1. Add JSON entry (3-10 lines)
  3. Adjust hardcoded values                 2. Maybe register 1 new type (~20 lines)
```

The data-driven approach means game designers can create new skills by editing JSON, without touching GDScript. Only genuinely novel mechanics require new code.
