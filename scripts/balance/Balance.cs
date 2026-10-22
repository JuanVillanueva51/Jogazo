using Godot;

/// <summary>
/// Valores de balance centralizados. Todo lo que afecta la dificultad, el daño,
/// la economía de la partida y la moneda global vive aquí para poder ajustarlo
/// en un solo lugar.
///
/// Modelo de dificultad inspirado en Risk of Rain 2:
///   coeficiente = (1 + minutos * 0.0506 * ValorDificultad * modificadorJugador) * FactorMapa^mapasCompletados
///   nivelEnemigo = 1 + (coeficiente - 1) / 0.33
///   vida enemiga  +33% por nivel, daño enemigo +20% por nivel
///   costo de cofres/tienda = costoBase * coeficiente
/// El ValorDificultad sale de la dificultad elegida en Configuración → Juego (Normal, Difícil, Pesadilla).
/// </summary>
public static class Balance
{
	// ───────────────────────── Dificultad ─────────────────────────
	/// <summary>Valor de dificultad por defecto (Normal). En RoR2: 1 = fácil, 2 = normal, 3 = difícil.</summary>
	public const float DifficultyValue = 0.1f; //original 3.0
	public const float TimeFactorPerMinute = 0.0506f;
	/// <summary>Multiplicador del coeficiente por cada mapa completado.</summary>
	public const float StageFactor = 1.25f;
	public const float CoefficientPerLevel = 0.33f;

	/// <summary>Niveles de dificultad que el jugador elige en Configuración → Juego (aplican a partidas nuevas).</summary>
	public static readonly DifficultyPreset[] Difficulties =
	{
		new DifficultyPreset {
			Name = "Normal", Description = "La experiencia pensada para el juego.",
			ScalingValue = 3.0f, EnemyHp = 1.00f, EnemyDamage = 1.00f, SpawnRate = 1.00f, EliteBonus = 0.00f, CreditMultiplier = 1.00f,
		},
		new DifficultyPreset {
			Name = "Difícil", Description = "Enemigos más duros y más numerosos. +25% de Créditos.",
			ScalingValue = 3.6f, EnemyHp = 1.15f, EnemyDamage = 1.25f, SpawnRate = 1.20f, EliteBonus = 0.03f, CreditMultiplier = 1.25f,
		},
		new DifficultyPreset {
			Name = "Pesadilla", Description = "Solo para expertos. +50% de Créditos.",
			ScalingValue = 4.5f, EnemyHp = 1.30f, EnemyDamage = 1.50f, SpawnRate = 1.40f, EliteBonus = 0.06f, CreditMultiplier = 1.50f,
		},
	};

	public static DifficultyPreset GetDifficulty(int index)
		=> Difficulties[Mathf.Clamp(index, 0, Difficulties.Length - 1)];

	public static float Coefficient(float totalRunSeconds, int stagesCompleted, float playerDifficultyModifier, float difficultyValue = DifficultyValue)
	{
		float minutes = Mathf.Max(0f, totalRunSeconds) / 60f;
		float timeFactor = TimeFactorPerMinute * difficultyValue * Mathf.Max(0.1f, playerDifficultyModifier);
		return (1f + minutes * timeFactor) * Mathf.Pow(StageFactor, Mathf.Max(0, stagesCompleted));
	}

	public static float EnemyLevel(float coefficient) => 1f + Mathf.Max(0f, coefficient - 1f) / CoefficientPerLevel;

	public static float EnemyHpMultiplier(float level)     => 1f + 0.33f * (level - 1f);
	public static float EnemyDamageMultiplier(float level) => 1f + 0.20f * (level - 1f);
	public static float EnemySpeedMultiplier(float level)  => Mathf.Min(1f + 0.03f * (level - 1f), 1.6f);
	public static float EnemyXpMultiplier(float level)     => 1f + 0.25f * (level - 1f);
	public static int   EnemyCoinValue(float level)        => Mathf.Max(1, Mathf.RoundToInt(1f + 0.25f * (level - 1f)));

	// Jefes: vida = base * (1 + coeficiente)
	public static float BossHpMultiplier(float coefficient) => 1f + coefficient;
	public const float BossXpMultiplier = 20f;
	public const int   BossCoinMultiplier = 25;

