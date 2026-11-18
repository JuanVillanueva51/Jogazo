# Sistemas de objetos, cofres, tienda, progreso, balance, personajes y configuración

Guía rápida para el equipo. Todo el código nuevo está en `scripts/` y los valores de balance en `scripts/balance/Balance.cs`.

## Controles nuevos

| Acción | Tecla | Uso |
|---|---|---|
| `interact` | E (o clic izquierdo) | Abrir cofres y la tienda |
| `use_item` | Q | Usar el objeto activo |
| `dash` | Espacio | Dash (cada personaje tiene el suyo) |

Todas (menos Esc) se pueden cambiar en **Configuración → Controles**.

## Objetos (`scripts/items/`)

- **Pasivos**: se apilan (cada copia suma su efecto).
- **Activos**: solo se puede llevar uno a la vez, se usan con Q y tienen enfriamiento. Si recoges otro, el anterior cae al suelo.
- **Rarezas**: Común (blanco), Poco común (verde), Raro (azul), Legendario (dorado) y **Maldito** (morado): un beneficio fuerte a cambio de una desventaja, como los objetos lunares de Risk of Rain 2.
- Catálogo completo: `ItemLibrary.cs`. Lógica de cada efecto: `ItemEffects.cs`.

### Agregar un objeto nuevo

1. Si solo cambia estadísticas, agrega una línea en `ItemLibrary.cs` con el helper `Stat(...)`:

   ```csharp
   Stat("mi_objeto", "Mi Objeto", "+5% de daño por copia.",
	   ItemRarity.Comun, false, (s, n) => s.DamageMultiplier += 0.05f * n),
   ```

2. Si necesita lógica, crea una clase que herede de `ItemEffect` en `ItemEffects.cs` y sobrescribe el gancho que necesites: `OnEnemyHit`, `OnEnemyKilled`, `OnProcess`, `ModifyIncomingDamage`, `TryPreventDeath` o, para un activo, `Activate`. Después regístrala en `ItemLibrary.cs` con `CreateEffect = () => new MiEfecto()`.
3. El `Id` se guarda en el perfil del jugador: **no lo cambies** una vez publicado.
4. `UnlockedByDefault = true` hace que el objeto esté disponible desde la primera partida. Si no, se compra en el menú **Desbloqueos**.

## Cofres y tienda (`scripts/interactables/`, `scenes/interactables/`)

- `chest.tscn`: arrástralo a un mapa y elige `Tier`.
  - **Pequeno**: 75% común / 21% poco común / 3.5% raro / 0.5% legendario.
  - **Grande**: 70% poco común / 25% raro / 5% legendario.
  - **Equipo**: solo objetos activos.
  - **Maldito**: solo objetos malditos.
  - **Jefe**: lo sueltan los jefes.
- `BaseCost = -1` usa el costo por defecto del tipo, `0` lo hace gratis, `IsFree` lo marca como gratis. El costo escala con la dificultad.
- Ya hay cofres y una tienda colocados en cada mapa, dentro del nodo **Botin** (cerca de las torres de mejora). Muévelos libremente en el editor.
- **Élites**: 25% de probabilidad de soltar un cofre gratis; no es garantizado.
- **Jefes**: siempre sueltan un cofre de jefe.
- `shop_terminal.tscn`: ofrece 3 objetos que se pagan con las **monedas** de la partida. Se pueden cambiar las ofertas pagando.

## Progreso persistente (`scripts/persistence/`)

- **Créditos**: moneda global que se gana jugando.
  - +4 por minuto.
  - +1 por élite.
  - +20 por jefe.
  - +10 por mapa superado.
  - +50 al ganar.
