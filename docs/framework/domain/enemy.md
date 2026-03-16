# Enemy Module

## Overview

Manages **enemy state, AI decision-making, and special characteristics** for dungeon encounters. Each enemy has a countdown timer that ticks each turn; when it reaches zero, the enemy attacks. Enemies can carry special shields and abilities that force the player to adapt their strategy (bring certain elements, hit combo thresholds, etc.).

## Module Files

| File | Purpose | Dependencies |
|---|---|---|
| `enemy_types` | EnemyState, EnemyAction, EnemyCharacteristic, CountdownState | None (leaf) |
| `enemy_ai` | EnemyAI: countdown ticking, action selection, status processing | enemy_types |

## EnemyState Fields

`EnemyState` represents one enemy in a dungeon wave. It is mutable and updated each turn.

| Field | Type | Description |
|---|---|---|
| `id` | `String` | Unique identifier for this enemy instance |
| `def_id` | `String` | Reference to the enemy definition template |
| `name` | `String` | Display name |
| `element` | `Element` | Enemy's element (WATER, FIRE, GRASS, LIGHT, DARK) |
| `max_hp` | `int` | Maximum hit points |
| `current_hp` | `int` | Current hit points |
| `atk` | `int` | Attack power dealt to player when attacking |
| `defense` | `int` | Damage reduction applied during Stage 7 of the combat pipeline |
| `countdown` | `int` | Current countdown value (attacks when this reaches 0) |
| `max_countdown` | `int` | Countdown resets to this value after attacking |
| `status_effects` | `Array[StatusEffect]` | Active status effects (poison, bind, delay, etc.) |
| `characteristics` | `Array[EnemyCharacteristic]` | Special defensive/offensive traits |
| `is_alive` | `bool` | False when current_hp <= 0 |
| `flags` | `Dictionary` | Arbitrary state for custom AI behaviors |

## EnemyAction Types

`EnemyAction` describes what an enemy does on its turn. Actions are returned by `decide_action()` and consumed by the DungeonRunner.

| Action Type | Fields | Description |
|---|---|---|
| `ATTACK` | `damage: int, element: Element` | Deal damage to the player |
| `ATTACK_MULTI` | `hits: int, damage_per_hit: int, element: Element` | Multi-hit attack |
| `HEAL_SELF` | `amount: int` | Enemy heals itself |
| `BUFF_SELF` | `stat: String, multiplier: float, turns: int` | Enemy buffs its own stats |
| `DEBUFF_PLAYER` | `type: String, params: Dictionary, turns: int` | Apply a debuff to the player (bind, blind, etc.) |
| `ENRAGE` | `atk_multiplier: float` | Permanently increase ATK (used at HP thresholds) |
| `SUMMON` | `enemy_def_id: String` | Spawn an additional enemy |
| `SKILL` | `skill_id: String` | Enemy uses a special skill |
| `NONE` | | Do nothing this turn (used for passive enemies) |

## Enemy Countdown Mechanic

The countdown is the core turn-timing mechanic for enemies. Each enemy has an independent countdown that decrements every player turn. When it hits zero, the enemy performs its action and the countdown resets.

```
Turn 1: Enemy countdown = 3
         Player acts. Countdown ticks: 3 -> 2.

Turn 2: Enemy countdown = 2
         Player acts. Countdown ticks: 2 -> 1.

Turn 3: Enemy countdown = 1
         Player acts. Countdown ticks: 1 -> 0.
         Enemy countdown reached 0 -> Enemy attacks!
         Countdown resets to max_countdown (3).

Turn 4: Enemy countdown = 3
         (cycle repeats)
```

### Multiple Enemies

Each enemy has its own independent countdown. In a wave with 3 enemies, they may attack on different turns:

```
          Turn 1   Turn 2   Turn 3   Turn 4   Turn 5
Enemy A:    3->2     2->1    1->0*    3->2     2->1
Enemy B:    1->0*    2->1     1->0*   2->1     1->0*
Enemy C:    2->1     1->0*    3->2    2->1     1->0*

* = attacks this turn
```

