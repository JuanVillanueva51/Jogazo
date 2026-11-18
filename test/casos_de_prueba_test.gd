extends GdUnitTestSuite
## Casos de prueba UABC RogueLike: UT-01 a UT-06, PUI-111 y PUI-112.
## Basado en "Casos_de_prueba_UABC_RogueLike.xlsx".
##
## UT-01 a UT-05 prueban la lógica de C# del juego (Balance, DamageCalculator y LootTable).
## GDScript no puede llamar clases estáticas de C#, así que lo hacen a través de
## res://scripts/pruebas/PuentePruebas.cs. Ese archivo debe estar en scripts/pruebas/
## (NO dentro de test/) y hay que compilar con el martillo antes de correr estas pruebas.

const PUENTE := "res://scripts/pruebas/PuentePruebas.cs"

# ============================================================================
# ESCENAS Y ACCIONES
# ============================================================================
const FOREST_MAP := "res://scenes/mapas/forest_map.tscn"
const CIMARRON := "res://scenes/player/cimarron_char.tscn"

const ACC_ARRIBA := "move_up"
const ACC_ABAJO := "move_down"
const ACC_IZQUIERDA := "move_left"
const ACC_DERECHA := "move_right"

# Dificultades (mismo orden que Balance.Difficulties)
const DIF_NORMAL := 0
const DIF_DIFICIL := 1
const DIF_PESADILLA := 2

# Punto libre en medio del campo de tierra, en coordenadas locales del mapa del tutorial:
# lejos de la casa, del NPC, de la torre y de los muros invisibles.
const PUNTO_LIBRE := Vector2(850, 550)

# Progreso de la partida: minutos jugados en el primer mapa
const MIN_INICIO := 1.0
const MIN_INTERMEDIO := 5.0
const MIN_AVANZADO := 10.0

# ============================================================================
# ESTADO Y UTILIDADES
# ============================================================================
var _puente = null
var _runner: GdUnitSceneRunner
var _mapa: Node
var _jugador: Node2D
var _guia: Node
var _partida_iniciada := false


func _tree() -> SceneTree:
	return Engine.get_main_loop() as SceneTree


func _gm() -> Node:
	return _tree().root.get_node_or_null("GameManager")


func _pausa(segundos: float) -> void:
	if segundos > 0.0:
		await _tree().create_timer(segundos).timeout


func _esperar(condicion: Callable, timeout_ms := 5000) -> bool:
	var fin := Time.get_ticks_msec() + timeout_ms
	while Time.get_ticks_msec() < fin:
		if condicion.call():
			return true
		await _tree().process_frame
	return false


## Objeto de C# que expone Balance, DamageCalculator y LootTable (ver PuentePruebas.cs).
func _logica():
	if _puente != null:
		return _puente
	if not ResourceLoader.exists(PUENTE):
		fail("No se encontró %s. Copia PuentePruebas.cs a scripts/pruebas/ y compila con el martillo." % PUENTE)
		return null
	var script = load(PUENTE)
	if script != null:
		_puente = script.new()
	if _puente == null:
		fail("No se pudo crear PuentePruebas. ¿Compilaste el proyecto con el martillo?")
	return _puente


func before_test() -> void:
	var gm := _gm()
	assert_object(gm).is_not_null()
	gm.set("NextRunIsDemo", false)
	_partida_iniciada = false
	_runner = null
	_mapa = null
	_jugador = null
	_guia = null


func after_test() -> void:
	_tree().paused = false
	var actual := _tree().current_scene
	if actual != null and actual.scene_file_path == FOREST_MAP:
		actual.queue_free()
		await _tree().process_frame
	var gm := _gm()
	if gm != null:
		gm.set("NextRunIsDemo", false)
		if _partida_iniciada:
			gm.call("ResetRun")


