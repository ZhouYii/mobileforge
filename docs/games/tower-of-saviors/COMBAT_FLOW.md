# Tower of Saviors Combat Flow

## Overview

Combat in Tower of Saviors follows a strict turn-based sequence. Each dungeon consists of multiple waves of enemies. The player clears waves by matching gems on the board, dealing damage through the combat pipeline, and surviving enemy counterattacks. The `MFDungeonRunner` orchestrates the full battle loop.

## Turn Sequence

Each turn has two phases: the **player phase** and the **enemy phase**.

```
PLAYER PHASE                              ENEMY PHASE
-----------                               -----------
1. Player drags gems on board             5. Tick enemy countdowns
2. Cascade resolves (matches found)       6. Ready enemies attack
3. Player damage dealt to enemies         7. Reset attacking enemies' countdowns
4. Heart gem healing applied              8. Tick enemy status effects
                                          9. Check team death (HP <= 0)

After both phases:
  - Check wave clear (all enemies dead)
  - If wave cleared and more waves remain -> load next wave
  - If wave cleared and no more waves -> battle won
  - If team HP <= 0 -> battle lost
```

## Player Phase Detail

### Step 1-2: Board Resolution

The player drags a gem across the board. `MFBoardLogic.move_gem_path()` performs the swaps, then `MFCascadeResolver.resolve()` produces an `Array[CascadeStep]` describing all matches and cascades.

### Step 3: Damage Calculation

`MFDungeonRunner.execute_player_turn()` processes damage:

1. **Aggregate matches**: Count total combos and gems matched per element across all cascade steps.

2. **Skill pipeline turn start**: Call `_skill_pipeline.process_turn_start(context)` to trigger any per-turn-start effects from persistent outcomes.

3. **For each combo, for each team monster, for each enemy**:
   - Skip if match element is `HEART` (handled separately for healing)
   - Build a `DamageContext` with attacker/defender stats
   - Run through the 5-hook damage pipeline (see below)
   - Apply damage to enemy via `enemy.take_damage()`
   - Track kills

4. **Skill pipeline turn end**: Call `_skill_pipeline.process_turn_end(context)` to tick durations and expire finished outcomes.

### Step 4: Heart Gem Healing

Heart gems heal the team instead of dealing damage. The formula:

```
healing = total_team_rec * heart_gems_matched * combo_multiplier * 0.1
```

Where:
- `total_team_rec` = sum of all team members' REC stat
- `heart_gems_matched` = total heart gems across all cascade steps
- `combo_multiplier` = `1.0 + (total_combos - 1) * 0.25`

Healing is capped at `max_hp`.

## The 5-Hook Damage Pipeline

`MFCombatResolver` processes each attack through a five-stage hook pipeline. This mirrors ToS's `SkillInstance` event system where skills register hooks at specific points to modify damage.

### Pipeline Stages

```
1. Calculate base gem damage
   base_damage = ATK * (1.0 + max(gems_matched - 3, 0) * 0.25)

2. Apply combo multiplier
   damage *= 1.0 + (combo_count - 1) * 0.25

3. PRE_ELEMENT hooks fire          <-- Skills modify damage before element calc

4. Element multiplier applied
   damage *= element_chart.get_multiplier(attacker, defender)

5. POST_ELEMENT hooks fire         <-- Skills modify damage after element calc

6. MAIN hooks fire                 <-- Leader skills, team skills apply multipliers

7. Subtract defense
   damage = max(damage - defender_defense, 1.0)

8. POST_DEFENSE hooks fire         <-- Damage reduction shields, etc.

9. CAN_ZERO hooks fire             <-- Some skills can force damage to 0
```

### Hook Registration

Skills register hooks via `MFCombatResolver.register_hook()`:

```gdscript
combat.register_hook(
    MFCombatTypes.DamageHook.MAIN,   # Which stage
    my_callback,                      # Callable that receives DamageContext
    100,                              # Priority (lower fires first)
    "my_buff_name"                    # Name for tracking/debugging
)
```

