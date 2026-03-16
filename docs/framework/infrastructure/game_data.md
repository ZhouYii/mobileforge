# GameData

## Purpose

GameData is the read-only definition storage for the game. It loads JSON files at startup, parses them into `Definition` wrapper objects, and provides O(1) lookup by type and id. Once loaded, definitions are immutable — they represent the game's static configuration (items, enemies, levels, shop products, etc.).

## Loading Flow

```
JSON file on disk
       │
       ▼
  Read file contents (FileAccess / System.IO)
       │
       ▼
  Parse JSON string → Array of Dictionary objects
       │
       ▼
  Wrap each Dictionary in a Definition object
       │
       ▼
  Index by type + id in internal lookup table
       │
       ▼
  Emit "data_loaded" event via EventBus
```

Each JSON file represents one definition type (e.g., `items.json` contains all item definitions). Every object in the array must have an `"id"` field.

## API Reference

### Godot (GDScript)

| Method | Params | Return | Description |
|--------|--------|--------|-------------|
| `load_file(path: String, type: StringName)` | file path, definition type name | `void` | Load a JSON file and register all definitions under the given type |
| `load_from_string(json: String, type: StringName)` | JSON string, type name | `void` | Parse a JSON string and register definitions |
| `get_def(type: StringName, id: StringName)` | type, definition id | `Definition` or `null` | Look up a single definition |
| `get_all(type: StringName)` | type | `Array[Definition]` | Get all definitions of a type |
| `get_ids(type: StringName)` | type | `Array[StringName]` | Get all ids registered under a type |
| `has_def(type: StringName, id: StringName)` | type, id | `bool` | Check if a definition exists |
| `get_type_count(type: StringName)` | type | `int` | Number of definitions of this type |
| `clear(type: StringName)` | type | `void` | Remove all definitions of a type |
| `clear_all()` | none | `void` | Remove all definitions |

### Unity (C#)

| Method | Params | Return | Description |
|--------|--------|--------|-------------|
| `LoadFile(string path, string type)` | file path, type name | `void` | Load JSON file, register definitions |
| `LoadFromString(string json, string type)` | JSON string, type | `void` | Parse JSON string, register definitions |
| `GetDef(string type, string id)` | type, id | `Definition` or `null` | Look up a definition |
| `GetAll(string type)` | type | `List<Definition>` | All definitions of a type |
| `GetIds(string type)` | type | `List<string>` | All ids for a type |
| `HasDef(string type, string id)` | type, id | `bool` | Check existence |
| `GetTypeCount(string type)` | type | `int` | Count of definitions |
| `Clear(string type)` | type | `void` | Remove definitions for type |
| `ClearAll()` | none | `void` | Remove everything |

## Definition Class API

A `Definition` wraps a parsed JSON dictionary and provides typed accessors with default values.

### Godot (GDScript)

| Method | Params | Return | Description |
|--------|--------|--------|-------------|
| `get_id()` | none | `StringName` | The definition's unique id |
| `get_int(key: StringName, default: int = 0)` | key, default | `int` | Read an integer field |
| `get_float(key: StringName, default: float = 0.0)` | key, default | `float` | Read a float field |
| `get_string(key: StringName, default: String = "")` | key, default | `String` | Read a string field |
| `get_bool(key: StringName, default: bool = false)` | key, default | `bool` | Read a boolean field |
| `get_array(key: StringName, default: Array = [])` | key, default | `Array` | Read an array field |
| `get_dict(key: StringName, default: Dictionary = {})` | key, default | `Dictionary` | Read a nested dictionary |
| `has(key: StringName)` | key | `bool` | Check if field exists |
| `get_raw()` | none | `Dictionary` | Get the underlying dictionary |

### Unity (C#)

| Method | Params | Return | Description |
|--------|--------|--------|-------------|
| `Id` | (property) | `string` | The definition's unique id |
| `GetInt(string key, int defaultValue = 0)` | key, default | `int` | Read integer |
| `GetFloat(string key, float defaultValue = 0f)` | key, default | `float` | Read float |
| `GetString(string key, string defaultValue = "")` | key, default | `string` | Read string |
| `GetBool(string key, bool defaultValue = false)` | key, default | `bool` | Read boolean |
| `GetArray(string key)` | key | `List<object>` | Read array (empty list if missing) |
| `GetDict(string key)` | key | `Dictionary<string, object>` | Read nested dict (empty if missing) |
| `Has(string key)` | key | `bool` | Check field exists |
| `Raw` | (property) | `Dictionary<string, object>` | Underlying dictionary |

