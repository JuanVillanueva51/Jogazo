extends GdUnitTestSuite

const MAIN_MENU := "res://scenes/ui/main_menu.tscn"
const CHARACTER_SELECT := "res://scenes/ui/character_select.tscn"
const CLASS_SELECT := "res://scenes/ui/class_select.tscn"
const CHAR_GRID := "LeftPanel/VBoxContainer/ScrollContainer/CharGrid"
const CLASS_GRID := "LeftPanel/VBox/ScrollContainer/ClassGrid"

const PAUSA_PANTALLA := 1.5
const PAUSA_SELECCION := 0.7


func _pausa(segundos: float) -> void:
	if segundos > 0.0:
		await (Engine.get_main_loop() as SceneTree).create_timer(segundos).timeout


func _gm() -> Node:
	return (Engine.get_main_loop() as SceneTree).root.get_node_or_null("GameManager")


func _set_demo(value: bool) -> void:
	var gm := _gm()
	assert_object(gm).is_not_null()
	gm.set("NextRunIsDemo", value)


func after_test() -> void:
	var gm := _gm()
	if gm != null:
		gm.set("NextRunIsDemo", false)


func _tooltip_name(card: Node) -> String:
	var button := card.get_child(0) as TextureButton
	return button.tooltip_text.split(" — ")[0]


func test_menu_principal_muestra_jugar_y_demo() -> void:
	var runner := scene_runner(MAIN_MENU)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	var play := runner.scene().get_node("CenterContainer/VBox/PlayRow/PlayButton") as Button
	var demo := runner.scene().get_node("CenterContainer/VBox/PlayRow/DemoButton") as Button
	assert_str(play.text).is_equal("JUGAR")
	assert_str(demo.text).is_equal("DEMO")
	assert_bool(play.visible and not play.disabled).is_true()
	assert_bool(demo.visible and not demo.disabled).is_true()


func test_menu_principal_muestra_todas_las_opciones_y_creditos() -> void:
	var runner := scene_runner(MAIN_MENU)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	var textos: Array = []
	for b in runner.scene().find_children("*", "Button", true, false):
		textos.append((b as Button).text)
	for esperado in ["JUGAR", "DEMO", "HISTORIA", "DESBLOQUEOS", "CONFIGURACION", "SALIR"]:
		assert_bool(textos.has(esperado)).override_failure_message("Falta el botón " + esperado).is_true()
	var hay_creditos := false
	for l in runner.scene().find_children("*", "Label", true, false):
		if (l as Label).text.begins_with("Créditos"):
			hay_creditos = true
	assert_bool(hay_creditos).is_true()


func test_seleccion_personaje_aviso_demo(demo: bool, test_parameters := [[true], [false]]) -> void:
	_set_demo(demo)
	var runner := scene_runner(CHARACTER_SELECT)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	var hay_aviso := false
	for l in runner.scene().find_children("*", "Label", true, false):
		if (l as Label).text.contains("MODO DEMO · partida rápida de un solo mapa"):
			hay_aviso = true
	assert_bool(hay_aviso).is_equal(demo)


func test_seleccion_personaje_primero_por_defecto() -> void:
	var runner := scene_runner(CHARACTER_SELECT)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	var grid := runner.scene().get_node(CHAR_GRID)
	var nombre := runner.scene().get_node("RightPanel/VBoxContainer/CharNameLabel") as Label
	assert_int(grid.get_child_count()).is_greater(1)
	assert_bool(nombre.text.begins_with(_tooltip_name(grid.get_child(0)))).is_true()


func test_seleccion_personaje_elegir_cada_uno() -> void:
	var runner := scene_runner(CHARACTER_SELECT)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	var grid := runner.scene().get_node(CHAR_GRID)
	var nombre := runner.scene().get_node("RightPanel/VBoxContainer/CharNameLabel") as Label
	var desc := runner.scene().get_node("RightPanel/VBoxContainer/CharDescLabel") as Label
	for card in grid.get_children():
		(card.get_child(0) as TextureButton).pressed.emit()
		await _pausa(PAUSA_SELECCION)
		assert_bool(nombre.text.begins_with(_tooltip_name(card))) \
			.override_failure_message("El panel no muestra a " + _tooltip_name(card)).is_true()
		assert_str(desc.text).is_not_empty()


func test_seleccion_clase_texto_segun_modo(demo: bool, inicio: String, test_parameters := [
		[true, "MODO DEMO: tutorial + un mapa rápido y caótico"],
		[false, "Dificultad:"]]) -> void:
	_set_demo(demo)
	var runner := scene_runner(CLASS_SELECT)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	var etiqueta := runner.scene().get_node("RightPanel/VBox/DifficultyLabel") as Label
	assert_bool(etiqueta.text.begins_with(inicio)).override_failure_message("Texto obtenido: " + etiqueta.text).is_true()


func test_seleccion_clase_elegir_cada_una() -> void:
	var runner := scene_runner(CLASS_SELECT)
	await await_idle_frame()
	await _pausa(PAUSA_PANTALLA)
	var grid := runner.scene().get_node(CLASS_GRID)
	var nombre := runner.scene().get_node("RightPanel/VBox/ClassNameLabel") as Label
	var arma := runner.scene().get_node("RightPanel/VBox/ClassWeaponLabel") as Label
	assert_int(grid.get_child_count()).is_greater(1)
	for card in grid.get_children():
		(card.get_child(0) as TextureButton).pressed.emit()
		await _pausa(PAUSA_SELECCION)
		assert_str(nombre.text).is_equal(_tooltip_name(card))
		assert_bool(arma.text.begins_with("Arma inicial: ")).is_true()
