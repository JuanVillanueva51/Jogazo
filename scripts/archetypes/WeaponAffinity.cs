using System.Collections.Generic;
using System.Text;
using Godot;

/// <summary>
/// Etiquetas derivadas de WeaponData para clasificar armas más allá de su tipo
/// (melee / ranged / magic).
/// </summary>
public static class WeaponTags
{
	public const string Orbital  = "orbital";
	public const string Zona     = "zona";
	public const string Objetivo = "objetivo";   // aparecen sobre el enemigo (dagas, rayo, meteoro)
	public const string Proyectil = "proyectil";

	public static bool Has(WeaponData weapon, string tag)
	{
		if (weapon == null) return false;
		return tag switch
		{
			Orbital  => weapon.IsOrbital,
			Zona     => weapon.IsZoneWeapon,
			Objetivo => weapon.IsOnEnemyWeapon,
			Proyectil => !weapon.IsOrbital && !weapon.IsZoneWeapon && !weapon.IsOnEnemyWeapon && weapon.Speed > 0f,
			_ => false,
		};
	}

	public static string TypeDisplayName(string weaponType) => weaponType switch
	{
		"melee"  => "cuerpo a cuerpo",
		"ranged" => "a distancia",
		"magic"  => "mágicas",
		_ => weaponType ?? "",
	};
}

/// <summary>
/// Afinidad sutil de un personaje o clase con cierto tipo de arma.
/// Da un pequeño bonus, nunca obliga a usar esas armas.
/// </summary>
public sealed class WeaponAffinity
{
	public string Label;          // texto para la UI, p. ej. "Armas cuerpo a cuerpo"
	public string WeaponType;     // "melee" / "ranged" / "magic" o null = cualquiera
	public string Tag;            // WeaponTags.* o null = cualquiera
	public float Damage;          // +% daño
	public float AttackSpeed;     // +% velocidad de ataque
	public float Size;            // +% tamaño
	public float ProjectileSpeed; // +% velocidad de proyectil

	public bool Matches(WeaponData weapon)
	{
		if (weapon == null) return false;
		if (WeaponType != null && weapon.WeaponType != WeaponType) return false;
		if (Tag != null && !WeaponTags.Has(weapon, Tag)) return false;
		return true;
	}

	public string Describe()
	{
		var parts = new List<string>();
		if (Damage != 0f)          parts.Add($"{Signed(Damage)} daño");
		if (AttackSpeed != 0f)     parts.Add($"{Signed(AttackSpeed)} vel. ataque");
		if (Size != 0f)            parts.Add($"{Signed(Size)} tamaño");
		if (ProjectileSpeed != 0f) parts.Add($"{Signed(ProjectileSpeed)} vel. proyectil");
		return $"{Label}: {string.Join(", ", parts)}";
	}

	private static string Signed(float value)
	{
		int pct = Mathf.RoundToInt(value * 100f);
		return pct >= 0 ? $"+{pct}%" : $"{pct}%";
	}

	public static string DescribeAll(IEnumerable<WeaponAffinity> affinities)
	{
		if (affinities == null) return "";
		var sb = new StringBuilder();
		foreach (var a in affinities)
		{
			if (sb.Length > 0) sb.Append('\n');
			sb.Append(a.Describe());
		}
		return sb.ToString();
	}
}

/// <summary>Suma de bonus de afinidad para un arma concreta.</summary>
public struct AffinityBonus
{
	public float Damage;
	public float AttackSpeed;
	public float Size;
	public float ProjectileSpeed;

	public void Add(WeaponAffinity a)
	{
		Damage += a.Damage;
		AttackSpeed += a.AttackSpeed;
		Size += a.Size;
		ProjectileSpeed += a.ProjectileSpeed;
	}

	public static AffinityBonus For(Player player, WeaponData weapon)
	{
		var bonus = new AffinityBonus();
		if (player == null || weapon == null) return bonus;
		if (player.Character?.Affinities != null)
			foreach (var a in player.Character.Affinities)
				if (a.Matches(weapon)) bonus.Add(a);
		if (player.ClassInfo?.Affinities != null)
			foreach (var a in player.ClassInfo.Affinities)
				if (a.Matches(weapon)) bonus.Add(a);
		return bonus;
	}

	/// <summary>Peso extra para ofrecer armas afines en la subida de nivel (sesgo sutil).</summary>
	public static float OfferWeight(CharacterData character, ClassData cls, WeaponData weapon)
	{
		float weight = 1f;
		if (character?.Affinities != null)
			foreach (var a in character.Affinities)
				if (a.Matches(weapon)) { weight += 0.5f; break; }
		if (cls?.Affinities != null)
			foreach (var a in cls.Affinities)
				if (a.Matches(weapon)) { weight += 0.5f; break; }
		return weight;
	}
}
