# MobileForge Testing Backlog

Every missing test, organized by module. Godot has full coverage (21 test files). Unity and game-specific tests have gaps.

---

## Unity Framework — Domain Tests (3 missing)

### CascadeResolverTests.cs
**Path:** `framework/unity/MobileForge.Tests/Domain/CascadeResolverTests.cs`
**Priority:** High
**Godot equivalent:** `framework/godot/tests/domain/test_cascade_resolver.gd` (exists)

| Test | What It Verifies |
|---|---|
| `Resolve_SingleMatch_RemovesAndDrops` | One match-3 clears gems and gravity fills gaps |
| `Resolve_MultiStepCascade_ChainsCorrectly` | Match → clear → drop → new match → clear chains |
| `Resolve_GravityFillsFromTop` | After clearing, gems above drop down, new gems spawn at top |
| `Resolve_NoMatch_ReturnsEmpty` | Board with no 3-in-a-row returns empty cascade steps |
| `Resolve_FullBoardCascade_CompletesWithoutInfiniteLoop` | Cascade terminates even with many chain reactions |
| `CascadeStep_ContainsCorrectMatchData` | Each step records matched positions, removed count, spawned elements |
| `Resolve_CrossPatternMatch_MergesOverlapping` | L-shape and T-shape matches merge into single match group |

### ComboCalculatorTests.cs
**Path:** `framework/unity/MobileForge.Tests/Domain/ComboCalculatorTests.cs`
**Priority:** Medium
**Godot equivalent:** None (tested inline within `test_combat_resolver.gd`)

| Test | What It Verifies |
|---|---|
| `ComboMultiplier_OneCombo_Returns1` | combo_mult = 1 + (1-1) * 0.25 = 1.0 |
| `ComboMultiplier_FiveCombos_Returns2` | combo_mult = 1 + (5-1) * 0.25 = 2.0 |
| `ComboMultiplier_TenCombos_Returns3_25` | combo_mult = 1 + (10-1) * 0.25 = 3.25 |
| `GemDamage_BaseFormula` | base_damage = atk * (1 + (gems-3) * 0.25) for 3, 4, 5, 6 gems |
| `GemDamage_MinimumThreeGems` | 3 gems = base multiplier of 1.0 |

### StaminaTimerTests.cs
**Path:** `framework/unity/MobileForge.Tests/Domain/StaminaTimerTests.cs`
**Priority:** Medium
**Godot equivalent:** None (tested inline within `test_economy.gd`)

| Test | What It Verifies |
|---|---|
| `Refill_CalculatesCorrectTime` | Given refill rate and current stamina, returns correct seconds until next point |
| `Refill_AtMaxStamina_ReturnsZero` | No refill needed when stamina is full |
| `Refill_PartialProgress_TracksCorrectly` | Fractional refill progress is tracked between ticks |
| `SecondsUntilFull_CalculatesFromCurrent` | Total time from current stamina to max |
| `Tick_AddsStaminaAtCorrectRate` | Calling tick(delta) adds stamina based on configured rate |

---

## Unity Framework — Presentation Tests (7 missing)

### ScreenRegistryTests.cs
**Path:** `framework/unity/MobileForge.Tests/Presentation/ScreenRegistryTests.cs`
**Priority:** High
**Godot equivalent:** Covered within `test_ui_router.gd`

| Test | What It Verifies |
|---|---|
| `Register_StoresFactory` | Registering a screen factory by ID makes it retrievable |
| `Create_ReturnsNewInstance` | Creating a screen from a registered factory returns a new instance |
| `HasScreen_ReturnsTrueForRegistered` | `HasScreen(id)` returns true for registered IDs |
| `HasScreen_ReturnsFalseForUnknown` | `HasScreen(id)` returns false for unregistered IDs |
| `Create_ThrowsForUnregistered` | Creating from an unregistered ID throws or returns null |

### VirtualListTests.cs
**Path:** `framework/unity/MobileForge.Tests/Presentation/VirtualListTests.cs`
**Priority:** High
**Godot equivalent:** `framework/godot/tests/presentation/test_virtual_list.gd` (exists)

