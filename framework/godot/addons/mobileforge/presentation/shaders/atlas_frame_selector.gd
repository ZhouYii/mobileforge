## Atlas Frame Selector
## Drives a shader that displays a specific frame from a texture atlas grid.
## Inspired by AQ Battle Gems' UV window system for multi-state gem rendering.
##
## Usage:
##   var selector = AtlasFrameSelector.new()
##   selector.setup(sprite, 2, 2)  # 2x2 atlas
##   selector.set_frame(0)  # top-left
##   selector.set_frame(3)  # bottom-right
class_name AtlasFrameSelector
extends RefCounted

var _material: ShaderMaterial
var _columns: int = 2
var _rows: int = 2


## Initialize with a CanvasItem that has a ShaderMaterial using atlas_frame.gdshader.
func setup(node: CanvasItem, columns: int, rows: int) -> void:
	_material = node.material as ShaderMaterial
	_columns = columns
	_rows = rows
	if not _material:
		push_error("AtlasFrameSelector: node needs ShaderMaterial with atlas_frame shader")


## Set the current frame index (0-based, left-to-right, top-to-bottom).
func set_frame(frame: int) -> void:
	if not _material:
		return
	frame = clampi(frame, 0, _columns * _rows - 1)
	var col := frame % _columns
	var row := frame / _columns
	var uv_window := Vector4(
		float(col) / float(_columns),
		float(row) / float(_rows),
		1.0 / float(_columns),
		1.0 / float(_rows)
	)
	_material.set_shader_parameter("uv_window", uv_window)


## Animate frame transition with a brief tween (for destruction sequences etc).
func animate_frames(node: CanvasItem, frames: Array[int], fps: float = 10.0) -> void:
	var delay := 1.0 / fps
	for frame_idx in frames:
		set_frame(frame_idx)
		await node.get_tree().create_timer(delay).timeout


## Get the total number of frames in this atlas.
func get_frame_count() -> int:
	return _columns * _rows