The player must decide which enemy to prioritize based on countdown, damage, and characteristics.

## EnemyAI API

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `tick_countdowns` | `enemies: Array[EnemyState]` | `Array[EnemyState]` | Decrement all alive enemies' countdowns by 1 |
| `can_attack` | `enemy: EnemyState` | `bool` | True if countdown == 0 |
| `decide_action` | `enemy: EnemyState, context: Dictionary` | `EnemyAction` | Determine what action this enemy takes (called only when countdown hits 0) |
| `reset_countdown` | `enemy: EnemyState` | `void` | Reset countdown to max_countdown after attacking |
| `tick_enemy_statuses` | `enemy: EnemyState` | `Array[String]` | Process status effects (reduce durations, apply DoT), return expired status names |
| `apply_status` | `enemy: EnemyState, effect: StatusEffect` | `void` | Add or refresh a status effect on an enemy |
| `is_delayed` | `enemy: EnemyState` | `bool` | True if enemy has an active delay status (countdown frozen) |
| `get_ready_enemies` | `enemies: Array[EnemyState]` | `Array[EnemyState]` | Filter to enemies whose countdown == 0 |

### Default AI Logic

The default `decide_action()` implementation uses a simple priority system:

```
1. If HP < 20% and has ENRAGE characteristic  -->  ENRAGE
2. If has heal ability and HP < 50%            -->  HEAL_SELF
3. Otherwise                                   -->  ATTACK
```

Game code can override `decide_action()` for boss-specific AI patterns.

## Enemy Characteristics

Enemy characteristics are special traits loaded from the enemy definition. They modify how damage is received or add special behaviors. These map directly to Tower of Saviors mechanics:

### ComboShield

The enemy requires the player to reach a minimum combo count before any damage is dealt. Combos below the threshold deal **zero damage**.

| Field | Type | Description |
|---|---|---|
| `type` | `"combo_shield"` | Characteristic identifier |
| `min_combos` | `int` | Minimum combo count required |

```
Player hits 4 combos. Enemy has ComboShield(min_combos=6).
Result: ALL damage is nullified. Player must solve better.

Player hits 7 combos. Enemy has ComboShield(min_combos=6).
Result: Damage passes through normally.
```

**Implementation:** Registers a `CAN_ZERO` hook that checks `ctx.total_combos < min_combos` and sets damage to 0.

### ElementShield

The enemy is **immune** to damage from specified elements. Attacks of those elements deal zero damage.

| Field | Type | Description |
|---|---|---|
| `type` | `"element_shield"` | Characteristic identifier |
| `immune_elements` | `Array[Element]` | Elements that deal no damage |

```
Enemy has ElementShield(immune_elements=[WATER, FIRE]).
Water attack: 0 damage.
Fire attack: 0 damage.
Grass attack: normal damage.
Light attack: normal damage.
```

**Implementation:** Registers a `CAN_ZERO` hook that checks if `ctx.element` is in the immune list.

### DamageAbsorb

The enemy **heals** from attacks of specified elements instead of taking damage.

| Field | Type | Description |
|---|---|---|
| `type` | `"damage_absorb"` | Characteristic identifier |
| `absorb_elements` | `Array[Element]` | Elements that heal the enemy |

```
Enemy has DamageAbsorb(absorb_elements=[DARK]).
Dark attack dealing 5000 damage: enemy HEALS 5000 HP instead.
Light attack dealing 5000 damage: normal damage.
```

**Implementation:** Registers a `CAN_ZERO` hook that inverts damage to negative (healing) for matching elements.

### DamageReduction

A percentage of all incoming damage is reduced.

| Field | Type | Description |
|---|---|---|
| `type` | `"damage_reduction"` | Characteristic identifier |
| `percent` | `float` | Reduction percentage (0.0 to 1.0) |

