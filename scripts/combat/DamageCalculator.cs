using Godot;

public readonly struct DamageRoll
{
	public readonly float Amount;
	public readonly bool IsCrit;

	public DamageRoll(float amount, bool isCrit)
	{
		Amount = amount;
		IsCrit = isCrit;
	}
}

/// <summary>
/// Cálculo único del daño del jugador. Antes cada arma lo calculaba por su cuenta
/// (y las orbitales ignoraban bonus de tipo). Ahora todas pasan por aquí:
///   daño = base * escalaGlobal * personaje * estadísticas * pasiva * bonus de tipo * afinidad * crítico
/// </summary>
public static class DamageCalculator
{
	public static float TypeBonus(PlayerStats stats, string weaponType) => weaponType switch
	{
		"melee"  => stats.MeleeDamageBonus,
		"ranged" => stats.RangedDamageBonus,
		"magic"  => stats.MagicDamageBonus,
		_ => 0f,
	};

	/// <summary>Daño sin crítico (útil para mostrar en UI o para efectos de área).</summary>
	public static float BaseForWeapon(Player player, WeaponData weapon, float rawDamage)
	{
		if (player == null) return rawDamage;
		PlayerStats stats = player.Stats;
		float dmg = rawDamage * Balance.PlayerDamageScale;
		dmg *= player.Character != null ? player.Character.DamageMultiplier : 1f;
		dmg *= Mathf.Max(0f, stats.DamageMultiplier);
		dmg *= player.PassiveDamageBonus;
		if (weapon != null)
		{
			dmg *= 1f + TypeBonus(stats, weapon.WeaponType);
			dmg *= 1f + AffinityBonus.For(player, weapon).Damage;
		}
		return Mathf.Max(0f, dmg);
	}

	public static DamageRoll ForWeapon(Player player, WeaponData weapon, float rawDamage)
	{
		float dmg = BaseForWeapon(player, weapon, rawDamage);
		return RollCrit(player, dmg);
	}

	public static DamageRoll RollCrit(Player player, float damage)
	{
		if (player == null) return new DamageRoll(damage, false);
		// Paso sombrío (dash de Bosch): crítico garantizado
		if (player.ConsumeGuaranteedCrit())
			return new DamageRoll(damage * Mathf.Max(1f, player.Stats.CritDamage), true);
		float chance = Mathf.Clamp(player.Stats.CritChance, 0f, 1f);
		if (chance > 0f && GD.Randf() < chance)
			return new DamageRoll(damage * Mathf.Max(1f, player.Stats.CritDamage), true);
		return new DamageRoll(damage, false);
	}

	/// <summary>Daño base que usan los objetos ("400% de daño base").</summary>
	public static float ItemBaseDamage(Player player)
	{
		if (player == null) return Balance.ItemBaseDamage(1) * Balance.PlayerDamageScale;
		int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
		float dmg = Balance.ItemBaseDamage(level) * Balance.PlayerDamageScale;
		dmg *= player.Character != null ? player.Character.DamageMultiplier : 1f;
		dmg *= Mathf.Max(0f, player.Stats.DamageMultiplier);
		dmg *= player.PassiveDamageBonus;
		return dmg;
	}
}
