# SaveManager

## Purpose

SaveManager serializes PlayerState to disk and restores it on load. It provides atomic writes (write to temp file, then rename) to prevent data corruption on crash or power loss. Save files are wrapped in an envelope with versioning and checksum validation. A migration chain upgrades old save formats to the current version automatically.

## Design Rationale

- **Atomic writes.** Save data is written to a temporary file first, then atomically renamed to the target path. If the game crashes mid-write, the previous save remains intact.
- **Envelope format.** Every save file is a JSON object containing `version`, `timestamp`, `checksum`, and `data`. This enables integrity validation and format migration.
- **Checksum validation.** A hash of the serialized data is stored in the envelope. On load, the checksum is recomputed and compared. Tampered or corrupted files are rejected.
- **Migration chain.** When the save format changes between game versions, migration functions upgrade the data step by step (v1 -> v2 -> v3 -> ... -> current). Old saves are never abandoned.
- **ISaveable interface.** Game objects that need persistence implement `save_to_dict()` and `load_from_dict()` for clean serialization boundaries.

## Module Files

| File | Purpose | Dependencies |
|---|---|---|
| `save_format` | SaveFormat: envelope creation and validation | None |
| `save_migrator` | SaveMigrator: version migration chain | None |
| `save_manager` | SaveManager: orchestrates save/load with PlayerState | SaveFormat, SaveMigrator, PlayerState, EventBus |

## SaveFormat

### Envelope Structure

```json
{
    "version": 3,
    "timestamp": 1710590400,
    "checksum": "a1b2c3d4e5f6...",
    "data": {
        "progress": { "level": 25, "stage": 7 },
        "inventory": { "gold": 5000, "gems": 120 },
        "settings": { "music": true, "sfx": true }
    }
}
```

### API

#### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `create_envelope(data: Dictionary, version: int)` | player data, format version | `Dictionary` | Create a save envelope with checksum |
| `validate(envelope: Dictionary)` | envelope dict | `bool` | Verify checksum integrity |
| `get_version(envelope: Dictionary)` | envelope dict | `int` | Extract version number |
| `get_data(envelope: Dictionary)` | envelope dict | `Dictionary` | Extract data payload |
| `get_timestamp(envelope: Dictionary)` | envelope dict | `int` | Extract save timestamp |

#### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `CreateEnvelope(Dictionary<string, object> data, int version)` | data, version | `Dictionary<string, object>` | Create envelope |
| `Validate(Dictionary<string, object> envelope)` | envelope | `bool` | Verify checksum |
| `GetVersion(Dictionary<string, object> envelope)` | envelope | `int` | Extract version |
| `GetData(Dictionary<string, object> envelope)` | envelope | `Dictionary<string, object>` | Extract data |
| `GetTimestamp(Dictionary<string, object> envelope)` | envelope | `long` | Extract timestamp |

### Checksum Algorithm

The checksum is computed by:

1. Serialize the `data` dictionary to a canonical JSON string (keys sorted alphabetically).
2. Compute SHA-256 hash of the JSON string.
3. Encode the hash as a lowercase hex string.

```gdscript
# Godot
func _compute_checksum(data: Dictionary) -> String:
    var json_str := JSON.stringify(data, "", true)  # sorted keys
    var ctx := HashingContext.new()
    ctx.start(HashingContext.HASH_SHA256)
    ctx.update(json_str.to_utf8_buffer())
    return ctx.finish().hex_encode()
```

```csharp
// Unity
string ComputeChecksum(Dictionary<string, object> data)
{
    string json = JsonConvert.SerializeObject(data, new JsonSerializerSettings
    {
        Formatting = Formatting.None,
        // keys sorted
    });
    using var sha = SHA256.Create();
    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
    return BitConverter.ToString(hash).Replace("-", "").ToLower();
}
```

## SaveMigrator

### API

#### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `register(from_version: int, to_version: int, migrator: Callable)` | source version, target version, migration function | `void` | Register a migration step |
| `migrate(data: Dictionary, from_version: int, to_version: int)` | data, current version, target version | `Dictionary` | Run migration chain |
| `can_migrate(from_version: int, to_version: int)` | source, target | `bool` | Check if a migration path exists |

#### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `Register(int fromVersion, int toVersion, Func<Dictionary<string, object>, Dictionary<string, object>> migrator)` | source, target, function | `void` | Register a migration step |
| `Migrate(Dictionary<string, object> data, int fromVersion, int toVersion)` | data, current, target | `Dictionary<string, object>` | Run migration chain |
| `CanMigrate(int fromVersion, int toVersion)` | source, target | `bool` | Check migration path |

### Migration Chain Example