	// Élites (estilo RoR2: más vida, algo más de daño)
	public const float EliteHpMultiplier     = 3.5f;
	public const float EliteDamageMultiplier = 1.5f;
	public const float EliteSpeedMultiplier  = 1.1f;
	public const float EliteXpMultiplier     = 3f;
	public const int   EliteCoinMultiplier   = 3;
	public const float EliteVisualScale      = 1.5f;
	public static readonly Color EliteTint   = new Color(1f, 0.45f, 0.45f);

	/// <summary>Probabilidad base de élite al iniciar la partida (las torres pueden subirla).</summary>
	public const float BaseEliteProbability = 0.05f;
	/// <summary>Probabilidad de que un enemigo aparezca como élite.</summary>
	public static float EliteChance(float baseProbability, float totalRunSeconds)
		=> Mathf.Min(baseProbability + 0.008f * (totalRunSeconds / 60f), 0.30f);

	// ───────────────────────── Enemigos ─────────────────────────
	/// <summary>
	/// Segundos entre golpes de contacto de un mismo enemigo (antes 3 s, por eso casi no hacían daño).
	/// La velocidad y el daño base de cada enemigo están en su escena (scenes/enemies/*.tscn).
	/// </summary>
	public const float EnemyContactCooldown = 1.0f;
	/// <summary>Distancia a la que un enemigo cuerpo a cuerpo golpea al jugador.</summary>
	public const float EnemyContactRange = 25f;
	public const float RangedSpawnChance = 0.15f;

	// Generación de enemigos
	public const float MinSpawnInterval = 0.09f;
	/// <summary>Multiplica la frecuencia de aparición en todos los mapas (1.35 = 35% más enemigos).</summary>
	public const float SpawnRateMultiplier = 1.35f;
	public static float SpawnInterval(float baseInterval, float minutesInMap, float playerDifficultyModifier, float spawnRate = 1f)
		=> Mathf.Max(MinSpawnInterval,
			baseInterval / (SpawnRateMultiplier * Mathf.Max(0.1f, spawnRate))
			/ (1f + 0.15f * minutesInMap * Mathf.Max(0.1f, playerDifficultyModifier)));

	/// <summary>Probabilidad de que cada aparición traiga un enemigo extra (grupos que crecen con el tiempo).</summary>
	public static float ExtraSpawnChance(float minutesInMap) => Mathf.Min(0.6f, 0.07f * minutesInMap);
	public const int MaxGroupSize = 4;

	// Oleadas: cada cierto tiempo aparecen muchos enemigos seguidos
	public const float WaveCooldown = 120f;
	public const float WaveDuration = 25f;
	public const float WaveSpawnInterval = 0.1f;
	/// <summary>Los enemigos aparecen justo fuera de la pantalla, a esta distancia del borde.</summary>
	public const float OffscreenSpawnMargin = 40f;

	/// <summary>Segundos en cada mapa antes de que aparezca el jefe (modo normal).</summary>
	public const float BossSpawnTime = 420f; //original 420f

	// ───────────────────────── Modo demo ─────────────────────────
	// Partida corta y caótica: tutorial + un solo mapa de combate; al vencer al jefe se gana.
	/// <summary>El jefe del modo demo aparece a los 2:30 (en vez de 7:00).</summary>
	public const float DemoBossTime = 150f;
	/// <summary>La dificultad sube así de rápido respecto al modo normal (3.5 = 3.5 veces).</summary>
	public const float DemoTimeScaling = 3.5f;
	/// <summary>Multiplica la frecuencia de aparición de enemigos.</summary>
	public const float DemoSpawnRate = 2.5f;
	/// <summary>Probabilidad extra de que cada aparición traiga un grupo.</summary>
	public const float DemoExtraSpawnBonus = 0.25f;
	public const int   DemoMaxAliveEnemies = 320;
	public const float DemoWaveCooldown = 40f;
	public const float DemoWaveDuration = 15f;
	public const float DemoEliteBonus = 0.10f;
	/// <summary>Probabilidad de que un élite suelte cofre en el demo (normal: EliteChestDropChance).</summary>
	public const float DemoEliteChestChance = 0.5f;
	public const float DemoXpMultiplier = 2.5f;
	public const float DemoGoldMultiplier = 2f;
	public const float DemoChestCostMultiplier = 0.5f;
	/// <summary>Las torres de mejora se cargan más rápido (15 s × 0.35 ≈ 5 s).</summary>
	public const float DemoTowerTimeMultiplier = 0.35f;
	/// <summary>El demo da la mitad de Créditos para que no sea la forma más rápida de conseguirlos.</summary>
	public const float DemoCreditMultiplier = 0.5f;

