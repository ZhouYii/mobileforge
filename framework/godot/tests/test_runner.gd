extends SceneTree
## MobileForge Test Runner
## Run from command line:  godot --headless -s tests/test_runner.gd
## Loads all test suites, runs them, prints summary, exits with 0/1.

func _init() -> void:
	var suites: Array = []

	# Infrastructure layer tests
	suites.append(preload("res://tests/infrastructure/test_event_bus.gd").new())
	suites.append(preload("res://tests/infrastructure/test_game_data.gd").new())
	suites.append(preload("res://tests/infrastructure/test_player_state.gd").new())

	# Domain layer tests — Phase 2 (Board, Combat, Monster)
	suites.append(preload("res://tests/domain/test_board_logic.gd").new())
	suites.append(preload("res://tests/domain/test_match_detector.gd").new())
	suites.append(preload("res://tests/domain/test_cascade_resolver.gd").new())
	suites.append(preload("res://tests/domain/test_combat_resolver.gd").new())
	suites.append(preload("res://tests/domain/test_element_chart.gd").new())
	suites.append(preload("res://tests/domain/test_monster_manager.gd").new())

	# Domain layer tests — Phase 3 (Skill, EnemyAI, Dungeon, Team)
	suites.append(preload("res://tests/domain/test_skill_pipeline.gd").new())
	suites.append(preload("res://tests/domain/test_skill_conditions.gd").new())
	suites.append(preload("res://tests/domain/test_enemy_ai.gd").new())
	suites.append(preload("res://tests/domain/test_dungeon_runner.gd").new())
	suites.append(preload("res://tests/domain/test_team_builder.gd").new())

	# Domain layer tests — Phase 5 (Gacha, Economy, Loot)
	suites.append(preload("res://tests/domain/test_gacha_roller.gd").new())
	suites.append(preload("res://tests/domain/test_economy.gd").new())
	suites.append(preload("res://tests/domain/test_loot_table.gd").new())

	# Infrastructure layer tests — Phase 4 (SaveManager)
	suites.append(preload("res://tests/infrastructure/test_save_manager.gd").new())

	# Presentation layer tests — Phase 4 (PopupStack, VirtualList, UIRouter)
	suites.append(preload("res://tests/presentation/test_popup_stack.gd").new())
	suites.append(preload("res://tests/presentation/test_virtual_list.gd").new())
	suites.append(preload("res://tests/presentation/test_ui_router.gd").new())

	print("=== MobileForge Test Runner ===")
	print("")

	var total_passed := 0
	var total_failed := 0

	for suite in suites:
		var result: Dictionary = suite.run_all()
		total_passed += result.passed
		total_failed += result.failed
		print("")

	print("=== Results: %d passed, %d failed ===" % [total_passed, total_failed])

	if total_failed > 0:
		print("SOME TESTS FAILED")
		quit(1)
	else:
		print("ALL TESTS PASSED")
		quit(0)
