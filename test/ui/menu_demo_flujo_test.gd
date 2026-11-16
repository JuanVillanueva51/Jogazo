extends GdUnitTestSuite

const MAIN_MENU := "res://scenes/ui/main_menu.tscn"
const CHARACTER_SELECT := "res://scenes/ui/character_select.tscn"
const CLASS_SELECT := "res://scenes/ui/class_select.tscn"
const FOREST_MAP := "res://scenes/mapas/forest_map.tscn"
const CHAR_GRID := "LeftPanel/VBoxContainer/ScrollContainer/CharGrid"
const CLASS_GRID := "LeftPanel/VBox/ScrollContainer/ClassGrid"

const PAUSA_PANTALLA := 1.5
const PAUSA_FINAL := 10.0
const ESCENAS := [MAIN_MENU, CHARACTER_SELECT, CLASS_SELECT, FOREST_MAP]


func _tree() -> SceneTree:
	return Engine.get_main_loop() as SceneTree


func _gm() -> Node:
	return _tree().root.get_node_or_null("GameManager")


func _wait_for_scene(path: String, timeout_ms := 5000) -> bool:
	var end := Time.get_ticks_msec() + timeout_ms
	while Time.get_ticks_msec() < end:
		var current := _tree().current_scene
		if current != null and current.scene_file_path == path:
			return true
		await _tree().process_frame
	return false


func _pausa(segundos: float) -> void:
	if segundos > 0.0:
		await _tree().create_timer(segundos).timeout



func _adoptar(runner: GdUnitSceneRunner) -> void:
	_tree().current_scene = runner.scene()


func _wait_frames(n: int) -> void:
	for i in n:
		await _tree().process_frame


func before_test() -> void:
	var gm := _gm()
	assert_object(gm).is_not_null()
	gm.set("NextRunIsDemo", false)
	gm.set("SelectedCharacterIndex", 0)
	gm.set("SelectedClassIndex", 0)


func after_test() -> void:
	var gm := _gm()
	if gm != null:
		gm.set("NextRunIsDemo", false)
	var actual := _tree().current_scene
	if actual != null and ESCENAS.has(actual.scene_file_path):
		actual.queue_free()
		await _tree().process_frame


func test_demo_abre_seleccion_de_personaje() -> void:
	var runner := scene_runner(MAIN_MENU)
	_adoptar(runner)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	(runner.scene().get_node("CenterContainer/VBox/PlayRow/DemoButton") as Button).pressed.emit()
	assert_bool(await _wait_for_scene(CHARACTER_SELECT)).is_true()
	assert_bool(_gm().get("NextRunIsDemo")).is_true()


func test_jugar_no_activa_demo() -> void:
	_gm().set("NextRunIsDemo", true)
	var runner := scene_runner(MAIN_MENU)
	_adoptar(runner)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	(runner.scene().get_node("CenterContainer/VBox/PlayRow/PlayButton") as Button).pressed.emit()
	assert_bool(await _wait_for_scene(CHARACTER_SELECT)).is_true()
	assert_bool(_gm().get("NextRunIsDemo")).is_false()


func test_confirmar_personaje_abre_seleccion_de_clase() -> void:
	_gm().set("NextRunIsDemo", true)
	var runner := scene_runner(CHARACTER_SELECT)
	_adoptar(runner)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	var grid := runner.scene().get_node(CHAR_GRID)
	assert_int(grid.get_child_count()).is_greater(1)
	(grid.get_child(1).get_child(0) as TextureButton).pressed.emit()  # segundo personaje
	(runner.scene().get_node("RightPanel/VBoxContainer/ConfirmButton") as Button).pressed.emit()
	assert_bool(await _wait_for_scene(CLASS_SELECT)).is_true()
	assert_int(_gm().get("SelectedCharacterIndex")).is_equal(1)


func test_flujo_demo_completo_inicia_la_partida() -> void:
	# Menú principal y seleciona la DEMO
	var menu := scene_runner(MAIN_MENU)
	_adoptar(menu)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	(menu.scene().get_node("CenterContainer/VBox/PlayRow/DemoButton") as Button).pressed.emit()
	assert_bool(await _wait_for_scene(CHARACTER_SELECT)).is_true()

	# Seleccióna el segundo personaje
	await _wait_frames(3)
	await _pausa(PAUSA_PANTALLA)
	var personajes := _tree().current_scene
	var grid := personajes.get_node(CHAR_GRID)
	assert_int(grid.get_child_count()).is_greater(1)
	(grid.get_child(1).get_child(0) as TextureButton).pressed.emit()
	await _pausa(PAUSA_PANTALLA)
	(personajes.get_node("RightPanel/VBoxContainer/ConfirmButton") as Button).pressed.emit()
	assert_bool(await _wait_for_scene(CLASS_SELECT)).is_true()

	# Selección de clase segunda clase
	await _wait_frames(3)
	await _pausa(PAUSA_PANTALLA)
	var clases := _tree().current_scene
	var grid_clases := clases.get_node(CLASS_GRID)
	assert_int(grid_clases.get_child_count()).is_greater(1)
	(grid_clases.get_child(1).get_child(0) as TextureButton).pressed.emit()
	await _pausa(PAUSA_PANTALLA)
	(clases.get_node("RightPanel/VBox/PlayButton") as Button).pressed.emit()

	# Inicia la partida con mapa del bosque y con la demo
	assert_bool(await _wait_for_scene(FOREST_MAP)).is_true()
	await _wait_frames(10)
	var gm := _gm()
	assert_bool(gm.get("IsDemo")).is_true()
	assert_int(gm.get("SelectedCharacterIndex")).is_equal(1)
	assert_int(gm.get("SelectedClassIndex")).is_equal(1)
	assert_int(gm.get("CurrentLevel")).is_equal(1)
	await _pausa(PAUSA_FINAL)
