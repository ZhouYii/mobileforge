class_name TosTheme extends RefCounted
## ToS visual constants — exact values from decompiled source.
## Centralizes element colors, animation timings, card states, layout zones,
## and factory methods for consistent styling across all game UI.

# ── Element Colors (from Element.cs) ──

const ELEMENT_COLORS := {
	0: Color(0.5, 0.5, 0.5),       # None - grey
	1: Color(0.251, 1.0, 1.0),     # Water - cyan
	2: Color(1.0, 0.251, 0.251),   # Fire - red
	3: Color(0.251, 1.0, 0.243),   # Grass - green
	4: Color(1.0, 1.0, 0.251),     # Light - yellow
	5: Color(1.0, 0.251, 1.0),     # Dark - magenta
	6: Color(1.0, 0.576, 0.749),   # Heart - pink
	7: Color(0.4, 0.4, 0.4),       # Jammer - dark grey
	8: Color(0.9, 0.1, 0.1),       # Bomb - bright red
	9: Color(0.3, 0.0, 0.4),       # Poison - dark purple
}

const ELEMENT_NAMES := {
	0: "None", 1: "Water", 2: "Fire", 3: "Earth",
	4: "Light", 5: "Dark", 6: "Heart",
	7: "Jammer", 8: "Bomb", 9: "Poison",
}

const ELEMENT_ICONS := {
	0: "\u25cf",  # ●
	1: "\u2248",  # ≈ water waves
	2: "\u2668",  # ♨ fire
	3: "\u2618",  # ☘ earth
	4: "\u2600",  # ☀ light
	5: "\u263d",  # ☽ dark
	6: "\u2665",  # ♥ heart
	7: "\u2716",  # ✖ jammer
	8: "\u25c6",  # ◆ bomb
	9: "\u2620",  # ☠ poison
}

# ── Card States (from DataCardIcon.cs) ──

const CARD_ALPHA_DISABLED := 0.6
const CARD_ALPHA_LOCKED := 0.55
const CARD_FRAME_LOCKED := Color(0.282, 0.282, 0.282)
const CARD_POP_SCALE := 1.12

# ── Animation Timings (from various source files) ──

const ANIM_PANEL := 0.5           # Top bar entrance
const ANIM_HP_BAR := 0.4          # GamePlayBar default
const ANIM_ENEMY_ENTER := 1.0     # Enemy come/leave
const ANIM_CARD_SHINE := 0.3      # Border shimmer
const ANIM_CARD_POP := 0.3        # 1.0→1.12→1.0 with easeInOutBack
const ANIM_GEM_MATCH := 0.33      # spitAnimate duration
const ANIM_GEM_EFFECT := 0.4      # White/yellow ball
const ANIM_DAMAGE := 0.46         # playerDamageTextJumpTime
const ANIM_COMBO := 0.3           # Combo particle
const ANIM_TRANSITION := 0.25     # Screen transition (ContainerObject)

# ── Gem Drag (from FollowMouse.cs) ──

const GEM_DRAG_SCALE := 1.06
const GEM_DRAG_ALPHA := 0.65

# ── Board Layout ──

const GEM_CELL_SIZE := 62.0
const GEM_SPACING := 3.0
const BOARD_CORNER_RADIUS := 6

# ── Layout Zones (from puzzle system) ──

const ZONE_ENEMY_HEIGHT := 0.295   # 29.5% from top
const ZONE_PUZZLE_START := 0.44444 # 44.4% from top

# ── Skill Button ──

const SKILL_BTN_SIZE := Vector2(80, 44)
const SKILL_BTN_FONT_SIZE := 12
const SKILL_ACTIVE_SCALE := 1.2
const SKILL_INACTIVE_SCALE := 1.0

# ── HP Bar ──

const HP_BAR_HEIGHT := 24.0

# ── Combo Color Ramp ──

const COMBO_COLORS := {
	1: Color.WHITE,
	3: Color(1.0, 1.0, 0.3),      # Yellow
	5: Color(1.0, 0.6, 0.1),      # Orange
	7: Color(1.0, 0.85, 0.0),     # Gold
}

