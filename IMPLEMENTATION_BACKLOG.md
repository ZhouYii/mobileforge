# MobileForge + Tower of Saviors — Implementation Backlog

## Status Key
- [x] Completed
- [ ] Not started

---

## TIER 1: Broken Data Pipes (Sprint 1) — COMPLETED

- [x] **1.1** Build team/stats from team_ids in battle_screen.gd
- [x] **1.2** Compute team HP from actual MonsterStats (was hardcoded 10000)
- [x] **1.3** Save gacha pulls to player state inventory
- [x] **1.4** Load owned monsters in monster_box (was sample data)
- [x] **1.5** Add get_def_data() to MFMonsterManager (team_select_screen fix)

## TIER 2a: Core Mechanics (Sprint 2) — COMPLETED

- [x] **2.1** Evaluate leader skills at battle start
- [x] **2.2** Active skill activation UI with buttons + cooldown display
- [x] **2.3** Roll loot table on battle win (result_screen.gd)
- [x] **2.4** Grant rewards to player state (currency + monster drops)
- [x] **2.7** Use enemy AI action results in dungeon_runner (heal/buff/attack branching)

## TIER 2b: Progression (Sprint 3) — COMPLETED

- [x] **2.5** Monster fusion (level up) and evolution UI in monster_box_screen
- [x] **2.6** Skill cooldown tracking (init, tick per turn, gate skill buttons)
- [x] **2.8** Enemy characteristics wired into combat (combo_shield, element_shield, damage_absorb, damage_reduction, damage_cap)

## TIER 3: Framework Quality (Sprint 4) — COMPLETED

- [x] **3.1** Wire MFSchemaValidator to GameData with opt-in schemas
- [x] **3.2** Network client retry/offline queue integration
- [x] **3.3** Audio manager fade/crossfade transitions
- [x] **3.4** Presentation layer animation helpers (tweens)
- [x] **3.5** Card view element icons/colors and rarity styling

## TIER 5: UI Components Architecture (Sprint 5) — COMPLETED

- [x] **5.1** Two-tier UI system (Primitives + Pages) replacing monolithic GameRenderer
- [x] **5.2** 8 Primitive components (MFButton, MFOverlay, MFProgressBar, MFScrollList, MFScrollGrid, MFGemBoard, MFMonsterCard, MFCurrencyDisplay)
- [x] **5.3** 9 Page implementations (TitlePage, LevelSelectPage, TeamSelectPage, BattlePage, ResultPage, GachaPage, InventoryPage, ShopPage, PlaceholderPage)
- [x] **5.4** UIComponentRouter + UIComponentRegistry replacing GameRenderer routing
- [x] **5.5** TosUISetup game-specific page registration
- [x] **5.6** SceneSetup.cs updated — UISystem GameObject replaces GameRenderer on Canvas

## TIER 4: Missing ToS Features (Future)

### Game Mechanics
- [x] Friend/helper system (6th team slot)
- [x] Dungeon difficulty tiers (Normal/Expert/Mythical/Annihilation)
- [x] Daily dungeons (rotating element-specific by day of week)
- [x] Floor effects ("no active skills", element restrictions, etc.)
- [x] Turn limits for specific dungeons
- [ ] Ranking dungeons with leaderboards

### Monster Progression
- [x] Plus stats system (+HP/+ATK/+REC, up to +297)
- [x] Skill inheritance (pass skills between monsters)
- [x] Skill leveling (increase level with duplicates/skill books)
- [x] Awakening (extra passive abilities with materials)
- [x] Limit break (raise max level: 99→109→119)

### Gacha Enhancements
- [x] Step-up banners (changing costs, milestone guarantees)
- [x] Daily free pull
- [x] Multi-pull discount (10 for price of 9)
- [x] Monster exchange/trade system
- [x] Beginner gacha with guaranteed top-rarity

### Economy
- [x] Daily login bonuses
- [x] Event currency for limited-time shops
- [x] Stamina overflow mechanics

### Board Mechanics (Advanced)
- [x] Gem status ticking during cascade (poison, burn)
- [x] Orb spawn skills (create specific gems)
- [x] Board size changes (7x6 for some dungeons)
- [x] Locked/unmatchable gems (enemy attacks)
- [x] Jammer/bomb/poison gems

### Skill Types Not Yet Registered
- [x] bind — prevent enemy from acting for N turns
- [x] poison_dot — damage over time on enemy
- [x] stun — skip enemy turn
- [x] orb_spawn — create gems on board
- [x] gravity_damage — percentage of enemy max HP
- [x] counter_attack — reflect received damage
- [x] void_damage_absorb — negate enemy damage absorption
- [x] attribute_absorb_shield — negate enemy element shield

---

## Files Modified

### Framework
- `framework/godot/addons/mobileforge/domain/monster/monster_manager.gd` — added get_def_data()
- `framework/godot/addons/mobileforge/domain/dungeon/dungeon_runner.gd` — enemy AI action branching, enemy characteristics
- `framework/godot/addons/mobileforge/domain/enemy/enemy_ai.gd` — bind/stun status checks in tick_countdowns
- `framework/godot/addons/mobileforge/infrastructure/game_data/game_data.gd` — schema validator integration
- `framework/godot/addons/mobileforge/infrastructure/network_client/network_client.gd` — retry + offline queue
- `framework/godot/addons/mobileforge/infrastructure/audio_manager/audio_manager.gd` — fade/crossfade
- `framework/godot/addons/mobileforge/presentation/anim/ui_anim.gd` — **new** animation helpers
- `framework/godot/addons/mobileforge/presentation/card/card_view.gd` — element/rarity styling

### Game
- `games/tower-of-saviors/godot/game/screens/battle_screen.gd` — team building, HP calc, leader skills, skill UI, cooldowns
- `games/tower-of-saviors/godot/game/screens/gacha_screen.gd` — persist pulls to player state
- `games/tower-of-saviors/godot/game/screens/monster_box_screen.gd` — load owned, fusion, evolution UI
- `games/tower-of-saviors/godot/game/screens/result_screen.gd` — loot rolling, reward granting
- `games/tower-of-saviors/godot/game/tos_game.gd` — schema registration, pass services to screens
- `games/tower-of-saviors/godot/game/skill_defs/register_all.gd` — registered bind, stun, poison_dot, gravity_damage
- `games/tower-of-saviors/godot/game/skill_defs/outcomes/bind_enemy.gd` — **new**
- `games/tower-of-saviors/godot/game/skill_defs/outcomes/stun_enemy.gd` — **new**
- `games/tower-of-saviors/godot/game/skill_defs/outcomes/poison_dot.gd` — **new**
- `games/tower-of-saviors/godot/game/skill_defs/outcomes/gravity_damage.gd` — **new**
- `games/tower-of-saviors/shared/data/skills.json` — added skills 11-14

### Unity (UI Components)
- `unity-project/Assets/MobileForge.UIComponents/PageBase.cs` — base class for all pages
- `unity-project/Assets/MobileForge.UIComponents/UIComponentRouter.cs` — replaces GameRenderer routing
- `unity-project/Assets/MobileForge.UIComponents/MFPrimitiveLibrary.cs` — type → prefab map
- `unity-project/Assets/MobileForge.UIComponents/Primitives/` — 8 primitive components
- `unity-project/Assets/MobileForge.UIComponents/Pages/` — 9 page implementations
- `unity-project/Assets/TosUISetup.cs` — game-specific page registration
- `unity-project/Assets/Editor/SceneSetup.cs` — UISystem GameObject setup
