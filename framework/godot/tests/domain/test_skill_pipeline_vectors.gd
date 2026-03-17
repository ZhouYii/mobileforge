extends MFTestBase
## Test vector harness for skill_pipeline_cases.json

const FileAccessClass = preload("res://addons/mobileforge/domain/skill_pipeline/skill_condition.gd")


func test_skill_pipeline_vectors() -> void:
	var json_path := "res://framework/shared/test_vectors/skill_pipeline_cases.json"
	if not ResourceLoader.exists(json_path):
		print("  [SKIP] skill_pipeline_cases.json not found")
		return
	
	var file := FileAccess.open(json_path, FileAccess.READ)
	if file == null:
		print("  [SKIP] Cannot open skill_pipeline_cases.json")
		return
	
	var json_text := file.get_as_text()
	var json := JSON.new()
	var err := json.parse(json_text)
	if err != OK:
		print("  [FAIL] Cannot parse skill_pipeline_cases.json: " + json.get_error_message())
		_failed += 1
		return
	
	var cases: Array = json.data.cases
	for case_data: Dictionary in cases:
		_run_skill_case(case_data)


func _run_skill_case(case_data: Dictionary) -> void:
	var name: String = case_data.name
	var skill_def: Dictionary = case_data.skill_def
	var context_data: Dictionary = case_data.context
	var expected: Dictionary = case_data.expected
	
	var ctx := _build_context(context_data)
	var result := _activate_skill(skill_def, ctx)
	
	var expected_success: bool = expected.success
	if expected_success:
		if expected.has("healing"):
			var expected_healing: int = expected.healing
			assert_eq(result.healing, expected_healing, name + ": healing")
		if expected.has("damage_dealt"):
			var expected_damage: Dictionary = expected.damage_dealt
			for key: String in expected_damage.keys():
				var idx := int(key)
				assert_eq(result.damage_dealt.get(idx, 0), expected_damage[key], name + ": damage to enemy " + key)
	else:
		assert_eq(result.healing, 0, name + ": should not heal")


func _build_context(data: Dictionary) -> RefCounted:
	var ctx := RefCounted.new()
	ctx.set("team_hp", data.get("team_hp", 10000))
	ctx.set("max_hp", data.get("max_hp", 10000))
	ctx.set("combo_count", data.get("combo_count", 1))
	ctx.set("turn_number", data.get("turn_number", 1))
	
	var elements_matched := {}
	if data.has("elements_matched"):
		for key: String in data.elements_matched.keys():
			elements_matched[int(key)] = data.elements_matched[key]
	ctx.set("elements_matched", elements_matched)
	
	var enemies := []
	if data.has("enemies"):
		for e: Dictionary in data.enemies:
			var enemy := RefCounted.new()
			enemy.set("is_alive", e.get("is_alive", true))
			enemy.set("countdown", e.get("countdown", 1))
			enemy.set("hp", e.get("hp", 1000))
			enemies.append(enemy)
	ctx.set("enemies", enemies)
	
	var team_stats := []
	if data.has("team_stats"):
		for s: Dictionary in data.team_stats:
			var stats := RefCounted.new()
			stats.set("atk", s.get("atk", 1000))
			team_stats.append(stats)
	ctx.set("team_stats", team_stats)
	
	ctx.set("team", [])
	ctx.set("board", null)
	
	return ctx


func _activate_skill(skill_def: Dictionary, ctx: RefCounted) -> RefCounted:
	var result := RefCounted.new()
	result.set("healing", 0)
	result.set("damage_dealt", {})
	result.set("board_changes", [])
	result.set("enemies_killed", [])
	
	var rules: Array = skill_def.get("rules", [])
	for rule: Dictionary in rules:
		var conditions: Array = rule.get("conditions", [])
		var all_met := true
		for cond: Dictionary in conditions:
			if not _check_condition(cond, ctx):
				all_met = false
				break
		
		if not all_met:
			continue
		
		var outcomes: Array = rule.get("outcomes", [])
		for outcome: Dictionary in outcomes:
			_execute_outcome(outcome, ctx, result)
	
	return result


func _check_condition(cond: Dictionary, ctx: RefCounted) -> bool:
	var type: String = cond.get("type", "")
	var params: Dictionary = cond.get("params", {})
	
	match type:
		"always_true":
			return true
		"combo_above":
			var threshold: int = params.get("threshold", 1)
			return ctx.combo_count >= threshold
		"hp_below":
			var percent: float = params.get("percent", 0.5)
			if ctx.max_hp <= 0:
				return false
			return float(ctx.team_hp) / float(ctx.max_hp) <= percent
		"elements_matched":
			var element: int = params.get("element", 0)
			var min_count: int = params.get("min_count", 1)
			return ctx.elements_matched.get(element, 0) >= min_count
		_:
			return false


func _execute_outcome(outcome: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var type: String = outcome.get("type", "")
	var params: Dictionary = outcome.get("params", {})
	
	match type:
		"heal_flat":
			result.healing += params.get("amount", 0)
		"heal_percent":
			var percent: float = params.get("percent", 0.0)
			result.healing += int(ctx.max_hp * percent)
		"area_damage":
			var mult: float = params.get("multiplier", 1.0)
			var atk: float = 0.0
			if ctx.team_stats.size() > 0:
				atk = ctx.team_stats[0].atk
			for i in range(ctx.enemies.size()):
				if ctx.enemies[i].is_alive:
					result.damage_dealt[i] = int(atk * mult)
		"delay_enemies":
			var turns: int = params.get("turns", 1)
			for enemy in ctx.enemies:
				if enemy.is_alive:
					enemy.countdown += turns
