using Godot;

/// <summary>
/// Valores de balance centralizados. Todo lo que afecta la dificultad, el daño,
/// la economía de la partida y la moneda global vive aquí para poder ajustarlo
/// en un solo lugar.
///
/// Modelo de dificultad inspirado en Risk of Rain 2:
///   coeficiente = (1 + minutos * 0.0506 * ValorDificultad * modificadorJugador) * FactorMapa^mapasCompletados
///   nivelEnemigo = 1 + (coeficiente - 1) / 0.33
///   vida enemiga  +30% por nivel, daño enemigo +20% por nivel
///   costo de cofres/tienda = costoBase * coeficiente
/// </summary>
public static class Balance
{
	// ───────────────────────── Dificultad ─────────────────────────
	/// <summary>1 = fácil, 2 = normal, 3 = difícil. Multiplica el crecimiento por tiempo.</summary>
	public const float DifficultyValue = 2.5f;
	public const float TimeFactorPerMinute = 0.0506f;
	/// <summary>Multiplicador del coeficiente por cada mapa completado.</summary>
	public const float StageFactor = 1.25f;
	public const float CoefficientPerLevel = 0.33f;

	public static float Coefficient(float totalRunSeconds, int stagesCompleted, float playerDifficultyModifier)
	{
		float minutes = Mathf.Max(0f, totalRunSeconds) / 60f;
		float timeFactor = TimeFactorPerMinute * DifficultyValue * Mathf.Max(0.1f, playerDifficultyModifier);
		return (1f + minutes * timeFactor) * Mathf.Pow(StageFactor, Mathf.Max(0, stagesCompleted));
	}

	public static float EnemyLevel(float coefficient) => 1f + Mathf.Max(0f, coefficient - 1f) / CoefficientPerLevel;

	public static float EnemyHpMultiplier(float level)     => 1f + 0.30f * (level - 1f);
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

	/// <summary>Probabilidad de que un enemigo aparezca como élite.</summary>
	public static float EliteChance(float baseProbability, float totalRunSeconds)
		=> Mathf.Min(baseProbability + 0.005f * (totalRunSeconds / 60f), 0.25f);

	// Generación de enemigos
	public const float MinSpawnInterval = 0.12f;
	public static float SpawnInterval(float baseInterval, float minutesInMap, float playerDifficultyModifier)
		=> Mathf.Max(MinSpawnInterval, baseInterval / (1f + 0.12f * minutesInMap * Mathf.Max(0.1f, playerDifficultyModifier)));

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
