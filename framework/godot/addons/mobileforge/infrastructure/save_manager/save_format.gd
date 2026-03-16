class_name MFSaveFormat extends RefCounted
## Save file envelope: version, timestamp, checksum, data.

const CURRENT_VERSION := 1

static func create_envelope(data: Dictionary, version: int = CURRENT_VERSION) -> Dictionary:
    var json_str := JSON.stringify(data)
    return {
        "version": version,
        "timestamp": Time.get_unix_time_from_system(),
        "checksum": json_str.sha256_text(),
        "data": data,
    }

static func validate_envelope(envelope: Dictionary) -> bool:
    if not envelope.has("version") or not envelope.has("data") or not envelope.has("checksum"):
        return false
    var json_str := JSON.stringify(envelope["data"])
    return json_str.sha256_text() == envelope["checksum"]

static func get_version(envelope: Dictionary) -> int:
    return int(envelope.get("version", 0))

static func get_data(envelope: Dictionary) -> Dictionary:
    return envelope.get("data", {})
