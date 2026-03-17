## Quality Material Switcher
## Swaps materials on MeshInstance3D nodes based on device quality level.
## Inspired by Overkill 3's MaterialSwapper + ShaderQuality system.
##
## Usage:
##   1. Attach to a Node3D parent of MeshInstance3D children
##   2. Assign basic_material and advanced_material in the inspector
##   3. Call apply_quality() at startup or when settings change
##
## For global quality control, use as an Autoload and call apply_quality_global().
class_name QualityMaterialSwitcher
extends Node3D

## Quality levels corresponding to increasing visual fidelity.
enum QualityLevel { LOW, MEDIUM, HIGH, ULTRA }

## The basic (low-quality) material to use on low-spec devices.
@export var basic_material: Material

## The advanced (high-quality) material to use on high-spec devices.
@export var advanced_material: Material

## Threshold quality level. At or above this level, use advanced material.
@export var advanced_threshold: QualityLevel = QualityLevel.HIGH

## Current quality level. Set this from your settings menu.
var current_quality: QualityLevel = QualityLevel.MEDIUM

## Auto-detect quality on ready.
@export var auto_detect: bool = true


func _ready() -> void:
	if auto_detect:
		current_quality = _detect_quality()
	apply_quality(current_quality)


## Apply the given quality level to all child MeshInstance3D nodes.
func apply_quality(quality: QualityLevel) -> void:
	current_quality = quality
	var use_advanced := quality >= advanced_threshold
	var target_mat := advanced_material if use_advanced else basic_material
	if not target_mat:
		return
	for child in _get_mesh_instances(self):
		child.material_override = target_mat


## Apply quality globally to all MeshInstance3D nodes with a group tag.
## Call this from an Autoload for whole-scene quality changes.
static func apply_quality_global(
	tree: SceneTree,
	group: StringName,
	basic: Material,
	advanced: Material,
	quality: QualityLevel,
	threshold: QualityLevel = QualityLevel.HIGH
) -> void:
	var use_advanced := quality >= threshold
	var target := advanced if use_advanced else basic
	for node in tree.get_nodes_in_group(group):
		if node is MeshInstance3D:
			node.material_override = target


## Simple quality auto-detection based on processor count.
## Override this for more sophisticated detection (GPU benchmarks, etc).
static func _detect_quality() -> QualityLevel:
	var cores := OS.get_processor_count()
	if cores <= 2:
		return QualityLevel.LOW
	elif cores <= 4:
		return QualityLevel.MEDIUM
	elif cores <= 8:
		return QualityLevel.HIGH
	else:
		return QualityLevel.ULTRA


func _get_mesh_instances(node: Node) -> Array[MeshInstance3D]:
	var result: Array[MeshInstance3D] = []
	for child in node.get_children():
		if child is MeshInstance3D:
			result.append(child)
		result.append_array(_get_mesh_instances(child))
	return result