| Test | What It Verifies |
|---|---|
| `SetData_PopulatesVisibleItems` | Setting data creates visible items within viewport |
| `SetData_RecyclesPoolItems` | Items scrolled out of view are returned to pool and reused |
| `VisibleRange_CalculatesCorrectly` | Given scroll offset and viewport height, returns correct start/end indices |
| `EmptyData_ShowsNothing` | Setting empty array clears all items |
| `UpdateData_RefreshesExistingItems` | Changing data array updates visible items without full rebuild |
| `ItemPool_ReusesInstances` | Pool returns previously freed items instead of creating new ones |

### GridViewTests.cs
**Path:** `framework/unity/MobileForge.Tests/Presentation/GridViewTests.cs`
**Priority:** Medium
**Godot equivalent:** None (Godot tested via `test_virtual_list.gd` shared pool logic)

| Test | What It Verifies |
|---|---|
| `SetData_CalculatesColumnLayout` | Items laid out in correct number of columns |
| `VisibleRange_AccountsForColumns` | Visible range calculation includes full rows |
| `CellRecycling_ReusesOnScroll` | Cells scrolled out are recycled into the pool |
| `EmptyData_ClearsGrid` | Setting empty array removes all cells |
| `SingleColumn_BehavesLikeList` | With columns=1, behaves identically to VirtualList |

### CurrencyBarTests.cs
**Path:** `framework/unity/MobileForge.Tests/Presentation/CurrencyBarTests.cs`
**Priority:** Medium
**Godot equivalent:** None

| Test | What It Verifies |
|---|---|
| `Bind_SubscribesToEventBus` | Calling `Bind("gems")` subscribes to currency_changed events |
| `CurrencyChanged_UpdatesDisplay` | Receiving a currency_changed event updates the displayed amount |
| `AnimatedCount_InterpolatesOverTime` | Display count animates from old value to new value |
| `Unbind_UnsubscribesFromEventBus` | Disposing the bar unsubscribes from events |

### CardViewTests.cs
**Path:** `framework/unity/MobileForge.Tests/Presentation/CardViewTests.cs`
**Priority:** Low
**Godot equivalent:** None

| Test | What It Verifies |
|---|---|
| `BindInstance_ShowsLevelAndStats` | Binding a MonsterInstance shows level, stats, and skill info |
| `BindDef_ShowsBaseInfo` | Binding a MonsterDef (no instance) shows base stats only |
| `BindNull_ClearsDisplay` | Binding null clears all displayed fields |
| `ElementColor_MapsCorrectly` | Element ID maps to correct display color/icon |

### OverlayManagerTests.cs
**Path:** `framework/unity/MobileForge.Tests/Presentation/OverlayManagerTests.cs`
**Priority:** Medium
**Godot equivalent:** None

| Test | What It Verifies |
|---|---|
| `Show_CreatesOverlay` | Showing an overlay by ID creates it via factory |
| `Hide_RemovesOverlay` | Hiding a shown overlay removes it |
| `HideAll_RemovesAllOverlays` | `HideAll()` clears all active overlays |
| `IsShowing_ReturnsTrueForActive` | `IsShowing(id)` returns true only for active overlays |
| `Show_DuplicateId_NoOp` | Showing an already-shown overlay does nothing |

### ToastLayerTests.cs
**Path:** `framework/unity/MobileForge.Tests/Presentation/ToastLayerTests.cs`
**Priority:** Low
**Godot equivalent:** None

| Test | What It Verifies |
|---|---|
| `ShowToast_CreatesToastItem` | Calling `Show(text)` creates a visible toast |
| `AutoDismiss_RemovesAfterDuration` | Toast disappears after its configured duration |
| `MaxToastLimit_OldestRemoved` | When max toasts are showing, oldest is removed for new one |
| `MultipleToasts_StackVertically` | Multiple simultaneous toasts stack without overlap |

---

## Unity Framework — UI Component Tests (4 missing)

### UIComponentRouterTests.cs
**Path:** `unity-project/Assets/MobileForge.UIComponents/Tests/UIComponentRouterTests.cs`
**Priority:** High