# ── Rarity ──

const RARITY_COLORS := {
	1: Color(0.7, 0.7, 0.7),      # Grey
	2: Color(0.6, 0.8, 0.6),      # Green-grey
	3: Color(0.4, 0.7, 1.0),      # Blue
	4: Color(0.7, 0.5, 1.0),      # Purple
	5: Color(1.0, 0.7, 0.1),      # Gold-orange
	6: Color(1.0, 0.85, 0.0),     # Gold
	7: Color(1.0, 0.3, 0.3),      # Red (legendary)
}


# ── Lookup Functions ──

static func element_color(element: int) -> Color:
	return ELEMENT_COLORS.get(element, Color(0.5, 0.5, 0.5))


static func element_name(element: int) -> String:
	return ELEMENT_NAMES.get(element, "None")


static func element_icon(element: int) -> String:
	return ELEMENT_ICONS.get(element, "\u25cf")


static func rarity_color(rarity: int) -> Color:
	return RARITY_COLORS.get(rarity, Color(0.7, 0.7, 0.7))


static func rarity_stars(rarity: int) -> String:
	return "\u2605".repeat(rarity)


static func combo_color(combo_count: int) -> Color:
	if combo_count >= 7:
		return COMBO_COLORS[7]
	elif combo_count >= 5:
		return COMBO_COLORS[5]
	elif combo_count >= 3:
		return COMBO_COLORS[3]
	return COMBO_COLORS[1]


# ── Factory Methods ──

## Create a dark styled panel with optional colored accent border.
static func make_panel(accent_color: Color = Color(0.3, 0.3, 0.35), corner: int = 6) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.1, 0.14, 0.92)
	style.corner_radius_top_left = corner
	style.corner_radius_top_right = corner
	style.corner_radius_bottom_left = corner
	style.corner_radius_bottom_right = corner
	style.border_width_bottom = 2
	style.border_color = accent_color
	style.content_margin_left = 8
	style.content_margin_right = 8
	style.content_margin_top = 6
	style.content_margin_bottom = 6
	return style


## Create a panel tinted by element color.
static func make_element_panel(element: int) -> StyleBoxFlat:
	var ec := element_color(element)
	var style := make_panel(ec)
	style.bg_color = Color(ec.r * 0.12, ec.g * 0.12, ec.b * 0.12, 0.92)
	return style


## Create a button StyleBoxFlat colored by element.
static func make_button_style(element: int) -> StyleBoxFlat:
	var ec := element_color(element)
	var style := StyleBoxFlat.new()
	style.bg_color = Color(ec.r * 0.25, ec.g * 0.25, ec.b * 0.25, 0.9)
	style.corner_radius_top_left = 6
	style.corner_radius_top_right = 6
	style.corner_radius_bottom_left = 6
	style.corner_radius_bottom_right = 6
	style.border_width_bottom = 2
	style.border_color = ec
	style.content_margin_left = 10
	style.content_margin_right = 10
	style.content_margin_top = 6
	style.content_margin_bottom = 6
	return style


## Create a rarity-colored border style for cards.
static func make_rarity_border(rarity: int) -> StyleBoxFlat:
	var rc := rarity_color(rarity)
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.12, 0.12, 0.16, 0.9)
	style.corner_radius_top_left = 6
	style.corner_radius_top_right = 6
	style.corner_radius_bottom_left = 6
	style.corner_radius_bottom_right = 6
	style.border_width_bottom = 3
	style.border_color = rc
	if rarity >= 5:
		style.border_width_top = 2
		style.border_width_left = 1
		style.border_width_right = 1
	elif rarity >= 4:
		style.border_width_top = 1
	style.content_margin_left = 4
	style.content_margin_right = 4
	style.content_margin_top = 4
	style.content_margin_bottom = 4
	return style


## Status icon strings for gem overlays.
static func gem_status_icon(status: int) -> String:
	# MFBoardTypes.GemStatus values
	match status:
		1: return "\u2744"  # ❄ Frozen
		2: return "\U0001f512"  # 🔒 Locked (fallback: use text)
		3: return "\u2620"  # ☠ Poison
		_: return ""
