extends Node
## Save/load orchestrator with atomic writes, corruption recovery, migration chain.
## Optional encryption: set encryption_key to enable XOR obfuscation of save data.

var _migrator: MFSaveMigrator
var _saveables: Dictionary = {}  # key -> MFSaveable
var _event_bus: Object
var _save_dir: String = "user://saves/"
var _auto_save_interval: float = 30.0
var _auto_save_timer: float = 0.0
var _dirty: bool = false

## Set to enable encryption. Empty string = no encryption (default).
var encryption_key: String = ""

## Segmented saves: named segments with different save frequencies.
## Frequency: "every_change", "periodic", "manual"
var _segments: Dictionary = {}  # segment_name -> {saveables: Array[key], frequency: String, dirty: bool}
var _segment_timers: Dictionary = {}  # segment_name -> float

func _ready() -> void:
    _migrator = MFSaveMigrator.new()
    _event_bus = get_node_or_null("/root/EventBus")
    DirAccess.make_dir_recursive_absolute(_save_dir)

## Register a saveable module
func register_saveable(key: StringName, saveable: MFSaveable) -> void:
    _saveables[key] = saveable

## Register a migration
func register_migrator(from_version: int, to_version: int, migrate: Callable) -> void:
    _migrator.register(from_version, to_version, migrate)

## Mark data as dirty (will auto-save on next timer tick)
func mark_dirty() -> void:
    _dirty = true

## Save to a slot
func save(slot: int = 0) -> Error:
    var data: Dictionary = {}
    for key in _saveables:
        data[key] = _saveables[key].save_to_dict()

    var envelope := MFSaveFormat.create_envelope(data)
    var json_str := JSON.stringify(envelope, "  ")

    # Encrypt if key is set
    var write_str := json_str
    if not encryption_key.is_empty():
        write_str = MFSaveFormat.encrypt_data(json_str, encryption_key)

    var path := _save_dir + "save_%d.json" % slot
    var tmp_path := path + ".tmp"

    # Atomic write: write to tmp, then rename
    var file := FileAccess.open(tmp_path, FileAccess.WRITE)
    if file == null:
        _emit(EventNames.SAVE_FAILED, {"error": "Cannot open file"})
        return ERR_FILE_CANT_WRITE
    file.store_string(write_str)
    file.close()

    # Rename tmp to final
    var dir := DirAccess.open(_save_dir)
    if dir != null:
        if dir.file_exists(path.get_file()):
            dir.remove(path.get_file())
        dir.rename(tmp_path.get_file(), path.get_file())

    _dirty = false
    _emit(EventNames.SAVE_COMPLETED, {"slot": slot})
    return OK

## Load from a slot
func load_save(slot: int = 0) -> Error:
    var path := _save_dir + "save_%d.json" % slot
    if not FileAccess.file_exists(path):
        return ERR_FILE_NOT_FOUND

    var file := FileAccess.open(path, FileAccess.READ)
    if file == null:
        return ERR_FILE_CANT_READ
    var raw_str := file.get_as_text()
    file.close()

    # Decrypt if encrypted
    var json_str := raw_str
    if not encryption_key.is_empty() and MFSaveFormat.is_encrypted(raw_str):
        json_str = MFSaveFormat.decrypt_data(raw_str, encryption_key)

    var parsed = JSON.parse_string(json_str)
    if parsed == null or not parsed is Dictionary:
        return ERR_PARSE_ERROR

    var envelope: Dictionary = parsed

    # Validate checksum
    if not MFSaveFormat.validate_envelope(envelope):
        push_warning("Save checksum mismatch, loading anyway")

    # Migrate if needed
    var version := MFSaveFormat.get_version(envelope)
    var data := MFSaveFormat.get_data(envelope)
    if version < MFSaveFormat.CURRENT_VERSION:
        data = _migrator.migrate(data, version, MFSaveFormat.CURRENT_VERSION)

    # Load into saveables
    for key in _saveables:
        if data.has(key):
            _saveables[key].load_from_dict(data[key])

    _emit(EventNames.STATE_LOADED, {"slot": slot})
    return OK

## Check if a save exists for a slot
func has_save(slot: int = 0) -> bool:
    return FileAccess.file_exists(_save_dir + "save_%d.json" % slot)

func _process(delta: float) -> void:
    if _dirty:
        _auto_save_timer += delta
        if _auto_save_timer >= _auto_save_interval:
            _auto_save_timer = 0.0
            save()

## Register a save segment with a frequency.
func register_segment(segment_name: String, saveable_keys: Array, frequency: String = "periodic") -> void:
    _segments[segment_name] = {"saveables": saveable_keys, "frequency": frequency, "dirty": false}
    _segment_timers[segment_name] = 0.0


## Mark a specific segment as dirty.
func mark_segment_dirty(segment_name: String) -> void:
    if _segments.has(segment_name):
        _segments[segment_name].dirty = true
        # EVERY_CHANGE segments save immediately
        if _segments[segment_name].frequency == "every_change":
            save_segment(segment_name)


## Save only a specific segment.
func save_segment(segment_name: String, slot: int = 0) -> Error:
    if not _segments.has(segment_name):
        return ERR_DOES_NOT_EXIST
    var seg: Dictionary = _segments[segment_name]
    var data: Dictionary = {}
    for key in seg.saveables:
        if _saveables.has(key):
            data[key] = _saveables[key].save_to_dict()

    var envelope := MFSaveFormat.create_envelope(data)
    var json_str := JSON.stringify(envelope, "  ")
    var write_str := json_str
    if not encryption_key.is_empty():
        write_str = MFSaveFormat.encrypt_data(json_str, encryption_key)
    var path := _save_dir + "save_%d_%s.json" % [slot, segment_name]
    var tmp_path := path + ".tmp"

    var file := FileAccess.open(tmp_path, FileAccess.WRITE)
    if file == null:
        return ERR_FILE_CANT_WRITE
    file.store_string(write_str)
    file.close()

    var dir := DirAccess.open(_save_dir)
    if dir != null:
        if dir.file_exists(path.get_file()):
            dir.remove(path.get_file())
        dir.rename(tmp_path.get_file(), path.get_file())

    seg.dirty = false
    return OK


func _emit(event: StringName, payload: Dictionary) -> void:
    if _event_bus != null and _event_bus.has_method("emit_event"):
        _event_bus.emit_event(event, payload)
