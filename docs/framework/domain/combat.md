# Combat Module

## Overview

A **5-hook damage pipeline** that mirrors Tower of Saviors' damage calculation. Each match group from the board flows through a sequence of multiplication/addition stages. Skills inject behavior by registering **hooks** at specific pipeline stages, making the system extensible without modifying the core resolver.

## Module Files

| File | Purpose | Dependencies |
|---|---|---|
| `combat_types` | DamageContext, DamageResult, HookType enum, HookEntry | None (leaf) |
| `combat_resolver` | CombatResolver: runs the damage pipeline with registered hooks | combat_types |
| `element_chart` | ElementChart: element advantage/disadvantage multipliers | combat_types |
| `combo_calculator` | ComboCalculator: combo count to multiplier conversion | combat_types |

## The Damage Pipeline

Every match group produces one `DamageContext` per attacking monster per defending target. The pipeline processes each context through 9 stages in strict order:

```
                       DamageContext created
                              |
     +----- STAGE 1 ----------+
     |  Base Gem Damage
     |  atk * (1 + (gems - 3) * 0.25)
     |
     +----- STAGE 2 ----------+
     |  Combo Multiplier
     |  * (1 + (combo - 1) * 0.25)
     |
     +----- STAGE 3 ----------+
     |  PRE_ELEMENT hooks
     |  (skill multipliers before element)
     |
     +----- STAGE 4 ----------+
     |  Element Multiplier
     |  * element_chart.get_multiplier(atk_element, def_element)
     |
     +----- STAGE 5 ----------+
     |  POST_ELEMENT hooks
     |  (skill multipliers after element)
     |
     +----- STAGE 6 ----------+
     |  MAIN hooks
     |  (primary skill damage multipliers)
     |
     +----- STAGE 7 ----------+
     |  Defense Subtraction
     |  damage = max(damage - defense, 1)
     |
     +----- STAGE 8 ----------+
     |  POST_DEFENSE hooks
     |  (armor break, true damage, etc.)
     |
     +----- STAGE 9 ----------+
     |  CAN_ZERO hooks
     |  (damage nullification shields, absorb)
     |
     +----- OUTPUT ------------+
            DamageResult
```

### Stage Details

**Stage 1 -- Base Gem Damage**
```
base_damage = attacker.atk * (1.0 + (matched_gems - 3) * 0.25)
```
A 3-gem match uses the monster's ATK at 1.0x. Each additional gem adds 0.25x. A 6-gem match is 1.75x ATK.

**Stage 2 -- Combo Multiplier**
```
combo_mult = 1.0 + (total_combos - 1) * 0.25
damage *= combo_mult
```
1 combo = 1.0x. 7 combos = 2.5x. This rewards board solving skill.

**Stage 3 -- PRE_ELEMENT Hooks**
Hooks registered at `HookType.PRE_ELEMENT` fire here. Typical use: skills that multiply damage before element advantage is applied (e.g., "2x ATK for Water monsters").

**Stage 4 -- Element Multiplier**
```
element_mult = element_chart.get_multiplier(atk_element, def_element)
damage *= element_mult
```
See Element Chart section below.

**Stage 5 -- POST_ELEMENT Hooks**
Hooks registered at `HookType.POST_ELEMENT` fire here. Typical use: skills that benefit from or modify element-adjusted damage.

**Stage 6 -- MAIN Hooks**
Hooks registered at `HookType.MAIN` fire here. This is the most common hook point for leader skills and active skill damage multipliers.

**Stage 7 -- Defense Subtraction**
```
damage = max(damage - target.defense, 1)
```
Damage is reduced by the target's defense stat, with a floor of 1 (attacks always deal at least 1 damage, unless nullified at Stage 9).

**Stage 8 -- POST_DEFENSE Hooks**
Hooks registered at `HookType.POST_DEFENSE` fire here. Typical use: armor break effects that set defense to 0 retroactively, or true damage additions.

**Stage 9 -- CAN_ZERO Hooks**
Hooks registered at `HookType.CAN_ZERO` fire here. This is the only stage where damage can be reduced to 0 or turned negative (absorb). Typical use: damage void shields, absorb shields.

## DamageContext Fields

