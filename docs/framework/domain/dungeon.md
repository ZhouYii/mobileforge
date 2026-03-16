# Dungeon Module

## Overview

**DungeonRunner is the ONLY domain orchestrator.** It is the single top-level class that wires together BoardLogic, CombatResolver, SkillPipeline, and EnemyAI into a complete turn-based dungeon encounter. No other domain class depends on more than one sibling module.

DungeonRunner sequences the entire battle flow: player turns (board resolution into damage calculation), enemy turns (countdown ticking and attacks), wave progression, and win/loss detection. It produces pure data results that the UI layer consumes for animation and display.

## Module Files

| File | Purpose | Dependencies |
|---|---|---|
| `dungeon_types` | DungeonDef, DungeonState, WaveDef, TurnResult, WaveResult | enemy_types, board_types, combat_types |
| `dungeon_runner` | DungeonRunner: turn orchestration, wave management, win/loss | dungeon_types, board_logic, combat_resolver, skill_pipeline, enemy_ai |

## DungeonDef Structure

`DungeonDef` is a JSON-loaded definition describing the full dungeon encounter:

```json
{
  "id": "fire_dragon_lair_expert",
  "name": "Fire Dragon's Lair - Expert",
  "stamina_cost": 25,
  "waves": [
    {
      "wave_number": 1,
      "enemies": [
        {
          "def_id": "fire_goblin_001",
          "hp": 50000,
          "atk": 3000,
          "defense": 100,
          "countdown": 2,
          "characteristics": []
        },
        {
          "def_id": "fire_goblin_002",
          "hp": 60000,
          "atk": 3500,
          "defense": 120,
          "countdown": 3,
          "characteristics": []
        }
      ]
    },
    {
      "wave_number": 2,
      "enemies": [
        {
          "def_id": "fire_knight_001",
          "hp": 200000,
          "atk": 8000,
          "defense": 300,
          "countdown": 2,
          "characteristics": [
            {"type": "combo_shield", "params": {"min_combos": 4}}
          ]
        }
      ]
    },
    {
      "wave_number": 3,
      "enemies": [
        {
          "def_id": "fire_dragon_boss",
          "hp": 5000000,
          "atk": 15000,
          "defense": 500,
          "countdown": 3,
          "characteristics": [
            {"type": "combo_shield", "params": {"min_combos": 6}},
            {"type": "damage_cap", "params": {"max_damage": 200000}}
          ]
        }
      ]
    }
  ],
  "rewards": {
    "coins": 10000,
    "rank_exp": 500,
    "drops": [
      {"monster_def_id": "fire_dragon_boss", "drop_rate": 0.40},
      {"item_id": "dragon_scale", "drop_rate": 0.80}
    ]
  }
}
```

### DungeonDef Fields

| Field | Type | Description |
|---|---|---|
| `id` | `String` | Unique dungeon identifier |
| `name` | `String` | Display name |
| `stamina_cost` | `int` | Stamina consumed to enter |
| `waves` | `Array[WaveDef]` | Ordered list of enemy waves |
| `rewards` | `RewardsDef` | Completion rewards (coins, exp, drops) |

### WaveDef Fields

| Field | Type | Description |
|---|---|---|
| `wave_number` | `int` | 1-indexed wave position |
| `enemies` | `Array[EnemyDef]` | Enemy configurations for this wave |

## The Complete Turn Flow

This is the authoritative battle turn control flow. Every turn follows this exact sequence:

