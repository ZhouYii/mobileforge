# MobileForge Framework Architecture

MobileForge is a reusable, cross-engine framework for building mobile games. You write game logic once against a shared domain layer, then run it on Godot 4 or Unity with thin engine-specific adapters. Shared JSON schemas and test vectors guarantee both implementations behave identically.

## 3-Layer Architecture

```
┌─────────────────────────────────────────────────┐
│                 PRESENTATION                     │
│  UI scenes/prefabs, input handling, animation    │
│  (Godot: Nodes/Scenes  |  Unity: MonoBehaviours)│
├─────────────────────────────────────────────────┤
│                    DOMAIN                        │
│  Pure game logic, rules, calculations            │
│  NO engine imports — plain GDScript / plain C#   │
├─────────────────────────────────────────────────┤
│                INFRASTRUCTURE                    │
│  Engine singletons: EventBus, GameData,          │
│  PlayerState, SaveManager, NetworkClient,        │
│  AudioManager                                    │
└─────────────────────────────────────────────────┘

         Events flow UP via EventBus
         Calls flow DOWN only
```

## Hard Rules

1. **No upward calls.** Infrastructure never imports domain. Domain never imports presentation. Only downward dependencies.
2. **Domain has zero engine imports.** No `Node`, no `MonoBehaviour`, no `Engine`, no `UnityEngine`. Pure language only.
3. **Events flow up via EventBus.** Lower layers emit events. Upper layers subscribe. No direct callbacks across layer boundaries.
4. **One responsibility per module.** If a class touches two concerns, split it.

## Module List

### Infrastructure Layer

| Module          | Purpose                                      |
|-----------------|----------------------------------------------|
| EventBus        | Pub/sub message broker for decoupled comms   |
| GameData        | Read-only definition storage (loaded from JSON) |
| PlayerState     | Mutable player data organized in sections    |
| SaveManager     | Serialize/deserialize player state to disk   |
| NetworkClient   | HTTP requests with retry and auth headers    |
| AudioManager    | Sound/music playback with pooling            |

### Domain Layer

| Module              | Purpose                                   |
|---------------------|-------------------------------------------|
| CurrencyService     | Earn, spend, check-afford logic           |
| InventoryService    | Add, remove, stack, capacity checks       |
| GachaService        | Banner definitions, pull logic, pity       |
| ProgressionService  | XP, level-up, stat scaling                |
| QuestService        | Objective tracking, completion, rewards   |
| Effect system       | Callable registry + abstract base class   |

### Presentation Layer

| Module           | Purpose                                    |
|------------------|--------------------------------------------|
| UIManager        | Screen stack, transitions, popups          |
| HUDController    | Top bar, currency display, notifications   |
| InventoryUI      | Grid/list views, item detail panels        |
| GachaUI          | Banner display, pull animations, results   |
| DialogueUI       | Text boxes, choices, portraits             |

## Cross-Engine Parity

Both Godot 4 and Unity implementations are built from the same design documents. Parity is enforced by:

- **Shared JSON schemas**: Definition files, save files, and network payloads use identical JSON structures.
- **Shared test vectors**: JSON files containing input/output pairs that both engines run against.
- **Naming alignment**: Module names, method signatures, and event names map 1:1 (with casing conventions applied per language).

See [CROSS_ENGINE_GUIDE.md](CROSS_ENGINE_GUIDE.md) for the full GDScript-to-C# translation table.

## Extension Model

### Simple Effects: Callable Registry

Register a function by name. The framework calls it by key with a payload dictionary.

```gdscript
# Godot
EffectRegistry.register("grant_gold", func(params):
    CurrencyService.add("gold", params["amount"])
)
```

```csharp
// Unity
EffectRegistry.Register("grant_gold", (params) => {
    CurrencyService.Add("gold", (int)params["amount"]);
});
```

### Complex Behaviors: Abstract Base Class

Subclass `BaseEffect` (GDScript) / `BaseEffect` (C#) for multi-step logic with setup/teardown.

```gdscript
# Godot
class_name BuffEffect extends BaseEffect

func apply(target, params: Dictionary) -> void:
    # add stat modifier
func remove(target) -> void:
    # remove stat modifier
func tick(delta: float) -> void:
    # count down duration
```

## Initialization Order

Infrastructure modules initialize in this fixed order:

```
1. EventBus          (no dependencies)
2. GameData          (depends on EventBus)
3. PlayerState       (depends on EventBus)
4. SaveManager       (depends on PlayerState, EventBus)
5. NetworkClient     (depends on EventBus)
6. AudioManager      (depends on EventBus, GameData)
```

Domain services initialize after all infrastructure is ready. They subscribe to events in their own `initialize()` method.

## Further Reading

- [GETTING_STARTED.md](GETTING_STARTED.md) — Setup guide for both engines
- [CROSS_ENGINE_GUIDE.md](CROSS_ENGINE_GUIDE.md) — GDScript / C# translation reference
- [infrastructure/README.md](infrastructure/README.md) — Infrastructure layer deep dive
- [domain/README.md](domain/README.md) — Domain layer deep dive
- [presentation/README.md](presentation/README.md) — Presentation layer deep dive