| Field | Type | Description |
|---|---|---|
| `attacker` | `MonsterInstance` | The monster dealing damage |
| `target` | `MonsterInstance` | The monster receiving damage |
| `element` | `Element` | Element of the matched gems |
| `matched_gems` | `int` | Number of gems in this match group |
| `total_combos` | `int` | Total combo count for this turn |
| `damage` | `float` | Running damage total (mutated by each stage) |
| `base_atk` | `float` | Snapshot of attacker's ATK at pipeline start |
| `is_main_attribute` | `bool` | True if match element == attacker's primary element |
| `is_sub_attribute` | `bool` | True if match element == attacker's sub element |
| `flags` | `Dictionary` | Arbitrary key-value pairs set by hooks for inter-hook communication |

## DamageResult Fields

| Field | Type | Description |
|---|---|---|
| `target` | `MonsterInstance` | Who received the damage |
| `final_damage` | `int` | Integer damage after all pipeline stages |
| `element` | `Element` | Element of the attack |
| `was_effective` | `bool` | True if element multiplier > 1.0 |
| `was_resisted` | `bool` | True if element multiplier < 1.0 |
| `was_voided` | `bool` | True if a CAN_ZERO hook nullified damage |
| `was_absorbed` | `bool` | True if damage was converted to healing |
| `overkill` | `int` | Damage beyond target's remaining HP (for UI effects) |
| `hooks_applied` | `Array[String]` | Names of hooks that modified this result (for debug/UI) |

## Hook Registration

```gdscript
combat_resolver.register_hook(
    HookType.MAIN,              # which pipeline stage
    my_skill_callback,          # Callable that takes DamageContext and returns DamageContext
    priority: 100,              # lower value = fires first
    name: "leader_2x_water"     # debug label
)
```

```csharp
combatResolver.RegisterHook(
    HookType.Main,
    mySkillCallback,            // Func<DamageContext, DamageContext>
    priority: 100,
    name: "leader_2x_water"
);
```

### Hook Signature

A hook receives a `DamageContext`, mutates it, and returns it:

```gdscript
func leader_2x_water(ctx: DamageContext) -> DamageContext:
    if ctx.element == Element.WATER:
        ctx.damage *= 2.0
    return ctx
```

```csharp
DamageContext Leader2xWater(DamageContext ctx)
{
    if (ctx.Element == Element.Water)
        ctx.Damage *= 2.0f;
    return ctx;
}
```

### Hook Priority

Hooks within the same `HookType` stage fire in **ascending priority order** (lower value fires first):

| Priority Range | Convention |
|---|---|
| 0 -- 49 | System hooks (element chart, base formulas) |
| 50 -- 99 | Leader skills |
| 100 -- 199 | Active skills |
| 200 -- 299 | Passive awakenings |
| 300+ | Debuffs and enemy effects |

If two hooks share the same priority, registration order is preserved (stable sort).

## Element Chart

Tower of Saviors uses a simple advantage triangle plus mutual weakness for light/dark:

```
        WATER
       /     \
      x2      x0.5
     /           \
  GRASS ---x2--- FIRE

  LIGHT <--x2--> DARK  (mutual advantage)

  HEART: neutral to all, neutral from all
```

| Attacker | Defender | Multiplier |
|---|---|---|
| Water | Fire | 2.0 |
| Fire | Grass | 2.0 |
| Grass | Water | 2.0 |
| Fire | Water | 0.5 |
| Grass | Fire | 0.5 |
| Water | Grass | 0.5 |
| Light | Dark | 2.0 |
| Dark | Light | 2.0 |
| Same | Same | 1.0 |
| Heart | Any | 1.0 |
| Any | Heart | 1.0 |

```gdscript
class_name ElementChart extends RefCounted

var _chart: Dictionary = {}  # {Element: {Element: float}}

func _init() -> void:
    _set(Element.WATER, Element.FIRE, 2.0)
    _set(Element.FIRE, Element.GRASS, 2.0)
    _set(Element.GRASS, Element.WATER, 2.0)
    _set(Element.FIRE, Element.WATER, 0.5)
    _set(Element.GRASS, Element.FIRE, 0.5)
    _set(Element.WATER, Element.GRASS, 0.5)
    _set(Element.LIGHT, Element.DARK, 2.0)
    _set(Element.DARK, Element.LIGHT, 2.0)

func get_multiplier(atk: Element, def: Element) -> float:
    if _chart.has(atk) and _chart[atk].has(def):
        return _chart[atk][def]
    return 1.0
```

## Combo Calculator Formulas

```gdscript
class_name ComboCalculator extends RefCounted

## Standard combo multiplier: 1 + (combos - 1) * 0.25
static func combo_multiplier(combos: int) -> float:
    return 1.0 + (combos - 1) * 0.25

## Gem count multiplier: 1 + (gems - min_match) * 0.25
static func gem_multiplier(gems: int, min_match: int = 3) -> float:
    return 1.0 + (gems - min_match) * 0.25
```