# ============================================================================
# UT-01  Reducción de daño por armadura (RA-01 a RA-04)
# Fórmula del juego: daño * 100 / (100 + armadura)  (Balance.ArmorMultiplier)
# ============================================================================
func test_UT_01_reduccion_de_dano_por_armadura() -> void:
	var logica = _logica()
	if logica == null:
		return
	var base := 100.0
	var armaduras := [0.0, 10.0, 50.0, 200.0]
	var resultados: Array[float] = []
	for armadura in armaduras:
		var dano: float = logica.DanoRecibido(base, armadura)
		resultados.append(dano)

	# RA-01: armadura 0 -> sin reducción
	assert_float(resultados[0]).override_failure_message(
		"RA-01: con armadura 0 el daño debe ser 100 y fue %.2f" % resultados[0]).is_equal_approx(100.0, 0.001)
	# RA-02: armadura 10 -> daño menor que 100
	assert_float(resultados[1]).override_failure_message(
		"RA-02: con armadura 10 el daño debe bajar de 100 y fue %.2f" % resultados[1]).is_less(100.0)
	# RA-03: armadura 50 -> daño menor que 99
	assert_float(resultados[2]).override_failure_message(
		"RA-03: con armadura 50 el daño debe bajar de 99 y fue %.2f" % resultados[2]).is_less(99.0)
	# Cada armadura mayor produce un daño menor o igual al anterior
	for i in range(1, resultados.size()):
		assert_float(resultados[i]).override_failure_message(
			"El daño aumentó al subir la armadura de %s a %s" % [armaduras[i - 1], armaduras[i]]
		).is_less_equal(resultados[i - 1])
	# RA-04: armadura muy alta -> el daño nunca es negativo
	for i in resultados.size():
		assert_float(resultados[i]).override_failure_message(
			"RA-04: daño negativo con armadura %s" % armaduras[i]).is_greater_equal(0.0)


# ============================================================================
# UT-02  Bonificaciones de daño (BD-01 a BD-04)
# Usa DamageCalculator.BaseForWeapon con un arma cuerpo a cuerpo y bonificación de tipo.
# ============================================================================
func test_UT_02_bonificaciones_de_dano() -> void:
	var logica = _logica()
	if logica == null:
		return
	var bd01: float = logica.DanoConBonificacion(0.0, 0.25)
	var bd02: float = logica.DanoConBonificacion(50.0, 0.0)
	var bd03: float = logica.DanoConBonificacion(50.0, 0.25)
	var bd04: float = logica.DanoConBonificacion(50.0, 1.0)

	assert_float(bd01).override_failure_message(
		"BD-01: daño base 0 con +25%% debe ser 0 y fue %.2f" % bd01).is_equal_approx(0.0, 0.001)
	assert_float(bd02).override_failure_message(
		"BD-02: daño 50 sin bonificación debe ser 50 y fue %.2f" % bd02).is_equal_approx(50.0, 0.001)
	assert_float(bd03).override_failure_message(
		"BD-03: daño 50 con +25%% debe ser 62.5 y fue %.2f" % bd03).is_equal_approx(62.5, 0.001)
	assert_float(bd04).override_failure_message(
		"BD-04: daño 50 con +100%% debe ser 100 y fue %.2f" % bd04).is_equal_approx(100.0, 0.001)
	# Crece con la bonificación
	assert_float(bd03).is_greater(bd02)
	assert_float(bd04).is_greater(bd03)


# ============================================================================
# UT-03  Escalado por dificultad (ED-01 a ED-03)
# Coeficiente de dificultad tras 5 minutos de partida en el primer mapa.
# ============================================================================
func test_UT_03_escalado_por_dificultad() -> void:
	var logica = _logica()
	if logica == null:
		return
	assert_int(logica.CantidadDificultades()).override_failure_message(
		"Se esperaban 3 dificultades (Normal, Difícil, Pesadilla)").is_equal(3)
	var normal: float = logica.Coeficiente(DIF_NORMAL, MIN_INTERMEDIO)
	var dificil: float = logica.Coeficiente(DIF_DIFICIL, MIN_INTERMEDIO)
	var pesadilla: float = logica.Coeficiente(DIF_PESADILLA, MIN_INTERMEDIO)

	assert_float(normal).override_failure_message(
		"ED-01: el coeficiente en Normal debe ser mayor que 0 y fue %.3f" % normal).is_greater(0.0)
	assert_float(dificil).override_failure_message(
		"ED-02: Difícil (%.3f) debe escalar más que Normal (%.3f)" % [dificil, normal]).is_greater(normal)
	assert_float(pesadilla).override_failure_message(
		"ED-03: Pesadilla (%.3f) debe escalar más que Difícil (%.3f)" % [pesadilla, dificil]).is_greater(dificil)