| Test | What It Verifies |
|---|---|
| `Navigate_MountsRegisteredPage` | Navigating to a screenId mounts the registered page |
| `Navigate_UnmountsPreviousPage` | Navigating away unmounts and cleans up the previous page |
| `Navigate_UnknownScreenId_NoException` | Navigating to an unregistered screenId does not throw |
| `Navigate_CallsBindWithScreen` | Mounted page receives the correct IScreen via Bind() |
| `Navigate_CallsRefreshOnRemount` | Re-navigating to the same screen calls Refresh() |

### PageBaseTests.cs
**Path:** `unity-project/Assets/MobileForge.UIComponents/Tests/PageBaseTests.cs`
**Priority:** Medium

| Test | What It Verifies |
|---|---|
| `SpawnPrimitive_CreatesCorrectType` | SpawnPrimitive<MFButton> creates an MFButton instance |
| `CreateRegion_AnchorsCorrectly` | CreateRegion with Top anchor sets RectTransform anchors to top |
| `Bind_SetsScreenProperty` | Calling Bind(screen) sets the Screen property |
| `OnRefresh_CalledOnRefresh` | Calling Refresh() invokes the OnRefresh() override |
| `Mount_AddsToParent` | Mount(parent) parents the page GameObject under parent |
| `Unmount_DestroysGameObject` | Unmount() destroys the page's GameObject |

### TosUISetupTests.cs
**Path:** `unity-project/Assets/MobileForge.UIComponents/Tests/TosUISetupTests.cs`
**Priority:** Medium

| Test | What It Verifies |
|---|---|
| `AllNinePages_Registered` | After setup, registry has entries for all 9 screen IDs |
| `Registry_ReturnsCorrectPageType` | Each screenId maps to the expected Page subclass |
| `Registry_TitleScreen_ReturnsTitlePage` | "title" screenId creates a TitlePage |

### PrimitiveTests.cs
**Path:** `unity-project/Assets/MobileForge.UIComponents/Tests/PrimitiveTests.cs`
**Priority:** Low

| Test | What It Verifies |
|---|---|
| `MFButton_ClickFiresEvent` | Clicking MFButton invokes OnClick |
| `MFButton_SetLabel_UpdatesText` | SetLabel changes the button text |
| `MFProgressBar_SetProgress_Clamps01` | SetProgress clamps values to 0-1 range |
| `MFGemBoard_SetElements_PopulatesGrid` | SetElements creates the correct number of gem cells |

---

## Unity Game Tests — Tower of Saviors (5 missing)

### TosIntegrationTests.cs
**Path:** `games/tower-of-saviors/unity/TowerOfSaviors/Tests/TosIntegrationTests.cs`
**Priority:** High
**Godot equivalent:** `games/tower-of-saviors/godot/game/tests/test_tos_battle.gd` (partial — 1 file exists)

| Test | What It Verifies |
|---|---|
| `FullBattleFlow_WinCondition` | Start dungeon → cascade → damage → kill enemies → wave clear → battle won |
| `FullBattleFlow_LoseCondition` | Start dungeon → enemies attack → HP reaches 0 → battle lost |
| `SkillRegistration_AllTypesAvailable` | `TosSkillRegistration.Register()` registers all 5 conditions + 5 effects |
| `SkillActivation_AreaDamage` | Activate area_damage skill → all alive enemies take damage |
| `SkillActivation_HealFlat` | Activate heal_flat skill → team HP increases by amount |
| `SkillActivation_ChangeGemElement` | Activate change_gem_element → board gems change from/to elements |
| `SkillActivation_DelayEnemies` | Activate delay_enemies → enemy countdowns increase |
| `GachaPull_DeductsCurrency` | Pulling from gacha pool spends correct currency amount |
| `GachaPull_PityTriggersAtThreshold` | After threshold pulls without top rarity, next pull is guaranteed |
| `BoardCascade_ResolvesCorrectly` | 5x6 board with known gem layout produces expected matches |
| `ElementAdvantage_CorrectMultipliers` | Water vs Fire = 1.5x, Fire vs Water = 0.5x, Light vs Dark = 1.5x |
| `TeamSkill_FullElementTeam` | All-water team activates "Tidal Formation" team skill |

