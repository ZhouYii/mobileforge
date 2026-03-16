# Getting Started with MobileForge

## Prerequisites

| Engine   | Version        | Notes                                |
|----------|----------------|--------------------------------------|
| Godot    | 4.x (4.2+)    | GDScript. C# build not required.     |
| Unity    | 2022.3+ LTS   | .NET Standard 2.1 or .NET Framework. |

Both engines need a JSON editor for definition files. VS Code with the JSON Schema extension is recommended.

## Godot Setup

### 1. Install the addon

Copy the `addons/mobileforge/` folder into your project root:

```
your_project/
├── addons/
│   └── mobileforge/
│       ├── plugin.cfg
│       ├── infrastructure/
│       ├── domain/
│       └── presentation/
├── data/          ← your game definition JSONs
└── project.godot
```

### 2. Enable the plugin

Go to **Project > Project Settings > Plugins** and enable **MobileForge**.

This registers all autoloads automatically:

| Autoload Name  | Script                                           |
|----------------|--------------------------------------------------|
| EventBus       | `addons/mobileforge/infrastructure/event_bus.gd` |
| GameData       | `addons/mobileforge/infrastructure/game_data.gd` |
| PlayerState    | `addons/mobileforge/infrastructure/player_state.gd` |
| SaveManager    | `addons/mobileforge/infrastructure/save_manager.gd` |
| NetworkClient  | `addons/mobileforge/infrastructure/network_client.gd` |
| AudioManager   | `addons/mobileforge/infrastructure/audio_manager.gd` |

### 3. Verify

Run the project. Check **Output** for:

```
[MobileForge] EventBus ready
[MobileForge] GameData ready
[MobileForge] PlayerState ready
[MobileForge] SaveManager ready
[MobileForge] NetworkClient ready
[MobileForge] AudioManager ready
```

## Unity Setup

### 1. Add the package

Option A — local folder reference in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.mobileforge.core": "file:../../mobileforge-unity"
  }
}
```

Option B — copy the `MobileForge/` folder into `Assets/Plugins/MobileForge/`.

### 2. Assembly references

MobileForge ships two assemblies:

| Assembly                       | Contains                        |
|--------------------------------|---------------------------------|
| `MobileForge.Infrastructure`   | EventBus, GameData, PlayerState, etc. |
| `MobileForge.Domain`           | Services, effect system         |

Add `MobileForge.Infrastructure` and `MobileForge.Domain` to your assembly definition's references.

### 3. Bootstrap

Add the `MobileForgeBootstrap` MonoBehaviour to a GameObject in your first scene. It initializes all singletons in the correct order.

Alternatively, use `[RuntimeInitializeOnLoadMethod]` — the bootstrap handles this if you prefer code-only setup.

## Quick Start

This example loads item definitions, subscribes to a state change event, and reads player gold.

### Godot (GDScript)

```gdscript
extends Node

func _ready():
    # Load definitions
    GameData.load_file("res://data/items.json", "item")

    # Subscribe to state changes
    EventBus.subscribe("state_changed", _on_state_changed)

    # Read player currency
    var gold = PlayerState.get_value("currencies", "gold", 0)
    print("Player has %d gold" % gold)

    # Set a value (triggers state_changed event)
    PlayerState.set_value("currencies", "gold", gold + 100)

func _on_state_changed(event: Dictionary):
    print("State changed: section=%s key=%s" % [event["section"], event["key"]])
```

### Unity (C#)

```csharp
using MobileForge.Infrastructure;
using UnityEngine;

public class QuickStartExample : MonoBehaviour
{
    void Start()
    {
        // Load definitions
        GameData.Instance.LoadFile("items", "item");

        // Subscribe to state changes
        EventBus.Instance.Subscribe("state_changed", OnStateChanged);

        // Read player currency
        int gold = PlayerState.Instance.GetValue<int>("currencies", "gold", 0);
        Debug.Log($"Player has {gold} gold");

        // Set a value (triggers state_changed event)
        PlayerState.Instance.SetValue("currencies", "gold", gold + 100);
    }

    void OnDestroy()
    {
        EventBus.Instance.Unsubscribe("state_changed", OnStateChanged);
    }

    void OnStateChanged(Dictionary<string, object> evt)
    {
        Debug.Log($"State changed: section={evt["section"]} key={evt["key"]}");
    }
}
```

## Running Tests

### Godot

Run all MobileForge tests from the command line:

```bash
godot --headless --script addons/mobileforge/tests/run_tests.gd
```

Or use the GUT (Godot Unit Test) plugin if installed — tests are in `addons/mobileforge/tests/`.

### Unity

Open **Window > General > Test Runner**. MobileForge tests appear under the `MobileForge.Tests` assembly. Run all with one click.

From command line:

```bash
unity -runTests -testPlatform EditMode -testFilter MobileForge
```

## Directory Structure

```
mobileforge/
├── docs/
│   └── framework/
│       ├── README.md              ← you are here (architecture overview)
│       ├── GETTING_STARTED.md     ← this file
│       ├── CROSS_ENGINE_GUIDE.md
│       ├── infrastructure/
│       │   ├── README.md
│       │   ├── event_bus.md
│       │   ├── game_data.md
│       │   └── player_state.md
│       ├── domain/
│       └── presentation/
├── godot/
│   └── addons/mobileforge/
│       ├── infrastructure/
│       ├── domain/
│       └── tests/
├── unity/
│   └── MobileForge/
│       ├── Infrastructure/
│       ├── Domain/
│       └── Tests/
└── shared/
    ├── schemas/               ← JSON schemas for definition files
    └── test_vectors/          ← shared input/output test data
```

## Next Steps

- Read [CROSS_ENGINE_GUIDE.md](CROSS_ENGINE_GUIDE.md) for the full GDScript-to-C# translation reference.
- See [infrastructure/README.md](infrastructure/README.md) for details on each infrastructure module.
- Check [README.md](README.md) for the architecture overview and hard rules.
