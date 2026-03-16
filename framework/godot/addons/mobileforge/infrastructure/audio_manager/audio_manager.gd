extends Node
## Audio playback manager. Manages SFX and BGM channels.

var _channels: Dictionary = {}  # StringName -> MFAudioChannel
var _preloaded: Dictionary = {}  # StringName -> AudioStream
var _bgm_player: AudioStreamPlayer
var _sfx_players: Array = []  # Pool of AudioStreamPlayers for SFX
var _event_bus: Object
const MAX_SFX_PLAYERS := 8

func _ready() -> void:
    _event_bus = get_node_or_null("/root/EventBus")
    _channels[&"bgm"] = MFAudioChannel.new(&"bgm")
    _channels[&"sfx"] = MFAudioChannel.new(&"sfx")

    _bgm_player = AudioStreamPlayer.new()
    _bgm_player.bus = "Master"
    add_child(_bgm_player)

    for i in range(MAX_SFX_PLAYERS):
        var player = AudioStreamPlayer.new()
        player.bus = "Master"
        add_child(player)
        _sfx_players.append(player)

func preload_audio(audio_id: StringName, path: String) -> void:
    var stream = load(path)
    if stream != null:
        _preloaded[audio_id] = stream

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

func play_bgm(audio_id: StringName, fade_duration: float = 0.5) -> void:
    var stream = _preloaded.get(audio_id)
    if stream == null:
        return
    var channel: MFAudioChannel = _channels[&"bgm"]
    _bgm_player.stream = stream
    _bgm_player.volume_db = linear_to_db(channel.get_effective_volume())
    _bgm_player.play()

func stop_bgm(fade_duration: float = 0.5) -> void:
    _bgm_player.stop()

func set_channel_volume(channel_name: StringName, volume: float) -> void:
    if _channels.has(channel_name):
        _channels[channel_name].volume = clampf(volume, 0.0, 1.0)

func set_channel_muted(channel_name: StringName, muted: bool) -> void:
    if _channels.has(channel_name):
        _channels[channel_name].is_muted = muted
