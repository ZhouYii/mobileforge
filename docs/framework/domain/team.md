# Team Module

## Overview

Handles **team building, validation, and stat aggregation** for dungeon runs. A team is a fixed-size array of monster slots where slot 0 is always the leader and the last slot is the friend/helper. The `TeamBuilder` class enforces composition rules, and `TeamSkillChecker` evaluates whether a team meets the conditions for leader skills and other team-based bonuses.

## Module Files

| File | Purpose | Dependencies |
|---|---|---|
| `team_types` | Team, TeamSlot, ValidationResult, TeamStats | monster_types |
| `team_builder` | TeamBuilder: slot assignment, validation, stat aggregation | team_types, monster_manager |
| `team_skill_checker` | TeamSkillChecker: team composition condition evaluation | team_types, monster_types |

## Team Structure

A team has a fixed number of slots (typically 6 in Tower of Saviors style games):

```
Slot 0       Slot 1       Slot 2       Slot 3       Slot 4       Slot 5
[LEADER]     [Sub 1]      [Sub 2]      [Sub 3]      [Sub 4]      [FRIEND]
   |                                                                  |
   +--- Leader skill applies to whole team                            |
                                                                      +--- Friend's leader skill also applies
```

- **Slot 0 (Leader):** This monster's leader skill is active for the entire run.
- **Slots 1-4 (Subs):** Regular team members. Contribute stats and active skills.
- **Slot 5 (Friend):** Borrowed from another player. Their leader skill also applies, giving the team two leader skill effects. The friend slot is not owned by the player and is returned after the run.

### Team Class Fields

| Field | Type | Description |
|---|---|---|
| `slots` | `Array[TeamSlot]` | Fixed-size array of team slots |
| `max_size` | `int` | Maximum number of slots (default 6) |
| `team_cost` | `int` | Total cost of all team members |
| `cost_limit` | `int` | Maximum allowed team cost (based on player rank) |

### TeamSlot Fields

| Field | Type | Description |
|---|---|---|
| `index` | `int` | Slot position (0 to max_size - 1) |
| `monster` | `MonsterInstance` | The monster assigned to this slot (or null if empty) |
| `is_leader` | `bool` | True for slot 0 |
| `is_friend` | `bool` | True for the last slot |

## TeamBuilder API

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `new` | `monster_manager: MonsterManager, max_size: int = 6` | `TeamBuilder` | Construct with a monster manager reference |
| `create_team` | `cost_limit: int` | `Team` | Create an empty team with a cost limit |
| `set_leader` | `team: Team, monster: MonsterInstance` | `ValidationResult` | Assign a monster to slot 0 |
| `set_member` | `team: Team, slot: int, monster: MonsterInstance` | `ValidationResult` | Assign a monster to a specific slot |
| `set_friend` | `team: Team, monster: MonsterInstance` | `ValidationResult` | Assign a friend to the last slot |
| `remove_member` | `team: Team, slot: int` | `void` | Remove a monster from a slot |
| `swap_slots` | `team: Team, slot_a: int, slot_b: int` | `ValidationResult` | Swap two team members |
| `validate` | `team: Team` | `ValidationResult` | Full validation check |
| `get_team_stats` | `team: Team` | `TeamStats` | Aggregate all member stats |
| `get_team_elements` | `team: Team` | `Array[Element]` | List of distinct elements on the team |
| `get_team_cost` | `team: Team` | `int` | Sum of all members' cost values |
| `is_slot_empty` | `team: Team, slot: int` | `bool` | Check if a slot has no monster |
| `get_filled_count` | `team: Team` | `int` | Number of slots with monsters |

## Validation Rules

`TeamBuilder.validate()` checks all rules and returns a `ValidationResult`:

### Rule 1: At Least 1 Member

The team must have at least one monster assigned (the leader). An empty team cannot enter a dungeon.

```
PASS: [Leader, _, _, _, _, _]         -- leader alone is valid
PASS: [Leader, Sub1, Sub2, _, _, _]   -- partial team is valid
FAIL: [_, _, _, _, _, _]              -- empty team
```