---

## Godot Game Tests — Tower of Saviors (4 missing)

### test_tos_conditions.gd
**Path:** `games/tower-of-saviors/godot/game/tests/test_tos_conditions.gd`
**Priority:** Medium
**Note:** Tests the extracted condition files (combo_above, hp_threshold, elements_matched, team_has_element)

| Test | What It Verifies |
|---|---|
| `combo_above_passes_at_threshold` | ComboAboveCondition returns true when combo_count >= threshold |
| `combo_above_fails_below_threshold` | Returns false when combo_count < threshold |
| `hp_threshold_passes_below` | HpThresholdCondition returns true when HP ratio <= percent |
| `hp_threshold_fails_above` | Returns false when HP ratio > percent |
| `hp_threshold_handles_zero_max_hp` | Returns false when max_hp = 0 (no division by zero) |
| `elements_matched_passes_at_count` | ElementsMatchedCondition returns true when element count >= min_count |
| `team_has_element_finds_element` | TeamHasElementCondition returns true when team has element |
| `team_has_element_missing_element` | Returns false when no team member has element |

### test_tos_outcomes.gd
**Path:** `games/tower-of-saviors/godot/game/tests/test_tos_outcomes.gd`
**Priority:** Medium
**Note:** Tests the extracted outcome files (area_damage, change_gem_element, delay_enemies)

| Test | What It Verifies |
|---|---|
| `area_damage_hits_all_alive` | AreaDamage deals atk * mult to all alive enemies |
| `area_damage_skips_dead` | Dead enemies receive no damage |
| `change_gem_element_converts` | ChangeGemElement converts all gems of from_elem to to_elem |
| `change_gem_element_no_board` | Handles null board gracefully |
| `delay_enemies_adds_turns` | DelayEnemies adds turns to all alive enemy countdowns |

### test_tos_team_skills.gd
**Path:** `games/tower-of-saviors/godot/game/tests/test_tos_team_skills.gd`
**Priority:** Low
**Note:** Tests team_skills.json data loading and team skill evaluation

| Test | What It Verifies |
|---|---|
| `tidal_formation_all_water` | All-water team activates ATK x1.5 for all |
| `inferno_alliance_three_fire` | 3+ fire members activates Fire ATK x2.0 |
| `mixed_team_no_activation` | Mixed team doesn't activate element-specific team skills |

### test_tos_battle.gd (4 missing tests in existing file)
**Path:** `games/tower-of-saviors/godot/game/tests/test_tos_battle.gd`
**Priority:** Medium
**Note:** File exists with 1 integration test; needs 4 more

| Test | What It Verifies |
|---|---|
| `battle_enemy_turn_deals_damage` | After player turn, enemy attacks reduce team HP |
| `battle_wave_progression` | Clearing all enemies advances to next wave |
| `battle_skill_activation_during_battle` | Activating a skill mid-battle modifies combat state |
| `battle_element_advantage_applied` | Water attacking Fire deals 1.5x damage |

---

## Godot Game Tests — Helper System

### test_tos_helper.gd
**Path:** `games/tower-of-saviors/godot/game/tests/test_tos_helper.gd`
**Priority:** Medium
**Note:** Tests the helper provider and friend helper integration in battle

| Test | What It Verifies |
|---|---|
| `helper_provider_returns_correct_count` | `get_available_helpers()` returns `helper_count` entries |
| `helper_provider_filters_by_leader_skill` | All returned helpers have `leader_skill_id >= 0` |
| `helper_provider_filters_by_rarity` | All returned helpers have `rarity >= 5` |
| `helper_provider_refresh_randomizes` | Two calls to `refresh()` produce different orderings (with high probability) |
| `helper_in_team_contributes_hp` | Team with 5 members + helper has higher HP than 5 alone |
| `friend_leader_skill_applied` | Both leader (slot 0) and friend (slot 5) leader skills produce buffs in `team_buffs` |
| `helper_skill_gets_cooldown` | `_init_skill_cooldowns()` creates entries for all 6 slots including helper |
| `no_helper_selected_works` | Entering battle without a helper uses 5-member team as before |

