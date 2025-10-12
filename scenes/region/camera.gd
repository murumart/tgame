extends Camera2D

const TILE_SIZE = Vector2(64, 32)

const SPEED = 360.0
const ACCEL = 60.0
const DECEL = 20.0

@onready var cursor: Sprite2D = $Cursor

@export var region_tiles: TileMapLayer

var velocity: Vector2
var zoom_size := 1.0


func _ready() -> void:
	var ui := $Ui
	remove_child(ui)
	UiLayer.add_child(ui)


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			zoom_size = minf(zoom_size + 0.1 * zoom_size, 8.0)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			zoom_size = maxf(zoom_size - 0.1 * zoom_size, 0.25)
		zoom = Vector2(zoom_size, zoom_size)


func _process(delta: float) -> void:
	_movement(delta)
	_mouse_highlight()


func _movement(delta: float) -> void:
	var speed := (SPEED / zoom_size)
	velocity = velocity.move_toward(Vector2.ZERO, delta * DECEL)
	if Input.is_key_pressed(KEY_SHIFT):
		velocity = Vector2.ZERO
		speed *= 0.5

	var dir := Input.get_vector(&"left", &"right", &"up", &"down")
	if dir.x: velocity.x = move_toward(velocity.x, dir.x * speed * delta, delta * ACCEL)
	if dir.y: velocity.y = move_toward(velocity.y, dir.y * speed * delta, delta * ACCEL)

	position += velocity


func _mouse_highlight() -> void:
	var mp := get_global_mouse_position()
	var lmp := region_tiles.to_local(mp)
	var tp := region_tiles.local_to_map(lmp)
	cursor.global_position = tile_pos_to_world_pos(tp)


func tile_pos_to_world_pos(tile_pos: Vector2i) -> Vector2:
	var half_ts := TILE_SIZE * 0.5
	@warning_ignore("integer_division")
	var tilecenter := Vector2(tile_pos.x, tile_pos.y / 2) * TILE_SIZE + half_ts
	if tile_pos.y % 2 != 0:
		tilecenter.x += half_ts.x
		tilecenter.y += half_ts.y if tile_pos.y > 0 else -half_ts.y
	return tilecenter