### Rule 2: No Duplicate Instances

The same `MonsterInstance` (same `instance_id`) cannot appear in multiple slots. Two different instances of the same species (same `def_id`) are allowed.

```
PASS: [FireDragon#001, FireDragon#002, _, _, _, _]  -- different instances, same species
FAIL: [FireDragon#001, FireDragon#001, _, _, _, _]  -- same instance in two slots
```

### Rule 3: Cost Limit

The total team cost must not exceed the player's cost limit. Each monster has a cost value determined by its rarity and evolution stage.

```
cost_limit = 200
Team cost: 40 + 35 + 50 + 30 + 25 + 20 = 200  -->  PASS (exactly at limit)
Team cost: 40 + 35 + 50 + 30 + 25 + 25 = 205  -->  FAIL (exceeds limit by 5)
```

### ValidationResult Fields

| Field | Type | Description |
|---|---|---|
| `valid` | `bool` | True if all rules pass |
| `errors` | `Array[String]` | List of validation error messages |

```gdscript
# GDScript
var result := builder.validate(team)
if not result.valid:
    for error in result.errors:
        print("Validation error: ", error)
    # e.g., "Team must have at least 1 member"
    # e.g., "Duplicate instance: fire_dragon#001 in slots 0 and 2"
    # e.g., "Team cost 205 exceeds limit 200"
```

## Team Stats Aggregation

`TeamBuilder.get_team_stats()` sums the final calculated stats of all team members using MonsterManager:

```
team_hp  = sum of calc_hp(member)  for each filled slot
team_atk = sum of calc_atk(member) for each filled slot
team_rec = sum of calc_rec(member) for each filled slot
```

### TeamStats Fields

| Field | Type | Description |
|---|---|---|
| `total_hp` | `int` | Sum of all members' final HP (used as player max HP in dungeon) |
| `total_atk` | `int` | Sum of all members' final ATK |
| `total_rec` | `int` | Sum of all members' final REC (used for heart healing) |
| `member_count` | `int` | Number of filled slots |
| `elements` | `Array[Element]` | Distinct elements represented on the team |
| `per_member` | `Array[Dictionary]` | Per-member stat breakdown for UI display |

```gdscript
# GDScript
var stats := builder.get_team_stats(team)
print("Team HP: ", stats.total_hp)    # e.g., 45000
print("Team ATK: ", stats.total_atk)  # e.g., 12000
print("Team REC: ", stats.total_rec)  # e.g., 3500
print("Members: ", stats.member_count) # e.g., 5
print("Elements: ", stats.elements)    # e.g., [WATER, FIRE, LIGHT]
```

```csharp
// C#
TeamStats stats = builder.GetTeamStats(team);
Console.WriteLine($"Team HP: {stats.TotalHp}");
Console.WriteLine($"Team ATK: {stats.TotalAtk}");
Console.WriteLine($"Team REC: {stats.TotalRec}");
Console.WriteLine($"Members: {stats.MemberCount}");
```

## TeamSkillChecker

`TeamSkillChecker` evaluates team composition conditions. These are used by leader skills, team skills, and conditional effects that depend on who is on the team.

### Condition Methods

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `all_same_element` | `team: Team` | `bool` | True if all members share the same primary element |
| `has_element` | `team: Team, element: Element` | `bool` | True if at least one member has the given element (primary or sub) |
| `min_rarity` | `team: Team, rarity: Rarity` | `bool` | True if all members meet or exceed the given rarity |
| `unique_elements` | `team: Team` | `int` | Count of distinct primary elements on the team |
| `contains_monster` | `team: Team, def_id: String` | `bool` | True if the team contains a monster with the given definition ID |
| `all_different_elements` | `team: Team` | `bool` | True if no two members share the same primary element |
| `count_element` | `team: Team, element: Element` | `int` | Number of members with the given primary element |
| `has_min_elements` | `team: Team, min_count: int` | `bool` | True if the team has at least N distinct primary elements |

