@tool
extends EditorPlugin
## MobileForge editor plugin.
## Registers infrastructure autoloads on enable, removes on disable.

const AUTOLOADS := {
	"MFEventBus": "res://addons/mobileforge/infrastructure/event_bus/event_bus.gd",
	"MFGameData": "res://addons/mobileforge/infrastructure/game_data/game_data.gd",
	"MFPlayerState": "res://addons/mobileforge/infrastructure/player_state/player_state.gd",
	"MFSaveManager": "res://addons/mobileforge/infrastructure/save_manager/save_manager.gd",
	"MFAudioManager": "res://addons/mobileforge/infrastructure/audio_manager/audio_manager.gd",
}


func _enter_tree() -> void:
	for autoload_name in AUTOLOADS:
		if not ProjectSettings.has_setting("autoload/" + autoload_name):
			add_autoload_singleton(autoload_name, AUTOLOADS[autoload_name])


func _exit_tree() -> void:
	for autoload_name in AUTOLOADS:
		if ProjectSettings.has_setting("autoload/" + autoload_name):
			remove_autoload_singleton(autoload_name)