## Usage Examples

### Loading Definitions

```gdscript
# Godot — typically in a loading screen or _ready()
func _ready():
    GameData.load_file("res://data/items.json", "item")
    GameData.load_file("res://data/enemies.json", "enemy")
    GameData.load_file("res://data/shop.json", "shop_product")
```

```csharp
// Unity
void Start()
{
    GameData.Instance.LoadFile("items", "item");
    GameData.Instance.LoadFile("enemies", "enemy");
    GameData.Instance.LoadFile("shop", "shop_product");
}
```

### Looking Up Definitions

```gdscript
# Godot
var sword = GameData.get_def("item", "sword_01")
if sword:
    var name = sword.get_string("name")
    var damage = sword.get_int("damage")
    var tags = sword.get_array("tags")
    print("%s deals %d damage" % [name, damage])

# Get all items
var all_items = GameData.get_all("item")
for item in all_items:
    print(item.get_id())
```

```csharp
// Unity
var sword = GameData.Instance.GetDef("item", "sword_01");
if (sword != null)
{
    string name = sword.GetString("name");
    int damage = sword.GetInt("damage");
    var tags = sword.GetArray("tags");
    Debug.Log($"{name} deals {damage} damage");
}

// Get all items
var allItems = GameData.Instance.GetAll("item");
foreach (var item in allItems)
{
    Debug.Log(item.Id);
}
```

### Using Definitions in Domain Logic

```gdscript
# Godot — domain service (no engine imports)
class_name CurrencyService

static func can_afford(product_id: StringName) -> bool:
    var product = GameData.get_def("shop_product", product_id)
    if not product:
        return false
    var cost = product.get_int("cost")
    var currency = product.get_string("currency", "gold")
    var current = PlayerState.get_value("currencies", currency, 0)
    return current >= cost
```

```csharp
// Unity — domain service
namespace MobileForge.Domain
{
    public static class CurrencyService
    {
        public static bool CanAfford(string productId)
        {
            var product = GameData.Instance.GetDef("shop_product", productId);
            if (product == null) return false;
            int cost = product.GetInt("cost");
            string currency = product.GetString("currency", "gold");
            int current = PlayerState.Instance.GetValue<int>("currencies", currency, 0);
            return current >= cost;
        }
    }
}
```

## JSON Format

Definition files are JSON arrays. Each element is an object with at minimum an `"id"` field.

### Example: `items.json`

```json
[
    {
        "id": "sword_01",
        "name": "Iron Sword",
        "type": "weapon",
        "damage": 25,
        "rarity": "common",
        "tags": ["melee", "sword"],
        "sell_price": 50
    },
    {
        "id": "shield_01",
        "name": "Wooden Shield",
        "type": "armor",
        "defense": 10,
        "rarity": "common",
        "tags": ["shield", "wood"],
        "sell_price": 30
    },
    {
        "id": "potion_hp_01",
        "name": "Small Health Potion",
        "type": "consumable",
        "heal_amount": 100,
        "rarity": "common",
        "max_stack": 99,
        "sell_price": 10
    }
]
```

### Rules

- The file must be a JSON array `[...]` at the top level.
- Each object must have a unique `"id"` field (string).
- All other fields are freeform — GameData does not validate structure. Validation is the responsibility of the consuming domain service.
- Nested objects and arrays are supported.
- Use `snake_case` for all field names.

## Performance Notes

- **O(1) lookup by id.** Internally, definitions are stored in a `Dictionary<StringName, Definition>` keyed by `type:id`.
- **Loaded once at startup.** Call `load_file()` during your loading screen. Do not reload per-frame.
- **Memory.** All definitions live in memory for the lifetime of the game. For mobile, keep total JSON size under a few MB.
- **No lazy loading.** All definitions for a type are parsed when `load_file()` is called. This keeps runtime access fast and predictable.