```
                    +==========================+
                    |    PLAYER TURN BEGIN     |
                    +==========================+
                              |
                              v
              1. User drags gems on the board
                              |
                              v
              BoardLogic.resolve_cascade()
                              |
                              v
                      CascadeResult
                       .steps[]            ---> BattleScreen animates
                       .total_combo              each CascadeStep
                       .matches_by_element
                              |
                              v
                    +==========================+
                    | DungeonRunner            |
                    | .execute_player_turn(    |
                    |    cascade, team, stats)  |
                    +==========================+
                              |
              +---------------+
              |
              v
    2. Aggregate matches by element
       - Sum matched gems per element
       - Count total combos
              |
              v
    3. For each combo + monster:
       CombatResolver.resolve_player_attack()
       - Monster attacks with its element
       - Each match group flows through
         the 9-stage damage pipeline
       - Enemy characteristics (shields,
         caps, absorb) apply via hooks
              |
              v
    4. Apply heart healing
       - Heart matches restore HP
       - heal = team_rec * heart_combos * multiplier
              |
              v
    5. SkillPipeline.process_turn_end()
       - Tick all persistent outcomes
       - Expire finished buffs/debuffs
       - Unregister expired combat hooks
              |
              v
    6. Check wave clear / battle won
       - If all enemies dead:
           - If more waves: advance wave
           - If last wave: BATTLE WON
              |
              v
              +--- TurnResult (player phase) emitted
              |
              v
                    +==========================+
                    |    ENEMY TURN BEGIN      |
                    +==========================+
                              |
              +---------------+
              |
              v
    7. EnemyAI.tick_countdowns(enemies)
       - All alive enemies: countdown -= 1
       - Skip delayed enemies
              |
              v
    8. For each enemy where countdown == 0:
       EnemyAI.decide_action(enemy, context)
       - Enemy performs its action
       - Damage dealt to player
       - Debuffs applied to player
       - Enemy countdown resets
              |
              v
    9. EnemyAI.tick_enemy_statuses(enemies)
       - Process poison damage on enemies
       - Decrement status durations
       - Remove expired statuses
              |
              v
   10. Check battle lost
       - If player HP <= 0: BATTLE LOST
              |
              v
              +--- TurnResult (enemy phase) emitted
              |
              v
                    +==========================+
                    |      TURN COMPLETE       |
                    +==========================+
                              |
                              v
                       Next turn begins
                       (return to step 1)
```

### Step-by-Step Breakdown

**Step 1 -- Board Resolution**
The player drags gems. `BoardLogic.resolve_cascade()` returns a `CascadeResult` containing every cascade step, total combos, and matches organized by element. This happens before DungeonRunner is called -- the UI layer drives the drag and calls the board.

**Step 2 -- Match Aggregation**
DungeonRunner aggregates the cascade results: how many gems of each element were matched, how many combos were scored, and which match groups map to which elements.