```
Enemy has DamageReduction(percent=0.5).
Attack dealing 10000 damage: 10000 * (1 - 0.5) = 5000 damage.
```

**Implementation:** Registers a `POST_DEFENSE` hook that multiplies damage by `(1.0 - percent)`.

### DamageCap

Maximum damage the enemy can receive per single hit. Any damage above the cap is discarded.

| Field | Type | Description |
|---|---|---|
| `type` | `"damage_cap"` | Characteristic identifier |
| `max_damage` | `int` | Maximum damage per hit |

```
Enemy has DamageCap(max_damage=200000).
Attack dealing 500000 damage: capped to 200000.
Attack dealing 100000 damage: passes through as-is (under cap).
```

**Implementation:** Registers a `POST_DEFENSE` hook that clamps damage to `min(damage, max_damage)`.

### Characteristics Summary Table

| Characteristic | Pipeline Hook | Effect |
|---|---|---|
| ComboShield | `CAN_ZERO` | Zero damage if combos < threshold |
| ElementShield | `CAN_ZERO` | Zero damage from specified elements |
| DamageAbsorb | `CAN_ZERO` | Heal from specified elements |
| DamageReduction | `POST_DEFENSE` | Multiply damage by (1 - percent) |
| DamageCap | `POST_DEFENSE` | Clamp damage to max per hit |

## Custom AI: Overriding decide_action

For bosses and special encounters, game code overrides the default AI behavior. The `context` dictionary provides full battle state for decision-making.

### Context Dictionary for decide_action

| Key | Type | Description |
|---|---|---|
| `"player_hp_ratio"` | `float` | Player's current HP / max HP |
| `"turn_number"` | `int` | Current turn count |
| `"wave_enemies_alive"` | `int` | Number of alive enemies in wave |
| `"last_combo_count"` | `int` | Player's combo count from previous turn |
| `"elements_matched"` | `Dictionary` | Elements the player matched last turn |

### Example: Boss with Phase Transitions

```gdscript
# GDScript
class_name DragonBossAI extends EnemyAI

## Dragon boss has 3 phases based on HP thresholds.
## Phase 1 (100%-60%): Normal attacks
## Phase 2 (60%-30%): Multi-hit attacks + debuffs
## Phase 3 (<30%): Enraged, heavy attacks every turn

func decide_action(enemy: EnemyState, context: Dictionary) -> EnemyAction:
    var hp_ratio := float(enemy.current_hp) / float(enemy.max_hp)

    # Phase 3: below 30% HP
    if hp_ratio < 0.3:
        if not enemy.flags.get("enraged", false):
            enemy.flags["enraged"] = true
            return EnemyAction.new(EnemyAction.ENRAGE, {
                "atk_multiplier": 2.5
            })
        return EnemyAction.new(EnemyAction.ATTACK, {
            "damage": int(enemy.atk * 2.5),
            "element": enemy.element
        })

    # Phase 2: below 60% HP
    if hp_ratio < 0.6:
        # Alternate between multi-hit and debuff
        if enemy.flags.get("last_was_debuff", false):
            enemy.flags["last_was_debuff"] = false
            return EnemyAction.new(EnemyAction.ATTACK_MULTI, {
                "hits": 3,
                "damage_per_hit": enemy.atk / 2,
                "element": enemy.element
            })
        else:
            enemy.flags["last_was_debuff"] = true
            return EnemyAction.new(EnemyAction.DEBUFF_PLAYER, {
                "type": "blind",
                "params": {"blind_count": 6},
                "turns": 3
            })

    # Phase 1: normal attack
    return EnemyAction.new(EnemyAction.ATTACK, {
        "damage": enemy.atk,
        "element": enemy.element
    })
```

