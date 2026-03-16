class_name MFSaveMigrator extends RefCounted
## Runs a chain of migrations to upgrade save data from old versions to current.

var _migrations: Dictionary = {}  # from_version -> {to_version: int, migrate: Callable}

func register(from_version: int, to_version: int, migrate: Callable) -> void:
    _migrations[from_version] = {"to_version": to_version, "migrate": migrate}

func migrate(data: Dictionary, from_version: int, to_version: int) -> Dictionary:
    var current := data.duplicate(true)
    var version := from_version
    while version < to_version:
        if not _migrations.has(version):
            push_error("No migration from version %d" % version)
            break
        var migration = _migrations[version]
        current = migration.migrate.call(current)
        version = migration.to_version
    return current

func has_migration(from_version: int) -> bool:
    return _migrations.has(from_version)
