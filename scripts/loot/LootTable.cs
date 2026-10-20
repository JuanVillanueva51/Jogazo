using System.Collections.Generic;
using Godot;

/// <summary>
/// Tiradas de botín: qué rareza sale de cada cofre y qué objeto concreto.
/// Solo se eligen objetos DESBLOQUEADOS en el perfil del jugador.
/// Probabilidades basadas en Risk of Rain 2 (cofre pequeño 79/20/1, cofre grande 80/20).
/// </summary>
public static class LootTable
{
	// Pesos: Común, Poco común, Raro, Legendario
	private static readonly float[] SmallChestWeights = { 75f, 21f, 3.5f, 0.5f };
	private static readonly float[] LargeChestWeights = { 0f, 70f, 25f, 5f };
	private static readonly float[] BossChestWeights  = { 0f, 45f, 40f, 15f };
	private static readonly float[] EquipmentWeights  = { 50f, 30f, 15f, 5f };
	private static readonly float[] ShopWeights       = { 60f, 30f, 9f, 1f };

	public static float[] WeightsFor(ChestTier tier) => tier switch
	{
		ChestTier.Pequeno => SmallChestWeights,
		ChestTier.Grande  => LargeChestWeights,
		ChestTier.Jefe    => BossChestWeights,
		ChestTier.Equipo  => EquipmentWeights,
		_ => SmallChestWeights,
	};

	/// <summary>
	/// Tira una rareza. La suerte da tiradas extra y se queda con la mejor
	/// (la parte decimal es probabilidad de una tirada extra más).
	/// </summary>
	public static ItemRarity RollRarity(float[] weights, float luck)
	{
		int rolls = 1 + Mathf.Max(0, Mathf.FloorToInt(luck));
		float frac = luck - Mathf.Floor(luck);
		if (luck > 0f && frac > 0f && GD.Randf() < frac) rolls++;

		ItemRarity best = RollOnce(weights);
		for (int i = 1; i < rolls; i++)
		{
			ItemRarity r = RollOnce(weights);
			if ((int)r > (int)best) best = r;
		}
		return best;
	}

	private static ItemRarity RollOnce(float[] weights)
	{
		float total = 0f;
		for (int i = 0; i < weights.Length; i++) total += weights[i];
		if (total <= 0f) return ItemRarity.Comun;
		float roll = GD.Randf() * total;
		for (int i = 0; i < weights.Length; i++)
		{
			roll -= weights[i];
			if (roll <= 0f) return (ItemRarity)i;
		}
		return (ItemRarity)(weights.Length - 1);
	}

	/// <summary>
	/// Objeto aleatorio desbloqueado de la rareza y tipo pedidos. Si no hay ninguno,
	/// baja de rareza (Legendario → Raro → ...). Devuelve null si no hay nada disponible.
	/// </summary>
	public static ItemDefinition RollItem(ItemRarity rarity, ItemKind kind, ICollection<string> exclude = null)
	{
		ItemRarity current = rarity;
		while (true)
		{
			var pool = Pool(current, kind, exclude);
			if (pool.Count > 0) return pool[GD.RandRange(0, pool.Count - 1)];
			if (!RarityInfo.TryLower(current, out current)) break;
		}
		// Último recurso: cualquier objeto desbloqueado del mismo tipo (sin malditos)
		var any = new List<ItemDefinition>();
		foreach (var def in ItemLibrary.All)
			if (def.Kind == kind && def.Rarity != ItemRarity.Maldito && IsAvailable(def, exclude))
				any.Add(def);
		return any.Count > 0 ? any[GD.RandRange(0, any.Count - 1)] : null;
	}

	/// <summary>Objeto maldito desbloqueado del tipo pedido, o null (no baja a otras rarezas).</summary>
	public static ItemDefinition RollCursed(ItemKind kind, ICollection<string> exclude = null)
	{
		var pool = Pool(ItemRarity.Maldito, kind, exclude);
		return pool.Count > 0 ? pool[GD.RandRange(0, pool.Count - 1)] : null;
	}

	public static List<ItemDefinition> Pool(ItemRarity rarity, ItemKind kind, ICollection<string> exclude = null)
	{
		var pool = new List<ItemDefinition>();
		foreach (var def in ItemLibrary.All)
			if (def.Rarity == rarity && def.Kind == kind && IsAvailable(def, exclude))
				pool.Add(def);
		return pool;
	}

	private static bool IsAvailable(ItemDefinition def, ICollection<string> exclude)
	{
		if (def.Hidden) return false;
		if (exclude != null && exclude.Contains(def.Id)) return false;
		return ProfileManager.Instance == null
			? def.UnlockedByDefault
			: ProfileManager.Instance.IsItemUnlocked(def.Id);
	}

	/// <summary>Objeto que sale de un cofre.</summary>
	public static ItemDefinition RollChest(ChestTier tier, float luck)
	{
		switch (tier)
		{
			case ChestTier.Maldito:
			{
				ItemDefinition cursed = null;
				if (GD.Randf() < 0.2f) cursed = RollCursed(ItemKind.Activo);
				cursed ??= RollCursed(ItemKind.Pasivo);
				return cursed ?? RollItem(RollRarity(LargeChestWeights, luck), ItemKind.Pasivo);
			}
			case ChestTier.Equipo:
			{
				if (GD.Randf() < 0.1f)
				{
					var cursed = RollCursed(ItemKind.Activo);
					if (cursed != null) return cursed;
				}
				return RollItem(RollRarity(EquipmentWeights, luck), ItemKind.Activo);
			}
			default:
				return RollItem(RollRarity(WeightsFor(tier), luck), ItemKind.Pasivo);
		}
	}

	/// <summary>Una oferta para la tienda.</summary>
	public static ItemDefinition RollShopItem(float luck, ICollection<string> exclude)
	{
		float r = GD.Randf();
		if (r < Balance.ShopCursedSlotChance)
		{
			var cursed = RollCursed(GD.Randf() < 0.25f ? ItemKind.Activo : ItemKind.Pasivo, exclude)
			             ?? RollCursed(ItemKind.Pasivo, exclude);
			if (cursed != null) return cursed;
		}
		else if (r < Balance.ShopCursedSlotChance + Balance.ShopActiveSlotChance)
		{
			var active = RollItem(RollRarity(EquipmentWeights, luck), ItemKind.Activo, exclude);
			if (active != null) return active;
		}
		return RollItem(RollRarity(ShopWeights, luck), ItemKind.Pasivo, exclude)
		       ?? RollItem(RollRarity(ShopWeights, luck), ItemKind.Pasivo);
	}

	public static int ChestCost(ChestTier tier, int baseCostOverride)
	{
		int baseCost = baseCostOverride >= 0 ? baseCostOverride : Balance.ChestBaseCost(tier);
		if (baseCost <= 0) return 0;
		float scale = GameManager.Instance != null ? GameManager.Instance.CostScale : 1f;
		if (GameManager.Instance != null && GameManager.Instance.IsDemo) scale *= Balance.DemoChestCostMultiplier;
		return Mathf.Max(1, Mathf.RoundToInt(baseCost * scale));
	}

	public static int ShopPrice(ItemDefinition item)
	{
		if (item == null) return 0;
		float scale = GameManager.Instance != null ? GameManager.Instance.CostScale : 1f;
		return Mathf.Max(1, Mathf.RoundToInt(Balance.ShopBasePrice(item.Rarity, item.Kind) * scale));
	}
}
