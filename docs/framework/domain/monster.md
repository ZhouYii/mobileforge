# Monster Module

## Overview

Monsters use a **definition/instance split**. A `MonsterDef` is an immutable template loaded from JSON (the game's bestiary entry). A `MonsterInstance` is a player-owned, mutable copy with its own level, experience, plus stats, and awakening state. This separation means 1000 players can each own a "Fire Dragon" without duplicating the template data.

## Module Files

| File | Purpose | Dependencies |
|---|---|---|
| `monster_types` | MonsterDef, MonsterInstance, Element enum, Rarity enum, ExpCurve | None (leaf) |
| `stat_calculator` | StatCalculator: level-based stat interpolation with curve exponents | monster_types |
| `monster_manager` | MonsterManager: creation, leveling, fusion, evolution operations | monster_types, stat_calculator |

## MonsterDef Fields

`MonsterDef` is the read-only template. One per monster species in the game.

| Field | Type | Description |
|---|---|---|
| `id` | `String` | Unique identifier (e.g., `"fire_dragon_001"`) |
| `name` | `String` | Display name |
| `element` | `Element` | Primary element (WATER, FIRE, GRASS, LIGHT, DARK) |
| `sub_element` | `Element` | Secondary element (or NONE) |
| `rarity` | `Rarity` | Star rating: COMMON(1), UNCOMMON(2), RARE(3), EPIC(4), LEGENDARY(5), MYTHIC(6) |
| `max_level` | `int` | Maximum level for this evolution stage |
| `exp_curve` | `ExpCurve` | Growth rate enum: FAST, STANDARD, SLOW, SUPER_SLOW |
| `base_hp` | `float` | HP at level 1 |
| `max_hp` | `float` | HP at max level (before plus stats) |
| `base_atk` | `float` | ATK at level 1 |
| `max_atk` | `float` | ATK at max level (before plus stats) |
| `base_rec` | `float` | REC at level 1 |
| `max_rec` | `float` | REC at max level (before plus stats) |
| `curve_exp` | `float` | Exponent controlling stat growth curve shape (default 1.0) |
| `leader_skill_id` | `String` | Reference to leader skill definition (or empty) |
| `active_skill_id` | `String` | Reference to active skill definition (or empty) |
| `awakenings` | `Array[String]` | List of awakening ability IDs |
| `evolves_to` | `String` | MonsterDef ID of next evolution (or empty) |
| `exp_value` | `int` | Base experience granted when used as fusion fodder |

## MonsterInstance Fields

`MonsterInstance` is the mutable, player-owned entity.

| Field | Type | Description |
|---|---|---|
| `instance_id` | `String` | Unique per-player instance UUID |
| `def_id` | `String` | Reference to MonsterDef |
| `level` | `int` | Current level (1 to def.max_level) |
| `exp` | `int` | Current experience toward next level |
| `plus_hp` | `int` | Bonus HP added after stat curve (from +egg fusion) |
| `plus_atk` | `int` | Bonus ATK added after stat curve |
| `plus_rec` | `int` | Bonus REC added after stat curve |
| `skill_level` | `int` | Active skill level (1 to max, reduces cooldown) |
| `awakenings_unlocked` | `int` | How many awakenings are activated (0 to def.awakenings.size()) |
| `is_favorited` | `bool` | Protection flag preventing accidental sale/fusion |
| `obtained_at` | `int` | Unix timestamp of acquisition |

## Stat Calculation Formula

Stats are interpolated between base (level 1) and max (max level) using a power curve:

```
stat = base + (max - base) * ((level - 1) / (maxLevel - 1)) ^ curveExp
```

Where:
- `base` = stat at level 1 (from MonsterDef)
- `max` = stat at max level (from MonsterDef)
- `level` = current level of the MonsterInstance
- `maxLevel` = maximum level from MonsterDef
- `curveExp` = curve exponent from MonsterDef (default 1.0 for linear)

After curve calculation, plus stats are added:

```
final_hp  = curve_hp  + plus_hp * 10
final_atk = curve_atk + plus_atk * 5
final_rec = curve_rec + plus_rec * 3
```

Each +1 in plus_hp/atk/rec translates to a fixed bonus multiplied by a per-stat weight.

### Implementation

```gdscript
class_name StatCalculator extends RefCounted

static func calc_stat(base: float, max_val: float, level: int,
                      max_level: int, curve_exp: float = 1.0) -> float:
    if max_level <= 1:
        return base
    var t := float(level - 1) / float(max_level - 1)
    return base + (max_val - base) * pow(t, curve_exp)

static func calc_final_hp(def: MonsterDef, inst: MonsterInstance) -> int:
    var curve := calc_stat(def.base_hp, def.max_hp, inst.level,
                           def.max_level, def.curve_exp)
    return int(curve) + inst.plus_hp * 10

static func calc_final_atk(def: MonsterDef, inst: MonsterInstance) -> int:
    var curve := calc_stat(def.base_atk, def.max_atk, inst.level,
                           def.max_level, def.curve_exp)
    return int(curve) + inst.plus_atk * 5

static func calc_final_rec(def: MonsterDef, inst: MonsterInstance) -> int:
    var curve := calc_stat(def.base_rec, def.max_rec, inst.level,
                           def.max_level, def.curve_exp)
    return int(curve) + inst.plus_rec * 3
```

```csharp
public static class StatCalculator
{
    public static float CalcStat(float baseVal, float maxVal, int level,
                                  int maxLevel, float curveExp = 1.0f)
    {
        if (maxLevel <= 1) return baseVal;
        float t = (float)(level - 1) / (maxLevel - 1);
        return baseVal + (maxVal - baseVal) * MathF.Pow(t, curveExp);
    }

    public static int CalcFinalHp(MonsterDef def, MonsterInstance inst)
        => (int)CalcStat(def.BaseHp, def.MaxHp, inst.Level,
                         def.MaxLevel, def.CurveExp) + inst.PlusHp * 10;

    public static int CalcFinalAtk(MonsterDef def, MonsterInstance inst)
        => (int)CalcStat(def.BaseAtk, def.MaxAtk, inst.Level,
                         def.MaxLevel, def.CurveExp) + inst.PlusAtk * 5;

    public static int CalcFinalRec(MonsterDef def, MonsterInstance inst)
        => (int)CalcStat(def.BaseRec, def.MaxRec, inst.Level,
                         def.MaxLevel, def.CurveExp) + inst.PlusRec * 3;
}
```

## Experience Curves

The `curve_exp` parameter on MonsterDef controls how stats grow across levels. Different curves suit different monster archetypes:

| ExpCurve | curveExp | Behavior |
|---|---|---|
| FAST | 0.7 | Front-loaded growth. Strong early, diminishing returns at high levels. Good for early-game monsters. |
| STANDARD | 1.0 | Linear growth. Stat gain is constant per level. The default. |
| SLOW | 1.5 | Back-loaded growth. Weak early, accelerates at high levels. Rewards investment. |
| SUPER_SLOW | 2.0 | Extremely back-loaded. Almost flat until final 20% of levels, then skyrockets. Endgame monsters. |

### Stat Growth by Curve (base=100, max=1000, maxLevel=99)

```
Stat
1000 |                                              __.--SUPER_SLOW
     |                                       __.--'
 900 |                                  __.--'
     |                            __.--'  __..--SLOW
 800 |                       __.--' __.--'
     |                  __.--'__.--'
 700 |             __.--'_.--'
     |         __.--_.--'   __..------STANDARD
 600 |     __.--_.--' __.--'
     |  __.-_.--'__.--'
 500 | _.-_.'_.-'
     |.-_.-'     __..------FAST
 400 ._.-' __.--'
     .-'_.-'
 300 ._-'
     |
 200 |
     |
 100 +----+----+----+----+----+----+----+----+----+-->
     1   11   22   33   44   55   66   77   88   99  Level
```

| Level | FAST (0.7) | STANDARD (1.0) | SLOW (1.5) | SUPER_SLOW (2.0) |
|---|---|---|---|---|
| 1 | 100 | 100 | 100 | 100 |
| 10 | 258 | 192 | 147 | 117 |
| 25 | 460 | 321 | 225 | 157 |
| 50 | 670 | 550 | 451 | 326 |
| 75 | 842 | 775 | 714 | 603 |
| 90 | 928 | 909 | 886 | 821 |
| 99 | 1000 | 1000 | 1000 | 1000 |

All curves converge at level 1 (base) and max level (max). The exponent only affects the path between them.

## MonsterManager API

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `new` | `defs: Dictionary[String, MonsterDef]` | `MonsterManager` | Construct with loaded definitions |
| `create_instance` | `def_id: String` | `MonsterInstance` | Create a new level 1 instance |
| `get_def` | `def_id: String` | `MonsterDef` | Look up a definition |
| `calc_hp` | `inst: MonsterInstance` | `int` | Calculate final HP |
| `calc_atk` | `inst: MonsterInstance` | `int` | Calculate final ATK |
| `calc_rec` | `inst: MonsterInstance` | `int` | Calculate final REC |
| `add_exp` | `inst: MonsterInstance, amount: int` | `LevelUpResult` | Grant exp, auto-level-up, return levels gained |
| `fuse` | `target: MonsterInstance, fodder: Array[MonsterInstance]` | `FuseResult` | Consume fodder to grant exp to target |
| `fuse_plus` | `target: MonsterInstance, fodder: MonsterInstance` | `void` | Transfer plus stats from fodder to target |
| `evolve` | `inst: MonsterInstance` | `EvolveResult` | Evolve to next form (requires max level) |
| `can_evolve` | `inst: MonsterInstance` | `bool` | Check if evolution requirements are met |
| `exp_to_next_level` | `inst: MonsterInstance` | `int` | Exp remaining for next level |
| `exp_for_level` | `def_id: String, level: int` | `int` | Total cumulative exp required to reach level |

## Fusion

When monsters are used as fusion material, they grant experience to the target:

```
exp_granted = fodder_level * fodder_rarity * 50
```

| Fodder Rarity | Multiplier | Lv1 Exp | Lv50 Exp | Lv99 Exp |
|---|---|---|---|---|
| COMMON (1) | 50 | 50 | 2,500 | 4,950 |
| UNCOMMON (2) | 100 | 100 | 5,000 | 9,900 |
| RARE (3) | 150 | 150 | 7,500 | 14,850 |
| EPIC (4) | 200 | 200 | 10,000 | 19,800 |
| LEGENDARY (5) | 250 | 250 | 12,500 | 24,750 |
| MYTHIC (6) | 300 | 300 | 15,000 | 29,700 |

Same-element bonus: if the fodder's element matches the target's element, exp is multiplied by 1.5x.

```gdscript
func _calc_fusion_exp(fodder: MonsterInstance, target_element: Element) -> int:
    var def := get_def(fodder.def_id)
    var base_exp := fodder.level * def.rarity * 50
    if def.element == target_element:
        base_exp = int(base_exp * 1.5)
    return base_exp
```

Up to 5 fodder monsters can be fused at once. Their exp values are summed and applied to the target, potentially causing multiple level-ups.

### Plus Stat Fusion

Plus stats (+HP, +ATK, +REC) are transferred through a separate fusion operation:

```gdscript
func fuse_plus(target: MonsterInstance, fodder: MonsterInstance) -> void:
    target.plus_hp = min(target.plus_hp + fodder.plus_hp, 99)
    target.plus_atk = min(target.plus_atk + fodder.plus_atk, 99)
    target.plus_rec = min(target.plus_rec + fodder.plus_rec, 99)
```

Each plus stat caps at +99. A fully plussed monster is described as "+297" (99+99+99).

## Evolution

Evolution transforms a monster into its next form. Requirements:

1. The monster must be at **max level** for its current form
2. The MonsterDef must have a non-empty `evolves_to` field
3. (Optional) specific evolution material monsters may be required

When evolution occurs:

```gdscript
func evolve(inst: MonsterInstance) -> EvolveResult:
    var current_def := get_def(inst.def_id)
    if inst.level < current_def.max_level:
        return EvolveResult.new(false, "Not at max level")
    if current_def.evolves_to.is_empty():
        return EvolveResult.new(false, "No evolution available")

    var new_def_id := current_def.evolves_to
    inst.def_id = new_def_id
    inst.level = 1
    inst.exp = 0
    # Plus stats, skill level, and awakenings are PRESERVED
    return EvolveResult.new(true, "", new_def_id)
```

```csharp
public EvolveResult Evolve(MonsterInstance inst)
{
    var currentDef = GetDef(inst.DefId);
    if (inst.Level < currentDef.MaxLevel)
        return new EvolveResult(false, "Not at max level");
    if (string.IsNullOrEmpty(currentDef.EvolvesTo))
        return new EvolveResult(false, "No evolution available");

    inst.DefId = currentDef.EvolvesTo;
    inst.Level = 1;
    inst.Exp = 0;
    // Plus stats, skill level, awakenings preserved
    return new EvolveResult(true, "", currentDef.EvolvesTo);
}
```

Key behaviors:
- Level resets to 1, exp resets to 0
- Plus stats (+HP, +ATK, +REC) are **preserved**
- Skill level is preserved
- Unlocked awakenings are preserved (capped to new def's awakening count)
- The new form typically has a higher max level and better base/max stats

## Code Examples

### Creating a Monster Instance

```gdscript
# GDScript
var defs := load_monster_defs_from_json("res://data/monsters.json")
var manager := MonsterManager.new(defs)

var instance := manager.create_instance("fire_dragon_001")
print(instance.instance_id)  # unique UUID
print(instance.def_id)       # "fire_dragon_001"
print(instance.level)        # 1
```

```csharp
// C#
var defs = LoadMonsterDefsFromJson("data/monsters.json");
var manager = new MonsterManager(defs);

var instance = manager.CreateInstance("fire_dragon_001");
Console.WriteLine(instance.InstanceId);  // unique UUID
Console.WriteLine(instance.DefId);       // "fire_dragon_001"
Console.WriteLine(instance.Level);       // 1
```

### Calculating Stats

```gdscript
# GDScript
# Given: fire_dragon_001 has base_hp=500, max_hp=3000, max_level=99, curve_exp=1.0
var instance := manager.create_instance("fire_dragon_001")
instance.level = 50
instance.plus_hp = 20

var hp := manager.calc_hp(instance)
# curve = 500 + (3000-500) * ((50-1)/(99-1))^1.0
# curve = 500 + 2500 * 0.5 = 1750
# final = 1750 + 20 * 10 = 1950
print(hp)  # 1950
```

```csharp
// C#
var instance = manager.CreateInstance("fire_dragon_001");
instance.Level = 50;
instance.PlusHp = 20;

int hp = manager.CalcHp(instance);
// curve = 500 + 2500 * 0.5 = 1750
// final = 1750 + 200 = 1950
Console.WriteLine(hp);  // 1950
```

### Leveling Up

```gdscript
# GDScript
var instance := manager.create_instance("fire_dragon_001")
print(instance.level)  # 1

var result := manager.add_exp(instance, 50000)
print(result.levels_gained)    # e.g., 12
print(instance.level)          # 13
print(result.overflow_exp)     # leftover exp after last level-up
```

```csharp
// C#
var instance = manager.CreateInstance("fire_dragon_001");
var result = manager.AddExp(instance, 50000);
Console.WriteLine($"Gained {result.LevelsGained} levels, now level {instance.Level}");
```

### Fusion

```gdscript
# GDScript
var target := manager.create_instance("fire_dragon_001")
target.level = 30

var fodder1 := manager.create_instance("fire_slime_001")  # rarity 1, level 5
var fodder2 := manager.create_instance("fire_goblin_002")  # rarity 2, level 10

var result := manager.fuse(target, [fodder1, fodder2])
# fodder1 exp: 5 * 1 * 50 * 1.5 (same element) = 375
# fodder2 exp: 10 * 2 * 50 * 1.5 (same element) = 1500
# total exp: 1875
print("Exp gained: ", result.exp_gained)        # 1875
print("Levels gained: ", result.levels_gained)
print("New level: ", target.level)

# Plus stat fusion (separate operation)
var plus_egg := manager.create_instance("plus_hp_egg")
plus_egg.plus_hp = 1
manager.fuse_plus(target, plus_egg)
print("Plus HP: ", target.plus_hp)  # 1
```

```csharp
// C#
var target = manager.CreateInstance("fire_dragon_001");
target.Level = 30;

var fodder = new[] {
    manager.CreateInstance("fire_slime_001"),
    manager.CreateInstance("fire_goblin_002")
};
fodder[0].Level = 5;
fodder[1].Level = 10;

var result = manager.Fuse(target, fodder);
Console.WriteLine($"Exp: {result.ExpGained}, Levels: {result.LevelsGained}");
```

### Evolution

```gdscript
# GDScript
# fire_dragon_001 evolves to fire_dragon_002 at max level 99
var instance := manager.create_instance("fire_dragon_001")
instance.level = 99
instance.plus_hp = 50
instance.plus_atk = 40
instance.plus_rec = 30

print(manager.can_evolve(instance))  # true

var result := manager.evolve(instance)
print(result.success)        # true
print(instance.def_id)       # "fire_dragon_002"
print(instance.level)        # 1 (reset)
print(instance.plus_hp)      # 50 (preserved)
print(instance.plus_atk)     # 40 (preserved)
print(instance.plus_rec)     # 30 (preserved)

# New form has higher max_level (e.g., 99 -> 120) and better stats
var new_def := manager.get_def("fire_dragon_002")
print(new_def.max_level)     # 120
print(new_def.max_hp)        # 5000 (up from 3000)
```

```csharp
// C#
var instance = manager.CreateInstance("fire_dragon_001");
instance.Level = 99;
instance.PlusHp = 50;
instance.PlusAtk = 40;
instance.PlusRec = 30;

if (manager.CanEvolve(instance))
{
    var result = manager.Evolve(instance);
    Console.WriteLine(instance.DefId);   // "fire_dragon_002"
    Console.WriteLine(instance.Level);   // 1
    Console.WriteLine(instance.PlusHp);  // 50 (preserved)
}
```