```gdscript
# Godot
var migrator := SaveMigrator.new()

# v1 -> v2: rename "coins" to "gold"
migrator.register(1, 2, func(data: Dictionary) -> Dictionary:
    if data.has("coins"):
        data["gold"] = data["coins"]
        data.erase("coins")
    return data
)

# v2 -> v3: add "gems" defaulting to 0
migrator.register(2, 3, func(data: Dictionary) -> Dictionary:
    if not data.has("gems"):
        data["gems"] = 0
    return data
)

# v3 -> v4: restructure inventory
migrator.register(3, 4, func(data: Dictionary) -> Dictionary:
    data["currencies"] = {
        "gold": data.get("gold", 0),
        "gems": data.get("gems", 0)
    }
    data.erase("gold")
    data.erase("gems")
    return data
)

# Loading a v1 save automatically runs: v1->v2->v3->v4
var migrated := migrator.migrate(old_data, 1, 4)
```

```csharp
// Unity
var migrator = new SaveMigrator();

migrator.Register(1, 2, data =>
{
    if (data.ContainsKey("coins"))
    {
        data["gold"] = data["coins"];
        data.Remove("coins");
    }
    return data;
});

migrator.Register(2, 3, data =>
{
    if (!data.ContainsKey("gems"))
        data["gems"] = 0;
    return data;
});

// Loading a v1 save: v1 -> v2 -> v3
var migrated = migrator.Migrate(oldData, 1, 3);
```

## SaveManager

### API

#### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `save(path: String)` | file path | `bool` | Save current PlayerState to file |
| `load_save(path: String)` | file path | `bool` | Load save file into PlayerState |
| `has_save(path: String)` | file path (optional, uses default) | `bool` | Check if a save file exists |
| `delete_save(path: String)` | file path | `bool` | Delete a save file |
| `get_save_info(path: String)` | file path | `Dictionary` | Get version and timestamp without full load |
| `set_current_version(version: int)` | version number | `void` | Set the current save format version |

#### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `Save(string path)` | file path | `bool` | Save PlayerState to file |
| `Load(string path)` | file path | `bool` | Load save into PlayerState |
| `HasSave(string path)` | file path (optional) | `bool` | Check if save exists |
| `DeleteSave(string path)` | file path | `bool` | Delete save file |
| `GetSaveInfo(string path)` | file path | `Dictionary<string, object>` | Get metadata |
| `CurrentVersion` | -- | `int` | Current format version |

## ISaveable Interface

Game objects that need to participate in save/load implement this interface:

```gdscript
# Godot
class_name ISaveable

## Serialize this object's state to a dictionary.
func save_to_dict() -> Dictionary:
    return {}

## Restore this object's state from a dictionary.
func load_from_dict(data: Dictionary) -> void:
    pass
```

```csharp
// Unity
public interface ISaveable
{
    Dictionary<string, object> SaveToDict();
    void LoadFromDict(Dictionary<string, object> data);
}
```

### Implementation Example

```gdscript
# Godot
class_name HeroData extends RefCounted

var name: String = ""
var level: int = 1
var experience: int = 0

func save_to_dict() -> Dictionary:
    return {
        "name": name,
        "level": level,
        "experience": experience
    }

func load_from_dict(data: Dictionary) -> void:
    name = data.get("name", "")
    level = data.get("level", 1)
    experience = data.get("experience", 0)
```

## Atomic Write Flow

```
1. Serialize PlayerState to Dictionary
2. Create envelope (add version, timestamp, checksum)
3. Convert envelope to JSON string
4. Write JSON to temp file: save_path + ".tmp"
5. Flush and close temp file
6. Rename temp file to save_path (atomic on most file systems)
7. Emit "save_completed" event

If any step fails:
  - Delete temp file if it exists
  - Emit "save_completed" with success=false
  - Original save file remains untouched
```

## Events Emitted

| Event | Payload | When |
|---|---|---|
| `save_completed` | `{ "success": bool, "path": String }` | After a save attempt |
| `save_loaded` | `{ "success": bool, "path": String, "version": int }` | After a load attempt |
| `save_migrated` | `{ "from_version": int, "to_version": int }` | After a migration runs |

## Default Save Path

| Platform | Path |
|---|---|
| Godot (all) | `user://save.json` |
| Unity Android | `Application.persistentDataPath + "/save.json"` |
| Unity iOS | `Application.persistentDataPath + "/save.json"` |
| Unity Editor | `Application.persistentDataPath + "/save.json"` |

## Best Practices

1. **Save on key events, not on timer.** Save after level completion, purchases, and settings changes. Avoid constant auto-save which wears flash storage.
2. **Always validate on load.** If the checksum fails, treat the save as corrupted. Fall back to a backup or start fresh.
3. **Register all migrations at startup.** The migration chain must be complete before any load is attempted.
4. **Keep migration functions pure.** A migration function takes a dictionary and returns a dictionary. No side effects, no file I/O.
5. **Test migration chains end-to-end.** Create test vectors with v1 saves and verify they migrate correctly to the latest version.
6. **Use `has_save()` to show continue vs. new game.** Check for a valid save file before showing the "Continue" button.
