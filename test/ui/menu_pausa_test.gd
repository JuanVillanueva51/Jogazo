extends GdUnitTestSuite

const PAUSE_SCREEN_PATH = "res://scenes/ui/pause_screen.tscn"

const MAIN_MENU = "res://scenes/ui/main_menu.tscn"
const CHARACTER_SELECT = "res://scenes/ui/character_select.tscn"
const CLASS_SELECT = "res://scenes/ui/class_select.tscn"
const FOREST_MAP = "res://scenes/mapas/forest_map.tscn"

const CHAR_GRID = "LeftPanel/VBoxContainer/ScrollContainer/CharGrid"
const CLASS_GRID = "LeftPanel/VBox/ScrollContainer/ClassGrid"
func test_pause_and_resume():
	var tree := Engine.get_main_loop() as SceneTree
	
	# Cargar el menú de pausa
	var packed_scene := load(PAUSE_SCREEN_PATH) as PackedScene
	assert_object(packed_scene).is_not_null()

	var pause_screen := packed_scene.instantiate()
	tree.root.add_child(pause_screen)

	# Comprobar que inicia oculto
	assert_bool(pause_screen.visible).is_false()
	await _pausa(3.0)

	# Simular la pausa
	pause_screen.PauseGame()
	await _pausa(3.0)

	assert_bool(pause_screen.visible).is_true()
	assert_bool(tree.paused).is_true()
	await _pausa(1.0)

	# Reanudar
	pause_screen.Resume()
	await _pausa(3.0)

	assert_bool(pause_screen.visible).is_false()
	assert_bool(tree.paused).is_false()
	await _pausa(1.0)

	# Limpiar
	pause_screen.queue_free()
	await _pausa(3.0)


func _tree() -> SceneTree:
	return Engine.get_main_loop() as SceneTree

func _pausa(segundos: float) -> void:
	await _tree().create_timer(segundos).timeout

func _wait_for_scene(path: String, timeout_ms := 5000) -> bool:
	var elapsed := 0

	while elapsed < timeout_ms:
		var current = _tree().current_scene

		if current != null and current.scene_file_path == path:
				return true

		await _tree().process_frame
		elapsed += 16

	return false


func _adoptar(runner):
	_tree().current_scene = runner.scene()


func _wait_frames(n: int):
	for i in range(n):
		await _tree().process_frame

func after_test() -> void:
	var gm:= _tree().root.get_node_or_null("GameManager")
	if gm != null:
		gm.set("NextRunIsDemo", false)
		gm.call("ResetRun")
	var actual := _tree().current_scene
	if actual != null and actual.scene_file_path in [CHARACTER_SELECT, CLASS_SELECT, FOREST_MAP]:
		actual.queue_free()
		await _tree().process_frame

func test_pause_screen_buttons_exist():
	var menu = await scene_runner(MAIN_MENU)
	_adoptar(menu)

	menu.scene().get_node("CenterContainer/VBox/PlayRow/DemoButton").emit_signal("pressed")

	assert_bool(await _wait_for_scene(CHARACTER_SELECT)).is_true()
	var character_scene = _tree().current_scene
	var grid = character_scene.get_node(CHAR_GRID)
	var character = grid.get_child(1).get_child(0) as TextureButton

	character.emit_signal("pressed")

	character_scene.get_node("RightPanel/VBoxContainer/ConfirmButton").emit_signal("pressed")

	assert_bool(await _wait_for_scene(CLASS_SELECT)).is_true()
	var class_scene = _tree().current_scene
	var grid_clases = class_scene.get_node(CLASS_GRID)
	var selected_class = grid_clases.get_child(1).get_child(0) as TextureButton

	selected_class.emit_signal("pressed")

	class_scene.get_node(
	    "RightPanel/VBox/PlayButton"
	).emit_signal("pressed")
	assert_bool(await _wait_for_scene(FOREST_MAP)).is_true()

	await _wait_frames(10)
	var pause_screen = _tree().root.find_child("PauseScreen", true, false)

	assert_object(pause_screen).is_not_null()
	var resume_button = pause_screen.get_node_or_null("MainPanel/ReanudarButton")

	var config_button = pause_screen.get_node_or_null(
	    "MainPanel/ConfigButton"
	)

	var exit_button = pause_screen.get_node_or_null(
	    "MainPanel/ExitButton"
	)

	assert_object(resume_button).is_not_null()
	assert_object(config_button).is_not_null()
	assert_object(exit_button).is_not_null()
