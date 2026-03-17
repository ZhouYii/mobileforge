extends Node
## Audio playback manager. Manages SFX and BGM channels.
## Supports fade in/out and crossfade for BGM transitions.

var _channels: Dictionary = {}  # StringName -> MFAudioChannel
var _preloaded: Dictionary = {}  # StringName -> AudioStream
var _bgm_player: AudioStreamPlayer
var _bgm_crossfade_player: AudioStreamPlayer  # Second player for crossfade
var _sfx_players: Array = []  # Pool of AudioStreamPlayers for SFX
var _event_bus: Object
var _active_bgm_tween: Tween = null
var _current_bgm_id: StringName = &""
const MAX_SFX_PLAYERS := 8
const MIN_DB := -80.0  # Silence threshold


func _ready() -> void:
	_event_bus = get_node_or_null("/root/EventBus")
	_channels[&"bgm"] = MFAudioChannel.new(&"bgm")
	_channels[&"sfx"] = MFAudioChannel.new(&"sfx")

	_bgm_player = AudioStreamPlayer.new()
	_bgm_player.bus = "Master"
	add_child(_bgm_player)

	_bgm_crossfade_player = AudioStreamPlayer.new()
	_bgm_crossfade_player.bus = "Master"
	add_child(_bgm_crossfade_player)

	for i in range(MAX_SFX_PLAYERS):
		var player = AudioStreamPlayer.new()
		player.bus = "Master"
		add_child(player)
		_sfx_players.append(player)


func preload_audio(audio_id: StringName, path: String) -> void:
	var stream = load(path)
	if stream != null:
		_preloaded[audio_id] = stream


## Play a sound effect (no fading — SFX are instant).
func play_sfx(audio_id: StringName, volume: float = 1.0) -> void:
	var stream = _preloaded.get(audio_id)
	if stream == null:
		return
	var channel: MFAudioChannel = _channels[&"sfx"]
	for player in _sfx_players:
		if not player.playing:
			player.stream = stream
			player.volume_db = linear_to_db(volume * channel.get_effective_volume())
			player.play()
			return


## Play background music with optional fade-in.
## If BGM is already playing, crossfades from old to new.
func play_bgm(audio_id: StringName, fade_duration: float = 0.5) -> void:
	var stream = _preloaded.get(audio_id)
	if stream == null:
		return

	# Skip if already playing the same track
	if audio_id == _current_bgm_id and _bgm_player.playing:
		return

	var channel: MFAudioChannel = _channels[&"bgm"]
	var target_db := linear_to_db(channel.get_effective_volume())

	# Kill any in-progress fade
	_kill_active_tween()

	if _bgm_player.playing and fade_duration > 0.0:
		# Crossfade: old track fades out on crossfade player, new fades in on main
		_bgm_crossfade_player.stream = _bgm_player.stream
		_bgm_crossfade_player.volume_db = _bgm_player.volume_db
		_bgm_crossfade_player.play(_bgm_player.get_playback_position())

		_bgm_player.stream = stream
		_bgm_player.volume_db = MIN_DB
		_bgm_player.play()

		_active_bgm_tween = create_tween()
		_active_bgm_tween.set_parallel(true)
		# Fade in new track
		_active_bgm_tween.tween_property(_bgm_player, "volume_db", target_db, fade_duration)
		# Fade out old track
		_active_bgm_tween.tween_property(_bgm_crossfade_player, "volume_db", MIN_DB, fade_duration)
		_active_bgm_tween.set_parallel(false)
		_active_bgm_tween.tween_callback(_bgm_crossfade_player.stop)
	elif fade_duration > 0.0:
		# Simple fade in from silence
		_bgm_player.stream = stream
		_bgm_player.volume_db = MIN_DB
		_bgm_player.play()

		_active_bgm_tween = create_tween()
		_active_bgm_tween.tween_property(_bgm_player, "volume_db", target_db, fade_duration)
	else:
		# Instant play
		_bgm_player.stream = stream
		_bgm_player.volume_db = target_db
		_bgm_player.play()

	_current_bgm_id = audio_id


## Stop background music with optional fade-out.
func stop_bgm(fade_duration: float = 0.5) -> void:
	_kill_active_tween()

	if fade_duration > 0.0 and _bgm_player.playing:
		_active_bgm_tween = create_tween()
		_active_bgm_tween.tween_property(_bgm_player, "volume_db", MIN_DB, fade_duration)
		_active_bgm_tween.tween_callback(_bgm_player.stop)
	else:
		_bgm_player.stop()

	_current_bgm_id = &""


## Pause BGM (preserves position). Optionally fades out first.
func pause_bgm(fade_duration: float = 0.0) -> void:
	_kill_active_tween()

	if fade_duration > 0.0 and _bgm_player.playing:
		_active_bgm_tween = create_tween()
		_active_bgm_tween.tween_property(_bgm_player, "volume_db", MIN_DB, fade_duration)
		_active_bgm_tween.tween_callback(func(): _bgm_player.stream_paused = true)
	elif _bgm_player.playing:
		_bgm_player.stream_paused = true


## Resume paused BGM. Optionally fades in.
func resume_bgm(fade_duration: float = 0.0) -> void:
	if not _bgm_player.stream_paused:
		return

	_kill_active_tween()

	var channel: MFAudioChannel = _channels[&"bgm"]
	var target_db := linear_to_db(channel.get_effective_volume())

	if fade_duration > 0.0:
		_bgm_player.volume_db = MIN_DB
		_bgm_player.stream_paused = false
		_active_bgm_tween = create_tween()
		_active_bgm_tween.tween_property(_bgm_player, "volume_db", target_db, fade_duration)
	else:
		_bgm_player.volume_db = target_db
		_bgm_player.stream_paused = false


## Get the currently playing BGM id (empty if none).
func get_current_bgm() -> StringName:
	return _current_bgm_id


func set_channel_volume(channel_name: StringName, volume: float) -> void:
	if _channels.has(channel_name):
		_channels[channel_name].volume = clampf(volume, 0.0, 1.0)
		# Live-update BGM volume if channel is bgm
		if channel_name == &"bgm" and _bgm_player.playing and _active_bgm_tween == null:
			_bgm_player.volume_db = linear_to_db(_channels[&"bgm"].get_effective_volume())


func set_channel_muted(channel_name: StringName, muted: bool) -> void:
	if _channels.has(channel_name):
		_channels[channel_name].is_muted = muted
		if channel_name == &"bgm" and _bgm_player.playing and _active_bgm_tween == null:
			_bgm_player.volume_db = linear_to_db(_channels[&"bgm"].get_effective_volume())


func _kill_active_tween() -> void:
	if _active_bgm_tween != null and _active_bgm_tween.is_valid():
		_active_bgm_tween.kill()
	_active_bgm_tween = null