### How Team Skills Work (ToS Context)

In Tower of Saviors, leader skills often have team composition requirements. For example:

- **"Water ATK x3 when team is all Water"** -- requires `all_same_element()` with WATER
- **"ATK x4 for teams with 5 unique elements"** -- requires `unique_elements() >= 5`
- **"HP x1.5 if team contains Fire Knight"** -- requires `contains_monster("fire_knight_001")`
- **"ATK x2.5 for EPIC or higher rarity monsters"** -- requires `min_rarity(Rarity.EPIC)`

The SkillPipeline evaluates these conditions at the start of each dungeon (for leader skills) or at activation time (for active skills):

```
DungeonRunner.start_dungeon()
  |
  +-- Evaluate leader skill (slot 0)
  |     |-- SkillPipeline checks leader skill conditions
  |     |-- TeamSkillChecker evaluates team composition
  |     |-- If conditions pass: register leader skill hooks on CombatResolver
  |
  +-- Evaluate friend leader skill (last slot)
        |-- Same process for the friend's leader skill
```

### Condition Composition

Multiple team conditions can be combined in a single skill definition:

```json
{
  "conditions": [
    {"type": "team_all_same_element", "params": {"element": "WATER"}},
    {"type": "team_min_rarity", "params": {"rarity": "EPIC"}}
  ],
  "outcomes": [
    {"type": "atk_multiplier", "params": {"multiplier": 4.0, "element": "WATER"}}
  ]
}
```

This skill only activates if **all** conditions pass: the team must be all-Water AND all members must be EPIC or higher.

## Code Examples

### Building a Team

```gdscript
# GDScript
var manager := MonsterManager.new(monster_defs)
var builder := TeamBuilder.new(manager, 6)  # 6 slots

# Create an empty team with cost limit based on player rank
var team := builder.create_team(200)  # cost limit 200

# Assign monsters to slots
var leader := manager.create_instance("water_dragon_001")
leader.level = 99

var sub1 := manager.create_instance("water_knight_001")
sub1.level = 80

var sub2 := manager.create_instance("water_mage_001")
sub2.level = 75

var sub3 := manager.create_instance("water_healer_001")
sub3.level = 70

var sub4 := manager.create_instance("water_tank_001")
sub4.level = 60

builder.set_leader(team, leader)
builder.set_member(team, 1, sub1)
builder.set_member(team, 2, sub2)
builder.set_member(team, 3, sub3)
builder.set_member(team, 4, sub4)

# Friend slot is set when entering a dungeon (selected from friend list)
var friend := manager.create_instance("water_goddess_001")
friend.level = 99
builder.set_friend(team, friend)

# Validate
var result := builder.validate(team)
if result.valid:
    print("Team is valid!")
    var stats := builder.get_team_stats(team)
    print("Total HP: ", stats.total_hp)
    print("Total ATK: ", stats.total_atk)
    print("Total REC: ", stats.total_rec)
else:
    for error in result.errors:
        print("Error: ", error)
```

```csharp
// C#
var manager = new MonsterManager(monsterDefs);
var builder = new TeamBuilder(manager, maxSize: 6);

var team = builder.CreateTeam(costLimit: 200);

var leader = manager.CreateInstance("water_dragon_001");
leader.Level = 99;

var sub1 = manager.CreateInstance("water_knight_001");
sub1.Level = 80;

var sub2 = manager.CreateInstance("water_mage_001");
sub2.Level = 75;

var sub3 = manager.CreateInstance("water_healer_001");
sub3.Level = 70;

var sub4 = manager.CreateInstance("water_tank_001");
sub4.Level = 60;

builder.SetLeader(team, leader);
builder.SetMember(team, 1, sub1);
builder.SetMember(team, 2, sub2);
builder.SetMember(team, 3, sub3);
builder.SetMember(team, 4, sub4);

var friend = manager.CreateInstance("water_goddess_001");
friend.Level = 99;
builder.SetFriend(team, friend);

var result = builder.Validate(team);
if (result.Valid)
{
    var stats = builder.GetTeamStats(team);
    Console.WriteLine($"Total HP: {stats.TotalHp}");
    Console.WriteLine($"Total ATK: {stats.TotalAtk}");
    Console.WriteLine($"Total REC: {stats.TotalRec}");
}
else
{
    foreach (string error in result.Errors)
        Console.WriteLine($"Error: {error}");
}
```

