# AudioManager

## Purpose

AudioManager handles all sound playback in MobileForge: sound effects (SFX), background music (BGM), and ambient sounds. It provides channel-based playback with volume control, audio pooling for frequently played SFX, and preloading for latency-sensitive sounds.

## Design Rationale

- **Channel separation.** SFX and BGM run on independent channels with separate volume controls. The player can mute music while keeping sound effects.
- **Audio pooling.** Frequently played SFX (like gem match sounds) are pooled. Instead of loading and creating a new audio player each time, a pool of pre-created players is reused.
- **Preloading.** Critical audio (UI taps, match sounds) is loaded at startup. No first-play delay.
- **Event-driven.** AudioManager subscribes to EventBus events to play sounds reactively. Domain events like `match_detected` or `level_up` trigger appropriate sounds without the domain layer knowing about audio.
- **Definition-driven.** Audio assignments (which sound plays for which event) are defined in GameData, not hard-coded.

## API Reference

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `play_sfx(sfx_id: String, volume: float)` | sound ID, volume override (-1 for default) | `void` | Play a sound effect |
| `play_bgm(bgm_id: String, fade_duration: float)` | music ID, crossfade seconds | `void` | Play background music with crossfade |
| `stop_bgm(fade_duration: float)` | fade-out seconds | `void` | Stop current BGM |
| `pause_bgm()` | none | `void` | Pause current BGM |
| `resume_bgm()` | none | `void` | Resume paused BGM |
| `set_sfx_volume(volume: float)` | 0.0 to 1.0 | `void` | Set SFX channel volume |
| `set_bgm_volume(volume: float)` | 0.0 to 1.0 | `void` | Set BGM channel volume |
| `set_master_volume(volume: float)` | 0.0 to 1.0 | `void` | Set master volume (multiplies all channels) |
| `preload_sfx(sfx_ids: Array[String])` | array of sound IDs | `void` | Preload sounds into memory |
| `preload_bgm(bgm_id: String)` | music ID | `void` | Preload a BGM track |
| `is_bgm_playing()` | none | `bool` | Check if BGM is currently playing |
| `current_bgm_id()` | none | `String` | ID of the currently playing BGM |

### Unity (C#)

| Method / Property | Params | Return | Description |
|---|---|---|---|
| `PlaySFX(string sfxId, float volume)` | sound ID, volume | `void` | Play sound effect |
| `PlayBGM(string bgmId, float fadeDuration)` | music ID, crossfade | `void` | Play BGM |
| `StopBGM(float fadeDuration)` | fade-out seconds | `void` | Stop BGM |
| `PauseBGM()` | none | `void` | Pause BGM |
| `ResumeBGM()` | none | `void` | Resume BGM |
| `SFXVolume` | -- | `float` | SFX volume (0-1) |
| `BGMVolume` | -- | `float` | BGM volume (0-1) |
| `MasterVolume` | -- | `float` | Master volume (0-1) |
| `PreloadSFX(string[] sfxIds)` | sound IDs | `void` | Preload sounds |
| `PreloadBGM(string bgmId)` | music ID | `void` | Preload BGM |
| `IsBGMPlaying` | -- | `bool` | Check BGM state |
| `CurrentBGMId` | -- | `string` | Current BGM ID |

## Audio Channels

```
┌─────────────────────────────────────────┐
│              Master Volume               │
│                 (0-1)                     │
├──────────────────┬──────────────────────┤
│   SFX Channel    │    BGM Channel       │
│    (0-1)         │     (0-1)            │
│                  │                      │
│  ┌─── Pool ───┐  │  ┌── Single ──┐     │
│  │ Player 1   │  │  │ Player     │     │
│  │ Player 2   │  │  │ (crossfade │     │
│  │ Player 3   │  │  │  between   │     │
│  │ ...        │  │  │  tracks)   │     │
│  │ Player N   │  │  └────────────┘     │
│  └────────────┘  │                      │
└──────────────────┴──────────────────────┘

Effective volume = master_volume * channel_volume * per_sound_volume
```

## SFX Pooling

The SFX pool pre-creates a fixed number of audio players. When `play_sfx()` is called:

1. Find an idle player in the pool.
2. Assign the audio clip.
3. Set volume to `master * sfx_channel * per_sound`.
4. Play.
5. When playback finishes, the player returns to idle state.

If all players are busy, the oldest playing sound is stopped and reused (steal-oldest policy).

