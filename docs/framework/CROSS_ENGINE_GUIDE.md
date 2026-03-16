# Cross-Engine Guide: GDScript / C# Translation Reference

This document maps every major pattern between the Godot (GDScript) and Unity (C#) implementations of MobileForge. Use it when porting logic or reviewing cross-engine parity.

## Type Mapping

| GDScript              | C#                              | Notes                                    |
|-----------------------|---------------------------------|------------------------------------------|
| `StringName`          | `string`                        | C# strings are interned when needed      |
| `String`              | `string`                        |                                          |
| `int`                 | `int` / `long`                  | GDScript int is 64-bit; use `long` if range matters |
| `float`               | `float` / `double`              | GDScript float is 64-bit; use `double` for parity  |
| `bool`                | `bool`                          |                                          |
| `Dictionary`          | `Dictionary<string, object>`    | Untyped in GDScript, string-keyed in C#  |
| `Array`               | `List<object>`                  | Or typed `List<T>` when element type is known |
| `Variant`             | `object`                        | Box/unbox as needed                      |
| `RefCounted`          | plain class                     | No special base class needed in C#       |
| `Node`                | `MonoBehaviour`                 | Only in presentation layer               |
| `Resource`            | `ScriptableObject`              | Only for engine-specific assets          |
| `PackedScene`         | `GameObject` (prefab)           |                                          |
| `null`                | `null`                          | See null safety section below            |

## Signal / Event Pattern

MobileForge uses EventBus instead of engine-native signals. The API is symmetric.

### Godot (GDScript)

```gdscript
# Subscribe
EventBus.subscribe("item_acquired", _on_item_acquired)

# Unsubscribe
EventBus.unsubscribe("item_acquired", _on_item_acquired)

# Emit
EventBus.emit("item_acquired", {
    "item_id": "sword_01",
    "quantity": 1
})

# Handler
func _on_item_acquired(event: Dictionary):
    var item_id = event["item_id"]
```

### Unity (C#)

```csharp
// Subscribe
EventBus.Instance.Subscribe("item_acquired", OnItemAcquired);

// Unsubscribe
EventBus.Instance.Unsubscribe("item_acquired", OnItemAcquired);

// Emit
EventBus.Instance.Emit("item_acquired", new Dictionary<string, object> {
    { "item_id", "sword_01" },
    { "quantity", 1 }
});

// Handler
void OnItemAcquired(Dictionary<string, object> evt)
{
    string itemId = (string)evt["item_id"];
}
```

### Key Differences

| Aspect     | GDScript                      | C#                                       |
|------------|-------------------------------|------------------------------------------|
| Callable   | `Callable(self, "_method")`   | `Action<Dictionary<string, object>>`     |
| Lifetime   | Subscribe in `_ready()`       | Subscribe in `Start()` or `OnEnable()`   |
| Cleanup    | Unsubscribe in `_exit_tree()` | Unsubscribe in `OnDestroy()` or `OnDisable()` |

## Autoload vs Singleton

### Godot

Autoloads are registered by the plugin. Access them globally by name:

```gdscript
EventBus.emit("some_event", {})
GameData.get_def("item", "sword_01")
PlayerState.get_value("currencies", "gold", 0)
```

### Unity

Singletons expose a static `Instance` property:

```csharp
EventBus.Instance.Emit("some_event", new Dictionary<string, object>());
GameData.Instance.GetDef("item", "sword_01");
PlayerState.Instance.GetValue<int>("currencies", "gold", 0);
```

Singletons are initialized by `MobileForgeBootstrap` or via `[RuntimeInitializeOnLoadMethod]`.

## File I/O

| Operation        | GDScript                                      | C#                                              |
|------------------|-----------------------------------------------|-------------------------------------------------|
| Read text file   | `FileAccess.get_file_as_string(path)`         | `File.ReadAllText(path)`                        |
| Write text file  | `var f = FileAccess.open(path, FileAccess.WRITE); f.store_string(data)` | `File.WriteAllText(path, data)` |
| Check exists     | `FileAccess.file_exists(path)`                | `File.Exists(path)`                             |
| User data dir    | `OS.get_user_data_dir()`                      | `Application.persistentDataPath`                |
| Res path prefix  | `res://`                                      | `Resources.Load<T>()` or streaming assets path  |

## JSON Handling

### Godot

```gdscript
# Parse
var data = JSON.parse_string(json_string)

# Stringify
var json_string = JSON.stringify(data, "\t")
```

### Unity

```csharp
// Using Newtonsoft.Json (recommended for Dictionary support)
var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);

// Using built-in JsonUtility (typed objects only, no Dictionary)
var obj = JsonUtility.FromJson<MyClass>(jsonString);
string jsonString = JsonUtility.ToJson(obj, true);
```

Use Newtonsoft for MobileForge internals — `JsonUtility` cannot handle `Dictionary` or untyped arrays.

## Null Safety

| Scenario                    | GDScript                        | C#                                    |
|-----------------------------|---------------------------------|---------------------------------------|
| Variable can be null        | Just use it (dynamic typing)    | Declare as nullable: `int?`, `string?` |
| Null check                  | `if value == null:`             | `if (value == null)` or `value is null` |
| Null coalesce               | Not available — use ternary     | `value ?? defaultValue`               |
| Dictionary missing key      | `dict.get("key", default)`      | `dict.TryGetValue("key", out var v)`  |
| Safe navigation             | Not available                   | `obj?.Method()`                       |

GDScript pitfall: `0`, `""`, and empty collections are falsy. In C#, only `null` and `false` are falsy. Always use explicit null checks for cross-engine logic.

## Naming Conventions

| Element          | GDScript (snake_case)       | C# (PascalCase)               |
|------------------|-----------------------------|--------------------------------|
| Class name       | `class_name CurrencyService`| `public class CurrencyService` |
| Method           | `add_currency()`            | `AddCurrency()`                |
| Variable/field   | `var max_stack_size`        | `private int _maxStackSize`    |
| Property         | N/A (use getter)            | `public int MaxStackSize { get; }` |
| Constant         | `const MAX_LEVEL = 100`     | `public const int MaxLevel = 100`  |
| Signal/event     | `"item_acquired"`           | `"item_acquired"` (keep snake_case for event names) |
| Enum value       | `ItemType.WEAPON`           | `ItemType.Weapon`              |

Event name strings are always `snake_case` in both engines to keep JSON schemas and test vectors identical.

## class_name vs namespace

### Godot

```gdscript
class_name CurrencyService

# Accessible globally after this declaration
```

### Unity

```csharp
namespace MobileForge.Domain
{
    public class CurrencyService
    {
        // Access via using MobileForge.Domain;
    }
}
```

MobileForge uses these namespaces in Unity:

| Namespace                    | Layer          |
|------------------------------|----------------|
| `MobileForge.Infrastructure` | Infrastructure |
| `MobileForge.Domain`         | Domain         |
| `MobileForge.Presentation`   | Presentation   |

## Resource Loading

### Godot

```gdscript
# Compile-time (cached, type-safe)
const ItemScene = preload("res://scenes/item.tscn")

# Runtime
var texture = load("res://textures/icon.png")

# Instantiate scene
var node = ItemScene.instantiate()
```

### Unity

```csharp
// From Resources/ folder (runtime)
var prefab = Resources.Load<GameObject>("Prefabs/Item");

// Addressables (recommended for production)
var handle = Addressables.LoadAssetAsync<GameObject>("item_prefab");

// Instantiate prefab
var go = Instantiate(prefab);
```

### Key Differences

| Aspect         | Godot                              | Unity                                 |
|----------------|------------------------------------|---------------------------------------|
| Compile-time   | `preload()` — resolved at parse    | No equivalent; use const paths        |
| Runtime        | `load()` — blocking                | `Resources.Load()` — blocking         |
| Async          | `ResourceLoader.load_threaded_*()` | `Addressables.LoadAssetAsync()`       |
| Path format    | `res://folder/file.ext`            | `"folder/file"` (no extension for Resources) |

## Common Gotchas

1. **GDScript `float` is 64-bit, C# `float` is 32-bit.** Use `double` in C# when precision matters (currency calculations, large numbers).

2. **Dictionary key ordering.** GDScript dictionaries preserve insertion order. C# `Dictionary<K,V>` does too in practice, but don't rely on it. Use `SortedDictionary` or sort keys explicitly for deterministic serialization.

3. **Array/List indexing.** Both are zero-indexed. But GDScript supports negative indices (`arr[-1]` = last element). C# does not — use `list[^1]` (range operator) or `list[list.Count - 1]`.

4. **String comparison.** GDScript `==` compares by value. C# `==` also compares by value for strings. No gotcha here, but be careful with `object` typed variables — cast to `string` first.

5. **Enum serialization.** GDScript enums are integers. C# enums are integers by default but can be serialized as strings via `[JsonConverter(typeof(StringEnumConverter))]`. MobileForge JSON uses string enum names.

6. **Lifecycle timing.** Godot `_ready()` fires when the node enters the tree. Unity `Start()` fires on the first frame after the object is active. `Awake()` is closer to `_ready()` for timing purposes, but MobileForge convention is `Start()` for subscriptions.

7. **Export vs SerializeField.** Godot `@export var hp: int = 100` maps to Unity `[SerializeField] private int _hp = 100`. Both expose the field in the inspector.

8. **Coroutines.** Godot uses `await` with signals. Unity uses `StartCoroutine` with `IEnumerator` or `async/await` with UniTask. Domain layer code must not use either — pass callbacks or use EventBus.