```csharp
// C#
public class DragonBossAI : EnemyAI
{
    public override EnemyAction DecideAction(EnemyState enemy,
                                              Dictionary<string, object> context)
    {
        float hpRatio = (float)enemy.CurrentHp / enemy.MaxHp;

        // Phase 3: below 30%
        if (hpRatio < 0.3f)
        {
            if (!enemy.Flags.ContainsKey("enraged"))
            {
                enemy.Flags["enraged"] = true;
                return new EnemyAction(EnemyActionType.Enrage,
                    atkMultiplier: 2.5f);
            }
            return new EnemyAction(EnemyActionType.Attack,
                damage: (int)(enemy.Atk * 2.5f),
                element: enemy.Element);
        }

        // Phase 2: below 60%
        if (hpRatio < 0.6f)
        {
            bool lastWasDebuff = enemy.Flags.ContainsKey("last_was_debuff")
                                 && (bool)enemy.Flags["last_was_debuff"];
            if (lastWasDebuff)
            {
                enemy.Flags["last_was_debuff"] = false;
                return new EnemyAction(EnemyActionType.AttackMulti,
                    hits: 3,
                    damagePerHit: enemy.Atk / 2,
                    element: enemy.Element);
            }
            else
            {
                enemy.Flags["last_was_debuff"] = true;
                return new EnemyAction(EnemyActionType.DebuffPlayer,
                    debuffType: "blind",
                    turns: 3);
            }
        }

        // Phase 1: normal
        return new EnemyAction(EnemyActionType.Attack,
            damage: enemy.Atk,
            element: enemy.Element);
    }
}
```

## Code Examples

### Creating Enemies for a Wave

```gdscript
# GDScript
var enemy_a := EnemyState.new()
enemy_a.id = "goblin_01"
enemy_a.name = "Fire Goblin"
enemy_a.element = Element.FIRE
enemy_a.max_hp = 50000
enemy_a.current_hp = 50000
enemy_a.atk = 3000
enemy_a.defense = 100
enemy_a.countdown = 2
enemy_a.max_countdown = 2

var enemy_b := EnemyState.new()
enemy_b.id = "dragon_boss"
enemy_b.name = "Inferno Dragon"
enemy_b.element = Element.FIRE
enemy_b.max_hp = 5000000
enemy_b.current_hp = 5000000
enemy_b.atk = 15000
enemy_b.defense = 500
enemy_b.countdown = 3
enemy_b.max_countdown = 3
enemy_b.characteristics = [
    EnemyCharacteristic.new("combo_shield", {"min_combos": 5}),
    EnemyCharacteristic.new("damage_cap", {"max_damage": 200000})
]

var enemies := [enemy_a, enemy_b]
```

```csharp
// C#
var enemyA = new EnemyState
{
    Id = "goblin_01",
    Name = "Fire Goblin",
    Element = Element.Fire,
    MaxHp = 50000,
    CurrentHp = 50000,
    Atk = 3000,
    Defense = 100,
    Countdown = 2,
    MaxCountdown = 2
};

var enemyB = new EnemyState
{
    Id = "dragon_boss",
    Name = "Inferno Dragon",
    Element = Element.Fire,
    MaxHp = 5000000,
    CurrentHp = 5000000,
    Atk = 15000,
    Defense = 500,
    Countdown = 3,
    MaxCountdown = 3,
    Characteristics = new List<EnemyCharacteristic>
    {
        new("combo_shield", new() { ["min_combos"] = 5 }),
        new("damage_cap", new() { ["max_damage"] = 200000 })
    }
};

var enemies = new List<EnemyState> { enemyA, enemyB };
```

### Running Enemy Turn Logic