- Los Créditos se guardan al momento y solo se gastan en **Menú principal → Desbloqueos** (`scenes/ui/unlocks_menu.tscn`), donde se desbloquean objetos y armas.
- El perfil guarda: Créditos, objetos y armas desbloqueados, y estadísticas (partidas, victorias, muertes, mejor tiempo).
- Ubicación en Windows: `%APPDATA%\Godot\app_userdata\uabc-roguelike\save\`.
  - `profile.json`: el perfil.
  - `profile.json.bak`: copia del guardado anterior.
  - `runs.jsonl`: historial de partidas.
- Para reiniciar el progreso, borra esa carpeta con el juego cerrado.

### Conectar una base de datos

`IPlayerDataRepository` es el único punto de contacto con el almacenamiento:

1. Crea una clase, por ejemplo `SqlitePlayerDataRepository` o `ApiPlayerDataRepository`, que implemente `Load`, `Save` y `AppendRunHistory`.
2. Devuélvela en `ProfileManager.CreateRepository()`.

`PlayerProfile` es un objeto plano: cada propiedad equivale a una columna, y `RunSummary` a una fila del historial. El resto del juego no cambia.

## Balance (`scripts/balance/Balance.cs`)

- **Dificultad** al estilo de Risk of Rain 2: un coeficiente que crece con el tiempo total de la partida (×1.25 por cada mapa superado) y que define el nivel de los enemigos.
  - Por cada nivel, los enemigos ganan +33% de vida y +20% de daño.
  - El nivel enemigo y la dificultad aparecen en el HUD.
  - **Niveles de dificultad** (Configuración → Juego, se aplican a partidas nuevas; `Balance.Difficulties`):

	| Dificultad | Crecimiento por tiempo | Vida enemiga | Daño enemigo | Aparición | Élites | Créditos |
	|---|---|---|---|---|---|---|
	| Normal | 3.0 | ×1 | ×1 | ×1 | +0% | ×1 |
	| Difícil | 3.6 | ×1.15 | ×1.25 | ×1.2 | +3% | ×1.25 |
	| Pesadilla | 4.5 | ×1.3 | ×1.5 | ×1.4 | +6% | ×1.5 |

- **Enemigos** (más peligrosos que antes):
  - Velocidad y daño base por tipo en cada escena de `scenes/enemies/` (antes todos caminaban a 30 px/s y no alcanzaban al jugador). Rápidos: lobo 90, murciélago 85, vampiro 80. Lentos y fuertes: caballero 55, guardián 50.
  - Golpe de contacto cada 1 s (antes 3 s): `Balance.EnemyContactCooldown`.
  - Aparecen justo fuera de la pantalla, en grupos que crecen con el tiempo (`ExtraSpawnChance`), 35% más seguido (`SpawnRateMultiplier`) y con oleadas cada 2 minutos (el HUD avisa con "¡OLEADA!").
  - Élites desde 5% (+0.8% por minuto, máx. 30%); 15% de enemigos a distancia.
  - **Los enemigos no chocan con muros** (solo entre ellos, capa 2): así no se quedan atorados. Si un élite o jefe muere detrás de un muro, su cofre aparece del lado del jugador.
- **Jefes**: vida × (1 + coeficiente).
- **Élites**: ×3.5 de vida, ×1.5 de daño, ×3 de experiencia y monedas.
- **Daño del jugador**: lo calcula un solo módulo, `DamageCalculator`, para todas las armas, incluidas las orbitales. El antiguo `DamageMultiplier = 6` se sustituyó por `PlayerDamageScale = 3`, compensado con enemigos que escalan menos al inicio de cada mapa.
- **Nuevas estadísticas**: crítico, armadura, daño recibido, robo de vida (con límite por golpe), espinas y enfriamiento de activos.
- **Torres de mejora**: ahora modifican `Player.BaseStats` y se recalculan junto con los objetos.

## Arquetipos (`scripts/archetypes/`, `scripts/classes/`)

Afinidades **sutiles**: un bonus de entre 5% y 20% con ciertos tipos de arma, más una probabilidad un poco mayor de que esas armas aparezcan al subir de nivel. Cualquier arma se puede usar sin problema. Personajes y clases suman sus afinidades.

### Personajes y dash (`CharacterLibrary.cs`, `DashProfile.cs`, `scripts/player/DashEffects.cs`)

Cada personaje tiene su propio dash. Los marcados como *provisional* usan el sprite de Cimarron con otro tinte; para darles arte propio solo cambia `ScenePath`, `IconPath` y quita `Tint` / `IsPlaceholder`.

| Personaje | Arquetipo | Dash | Afinidad |
|---|---|---|---|
| Cimarron | Tirador ágil | Rodada: 2 cargas, recarga rápida | A distancia |
| Abad | Muro académico | Embestida: daña, empuja y aturde lo que atraviesa (1 carga) | Cuerpo a cuerpo |
| Edwin | Erudito arcano | Parpadeo: teletransporte con explosión arcana | Mágicas |
| Anguiano | Control de área | Onda expansiva: empuja y ralentiza al terminar | Orbitales y de zona |
| Bosch (provisional) | Crítico implacable | Paso sombrío: el siguiente golpe es crítico | Proyectiles |
| El Prefecto (provisional) | Tanque inamovible | Bastión: 60% menos daño 1.5 s | Orbitales y cuerpo a cuerpo |
| La Atleta (provisional) | Velocista | Sprint: 3 cargas cortas, +30% velocidad | Sobre el objetivo |
| La Ingeniera (provisional) | Constructora | Propulsor: deja una mina | Orbitales y de zona |
| El Becario (provisional) | Afortunado | Golpe de suerte: 35% de no gastar carga | Proyectiles |
| La Doctora (provisional) | Sanadora | Paso vital: cura 4% | Mágicas y de zona |
| El Químico (provisional) | Alquimista de área | Estela tóxica: charcos que dañan | De zona y sobre el objetivo |

Para crear un dash nuevo: agrega un valor a `DashStyle` y su efecto en `DashEffects` (`OnStart`, `OnTick` u `OnEnd`).

### Clases (`ClassLibrary.cs`, `ClassPassiveEffects.cs`)

Ahora hay una clase por cada arma. Todas las clases se pueden elegir desde el inicio y empiezan con su arma aunque esa arma esté bloqueada; desbloquearla en el menú solo hace que aparezca al subir de nivel con otras clases.

| Clase | Arquetipo | Arma inicial |
|---|---|---|
| Caballero | Defensor cuerpo a cuerpo | Espada Larga |
| Arquero | Tirador a distancia | Arco |
| Asesino | Golpes rápidos | Dagas |
| Berserker | Peleador de multitudes | Mazo |
| Catalizador | Bola de nieve | Catalizador de Energia |
| Ogro | Coloso furioso | Hacha de Acero |
| Mago Maestro | Hechicero | Libro Magico |
| Caballero con Corcel | Jinete en movimiento | Lanza de Acero |
| Piromante | Fuego persistente | Fireball |
| Electromante | Cadenas eléctricas | Lightning |
| Astrónomo | Artillería celeste | Meteor |
| Técnico Láser | Control rotatorio | Laser |
| Guardián | Tanque defensivo | Shield |
| Caminante Ígneo | Hostigador móvil | Fire Walker |
| Estudiante | Aprendiz versátil | Lapiz |
| Pistolero | Francotirador crítico | Revolver |
| Nigromante | Drenaje de vida | Hueso |
| Segador | Ejecutor | Guadaña |
| Cazador | Perforación | Hacha Bumerán |
| Aeromante | Control de masas | Tornado |
| Criomante | Ralentización | Aura de Escarcha |
| Ingeniero | Torretas y artefactos | Dron de Vigilancia |

Las pasivas con lógica (quemar, estrellas, empujar...) son `ItemEffect` que se agregan al inventario como **efectos innatos** (`PlayerInventory.AddInnateEffect`): usan los mismos ganchos que los objetos, pero no ocupan espacio ni se guardan. Agrega siempre las clases nuevas **al final** de la lista: el índice de la clase elegida se guarda en `GameManager`.

## Armas nuevas (`scripts/weapons/`, `scenes/weapons/`)

| Arma | Tipo | Cómo funciona | Desbloqueo |
|---|---|---|---|
| Guadaña | Cuerpo a cuerpo | Da una vuelta completa alrededor del jugador | 120 créditos |
| Hacha Bumerán | A distancia, proyectil | Sale y regresa; golpea de ida y de vuelta | 100 créditos |
| Tornado | Mágica, proyectil | Avanza lento, atrapa y daña varias veces | 150 créditos |
| Aura de Escarcha | Mágica, de zona | Aura permanente que daña y ralentiza | 150 créditos |
| Dron de Vigilancia | A distancia, orbital | Orbita y dispara al enemigo más cercano | 180 créditos |

Las armas nuevas implementan `IWeaponSpawn`: `WeaponManager` les entrega en `Launch()` el daño, tamaño, velocidad y perforación ya calculados. Los escudos (Shield) ahora también golpean y empujan al chocar, para que el Guardián pueda matar desde el inicio. Los íconos pueden venir de una hoja de sprites con `WeaponData.IconRegion` (`WeaponLibrary.GetIcon`).

## HUD (`scripts/gui/Hud.cs`, `DashIndicator.cs`)

- Vida con número (parpadea en rojo con poca vida).
- Indicador de dash: tecla, una barra por carga (la que se recarga se va llenando) y nombre del dash. También aparecen barritas debajo del personaje mientras recarga (se pueden apagar).
- Monedas: moneda girando, número que "salta" al cambiar y ganancia reciente (+N); debajo, los Créditos.
- Nivel: insignia junto a una barra de experiencia animada con el texto `XP actual / necesaria`, destello y aviso "¡NIVEL N!" al subir.

## Pausa y configuración (`scripts/settings/`)

- El menú de pausa ahora tiene tres botones con su propio texto: **Reanudar**, **Configuración** y **Salir**.
- **Configuración** se abre desde el menú principal (`scenes/ui/configuration_menu.tscn`) o desde la pausa. Los cambios se aplican y guardan al momento en `user://settings.cfg` (`%APPDATA%\Godot\app_userdata\uabc-roguelike\settings.cfg`).
  - **Audio**: volumen general, música y efectos (se crean los buses `Musica` y `Efectos`; los reproductores nuevos se asignan solos), silenciar.
  - **Video**: ventana / pantalla completa sin bordes / pantalla completa exclusiva, tamaño de ventana, límite de FPS y VSync.
  - **Controles**: cambiar la tecla de cada acción (si otra acción ya la usaba, intercambian teclas) y restaurar.
  - **Juego**: dificultad (Normal, Difícil, Pesadilla), cargas de dash bajo el personaje, avisos de objetos, nivel enemigo y FPS.