# ============================================================================
# UT-04  Nivel de enemigo (NE-01 a NE-12)
# Nivel a los 1, 5 y 10 minutos en cada dificultad.
# ============================================================================
func test_UT_04_nivel_de_enemigo() -> void:
	var logica = _logica()
	if logica == null:
		return
	var dificultades := [DIF_NORMAL, DIF_DIFICIL, DIF_PESADILLA]
	var nombres := ["Normal", "Difícil", "Pesadilla"]
	var minutos := [MIN_INICIO, MIN_INTERMEDIO, MIN_AVANZADO]
	var niveles := {}   # dificultad -> [nivel al inicio, intermedio, avanzado]
	for d in dificultades:
		var fila: Array[float] = []
		for m in minutos:
			fila.append(float(logica.NivelEnemigo(d, m)))
		niveles[d] = fila

	# NE-01, 04, 07 (y el resto): el nivel siempre es al menos 1
	for d in dificultades:
		for i in minutos.size():
			assert_float(niveles[d][i]).override_failure_message(
				"Nivel menor que 1 en %s al minuto %s" % [nombres[d], minutos[i]]).is_greater_equal(1.0)
	# NE-02, 03, 05, 06, 08, 09: con la misma dificultad, el nivel no baja al avanzar
	for d in dificultades:
		for i in range(1, minutos.size()):
			assert_float(niveles[d][i]).override_failure_message(
				"En %s el nivel bajó del minuto %s al %s" % [nombres[d], minutos[i - 1], minutos[i]]
			).is_greater_equal(niveles[d][i - 1])
	# NE-10, 11, 12: en el mismo momento, Pesadilla >= Difícil >= Normal
	for i in minutos.size():
		assert_float(niveles[DIF_DIFICIL][i]).override_failure_message(
			"Al minuto %s Difícil tiene menor nivel que Normal" % minutos[i]
		).is_greater_equal(niveles[DIF_NORMAL][i])
		assert_float(niveles[DIF_PESADILLA][i]).override_failure_message(
			"Al minuto %s Pesadilla tiene menor nivel que Difícil" % minutos[i]
		).is_greater_equal(niveles[DIF_DIFICIL][i])


# ============================================================================
# UT-05  Tabla de botín con pesos (TB-01 a TB-04)
# Usa LootTable.RollRarity (la misma tirada de rareza de los cofres).
# ============================================================================
func test_UT_05_tabla_de_botin_con_pesos() -> void:
	var logica = _logica()
	if logica == null:
		return
	const TIRADAS := 10000

	# Tabla A: Común = 1, Raro = 0
	var a: Dictionary = logica.ContarTiradas({"Comun": 1, "Raro": 0}, TIRADAS)
	assert_int(int(a.get("Raro", 0))).override_failure_message(
		"TB-01: Raro con peso 0 apareció %d veces" % int(a.get("Raro", 0))).is_equal(0)
	assert_int(int(a.get("Comun", 0))).override_failure_message(
		"TB-02: Común salió %d de %d veces" % [int(a.get("Comun", 0)), TIRADAS]).is_equal(TIRADAS)

	# Tabla B: Común = 70, Raro = 30 (margen de ±5%)
	var b: Dictionary = logica.ContarTiradas({"Comun": 70, "Raro": 30}, TIRADAS)
	var pct_comun := float(b.get("Comun", 0)) / TIRADAS * 100.0
	var pct_raro := float(b.get("Raro", 0)) / TIRADAS * 100.0
	assert_float(pct_comun).override_failure_message(
		"TB-03: Común salió %.1f%% (se esperaba ~70%%)" % pct_comun).is_between(65.0, 75.0)
	assert_float(pct_raro).override_failure_message(
		"TB-04: Raro salió %.1f%% (se esperaba ~30%%)" % pct_raro).is_between(25.0, 35.0)


# ============================================================================
# TUTORIAL: utilidades (UT-06, PUI-111, PUI-112)
# ============================================================================
func _texto_paso() -> String:
	if _guia == null:
		return ""
	for l in _guia.find_children("*", "Label", true, false):
		if (l as Label).text.contains("PASO "):
			return (l as Label).text
	return ""


func _esperar_paso(n: int) -> bool:
	var ok := await _esperar(func(): return _texto_paso().ends_with("PASO %d/6" % n))
	if not ok:
		fail("Se esperaba el paso %d y la guía sigue en '%s'" % [n, _texto_paso()])
	return ok


## Inicia una partida nueva con Cimarrón y carga el mapa del tutorial (paso 1/6).
func _cargar_tutorial() -> bool:
	var gm := _gm()
	gm.set("NextRunIsDemo", false)
	gm.set("SelectedCharacterIndex", 0)
	gm.set("SelectedCharacterScene", CIMARRON)
	gm.set("SelectedClassIndex", 0)
	gm.call("StartNewRun")
	_partida_iniciada = true
	_runner = scene_runner(FOREST_MAP)
	_mapa = _runner.scene()
	_tree().current_scene = _mapa
	await _pausa(0.5)   # WorldSpawner crea al jugador
	_jugador = _tree().get_first_node_in_group("player") as Node2D
	_guia = _mapa.get_node_or_null("GuiaTutorial")
	assert_object(_jugador).override_failure_message("No se encontró al jugador").is_not_null()
	assert_object(_guia).override_failure_message("No se encontró la guía del tutorial").is_not_null()
	if is_failure():
		return false
	assert_str(_texto_paso()).ends_with("PASO 1/6")
	return not is_failure()