---

## Cross-Engine Test Vectors (2 files — created, need test harnesses)

### skill_pipeline_cases.json
**Path:** `framework/shared/test_vectors/skill_pipeline_cases.json`
**Status:** File created (10 test cases)
**Missing:** Unity and Godot test harnesses that load and run these cases

| Test Case | What It Verifies |
|---|---|
| `always_true_condition_passes` | Always-true condition activates skill |
| `combo_above_condition_fails` | Combo below threshold blocks activation |
| `combo_above_condition_passes` | Combo at/above threshold allows activation |
| `hp_below_condition_passes` | HP below threshold allows activation |
| `hp_below_condition_fails_above_threshold` | HP above threshold blocks activation |
| `elements_matched_condition` | Element match count check |
| `multiple_conditions_all_must_pass` | AND logic across conditions |
| `multiple_outcomes_accumulate` | Multiple outcomes stack their effects |
| `area_damage_outcome` | Area damage hits all alive enemies |
| `delay_enemies_outcome` | Delay adds turns to enemy countdowns |

### gacha_distribution_cases.json
**Path:** `framework/shared/test_vectors/gacha_distribution_cases.json`
**Status:** File created (7 test cases)
**Missing:** Unity and Godot test harnesses that load and run these cases

| Test Case | What It Verifies |
|---|---|
| `pity_triggers_at_threshold` | Forced top-rarity at pity threshold |
| `pity_does_not_trigger_below_threshold` | Normal random below threshold |
| `pity_resets_on_top_rarity` | Counter resets to 0 on top-rarity pull |
| `displayed_rates_match_weights` | Rate display matches weight percentages |
| `multi_pull_pity_tracks_across_pulls` | Pity accumulates across multi-pull |
| `weighted_distribution_statistical` | 10K rolls approximate expected distribution |
| `empty_pool_handling` | Empty pool fails gracefully |

---

---

## New Tests Needed — Game Functionality Completion (Tasks 1-8)

### test_tos_new_conditions.gd
**Path:** `games/tower-of-saviors/godot/game/tests/test_tos_new_conditions.gd`
**Priority:** Medium

| Test | What It Verifies |
|---|---|
| `combo_gte_passes_at_min` | ComboGteCondition returns true when combo_count >= min_combo |
| `combo_gte_fails_below_min` | Returns false when combo_count < min_combo |
| `combo_lt_passes_below_max` | ComboLtCondition returns true when combo_count < max_combo |
| `combo_lt_fails_at_max` | Returns false when combo_count >= max_combo |

### test_tos_new_outcomes.gd
**Path:** `games/tower-of-saviors/godot/game/tests/test_tos_new_outcomes.gd`
**Priority:** Medium

| Test | What It Verifies |
|---|---|
| `single_target_damage_targets_highest_hp` | Hits only the highest-HP alive enemy |
| `gem_conversion_converts_from_to` | Converts gems using from_element/to_element params |
| `self_damage_reduces_hp_by_percent` | Team HP reduced by hp_percent of max_hp, minimum 1 |
| `lifesteal_heals_from_previous_damage` | Healing = total damage_dealt * percent_of_damage |
| `heal_over_time_heals_each_turn` | HealOverTime heals on activate and on_turn_start |
| `atk_buff_multiplies_for_duration` | AtkBuff active for duration_turns then expires |
| `defense_buff_reduces_damage` | DefenseBuff reduces incoming damage by damage_reduction |
| `combo_scaling_atk_scales_with_combos` | Bonus increases with combo_count |

### test_tos_enemy_ai_behaviors.gd
**Path:** `games/tower-of-saviors/godot/game/tests/test_tos_enemy_ai_behaviors.gd`
**Priority:** Medium