```gdscript
# Godot -- pool configuration
AudioManager.sfx_pool_size = 8  # max simultaneous SFX
```

```csharp
// Unity
AudioManager.SFXPoolSize = 8;
```

## BGM Crossfade

When switching BGM tracks, AudioManager crossfades between the old and new tracks:

```
Old BGM:  ████████████▓▓▓▒▒░░        (fading out)
New BGM:           ░░▒▒▓▓████████████ (fading in)
          |--------|
          fade_duration
```

```gdscript
# Godot
AudioManager.play_bgm("battle_theme", 1.5)  # 1.5 second crossfade
```

If the same BGM ID is requested while it is already playing, the call is ignored (no restart).

## Audio Definitions in GameData

Audio mappings are defined in a JSON file loaded by GameData:

```json
{
    "sfx": {
        "gem_match": { "path": "sfx/gem_match.ogg", "volume": 0.8 },
        "gem_move": { "path": "sfx/gem_move.ogg", "volume": 0.6 },
        "button_tap": { "path": "sfx/button_tap.ogg", "volume": 0.7 },
        "level_up": { "path": "sfx/level_up.ogg", "volume": 1.0 },
        "cascade_combo": { "path": "sfx/cascade_%d.ogg", "volume": 0.9 }
    },
    "bgm": {
        "main_menu": { "path": "bgm/main_menu.ogg", "loop": true },
        "battle_theme": { "path": "bgm/battle.ogg", "loop": true },
        "boss_theme": { "path": "bgm/boss.ogg", "loop": true },
        "victory": { "path": "bgm/victory.ogg", "loop": false }
    }
}
```

## Event-Driven Sound Playback

AudioManager subscribes to domain events and plays sounds automatically:

```gdscript
# Godot -- internal AudioManager setup
func _setup_event_listeners() -> void:
    EventBus.subscribe("match_detected", func(e: Dictionary):
        play_sfx("gem_match")
    )
    EventBus.subscribe("cascade_step", func(e: Dictionary):
        var combo: int = e.get("combo_index", 0)
        play_sfx("cascade_combo_%d" % min(combo, 7))
    )
    EventBus.subscribe("level_up", func(_e: Dictionary):
        play_sfx("level_up")
    )
    EventBus.subscribe("screen_entered", func(e: Dictionary):
        var screen_id: String = e.get("screen_id", "")
        var bgm: String = _screen_bgm.get(screen_id, "")
        if bgm != "" and bgm != current_bgm_id():
            play_bgm(bgm, 1.0)
    )
```

## Preloading

Preload audio during loading screens to eliminate first-play latency:

```gdscript
# Godot
func _on_loading_started() -> void:
    AudioManager.preload_sfx(["gem_match", "gem_move", "button_tap"])
    AudioManager.preload_bgm("battle_theme")
```

```csharp
// Unity
void OnLoadingStarted()
{
    AudioManager.PreloadSFX(new[] { "gem_match", "gem_move", "button_tap" });
    AudioManager.PreloadBGM("battle_theme");
}
```

## Volume Persistence

Volume settings are stored in PlayerState and restored on startup:

```gdscript
# Godot
func _restore_audio_settings() -> void:
    var sfx_vol = PlayerState.get_value("settings", "sfx_volume", 1.0)
    var bgm_vol = PlayerState.get_value("settings", "bgm_volume", 1.0)
    AudioManager.set_sfx_volume(sfx_vol)
    AudioManager.set_bgm_volume(bgm_vol)
```

## Events Emitted

| Event | Payload | When |
|---|---|---|
| `bgm_changed` | `{ "bgm_id": String, "previous_id": String }` | BGM track changes |
| `bgm_stopped` | `{}` | BGM stops playing |
| `audio_error` | `{ "sfx_id": String, "error": String }` | Failed to load or play audio |

## Best Practices

1. **Preload all battle SFX before entering combat.** Loading audio during gameplay causes frame drops.
2. **Use crossfade for BGM transitions.** Abrupt cuts sound jarring.
3. **Keep SFX pool size reasonable.** 8-12 simultaneous sounds is sufficient for most mobile games.
4. **Mute audio on app background.** On mobile, stop or pause all audio when the app loses focus.
5. **Use compressed formats.** OGG Vorbis for Godot, MP3/AAC for Unity. Uncompressed WAV wastes memory.
6. **Test with audio disabled.** Ensure no errors occur when the device is muted or when audio playback fails.