# ============================================================================
# UT-06  Movimiento direccional (MD-01 a MD-05)
# ============================================================================
func _probar_movimiento(accion: String, direccion: Vector2) -> void:
	if not (await _cargar_tutorial()):
		return
	# El jugador aparece justo debajo de la casa: hacia arriba choca con ella a los ~26 px.
	# Para probar las 4 direcciones con el mismo espacio, se le pone en un punto libre del campo.
	_jugador.global_position = _mapa.to_global(PUNTO_LIBRE)
	await _tree().physics_frame
	await _tree().physics_frame
	var inicio := _jugador.global_position
	_runner.simulate_action_press(accion)
	await _pausa(0.6)
	_runner.simulate_action_release(accion)
	var desplazamiento := _jugador.global_position - inicio
	assert_float(desplazamiento.dot(direccion)).override_failure_message(
		"El jugador no se movió hacia %s (desplazamiento %s)" % [accion, desplazamiento]).is_greater(30.0)


func test_UT_06_MD01_mover_arriba() -> void:
	await _probar_movimiento(ACC_ARRIBA, Vector2.UP)


func test_UT_06_MD02_mover_abajo() -> void:
	await _probar_movimiento(ACC_ABAJO, Vector2.DOWN)


func test_UT_06_MD03_mover_izquierda() -> void:
	await _probar_movimiento(ACC_IZQUIERDA, Vector2.LEFT)


func test_UT_06_MD04_mover_derecha() -> void:
	await _probar_movimiento(ACC_DERECHA, Vector2.RIGHT)


func test_UT_06_MD05_fase_1_del_tutorial_completada() -> void:
	if not (await _cargar_tutorial()):
		return
	var inicio := _jugador.global_position
	var mas_lejos := 0.0
	for accion in [ACC_DERECHA, ACC_ARRIBA, ACC_IZQUIERDA, ACC_ABAJO]:
		_runner.simulate_action_press(accion)
		await _pausa(0.5)
		_runner.simulate_action_release(accion)
		mas_lejos = maxf(mas_lejos, _jugador.global_position.distance_to(inicio))
	assert_float(mas_lejos).override_failure_message(
		"El jugador no se alejó lo suficiente del inicio (%.1f px)" % mas_lejos).is_greater(70.0)
	await _esperar_paso(2)


# ============================================================================
# PUI-111 y PUI-112  Menú de pausa
# ============================================================================
func _normalizar(texto: String) -> String:
	return texto.strip_edges().to_upper().replace("Ó", "O")


func _botones_visibles_en_arbol() -> Array:
	var textos: Array = []
	for b in _tree().root.find_children("*", "Button", true, false):
		if (b as Button).is_visible_in_tree():
			textos.append(_normalizar((b as Button).text))
	return textos


## Presiona Escape al inicio de un frame, antes de que los nodos corran _Process.
## Así el menú de pausa la ve como "recién presionada" (IsActionJustPressed) en ese mismo frame.
func _presionar_escape() -> void:
	await _tree().process_frame
	_runner.simulate_key_press(KEY_ESCAPE)
	await _tree().process_frame
	_runner.simulate_key_release(KEY_ESCAPE)
	await _tree().process_frame


func _abrir_pausa() -> bool:
	if not (await _cargar_tutorial()):
		return false
	await _presionar_escape()
	await _pausa(0.5)
	return true


func test_PUI_111_menu_de_pausa_se_muestra() -> void:
	if not (await _abrir_pausa()):
		return
	assert_bool(_tree().paused).override_failure_message("ESC no pausó la partida").is_true()
	var pausa := _tree().root.find_child("PauseScreen", true, false) as CanvasLayer
	assert_object(pausa).override_failure_message("No se encontró el menú de pausa").is_not_null()
	if pausa != null:
		assert_bool(pausa.visible).override_failure_message("El menú de pausa no se mostró").is_true()


func test_PUI_112_menu_de_pausa_tiene_reanudar_configuracion_y_salir() -> void:
	if not (await _abrir_pausa()):
		return
	var textos := _botones_visibles_en_arbol()
	for esperado in ["REANUDAR", "CONFIGURACION", "SALIR"]:
		assert_bool(textos.has(esperado)).override_failure_message(
			"Falta el botón %s en el menú de pausa. Botones visibles: %s" % [esperado, textos]).is_true()
