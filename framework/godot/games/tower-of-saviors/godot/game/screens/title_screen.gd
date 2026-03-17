extends MFBaseScreen
## ToS Title Screen — Main menu with navigation to all game screens.
## 36px golden title with fade-in, dark gradient background,
## styled buttons with MFButtonFeedback, staggered entrance (0.5s per ToS).
## Login bonus popup via MFConfirmDialog + MFUIAnim.pop_in.

var _vbox: VBoxContainer
var _login_bonus: MFLoginBonus
var _economy: MFEconomy
var _popup_stack: Node  # MFPopupStack (optional)
var _btn_feedbacks: Array = []  # Array[MFButtonFeedback] to prevent GC


func setup(login_bonus: MFLoginBonus = null, economy: MFEconomy = null, popup_stack: Node = null) -> void:
	_login_bonus = login_bonus
	_economy = economy
	_popup_stack = popup_stack


func _init() -> void:
	# Dark gradient background
	var bg = ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.color = Color(0.05, 0.05, 0.08)
	bg.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(bg)

	_vbox = VBoxContainer.new()
	_vbox.set_anchors_preset(Control.PRESET_CENTER)
	_vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	_vbox.add_theme_constant_override("separation", 12)
	add_child(_vbox)

	# Golden title
	var title = Label.new()
	title.text = "Tower of Saviors"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 36)
	title.add_theme_color_override("font_color", Color(1.0, 0.85, 0.0))
	title.add_theme_color_override("font_shadow_color", Color(0.4, 0.2, 0.0, 0.7))
	title.add_theme_constant_override("shadow_offset_x", 2)
	title.add_theme_constant_override("shadow_offset_y", 2)
	_vbox.add_child(title)

	var spacer = Control.new()
	spacer.custom_minimum_size.y = 30
	_vbox.add_child(spacer)

	# Navigation buttons
	_add_nav_button("Dungeon Select", &"dungeon_select", Color(0.3, 0.5, 0.8))
	_add_nav_button("Gacha", &"gacha", Color(0.8, 0.6, 0.1))
	_add_nav_button("Monster Box", &"monster_box", Color(0.3, 0.7, 0.4))
	_add_nav_button("Shop", &"shop", Color(0.7, 0.4, 0.8))


func on_enter(_params: Dictionary = {}) -> void:
	# Animate entrance (must be in tree for tweens to work)
	var title_node = _vbox.get_child(0)  # First child is the title label
	if title_node != null:
		MFUIAnim.fade_in(title_node, 0.6)
	# Stagger animate buttons (skip title + spacer = first 2 children)
	var buttons: Array = []
	for i in range(2, _vbox.get_child_count()):
		buttons.append(_vbox.get_child(i))
	if not buttons.is_empty():
		MFUIAnim.stagger_fade_in(buttons, 0.3, 0.08)
	# Check daily login bonus
	if _login_bonus != null and not _login_bonus.has_checked_in_today():
		var result = _login_bonus.check_in()
		if result.is_new_day and result.reward != null:
			_grant_login_reward(result.reward)
			_show_login_popup(result)


func _grant_login_reward(reward: RefCounted) -> void:
	if _economy == null:
		return
	if reward.type == "currency":
		_economy.earn(reward.currency, reward.count)


func _show_login_popup(result: RefCounted) -> void:
	# Build reward text
	var reward = result.reward
	var reward_text: String
	if reward.type == "currency":
		reward_text = "Received: %d %s" % [reward.count, reward.currency]
	else:
		reward_text = "Received: %s x%d" % [reward.type, reward.count]

	if _popup_stack != null:
		# Use MFConfirmDialog via popup stack
		var dialog := MFConfirmDialog.new()
		dialog.set_title("Daily Login Bonus!") \
			.set_message("Login Streak: %d days\nDay %d of %d\n\n%s" % [
				result.streak, result.day_in_cycle, result.cycle_length, reward_text]) \
			.add_button("OK", Callable(), "primary")
		dialog.show_via(_popup_stack, &"login_bonus")
	else:
		# Fallback: inline popup
		_show_inline_login_popup(result, reward_text)


func _show_inline_login_popup(result: RefCounted, reward_text: String) -> void:
	var popup = PanelContainer.new()
	popup.set_anchors_preset(Control.PRESET_CENTER)
	popup.z_index = 10
	var popup_style := TosTheme.make_panel(Color(1.0, 0.85, 0.0))
	popup.add_theme_stylebox_override("panel", popup_style)
	add_child(popup)

	var panel_vbox = VBoxContainer.new()
	panel_vbox.add_theme_constant_override("separation", 8)
	popup.add_child(panel_vbox)

	var header = Label.new()
	header.text = "Daily Login Bonus!"
	header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	header.add_theme_font_size_override("font_size", 22)
	header.add_theme_color_override("font_color", Color(1.0, 0.85, 0.0))
	panel_vbox.add_child(header)

	var streak_lbl = Label.new()
	streak_lbl.text = "Login Streak: %d days" % result.streak
	streak_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	panel_vbox.add_child(streak_lbl)

	var day_lbl = Label.new()
	day_lbl.text = "Day %d of %d" % [result.day_in_cycle, result.cycle_length]
	day_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	day_lbl.add_theme_font_size_override("font_size", 12)
	panel_vbox.add_child(day_lbl)

	var reward_lbl = Label.new()
	reward_lbl.text = reward_text
	reward_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	reward_lbl.add_theme_font_size_override("font_size", 18)
	reward_lbl.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3))
	panel_vbox.add_child(reward_lbl)

	var ok_btn = Button.new()
	ok_btn.text = "OK"
	ok_btn.custom_minimum_size = Vector2(120, 40)
	ok_btn.add_theme_stylebox_override("normal", TosTheme.make_button_style(0))
	ok_btn.pressed.connect(func(): popup.queue_free())
	panel_vbox.add_child(ok_btn)

	# Pop-in animation
	MFUIAnim.pop_in(popup, TosTheme.ANIM_TRANSITION)


func _add_nav_button(label: String, screen_name: StringName, accent: Color = Color.WHITE) -> Button:
	var btn = Button.new()
	btn.text = label
	btn.custom_minimum_size = Vector2(220, 50)
	btn.add_theme_font_size_override("font_size", 16)
	btn.pressed.connect(func(): _navigate_to(screen_name))

	# Styled button
	var style := StyleBoxFlat.new()
	style.bg_color = Color(accent.r * 0.2, accent.g * 0.2, accent.b * 0.2, 0.85)
	style.corner_radius_top_left = 8
	style.corner_radius_top_right = 8
	style.corner_radius_bottom_left = 8
	style.corner_radius_bottom_right = 8
	style.border_width_bottom = 3
	style.border_color = accent
	style.content_margin_left = 16
	style.content_margin_right = 16
	style.content_margin_top = 10
	style.content_margin_bottom = 10
	btn.add_theme_stylebox_override("normal", style)

	# Button feedback
	var feedback := MFButtonFeedback.new(btn)
	feedback.add_scale(0.95)
	_btn_feedbacks.append(feedback)

	_vbox.add_child(btn)
	return btn


func _navigate_to(screen_name: StringName) -> void:
	var router = get_node_or_null("/root/TosGame")
	if router and router._ui_router:
		router._ui_router.navigate(screen_name)
