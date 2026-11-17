extends GdUnitTestSuite
const FOREST_MAP := "res://scenes/mapas/forest_map.tscn"
const CIMARRON := "res://scenes/player/cimarron_char.tscn"
const PANTALLA_CARGA := "res://scenes/loadscreens/load_screen_1.tscn"
const MAPA_2 := "res://scenes/mapas/forest_map_2.tscn"

func _tree() -> SceneTree:
	return Engine.get_main_loop() as SceneTree


func _gm() -> Node:
	return _tree().root.get_node_or_null("GameManager")


func _pausa(segundos: float) -> void:
	await _tree().create_timer(segundos).timeout

#Esta lee el encabezado para ver los pasos
func _texto_paso(guia: Node) -> String:
	for l in guia.find_children("*", "Label", true, false):
		if (l as Label).text.contains("PASO "):
			return (l as Label).text
	return ""
func _esperar(condicion: Callable, timeout_ms := 5000) -> bool:
	var fin := Time.get_ticks_msec() + timeout_ms
	while Time.get_ticks_msec() < fin:
		if condicion.call():
			return true
		await _tree().process_frame
	return false
	
func _escena_actual_es(ruta: String) -> bool:
	var escena := _tree().current_scene
	return escena != null and escena.scene_file_path == ruta
	
func _esperar_paso(guia: Node, n: int) -> bool:
	var ok := await _esperar(func(): return _texto_paso(guia).ends_with("PASO %d/6" % n))
	if not ok:
		fail("Se esperaba el paso %d y la guía sigue en '%s'" % [n, _texto_paso(guia)])
	return ok
func _llevar(jugador: Node2D, destino: Vector2) -> void:
	jugador.global_position = destino
	await _tree().physics_frame
	await _tree().physics_frame
	
func _tocar(runner: GdUnitSceneRunner, accion: String) -> void:
	runner.simulate_action_press(accion)
	await _tree().physics_frame
	await _tree().physics_frame
	runner.simulate_action_release(accion)
	await _tree().physics_frame
func _objeto_en_el_suelo(mapa: Node) -> Node2D:
	for n in mapa.get_children():
		var script := n.get_script() as Script
		if script != null and script.resource_path.ends_with("ItemPickup.cs"):
			return n as Node2D
	return null
	
func before_test() -> void:
	var gm := _gm()
	assert_object(gm).is_not_null()
	gm.set("NextRunIsDemo", false)
	gm.set("SelectedCharacterIndex", 0)
	gm.set("SelectedCharacterScene", CIMARRON)
	gm.set("SelectedClassIndex", 0)
	gm.call("StartNewRun")

#Resetamos la partida en cada test
func after_test() -> void:
	_tree().paused = false
	
	var actual := _tree().current_scene
	if actual != null and actual.scene_file_path in [PANTALLA_CARGA, MAPA_2]:
		actual.queue_free()
		await _tree().process_frame
	var gm := _gm()
	if gm != null:
		gm.call("ResetRun")

func test_tutorial_completo() -> void:
	var runner := scene_runner(FOREST_MAP)
	var mapa := runner.scene()
	_tree().current_scene = mapa
	await _pausa(0.5)
	var jugador := _tree().get_first_node_in_group("player") as Node2D
	var guia := mapa.get_node("GuiaTutorial")
	assert_object(jugador).is_not_null()
	assert_str(_texto_paso(guia)).ends_with("PASO 1/6")
	var inicio := jugador.global_position
	runner.simulate_action_press("move_right")
	await _pausa(1.0)
	runner.simulate_action_release("move_right") 
	assert_float(jugador.global_position.distance_to(inicio)).is_greater(70.0)
	if not (await _esperar_paso(guia, 2)): return
	var cofre := mapa.get_node("Botin/CofreTutorial") as Node2D
	await _llevar(jugador, cofre.global_position + Vector2(0, -20))
	await _tocar(runner, "interact")
	assert_bool(cofre.get("IsOpened")).override_failure_message("El cofre no se abrió").is_true()
	if not (await _esperar_paso(guia, 3)): return
		
	await _pausa(0.7)                               
	var objeto := _objeto_en_el_suelo(mapa)
	assert_object(objeto).override_failure_message("El cofre no soltó ningún objeto").is_not_null()
	if is_failure(): return
	await _llevar(jugador, objeto.global_position) 
	if not (await _esperar_paso(guia, 4)): return
	assert_bool(is_instance_valid(objeto)).override_failure_message("El objeto sigue en el suelo").is_false()
	var cargas: int = jugador.get("DashCharges")   
	await _tocar(runner, "dash")                    
	assert_int(jugador.get("DashCharges")).override_failure_message("El dash no gastó carga").is_less(cargas)
	if not (await _esperar_paso(guia, 5)): return

	var torre := mapa.get_node("LevelUpTower") as Node2D
	torre.set("WaitTime", 1.0)                       
	await _llevar(jugador, torre.global_position)    
	var usada := await _esperar(func(): return torre.get("IsUsed"))
	assert_bool(usada).override_failure_message("La torre no terminó de cargarse").is_true()
	if is_failure(): return
	assert_bool(_tree().paused).is_true()            
	await _pausa(1.0)                                
	(torre.get_node("TowerLevelUpScreen/Panel/VBox/UpgradeCard1") as Button).pressed.emit()
	assert_bool(_tree().paused).is_false()          
	if not (await _esperar_paso(guia, 6)): return

	var salida := mapa.get_node("AreaTransicion/CollisionShape2D") as Node2D
	await _llevar(jugador, salida.global_position)   
	var cambio := await _esperar(func(): return _escena_actual_es(PANTALLA_CARGA))
	assert_bool(cambio).override_failure_message("No se cambió a la pantalla de carga").is_true()
	assert_int(_gm().get("currentMapLevel")).is_equal(1)

	await _pausa(1.0)     