Multiple hooks at the same stage fire in priority order. Each hook receives the `DamageContext` and can modify its `damage` field.

### DamageContext Fields

| Field | Type | Description |
|---|---|---|
| `attacker_element` | `int` | Element of the attacking monster |
| `defender_element` | `int` | Element of the defending enemy |
| `base_damage` | `float` | Damage before any hooks |
| `damage` | `float` | Running total, modified by each stage |
| `combo_count` | `int` | Total combos this turn |
| `combo_index` | `int` | Which combo this attack belongs to |
| `gems_matched` | `int` | Gems of this element matched in this combo |
| `attacker_atk` | `float` | Monster's ATK stat |
| `defender_defense` | `float` | Enemy's DEF stat |
| `is_skill` | `bool` | Whether this is skill damage vs normal attack |
| `extra` | `Dictionary` | Arbitrary data for hook communication |

### DamageResult Output

The pipeline returns `MFCombatTypes.DamageResult`:

| Field | Type | Description |
|---|---|---|
| `final_damage` | `int` | The integer damage dealt |
| `element_multiplier` | `float` | The element advantage/disadvantage multiplier used |
| `combo_multiplier` | `float` | The combo multiplier used |
| `hooks_applied` | `Array[String]` | Names of hooks that fired |

## Element Chart

The element advantage system uses `MFElementChart` with the following relationships:

### Advantage (1.5x damage)

```
Water (1) ---> Fire (2)       "Water beats Fire"
Fire  (2) ---> Grass (3)      "Fire beats Grass"
Grass (3) ---> Water (1)      "Grass beats Water"
Light (4) ---> Dark (5)       "Light beats Dark"
Dark  (5) ---> Light (4)      "Dark beats Light"
```

### Disadvantage (0.5x damage)

```
Water (1) -x-> Grass (3)      "Water weak to Grass"
Fire  (2) -x-> Water (1)      "Fire weak to Water"
Grass (3) -x-> Fire (2)       "Grass weak to Fire"
```

### Neutral (1.0x damage)

All other combinations, including:
- Same element vs same element
- Light/Dark vs Water/Fire/Grass
- Heart gems (element 6) do not deal damage

The triangle (Water > Fire > Grass > Water) and the dual (Light <> Dark mutual advantage) are the core strategic considerations in team building.

## Healing Mechanics

### Heart Gem Healing (Per Turn)

Heart gems matched during cascade resolution trigger healing during the player phase:

```
healing = sum(team_rec) * heart_gems_matched * combo_multiplier * 0.1
```

This means:
- More heart gems = more healing
- Higher combo count boosts healing (same multiplier as damage)
- Team REC stat matters for sustainability

### Skill-Based Healing

Skills can heal via two mechanisms:

1. **Flat heal** (`heal_flat` effect): Adds a fixed amount to `SkillResult.healing`. Applied immediately when the skill activates.

2. **Percent heal** (`heal_percent` effect): Heals a percentage of `max_hp`. Applied immediately.

3. **Heal over time** (`heal_over_time` outcome): A persistent outcome that heals each turn for a duration. Uses the outcome's `on_turn_start` or `on_turn_end` callback.

### HP Capping

All healing is capped at `max_hp`:
```gdscript
state.team_hp = mini(state.team_hp + healing, state.max_hp)
```

## Enemy Phase Detail

### Step 5: Countdown Ticking

Each enemy has a `countdown` that decreases by 1 each turn. `MFEnemyAI.tick_countdowns()` processes all living enemies and returns those whose countdown reaches 0 (ready to attack).

### Step 6: Enemy Attacks

Ready enemies attack the team. By default (`MFEnemyAI.decide_action()`), every enemy simply attacks with its ATK stat:

```
damage = max(enemy_atk - team_defense, 1)
team_hp -= damage
```

Enemy damage bypasses the element chart and combo system -- it is a direct ATK-to-HP reduction with defense subtraction. The minimum damage is always 1.