```gdscript
# GDScript
var ai := EnemyAI.new()

# Tick all countdowns
ai.tick_countdowns(enemies)

# Find which enemies are ready to attack
var ready := ai.get_ready_enemies(enemies)

# Each ready enemy decides its action
var total_damage := 0
for enemy in ready:
    var action := ai.decide_action(enemy, {
        "player_hp_ratio": float(player_hp) / float(max_hp),
        "turn_number": current_turn,
        "wave_enemies_alive": enemies.filter(func(e): return e.is_alive).size(),
        "last_combo_count": last_combo
    })

    match action.type:
        EnemyAction.ATTACK:
            total_damage += action.damage
        EnemyAction.ATTACK_MULTI:
            total_damage += action.hits * action.damage_per_hit
        EnemyAction.HEAL_SELF:
            enemy.current_hp = min(enemy.current_hp + action.amount, enemy.max_hp)
        EnemyAction.ENRAGE:
            enemy.atk = int(enemy.atk * action.atk_multiplier)
        EnemyAction.DEBUFF_PLAYER:
            apply_player_debuff(action)

    # Reset countdown after acting
    ai.reset_countdown(enemy)

# Process status effects on all enemies
for enemy in enemies:
    if enemy.is_alive:
        var expired := ai.tick_enemy_statuses(enemy)
        for status_name in expired:
            print(enemy.name, ": ", status_name, " expired")
```

```csharp
// C#
var ai = new EnemyAI();

ai.TickCountdowns(enemies);

var ready = ai.GetReadyEnemies(enemies);

int totalDamage = 0;
foreach (var enemy in ready)
{
    var context = new Dictionary<string, object>
    {
        ["player_hp_ratio"] = (float)playerHp / maxHp,
        ["turn_number"] = currentTurn,
        ["wave_enemies_alive"] = enemies.Count(e => e.IsAlive),
        ["last_combo_count"] = lastCombo
    };

    var action = ai.DecideAction(enemy, context);

    switch (action.Type)
    {
        case EnemyActionType.Attack:
            totalDamage += action.Damage;
            break;
        case EnemyActionType.AttackMulti:
            totalDamage += action.Hits * action.DamagePerHit;
            break;
        case EnemyActionType.HealSelf:
            enemy.CurrentHp = Math.Min(enemy.CurrentHp + action.Amount, enemy.MaxHp);
            break;
        case EnemyActionType.Enrage:
            enemy.Atk = (int)(enemy.Atk * action.AtkMultiplier);
            break;
        case EnemyActionType.DebuffPlayer:
            ApplyPlayerDebuff(action);
            break;
    }

    ai.ResetCountdown(enemy);
}

foreach (var enemy in enemies.Where(e => e.IsAlive))
{
    var expired = ai.TickEnemyStatuses(enemy);
    foreach (string statusName in expired)
        Console.WriteLine($"{enemy.Name}: {statusName} expired");
}
```

### Applying Status Effects to Enemies

```gdscript
# GDScript
var ai := EnemyAI.new()

# Player skill poisons an enemy for 3 turns
var poison := StatusEffect.new()
poison.type = "poison"
poison.damage_per_turn = 10000
poison.turns_left = 3
ai.apply_status(enemy, poison)

# Player skill delays an enemy by 2 turns (adds to countdown)
var delay := StatusEffect.new()
delay.type = "delay"
delay.delay_turns = 2
delay.turns_left = 1  # one-shot effect
ai.apply_status(enemy, delay)
enemy.countdown += delay.delay_turns  # delay increases countdown

# Check if enemy is delayed (countdown frozen)
if ai.is_delayed(enemy):
    print(enemy.name, " is delayed, will not tick countdown")
```

```csharp
// C#
var ai = new EnemyAI();

var poison = new StatusEffect
{
    Type = "poison",
    DamagePerTurn = 10000,
    TurnsLeft = 3
};
ai.ApplyStatus(enemy, poison);

var delay = new StatusEffect
{
    Type = "delay",
    DelayTurns = 2,
    TurnsLeft = 1
};
ai.ApplyStatus(enemy, delay);
enemy.Countdown += delay.DelayTurns;

if (ai.IsDelayed(enemy))
    Console.WriteLine($"{enemy.Name} is delayed");
```
