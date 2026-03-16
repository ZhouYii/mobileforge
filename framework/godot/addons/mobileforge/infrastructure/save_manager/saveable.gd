class_name MFSaveable extends RefCounted
## Interface for modules that can be saved/loaded.
## Implement save_to_dict() and load_from_dict() in subclasses.

func save_to_dict() -> Dictionary:
    return {}

func load_from_dict(_data: Dictionary) -> void:
    pass