## Límites de los mapas (`scripts/managers/MapBounds.cs`)

Cada mapa tiene un nodo **LimitesMapa** con un rectángulo `CameraLimits` (se ve en azul en el editor). Al empezar, la cámara del jugador queda limitada a ese rectángulo: ya no se ve el espacio oscuro fuera del mapa.

- Los muros invisibles siguen siendo `StaticBody2D` (capas 1 y 3). En el tutorial se acercaron un poco al borde pintado para que el personaje no quede pegado al borde de la pantalla. En el mapa 2 se agregó `LimitesMapa/MuroDerecho`, que cierra un hueco arriba a la derecha por donde se podía salir del mapa.
- Si cambias el tamaño de un mapa, ajusta `CameraLimits` para que cubra solo el área pintada.
- Los enemigos aparecen dentro de este rectángulo, justo fuera de la pantalla.

## Menú principal: Demo e Historia

- **DEMO** (junto a JUGAR): partida corta y caótica. Se elige personaje y clase, se pasa por el tutorial y luego **un solo mapa de combate**; al vencer al jefe se gana (`BossEnemy`). Todos los valores están en `Balance.cs`, sección "Modo demo":
  - El jefe aparece a los 2:30 (`DemoBossTime`) y la dificultad sube 3.5 veces más rápido (`DemoTimeScaling`).
  - 2.5 veces más enemigos, grupos más grandes, oleadas cada 40 s, +10% de élites y 50% de que un élite suelte cofre.
  - Experiencia ×2.5, monedas ×2, cofres a mitad de precio, torres de mejora en ~5 s.
  - Da la mitad de Créditos (`DemoCreditMultiplier`).
  - El modo se guarda en `GameManager.IsDemo` (el menú pone `NextRunIsDemo` antes de elegir personaje).
- **HISTORIA**: pantalla `scenes/ui/historia.tscn` (`HistoryScreen.cs`). Reproduce la narración `sprites/music/historia_introduccion.mp3` con subtítulos sincronizados, barra para adelantar, pausa (Espacio), texto completo (clic en una línea para escuchar desde ahí) y Escape para volver. El texto y los tiempos de cada línea están en `scripts/gui/HistoryText.cs`.

## Nivel inicial (tutorial)

- Más pequeño: los muros invisibles y `LimitesMapa` encierran solo la zona de la casa y el campo de tierra; la salida (`AreaTransicion`) quedó a la derecha del campo y la torre de mejora dentro del campo.
- **Guía** (`GuiaTutorial`, `scripts/tutorialScripts/TutorialGuide.cs`): abajo de la pantalla muestra el paso actual (moverse → abrir el cofre → recoger el objeto → dash → torre, opcional → salida) con las teclas configuradas, y una flecha (`ObjectivePointer`) que apunta al cofre, al objeto, a la torre o a la salida. En la salida hay flechas animadas en el suelo.
- En los mapas de combate, el HUD muestra "Jefe en m:ss", una flecha hacia el jefe cuando aparece y, después de vencerlo, otra hacia la salida.
