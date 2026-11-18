using Godot;

/// <summary>
/// Cofres que sueltan los enemigos al morir:
///  - Jefes: SIEMPRE sueltan un cofre de jefe (gratis).
///  - Élites: probabilidad (Balance.EliteChestDropChance), NO garantizado.
///  - Enemigos normales: nunca.
/// </summary>
public static class LootDropper
{
	public const string ChestScenePath = "res://scenes/interactables/chest.tscn";

	public static void HandleKill(KillInfo kill)
	{
		if (kill.IsBoss)
		{
			SpawnFreeChest(ChestTier.Jefe, kill.Position);
			return;
		}
		float chance = GameManager.Instance != null ? GameManager.Instance.EliteChestChance : Balance.EliteChestDropChance;
		if (kill.IsElite && GD.Randf() < chance)
		{
			ChestTier tier = ChestTier.Pequeno;
			float r = GD.Randf();
			if (r < Balance.EliteChestIsEquipmentChance) tier = ChestTier.Equipo;
			else if (r < Balance.EliteChestIsEquipmentChance + Balance.EliteChestIsLargeChance) tier = ChestTier.Grande;
			SpawnFreeChest(tier, kill.Position);
		}
	}

	public static void SpawnFreeChest(ChestTier tier, Vector2 position)
	{
		var scene = GD.Load<PackedScene>(ChestScenePath);
		if (scene == null)
		{
			GD.PushError($"LootDropper: no se encontró {ChestScenePath}");
			return;
		}
		var chest = scene.Instantiate<Chest>();
		chest.Tier = tier;
		chest.IsFree = true;
		// Los enemigos atraviesan muros: si murió detrás de uno, el cofre aparece del lado del jugador
		World.SpawnDeferredReachable(chest, position);
		FloatingText.Spawn(position, "¡Cofre!", RarityInfo.GetColor(ItemRarity.Legendario), 1.2f);
	}
}