	// ───────────────────────── Jugador ─────────────────────────
	/// <summary>Escala global del daño de las armas (sustituye al antiguo DamageMultiplier = 6).</summary>
	public const float PlayerDamageScale = 3f;
	/// <summary>Daño base usado por los objetos ("300% de daño base").</summary>
	public static float ItemBaseDamage(int playerLevel) => 20f + 3f * (Mathf.Max(1, playerLevel) - 1);
	public const float MaxHpPerLevel  = 3f;
	public const float BaseCritChance = 0.03f;
	public const float BaseCritDamage = 2f;
	/// <summary>El robo de vida no puede curar más que este % de la vida máxima por golpe.</summary>
	public const float LifeStealCapPerHit = 0.02f;
	/// <summary>Reducción de daño por armadura (fórmula de RoR2): 100 / (100 + armadura).</summary>
	public static float ArmorMultiplier(float armor)
		=> armor >= 0f ? 100f / (100f + armor) : 2f - 100f / (100f - armor);

	// Pasivas de clase
	public const float CatalystPerKill       = 0.02f;
	public const float CatalystMax           = 3f;
	public const float BerserkerPerEnemy     = 0.05f;
	public const float BerserkerMaxBonus     = 0.5f;
	public const float OgreMaxMissingHpBonus = 0.75f;

	// Experiencia
	public const float FirstLevelXp = 100f;
	public static float NextXpRequirement(float currentRequirement) => currentRequirement * 1.12f + 20f;

	// ───────────────────────── Economía de la partida (monedas) ─────────────────────────
	public static float CostScale(float coefficient) => Mathf.Max(1f, coefficient);

	public static int ChestBaseCost(ChestTier tier) => tier switch
	{
		ChestTier.Pequeno => 25,
		ChestTier.Grande  => 50,
		ChestTier.Equipo  => 35,
		ChestTier.Maldito => 40,
		ChestTier.Jefe    => 0,
		_ => 25,
	};

	public static int ShopBasePrice(ItemRarity rarity, ItemKind kind)
	{
		int price = rarity switch
		{
			ItemRarity.Comun      => 40,
			ItemRarity.PocoComun  => 80,
			ItemRarity.Raro       => 160,
			ItemRarity.Legendario => 320,
			ItemRarity.Maldito    => 100,
			_ => 40,
		};
		return kind == ItemKind.Activo ? Mathf.RoundToInt(price * 1.2f) : price;
	}

	public const int   ShopRerollBaseCost  = 15;
	public const float ShopRerollGrowth    = 1.5f;
	public const float ShopActiveSlotChance = 0.2f;
	public const float ShopCursedSlotChance = 0.1f;

	/// <summary>Los élites NO garantizan cofre; los jefes sí.</summary>
	public const float EliteChestDropChance = 0.25f;
	public const float EliteChestIsLargeChance = 0.15f;
	public const float EliteChestIsEquipmentChance = 0.10f;

	// ───────────────────────── Moneda global (Créditos) ─────────────────────────
	public const string MetaCurrencyName = "Créditos";
	public const int CreditsPerMinute       = 4;
	public const int CreditsPerEliteKill    = 1;
	public const int CreditsPerBossKill     = 20;
	public const int CreditsPerStageCleared = 10;
	public const int CreditsVictoryBonus    = 50;

	public static int UnlockCost(ItemRarity rarity, ItemKind kind)
	{
		int cost = rarity switch
		{
			ItemRarity.Comun      => 40,
			ItemRarity.PocoComun  => 80,
			ItemRarity.Raro       => 150,
			ItemRarity.Legendario => 300,
			ItemRarity.Maldito    => 120,
			_ => 50,
		};
		return kind == ItemKind.Activo ? cost + 20 : cost;
	}
}

/// <summary>Un nivel de dificultad (ver Balance.Difficulties).</summary>
public sealed class DifficultyPreset
{
	public string Name;
	public string Description;
	/// <summary>Valor de dificultad de RoR2: qué tan rápido crece el coeficiente con el tiempo.</summary>
	public float ScalingValue = Balance.DifficultyValue;
	public float EnemyHp = 1f;
	public float EnemyDamage = 1f;
	/// <summary>Multiplica la frecuencia de aparición de enemigos.</summary>
	public float SpawnRate = 1f;
	/// <summary>Probabilidad de élite adicional.</summary>
	public float EliteBonus;
	/// <summary>Multiplica los Créditos ganados en la partida.</summary>
	public float CreditMultiplier = 1f;
}
