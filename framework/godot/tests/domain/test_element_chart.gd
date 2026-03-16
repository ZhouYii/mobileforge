extends MFTestBase
## Tests for ElementChart (domain/combat/element_chart.gd)
## Uses element_advantage_cases.json test vectors for validation.

const ElementChartScript = preload("res://addons/mobileforge/domain/combat/element_chart.gd")
const BoardTypesScript = preload("res://addons/mobileforge/domain/board/board_types.gd")

var _chart: MFElementChart


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_chart = MFElementChart.new()


# ---------------------------------------------------------------------------
# Tests — element_advantage_cases test vectors
# ---------------------------------------------------------------------------

func test_water_beats_fire() -> void:
	var mult := _chart.get_multiplier(MFBoardTypes.Element.WATER, MFBoardTypes.Element.FIRE)
	assert_eq(mult, 1.5, "water vs fire should be 1.5x")


func test_fire_beats_grass() -> void:
	var mult := _chart.get_multiplier(MFBoardTypes.Element.FIRE, MFBoardTypes.Element.GRASS)
	assert_eq(mult, 1.5, "fire vs grass should be 1.5x")


func test_grass_beats_water() -> void:
	var mult := _chart.get_multiplier(MFBoardTypes.Element.GRASS, MFBoardTypes.Element.WATER)
	assert_eq(mult, 1.5, "grass vs water should be 1.5x")


func test_light_dark_mutual() -> void:
	var light_vs_dark := _chart.get_multiplier(MFBoardTypes.Element.LIGHT, MFBoardTypes.Element.DARK)
	assert_eq(light_vs_dark, 1.5, "light vs dark should be 1.5x")
	var dark_vs_light := _chart.get_multiplier(MFBoardTypes.Element.DARK, MFBoardTypes.Element.LIGHT)
	assert_eq(dark_vs_light, 1.5, "dark vs light should be 1.5x")


func test_water_weak_to_grass() -> void:
	var mult := _chart.get_multiplier(MFBoardTypes.Element.WATER, MFBoardTypes.Element.GRASS)
	assert_eq(mult, 0.5, "water vs grass should be 0.5x")


func test_same_element_neutral() -> void:
	var mult := _chart.get_multiplier(MFBoardTypes.Element.WATER, MFBoardTypes.Element.WATER)
	assert_eq(mult, 1.0, "same element should be 1.0x (neutral)")
	var mult2 := _chart.get_multiplier(MFBoardTypes.Element.FIRE, MFBoardTypes.Element.FIRE)
	assert_eq(mult2, 1.0, "same element should be 1.0x (neutral)")


func test_heart_neutral() -> void:
	# Heart vs everything should be neutral
	assert_eq(_chart.get_multiplier(MFBoardTypes.Element.HEART, MFBoardTypes.Element.WATER), 1.0,
		"heart vs water should be 1.0x")
	assert_eq(_chart.get_multiplier(MFBoardTypes.Element.HEART, MFBoardTypes.Element.FIRE), 1.0,
		"heart vs fire should be 1.0x")
	assert_eq(_chart.get_multiplier(MFBoardTypes.Element.HEART, MFBoardTypes.Element.GRASS), 1.0,
		"heart vs grass should be 1.0x")
	assert_eq(_chart.get_multiplier(MFBoardTypes.Element.HEART, MFBoardTypes.Element.LIGHT), 1.0,
		"heart vs light should be 1.0x")
	assert_eq(_chart.get_multiplier(MFBoardTypes.Element.HEART, MFBoardTypes.Element.DARK), 1.0,
		"heart vs dark should be 1.0x")
	# Anything vs heart should also be neutral
	assert_eq(_chart.get_multiplier(MFBoardTypes.Element.WATER, MFBoardTypes.Element.HEART), 1.0,
		"water vs heart should be 1.0x")
	assert_eq(_chart.get_multiplier(MFBoardTypes.Element.FIRE, MFBoardTypes.Element.HEART), 1.0,
		"fire vs heart should be 1.0x")