| Combos | Multiplier | | Matched Gems | Multiplier |
|---|---|---|---|---|
| 1 | 1.00x | | 3 | 1.00x |
| 2 | 1.25x | | 4 | 1.25x |
| 3 | 1.50x | | 5 | 1.50x |
| 4 | 1.75x | | 6 | 1.75x |
| 5 | 2.00x | | 7 | 2.00x |
| 6 | 2.25x | | 8 | 2.25x |
| 7 | 2.50x | | 9 | 2.50x |
| 8 | 2.75x | | 10+ | continues |
| 10 | 3.25x | | | |

## Code Examples

### Basic Damage Calculation

```gdscript
# GDScript
var chart := ElementChart.new()
var resolver := CombatResolver.new(chart)

# Simulate: a Water monster (1000 ATK) hits a Fire enemy
# with a 4-gem Water match, 3 total combos
var ctx := DamageContext.new()
ctx.attacker = my_water_monster   # atk = 1000
ctx.target = fire_enemy           # defense = 200
ctx.element = Element.WATER
ctx.matched_gems = 4
ctx.total_combos = 3

var result: DamageResult = resolver.resolve_single(ctx)

# Expected calculation:
# base      = 1000 * (1 + (4-3)*0.25) = 1000 * 1.25 = 1250
# combo     = 1250 * (1 + (3-1)*0.25) = 1250 * 1.50 = 1875
# element   = 1875 * 2.0 (water vs fire) = 3750
# defense   = max(3750 - 200, 1) = 3550
print(result.final_damage)  # 3550
```

```csharp
// C#
var chart = new ElementChart();
var resolver = new CombatResolver(chart);

var ctx = new DamageContext
{
    Attacker = myWaterMonster,   // Atk = 1000
    Target = fireEnemy,          // Defense = 200
    Element = Element.Water,
    MatchedGems = 4,
    TotalCombos = 3
};

DamageResult result = resolver.ResolveSingle(ctx);
// Expected: 3550
Console.WriteLine(result.FinalDamage);
```

### Registering a Skill Hook That Doubles Damage

```gdscript
# GDScript -- leader skill: "Water ATK x2"
func _setup_leader_skill(resolver: CombatResolver) -> void:
    resolver.register_hook(
        HookType.MAIN,
        func(ctx: DamageContext) -> DamageContext:
            if ctx.element == Element.WATER:
                ctx.damage *= 2.0
                ctx.flags["leader_water_2x"] = true
            return ctx,
        50,         # priority: leader skill range
        "leader_water_2x"
    )

# Now the same 4-gem 3-combo Water attack becomes:
# base=1250 -> combo=1875 -> element=3750 -> MAIN hook: 3750*2=7500 -> defense: 7300
```

```csharp
// C#
void SetupLeaderSkill(CombatResolver resolver)
{
    resolver.RegisterHook(
        HookType.Main,
        ctx =>
        {
            if (ctx.Element == Element.Water)
            {
                ctx.Damage *= 2.0f;
                ctx.Flags["leader_water_2x"] = true;
            }
            return ctx;
        },
        priority: 50,
        name: "leader_water_2x"
    );
}
```

### Element Chart Lookup

```gdscript
# GDScript
var chart := ElementChart.new()

print(chart.get_multiplier(Element.WATER, Element.FIRE))   # 2.0
print(chart.get_multiplier(Element.FIRE, Element.WATER))   # 0.5
print(chart.get_multiplier(Element.LIGHT, Element.DARK))   # 2.0
print(chart.get_multiplier(Element.DARK, Element.LIGHT))   # 2.0
print(chart.get_multiplier(Element.WATER, Element.WATER))  # 1.0
print(chart.get_multiplier(Element.HEART, Element.FIRE))   # 1.0
```

```csharp
// C#
var chart = new ElementChart();

chart.GetMultiplier(Element.Water, Element.Fire);   // 2.0
chart.GetMultiplier(Element.Fire, Element.Water);   // 0.5
chart.GetMultiplier(Element.Light, Element.Dark);   // 2.0
chart.GetMultiplier(Element.Dark, Element.Light);   // 2.0
chart.GetMultiplier(Element.Water, Element.Water);  // 1.0
chart.GetMultiplier(Element.Heart, Element.Fire);   // 1.0
```
