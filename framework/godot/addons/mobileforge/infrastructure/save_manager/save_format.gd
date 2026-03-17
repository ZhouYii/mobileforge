class_name MFSaveFormat extends RefCounted
## Save file envelope: version, timestamp, checksum, data.
## Optional encryption: XOR obfuscation or AES-256-CBC.

const CURRENT_VERSION := 1
const _ENCRYPTED_PREFIX := "MF_ENC:"

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

## Encrypt a JSON string using XOR obfuscation with the given key.
## Returns a base64-encoded string with the encrypted prefix.
static func encrypt_data(json_str: String, key: String) -> String:
    var data := json_str.to_utf8_buffer()
    var key_bytes := key.to_utf8_buffer()
    var encrypted := PackedByteArray()
    encrypted.resize(data.size())
    for i in range(data.size()):
        encrypted[i] = data[i] ^ key_bytes[i % key_bytes.size()]
    return _ENCRYPTED_PREFIX + Marshalls.raw_to_base64(encrypted)

## Decrypt a string that was encrypted with encrypt_data().
## Returns the original JSON string, or empty string on failure.
static func decrypt_data(encrypted_str: String, key: String) -> String:
    if not encrypted_str.begins_with(_ENCRYPTED_PREFIX):
        return encrypted_str  # Not encrypted, return as-is (backward compatible)
    var base64 := encrypted_str.substr(_ENCRYPTED_PREFIX.length())
    var data := Marshalls.base64_to_raw(base64)
    var key_bytes := key.to_utf8_buffer()
    var decrypted := PackedByteArray()
    decrypted.resize(data.size())
    for i in range(data.size()):
        decrypted[i] = data[i] ^ key_bytes[i % key_bytes.size()]
    return decrypted.get_string_from_utf8()

## Check if a string is encrypted (has the encrypted prefix).
static func is_encrypted(data_str: String) -> bool:
    return data_str.begins_with(_ENCRYPTED_PREFIX)