| Test | What It Verifies |
|---|---|
| `normal_behavior_always_attacks` | Default behavior returns "attack" action |
| `heavy_attack_2x_every_3rd` | Returns 2x damage on 3rd, 6th, 9th attack |
| `heal_self_triggers_below_30_percent` | Heals 20% max HP when HP < 30%, attacks otherwise |
| `buff_allies_triggers_once` | First action = buff, subsequent = attack |
| `buff_allies_boosts_other_enemies` | apply_buff_allies multiplies other enemies' ATK |

### test_tos_team_select.gd
**Path:** `games/tower-of-saviors/godot/game/tests/test_tos_team_select.gd`
**Priority:** Low

| Test | What It Verifies |
|---|---|
| `select_monster_adds_to_team` | Selecting a monster adds its ID to selected_ids |
| `cannot_exceed_max_team_size` | Selecting 6th monster is rejected |
| `cannot_add_duplicate` | Same monster ID cannot be added twice |
| `remove_slot_removes_monster` | Tapping a filled slot removes the monster |
| `start_battle_passes_team_ids` | Battle params include team_ids array |

### test_tos_save_load.gd
**Path:** `games/tower-of-saviors/godot/game/tests/test_tos_save_load.gd`
**Priority:** Medium

| Test | What It Verifies |
|---|---|
| `save_creates_file` | Calling save(0) creates save_0.json |
| `load_restores_state` | Loading restores currencies, monsters, progress |
| `dirty_flag_set_on_currency_change` | Currency change marks save as dirty |
| `stamina_refills_over_time` | StaminaTimer adds stamina after elapsed seconds |

### TosNewSkillTests.cs (Unity)
**Path:** `games/tower-of-saviors/unity/TowerOfSaviors/Tests/TosNewSkillTests.cs`
**Priority:** Medium

| Test | What It Verifies |
|---|---|
| `AllSkillJsonTypes_AreRegistered` | Every condition/effect type in skills.json is registered |
| `AllTeamSkillJsonTypes_AreRegistered` | Every type in team_skills.json is registered |
| `SingleTargetDamage_TargetsHighestHp` | Hits only the highest-HP enemy |
| `SelfDamage_FloorAtOne` | Self damage cannot kill the player |
| `Lifesteal_ReadsFromResult` | Lifesteal reads damage_dealt from same activation |
| `EnemyAI_HeavyAttack_DoublesEvery3rd` | Attack 3 returns 2x damage |
| `EnemyAI_HealSelf_Below30Percent` | Heal triggers below 30% HP |
| `EnemyAI_BuffAllies_OnceOnly` | Buff triggers once, attack thereafter |

---

## Summary

| Category | Missing Tests | Priority Breakdown |
|---|---|---|
| Unity Domain | 3 files, ~17 tests | 1 High, 2 Medium |
| Unity Presentation | 7 files, ~33 tests | 2 High, 3 Medium, 2 Low |
| Unity UI Components | 4 files, ~19 tests | 1 High, 2 Medium, 1 Low |
| Unity Game (ToS) | 2 files, ~20 tests | 1 High, 1 Medium |
| Godot Game (ToS) | 7 files + 4 additions, ~45 tests | 0 High, 5 Medium, 2 Low |
| Test Vector Harnesses | 2 harnesses per engine (4 total) | 2 Medium |
| **Total** | **~27 files, ~142 tests** | **5 High, 16 Medium, 6 Low** |

### Recommended Implementation Order

1. **High priority first:** CascadeResolverTests.cs, ScreenRegistryTests.cs, VirtualListTests.cs, UIComponentRouterTests.cs, TosIntegrationTests.cs
2. **Medium priority:** ComboCalculatorTests.cs, StaminaTimerTests.cs, GridViewTests.cs, CurrencyBarTests.cs, OverlayManagerTests.cs, test_tos_conditions.gd, test_tos_outcomes.gd, test_tos_new_conditions.gd, test_tos_new_outcomes.gd, test_tos_enemy_ai_behaviors.gd, test_tos_save_load.gd, TosNewSkillTests.cs, test vector harnesses
3. **Low priority:** CardViewTests.cs, ToastLayerTests.cs, test_tos_team_skills.gd, test_tos_team_select.gd
