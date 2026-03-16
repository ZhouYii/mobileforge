# Infrastructure Layer

The infrastructure layer provides engine-specific singletons that deliver core services to the rest of the framework. These are the only modules allowed to import engine APIs (`Node`, `MonoBehaviour`, file system, audio, HTTP).

Domain and presentation layers consume infrastructure through its public API. Infrastructure never imports from domain or presentation.

## Initialization Order

Modules initialize in this fixed sequence. Each module may depend on modules initialized before it, never after.

```
┌──────────────┐
│   EventBus   │  1. No dependencies
└──────┬───────┘
       │
┌──────┴───────┐
│   GameData   │  2. Depends on EventBus
└──────┬───────┘
       │
┌──────┴───────┐
│ PlayerState  │  3. Depends on EventBus
└──────┬───────┘
       │
┌──────┴───────┐
│ SaveManager  │  4. Depends on PlayerState, EventBus
└──────┬───────┘
       │
┌──────┴────────┐
│ NetworkClient │  5. Depends on EventBus
└──────┬────────┘
       │
┌──────┴───────┐
│ AudioManager │  6. Depends on EventBus, GameData
└──────────────┘
```

## Module Summary

| Module          | Purpose                                                                 | Doc                        |
|-----------------|-------------------------------------------------------------------------|----------------------------|
| **EventBus**    | Pub/sub message broker. All cross-module communication flows through it. | [event_bus.md](event_bus.md) |
| **GameData**    | Read-only storage for game definitions loaded from JSON files.           | [game_data.md](game_data.md) |
| **PlayerState** | Mutable player data organized into named sections. Emits change events.  | [player_state.md](player_state.md) |
| **SaveManager** | Serializes PlayerState to disk and restores it on load.                  | save_manager.md            |
| **NetworkClient** | HTTP client with retry logic, auth headers, and request queuing.       | network_client.md          |
| **AudioManager** | Plays sound effects and music with pooling and volume control.          | audio_manager.md           |

## Dependency Diagram

```
EventBus ◄──────────────── GameData
    ▲                         ▲
    │                         │
    ├──── PlayerState         │
    │         ▲               │
    │         │               │
    ├──── SaveManager         │
    │                         │
    ├──── NetworkClient       │
    │                         │
    └──── AudioManager ───────┘
```

Arrows point from dependent to dependency. Every module depends on EventBus. AudioManager also depends on GameData (for audio definition lookups). SaveManager depends on PlayerState (for serialization).

## Implementation Notes

### Godot

Each module is a GDScript autoload (`Node` or `RefCounted` subclass) registered by the MobileForge plugin. They live in `addons/mobileforge/infrastructure/`.

```
addons/mobileforge/infrastructure/
├── event_bus.gd
├── game_data.gd
├── player_state.gd
├── save_manager.gd
├── network_client.gd
└── audio_manager.gd
```

### Unity

Each module is a C# singleton class in the `MobileForge.Infrastructure` namespace. `MobileForgeBootstrap` initializes them in order.

```
MobileForge/Infrastructure/
├── EventBus.cs
├── GameData.cs
├── PlayerState.cs
├── SaveManager.cs
├── NetworkClient.cs
└── AudioManager.cs
```

## Further Reading

- [event_bus.md](event_bus.md) — Full API reference, usage examples, best practices
- [game_data.md](game_data.md) — Definition loading, lookup API, JSON format
- [player_state.md](player_state.md) — Sections, value access, save/load integration
- [../README.md](../README.md) — Framework architecture overview
- [../CROSS_ENGINE_GUIDE.md](../CROSS_ENGINE_GUIDE.md) — GDScript/C# translation reference
