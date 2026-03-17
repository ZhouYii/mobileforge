class_name MFLoginBonus extends RefCounted
## Tracks daily login streaks and grants rewards.
## Stores state in PlayerState. Game layer defines the reward schedule.
##
## Usage:
##   var lb = MFLoginBonus.new(player_state, rewards_schedule)
##   var result = lb.check_in()  # call once on app launch
##   if result.reward != null: grant(result.reward)


## One day's reward definition.
class DayReward extends RefCounted:
	var day: int  ## 1-based day number in the cycle
	var type: String  ## "currency", "monster", "item"
	var id: int  ## currency name index or monster/item id
	var currency: String  ## e.g. "gems", "coins" — only for type="currency"
	var count: int

	func _init(data: Dictionary = {}) -> void:
		day = int(data.get("day", 1))
		type = str(data.get("type", "currency"))
		id = int(data.get("id", 0))
		currency = str(data.get("currency", "coins"))
		count = int(data.get("count", 0))


## Result of a check_in attempt.
class CheckInResult extends RefCounted:
	var is_new_day: bool  ## True if this is the first check-in today
	var streak: int  ## Current consecutive login streak
	var day_in_cycle: int  ## Which day of the reward cycle (1-based)
	var reward: DayReward  ## The reward for today (null if already checked in)
	var cycle_length: int  ## Total days in the reward cycle

	func _init() -> void:
		is_new_day = false
		streak = 0
		day_in_cycle = 0
		reward = null
		cycle_length = 0


var _player_state: Object  ## PlayerState node
var _schedule: Array  ## Array[DayReward]
var _section_name: StringName = &"login_bonus"


func _init(player_state: Object, schedule: Array = []) -> void:
	_player_state = player_state
	_schedule = schedule


## Set the reward schedule (Array of DayReward or Dictionaries).
func set_schedule(schedule: Array) -> void:
	_schedule.clear()
	for entry in schedule:
		if entry is DayReward:
			_schedule.append(entry)
		elif entry is Dictionary:
			_schedule.append(DayReward.new(entry))


## Check in for today. Returns CheckInResult.
## Call this once on app launch / title screen enter.
func check_in() -> CheckInResult:
	var result := CheckInResult.new()
	result.cycle_length = _schedule.size()

	_ensure_section()
	var section = _player_state.get_section(_section_name)
	var today := _today_string()
	var last_date: String = str(section.get_value(&"last_login_date", ""))
	var streak: int = int(section.get_value(&"streak", 0))
	var total_days: int = int(section.get_value(&"total_days", 0))

	if last_date == today:
		# Already checked in today
		result.is_new_day = false
		result.streak = streak
		result.day_in_cycle = (total_days % _schedule.size()) + 1 if not _schedule.is_empty() else 0
		return result

	# New day!
	result.is_new_day = true

	# Check if streak continues (yesterday) or resets
	if _is_consecutive(last_date, today):
		streak += 1
	else:
		streak = 1

	total_days += 1

	# Save updated state
	section.set_value(&"last_login_date", today)
	section.set_value(&"streak", streak)
	section.set_value(&"total_days", total_days)

	result.streak = streak

	# Determine reward
	if not _schedule.is_empty():
		var day_idx := (total_days - 1) % _schedule.size()
		result.day_in_cycle = day_idx + 1
		result.reward = _schedule[day_idx]

	return result


## Get current streak without checking in.
func get_streak() -> int:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	return int(section.get_value(&"streak", 0))


## Get total login days.
func get_total_days() -> int:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	return int(section.get_value(&"total_days", 0))


## Check if already checked in today.
func has_checked_in_today() -> bool:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	return str(section.get_value(&"last_login_date", "")) == _today_string()


func _ensure_section() -> void:
	if not _player_state.has_section(_section_name):
		_player_state.register_section(_section_name, {})


func _today_string() -> String:
	var dt := Time.get_date_dict_from_system()
	return "%04d-%02d-%02d" % [dt["year"], dt["month"], dt["day"]]


func _is_consecutive(prev_date: String, current_date: String) -> bool:
	if prev_date.is_empty():
		return false
	# Parse dates and check if they're exactly 1 day apart
	var prev_unix := Time.get_unix_time_from_datetime_string(prev_date + "T00:00:00")
	var curr_unix := Time.get_unix_time_from_datetime_string(current_date + "T00:00:00")
	var diff := curr_unix - prev_unix
	return diff > 0 and diff <= 86400 * 1.5  # Allow some tolerance for timezone drift