### Checking Team Composition for Skills

```gdscript
# GDScript
var checker := TeamSkillChecker.new()

# Check if team qualifies for "all Water" leader skill
if checker.all_same_element(team):
    print("All same element -- mono-element leader skill active!")

# Check unique element count for rainbow leader skill
var unique := checker.unique_elements(team)
print("Unique elements: ", unique)
if unique >= 5:
    print("Rainbow leader skill active! (5+ elements)")

# Check if team contains a specific monster for synergy
if checker.contains_monster(team, "water_goddess_001"):
    print("Water Goddess synergy bonus active!")

# Check minimum rarity for elite team bonus
if checker.min_rarity(team, Rarity.EPIC):
    print("All members are EPIC or higher -- elite bonus!")

# Count members of a specific element
var water_count := checker.count_element(team, Element.WATER)
print("Water members: ", water_count)
```

```csharp
// C#
var checker = new TeamSkillChecker();

if (checker.AllSameElement(team))
    Console.WriteLine("Mono-element leader skill active!");

int unique = checker.UniqueElements(team);
if (unique >= 5)
    Console.WriteLine("Rainbow leader skill active!");

if (checker.ContainsMonster(team, "water_goddess_001"))
    Console.WriteLine("Water Goddess synergy bonus active!");

if (checker.MinRarity(team, Rarity.Epic))
    Console.WriteLine("All members EPIC or higher!");

int waterCount = checker.CountElement(team, Element.Water);
Console.WriteLine($"Water members: {waterCount}");
```

### Swapping Team Members

```gdscript
# GDScript
# Swap slot 1 and slot 3
var swap_result := builder.swap_slots(team, 1, 3)
if swap_result.valid:
    print("Swapped successfully")
else:
    print("Swap failed: ", swap_result.errors[0])

# Remove a member from slot 2
builder.remove_member(team, 2)
print("Slot 2 empty: ", builder.is_slot_empty(team, 2))  # true
print("Filled slots: ", builder.get_filled_count(team))
```

```csharp
// C#
var swapResult = builder.SwapSlots(team, 1, 3);
if (swapResult.Valid)
    Console.WriteLine("Swapped successfully");

builder.RemoveMember(team, 2);
Console.WriteLine($"Slot 2 empty: {builder.IsSlotEmpty(team, 2)}");
Console.WriteLine($"Filled slots: {builder.GetFilledCount(team)}");
```

### Integrating Team with DungeonRunner

```gdscript
# GDScript
# Validate team before entering dungeon
var validation := builder.validate(team)
if not validation.valid:
    print("Cannot enter dungeon: ", validation.errors)
    return

# Get aggregated stats for dungeon initialization
var stats := builder.get_team_stats(team)

# Extract the monster instances array for DungeonRunner
var team_monsters: Array[MonsterInstance] = []
for slot in team.slots:
    if slot.monster != null:
        team_monsters.append(slot.monster)

# Start dungeon -- DungeonRunner uses team stats for player HP
var state := runner.start_dungeon(dungeon_def, team_monsters)
# state.player_max_hp == stats.total_hp
# state.player_hp == stats.total_hp
```

```csharp
// C#
var validation = builder.Validate(team);
if (!validation.Valid)
{
    Console.WriteLine($"Cannot enter dungeon: {string.Join(", ", validation.Errors)}");
    return;
}

var stats = builder.GetTeamStats(team);

var teamMonsters = team.Slots
    .Where(s => s.Monster != null)
    .Select(s => s.Monster)
    .ToList();

var state = runner.StartDungeon(dungeonDef, teamMonsters);
// state.PlayerMaxHp == stats.TotalHp
```