### Step 7: Countdown Reset

After attacking, each enemy's countdown resets to `max_countdown`:

```gdscript
static func reset_countdown(enemy: RefCounted) -> void:
    enemy.countdown = enemy.max_countdown
```

### Step 8: Status Effect Ticking

`MFEnemyAI.tick_enemy_statuses()` decrements the `turns` field on all enemy status effects and removes expired ones.

### Step 9: Death Check

If `team_hp` drops to 0 or below, the battle is lost. `dungeon_runner.state.is_active` is set to `false` and a `battle_lost` event is emitted.

## Enemy AI

The current AI implementation is simple: all enemies attack when their countdown reaches zero. The `MFEnemyAI` module provides static functions that can be extended for more complex patterns:

| Function | Description |
|---|---|
| `tick_countdowns(enemies)` | Decrements all living enemies' countdowns. Returns ready enemies. |
| `decide_action(enemy)` | Returns an `EnemyAction`. Default: always "attack". |
| `reset_countdown(enemy)` | Resets countdown to `max_countdown` after attack. |
| `can_attack(enemy)` | Check if countdown <= 0 and enemy is alive. |
| `tick_enemy_statuses(enemies)` | Tick and expire status effects on all enemies. |

### Enemy State

Each enemy is represented by `MFEnemyTypes.EnemyState`:

| Field | Type | Description |
|---|---|---|
| `id` | `int` | Unique ID within the wave |
| `name` | `String` | Display name |
| `element` | `int` | Element (1-5) |
| `hp` / `max_hp` | `int` | Current and maximum HP |
| `atk` | `float` | Attack power |
| `defense` | `float` | Defense (subtracted from incoming damage) |
| `countdown` | `int` | Turns until next attack |
| `max_countdown` | `int` | Countdown reset value |
| `status_effects` | `Array[Dictionary]` | Active status effects |
| `is_alive` | `bool` | Computed: `hp > 0` |

### Enemy Countdown Display

The countdown number is shown next to each enemy in the UI. A countdown of 1 means the enemy attacks **this turn** (after tick). Players use this information to prioritize killing dangerous enemies before they attack.

## Wave Progression

Dungeons have multiple waves of enemies. When all enemies in a wave are killed:

1. `TurnResult.wave_cleared` is set to `true`.
2. If more waves remain, `_load_wave(next_index)` spawns the next set of enemies.
3. If no more waves remain, `TurnResult.battle_won` is set to `true`, rewards are assigned, and the dungeon run ends.

### Wave Loading

Each wave is defined as an array of enemy data dictionaries. `MFDungeonRunner._load_wave()` clears the current enemies and creates new `EnemyState` instances from the wave definition.

## Complete Turn Flow Diagram

```
Player drags gem on board
         |
         v
   move_gem_path()
         |
         v
   resolve_cascade() -> CascadeSteps
         |
         v
   execute_player_turn(steps, team, stats)
    |
    +-- Aggregate matches by element
    +-- process_turn_start() (skill pipeline)
    +-- For each combo:
    |     For each team monster:
    |       For each living enemy:
    |         build DamageContext
    |         resolve_player_attack(ctx) -> DamageResult
    |         enemy.take_damage(result.final_damage)
    +-- Apply heart healing
    +-- process_turn_end() (skill pipeline)
    +-- Check wave clear
    |
    v
   TurnResult
    |
    +-- If wave_cleared and more waves -> load_wave()
    +-- If battle_won -> navigate to result screen
    +-- If still fighting -> execute_enemy_turn()
           |
           +-- tick_countdowns() -> ready enemies
           +-- For each ready enemy:
           |     decide_action() -> EnemyAction
           |     resolve_enemy_attack(atk) -> damage
           |     team_hp -= damage
           |     reset_countdown()
           +-- tick_enemy_statuses()
           +-- If team_hp <= 0 -> battle lost
           |
           v
         Refresh UI for next turn
```