**Step 3 -- Damage Calculation**
For each attacking monster on the team, for each relevant match (matching the monster's element or sub-element), DungeonRunner calls `CombatResolver.resolve_player_attack()`. This runs the full 9-stage damage pipeline including all registered hooks from leader skills, active skills, and enemy characteristics.

**Step 4 -- Heart Healing**
Heart element matches heal the player. The formula is:
```
heal = team_total_rec * (1.0 + (heart_combos - 1) * 0.25)
```
Where `team_total_rec` is the sum of all team members' REC stats.

**Step 5 -- Skill Turn End**
`SkillPipeline.process_turn_end()` ticks all persistent outcomes, decrements their `turns_left`, and removes expired ones. Expired outcomes call `on_expire()` to unregister their combat hooks.

**Step 6 -- Wave / Victory Check**
If all enemies in the current wave are dead, the wave is cleared. If there are more waves, `_load_wave()` is called to advance. If this was the final wave, the battle is won.

**Step 7 -- Countdown Tick**
`EnemyAI.tick_countdowns()` decrements every alive enemy's countdown by 1. Delayed enemies (with a delay status) are skipped.

**Step 8 -- Enemy Actions**
For each enemy whose countdown reached 0, `EnemyAI.decide_action()` determines their action. The action is executed (damage to player, debuffs, heals, enrage, etc.) and the enemy's countdown resets.

**Step 9 -- Enemy Status Tick**
Status effects on enemies (poison, bind, etc.) are processed. Poison deals damage, durations decrement, and expired statuses are removed.

**Step 10 -- Defeat Check**
If the player's HP drops to 0 or below at any point during the enemy phase, the battle is lost.

## TurnResult Fields

`TurnResult` is returned by both `execute_player_turn()` and `execute_enemy_turn()`:

| Field | Type | Description |
|---|---|---|
| `phase` | `String` | `"player"` or `"enemy"` |
| `damage_results` | `Array[DamageResult]` | All damage dealt this phase (player attacks or enemy attacks) |
| `total_damage_dealt` | `int` | Sum of all damage dealt to enemies (player phase) |
| `total_damage_received` | `int` | Sum of all damage received from enemies (enemy phase) |
| `healing` | `int` | HP healed this phase (heart matches or skills) |
| `combos` | `int` | Total combo count (player phase only) |
| `kills` | `Array[String]` | IDs of enemies killed this phase |
| `wave_cleared` | `bool` | True if this phase cleared the current wave |
| `next_wave` | `int` | Next wave number if wave was cleared (0 if not) |
| `battle_won` | `bool` | True if the final wave was cleared |
| `battle_lost` | `bool` | True if player HP reached 0 |
| `enemy_actions` | `Array[EnemyAction]` | Actions enemies took (enemy phase only) |
| `expired_buffs` | `Array[String]` | Names of buffs/outcomes that expired this turn |
| `expired_statuses` | `Array[String]` | Names of enemy statuses that expired |
| `player_hp_after` | `int` | Player HP after this phase |
| `active_buffs` | `Array[Dictionary]` | Currently active buff descriptors with remaining turns |

## DungeonState Fields

`DungeonState` tracks the mutable state of an ongoing dungeon run:

| Field | Type | Description |
|---|---|---|
| `dungeon_def` | `DungeonDef` | The dungeon definition being run |
| `current_wave` | `int` | Current wave number (1-indexed) |
| `enemies` | `Array[EnemyState]` | Enemies in the current wave |
| `player_hp` | `int` | Current player HP |
| `player_max_hp` | `int` | Maximum player HP (sum of team HP) |
| `turn_number` | `int` | Total turns elapsed |
| `team` | `Array[MonsterInstance]` | Player's team for this run |
| `board` | `BoardLogic` | The board instance for this run |
| `combat_resolver` | `CombatResolver` | The combat resolver instance |
| `skill_pipeline` | `SkillPipeline` | The skill pipeline instance |
| `enemy_ai` | `EnemyAI` | The enemy AI instance |
| `is_active` | `bool` | False when battle ends (win or lose) |
| `result` | `String` | `""`, `"won"`, or `"lost"` |
| `total_damage_dealt` | `int` | Running total of all damage dealt to enemies |
| `max_combo` | `int` | Highest combo count achieved in any single turn |

## Wave Progression

When all enemies in a wave are killed, `_load_wave()` advances to the next wave:

```gdscript
func _load_wave(state: DungeonState) -> void:
    state.current_wave += 1
    if state.current_wave > state.dungeon_def.waves.size():
        state.is_active = false
        state.result = "won"
        return

    var wave_def: WaveDef = state.dungeon_def.waves[state.current_wave - 1]
    state.enemies.clear()

    for enemy_def in wave_def.enemies:
        var enemy := EnemyState.new()
        enemy.id = enemy_def.def_id + "_" + str(state.current_wave)
        enemy.def_id = enemy_def.def_id
        enemy.max_hp = enemy_def.hp
        enemy.current_hp = enemy_def.hp
        enemy.atk = enemy_def.atk
        enemy.defense = enemy_def.defense
        enemy.countdown = enemy_def.countdown
        enemy.max_countdown = enemy_def.countdown
        # Register characteristic hooks on the combat resolver
        for char_def in enemy_def.characteristics:
            var char := EnemyCharacteristic.new(char_def.type, char_def.params)
            enemy.characteristics.append(char)
            _register_characteristic_hooks(char, enemy, state.combat_resolver)
        state.enemies.append(enemy)
```

```csharp
private void LoadWave(DungeonState state)
{
    state.CurrentWave++;
    if (state.CurrentWave > state.DungeonDef.Waves.Count)
    {
        state.IsActive = false;
        state.Result = "won";
        return;
    }

    var waveDef = state.DungeonDef.Waves[state.CurrentWave - 1];
    state.Enemies.Clear();

    foreach (var enemyDef in waveDef.Enemies)
    {
        var enemy = new EnemyState
        {
            Id = $"{enemyDef.DefId}_{state.CurrentWave}",
            DefId = enemyDef.DefId,
            MaxHp = enemyDef.Hp,
            CurrentHp = enemyDef.Hp,
            Atk = enemyDef.Atk,
            Defense = enemyDef.Defense,
            Countdown = enemyDef.Countdown,
            MaxCountdown = enemyDef.Countdown
        };
        foreach (var charDef in enemyDef.Characteristics)
        {
            var ch = new EnemyCharacteristic(charDef.Type, charDef.Params);
            enemy.Characteristics.Add(ch);
            RegisterCharacteristicHooks(ch, enemy, state.CombatResolver);
        }
        state.Enemies.Add(enemy);
    }
}
```

Key behaviors:
- Wave number is 1-indexed.
- Enemy characteristic hooks are registered on the CombatResolver when the wave loads.
- When a wave is cleared, old characteristic hooks are unregistered before loading the next wave.
- If `current_wave` exceeds the wave count, the battle is won.

## Events Emitted

DungeonRunner emits events at key moments. The UI layer connects to these for animation and display.

| Event | Payload | When |
|---|---|---|
| `dungeon_started` | `DungeonState` | After initialization, before first turn |
| `wave_started` | `{wave_number: int, enemies: Array[EnemyState]}` | When a new wave loads |
| `player_turn_resolved` | `TurnResult` | After execute_player_turn completes |
| `enemy_turn_resolved` | `TurnResult` | After execute_enemy_turn completes |
| `battle_won` | `{dungeon_id: String, turns: int, max_combo: int, rewards: RewardsDef}` | When the final wave is cleared |
| `battle_lost` | `{dungeon_id: String, turns: int, wave: int}` | When player HP reaches 0 |

```gdscript
# GDScript -- signal declarations
signal dungeon_started(state: DungeonState)
signal wave_started(info: Dictionary)
signal player_turn_resolved(result: TurnResult)
signal enemy_turn_resolved(result: TurnResult)
signal battle_won(info: Dictionary)
signal battle_lost(info: Dictionary)
```

```csharp
// C# -- event declarations
public event Action<DungeonState> DungeonStarted;
public event Action<WaveStartedInfo> WaveStarted;
public event Action<TurnResult> PlayerTurnResolved;
public event Action<TurnResult> EnemyTurnResolved;
public event Action<BattleWonInfo> BattleWon;
public event Action<BattleLostInfo> BattleLost;
```

## DungeonRunner API

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `new` | `board: BoardLogic, combat: CombatResolver, skills: SkillPipeline, ai: EnemyAI` | `DungeonRunner` | Construct with all module dependencies |
| `start_dungeon` | `def: DungeonDef, team: Array[MonsterInstance]` | `DungeonState` | Initialize a new dungeon run |
| `execute_player_turn` | `cascade: CascadeResult, state: DungeonState` | `TurnResult` | Process player's turn from cascade results |
| `execute_enemy_turn` | `state: DungeonState` | `TurnResult` | Process enemy turn (countdowns, attacks) |
| `get_state` | | `DungeonState` | Read current dungeon state |
| `is_battle_active` | | `bool` | True if battle is ongoing |
| `get_current_enemies` | | `Array[EnemyState]` | Enemies in the current wave |
| `forfeit` | `state: DungeonState` | `void` | End the battle as a loss |

## Code Examples

### Setting Up a Dungeon Run

```gdscript
# GDScript
# Load the dungeon definition
var dungeon_def: DungeonDef = DungeonLoader.load("res://data/dungeons/fire_dragon_lair.json")

# Create fresh domain instances (per-session, never singletons)
var board_config := BoardConfig.new()
board_config.rows = 5
board_config.cols = 6
var board := BoardLogic.new(board_config)
var rng := RandomNumberGenerator.new()
board.init_board(rng)

var chart := ElementChart.new()
var combat := CombatResolver.new(chart)
var registry := EffectRegistry.new()
# ... register effects ...
var skills := SkillPipeline.new(registry)
var ai := EnemyAI.new()

var runner := DungeonRunner.new(board, combat, skills, ai)

# Start the dungeon with the player's team
var team: Array[MonsterInstance] = team_builder.get_team()
var state: DungeonState = runner.start_dungeon(dungeon_def, team)

print("Dungeon started: ", state.dungeon_def.name)
print("Wave 1 enemies: ", state.enemies.size())
print("Player HP: ", state.player_hp, " / ", state.player_max_hp)
```

```csharp
// C#
var dungeonDef = DungeonLoader.Load("data/dungeons/fire_dragon_lair.json");

var boardConfig = new BoardConfig { Rows = 5, Cols = 6 };
var board = new BoardLogic(boardConfig);
board.InitBoard(new Random());

var combat = new CombatResolver(new ElementChart());
var registry = new EffectRegistry();
// ... register effects ...
var skills = new SkillPipeline(registry);
var ai = new EnemyAI();

var runner = new DungeonRunner(board, combat, skills, ai);

var team = teamBuilder.GetTeam();
DungeonState state = runner.StartDungeon(dungeonDef, team);

Console.WriteLine($"Dungeon started: {state.DungeonDef.Name}");
Console.WriteLine($"Wave 1 enemies: {state.Enemies.Count}");
Console.WriteLine($"Player HP: {state.PlayerHp} / {state.PlayerMaxHp}");
```

### Executing a Full Turn

```gdscript
# GDScript
# Player drags gems on the board
board.move_gem_path(drag_path)
var cascade: CascadeResult = board.resolve_cascade()

# --- Player Phase ---
var player_result: TurnResult = runner.execute_player_turn(cascade, state)

print("Combos: ", player_result.combos)
print("Damage dealt: ", player_result.total_damage_dealt)
print("Healing: ", player_result.healing)
for kill_id in player_result.kills:
    print("Killed: ", kill_id)
if player_result.wave_cleared:
    print("Wave cleared! Next wave: ", player_result.next_wave)
if player_result.battle_won:
    print("VICTORY!")
    return

# --- Enemy Phase ---
var enemy_result: TurnResult = runner.execute_enemy_turn(state)

print("Damage received: ", enemy_result.total_damage_received)
print("Player HP: ", enemy_result.player_hp_after)
for action in enemy_result.enemy_actions:
    print("Enemy action: ", action.type, " damage=", action.damage)
if enemy_result.battle_lost:
    print("DEFEAT!")
    return
```

```csharp
// C#
board.MoveGemPath(dragPath);
CascadeResult cascade = board.ResolveCascade();

// Player phase
TurnResult playerResult = runner.ExecutePlayerTurn(cascade, state);

Console.WriteLine($"Combos: {playerResult.Combos}");
Console.WriteLine($"Damage dealt: {playerResult.TotalDamageDealt}");
Console.WriteLine($"Healing: {playerResult.Healing}");
foreach (string killId in playerResult.Kills)
    Console.WriteLine($"Killed: {killId}");
if (playerResult.WaveCleared)
    Console.WriteLine($"Wave cleared! Next wave: {playerResult.NextWave}");
if (playerResult.BattleWon)
{
    Console.WriteLine("VICTORY!");
    return;
}

// Enemy phase
TurnResult enemyResult = runner.ExecuteEnemyTurn(state);

Console.WriteLine($"Damage received: {enemyResult.TotalDamageReceived}");
Console.WriteLine($"Player HP: {enemyResult.PlayerHpAfter}");
foreach (var action in enemyResult.EnemyActions)
    Console.WriteLine($"Enemy action: {action.Type} damage={action.Damage}");
if (enemyResult.BattleLost)
{
    Console.WriteLine("DEFEAT!");
    return;
}
```

### Handling Battle End

```gdscript
# GDScript
# Connect to dungeon events for UI
runner.battle_won.connect(func(info: Dictionary) -> void:
    print("=== BATTLE WON ===")
    print("Turns taken: ", info.turns)
    print("Max combo: ", info.max_combo)
    print("Rewards:")
    print("  Coins: ", info.rewards.coins)
    print("  Rank EXP: ", info.rewards.rank_exp)
    for drop in info.rewards.drops:
        # Roll for each drop
        if randf() <= drop.drop_rate:
            print("  Drop: ", drop.monster_def_id if drop.has("monster_def_id")
                  else drop.item_id)
)

runner.battle_lost.connect(func(info: Dictionary) -> void:
    print("=== BATTLE LOST ===")
    print("Died on wave ", info.wave, " after ", info.turns, " turns")
    show_continue_screen()  # offer stone continue
)
```

```csharp
// C#
runner.BattleWon += info =>
{
    Console.WriteLine("=== BATTLE WON ===");
    Console.WriteLine($"Turns taken: {info.Turns}");
    Console.WriteLine($"Max combo: {info.MaxCombo}");
    Console.WriteLine($"Coins: {info.Rewards.Coins}");
    Console.WriteLine($"Rank EXP: {info.Rewards.RankExp}");
    foreach (var drop in info.Rewards.Drops)
    {
        if (Random.Shared.NextDouble() <= drop.DropRate)
            Console.WriteLine($"  Drop: {drop.Id}");
    }
};

runner.BattleLost += info =>
{
    Console.WriteLine("=== BATTLE LOST ===");
    Console.WriteLine($"Died on wave {info.Wave} after {info.Turns} turns");
    ShowContinueScreen();
};
```

### Full Game Loop (Simplified)

```gdscript
# GDScript -- complete dungeon game loop
func run_dungeon(dungeon_def: DungeonDef, team: Array[MonsterInstance]) -> void:
    var runner := _create_runner()
    var state := runner.start_dungeon(dungeon_def, team)

    while state.is_active:
        # Wait for player input (drag gems)
        var drag_path: Array[int] = await board_screen.gem_dragged
        state.board.move_gem_path(drag_path)
        var cascade := state.board.resolve_cascade()

        # Animate cascades
        await battle_screen.play_cascade(cascade)

        # Player turn
        var p_result := runner.execute_player_turn(cascade, state)
        await battle_screen.play_player_results(p_result)

        if p_result.battle_won:
            await battle_screen.play_victory()
            _grant_rewards(state)
            return

        # Enemy turn
        var e_result := runner.execute_enemy_turn(state)
        await battle_screen.play_enemy_results(e_result)

        if e_result.battle_lost:
            await battle_screen.play_defeat()
            return
```

```csharp
// C# -- complete dungeon game loop
public async Task RunDungeon(DungeonDef dungeonDef, List<MonsterInstance> team)
{
    var runner = CreateRunner();
    var state = runner.StartDungeon(dungeonDef, team);

    while (state.IsActive)
    {
        // Wait for player input
        int[] dragPath = await boardScreen.WaitForGemDrag();
        state.Board.MoveGemPath(dragPath);
        var cascade = state.Board.ResolveCascade();

        // Animate cascades
        await battleScreen.PlayCascade(cascade);

        // Player turn
        var pResult = runner.ExecutePlayerTurn(cascade, state);
        await battleScreen.PlayPlayerResults(pResult);

        if (pResult.BattleWon)
        {
            await battleScreen.PlayVictory();
            GrantRewards(state);
            return;
        }

        // Enemy turn
        var eResult = runner.ExecuteEnemyTurn(state);
        await battleScreen.PlayEnemyResults(eResult);

        if (eResult.BattleLost)
        {
            await battleScreen.PlayDefeat();
            return;
        }
    }
}
```
