using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Datos de cada personaje jugable. Antes estaban duplicados en CharacterSelect y Player.
/// Cada personaje tiene un arquetipo: es un poco mejor con ciertas armas, pero puede usar cualquiera,
/// y un dash propio que encaja con su estilo.
/// </summary>
public sealed class CharacterData
{
	public string Name;
	public string Description;
	public string Archetype;           // nombre corto del arquetipo
	public string ScenePath;
	public string IconPath;
	/// <summary>Tinte del sprite (para distinguir a los personajes provisionales).</summary>
	public Color  Tint = Colors.White;
	/// <summary>True si todavía usa el sprite de otro personaje.</summary>
	public bool   IsPlaceholder;
	public float  Speed;
	public float  MaxHp;
	public float  DamageMultiplier = 1f;
	public float  BonusCritChance;
	public float  BonusArmor;
	/// <summary>Bonus extra de estadísticas (opcional) y su texto para la UI.</summary>
	public Action<PlayerStats> ExtraStats;
	public string ExtraStatsText;
	public DashProfile Dash = DashProfile.Default();
	public List<WeaponAffinity> Affinities = new();

	public void ApplyBaseStats(PlayerStats stats)
	{
		stats.CritChance += BonusCritChance;
		stats.Armor      += BonusArmor;
		ExtraStats?.Invoke(stats);
	}

	public string StatsText()
	{
		string line2 = $"Vel. {Speed} · Vida {MaxHp} · Daño x{DamageMultiplier:0.00}";
		if (BonusCritChance > 0f) line2 += $" · Crít. +{Mathf.RoundToInt(BonusCritChance * 100f)}%";
		if (BonusArmor > 0f)      line2 += $" · Armadura +{BonusArmor}";
		if (!string.IsNullOrEmpty(ExtraStatsText)) line2 += $" · {ExtraStatsText}";
		string text = $"Arquetipo: {Archetype}\n{line2}";
		if (Dash != null) text += "\n" + Dash.StatsText();
		if (Affinities.Count > 0) text += "\nAfinidad: " + WeaponAffinity.DescribeAll(Affinities);
		return text;
	}
}

public static class CharacterLibrary
{
	private const string CimarronScene = "res://scenes/player/cimarron_char.tscn";
	private const string CimarronIcon  = "res://sprites/Personajes/cimarronIcon.png";

	private static List<CharacterData> _all;

	public static List<CharacterData> All => _all ??= Build();

	public static CharacterData Get(int index)
	{
		var all = All;
		return index >= 0 && index < all.Count ? all[index] : all[0];
	}

	public static Texture2D GetIcon(CharacterData data)
	{
		if (data == null || string.IsNullOrEmpty(data.IconPath) || !ResourceLoader.Exists(data.IconPath)) return null;
		return GD.Load<Texture2D>(data.IconPath);
	}

	// El orden importa: GameManager guarda el índice del personaje elegido.
	private static List<CharacterData> Build() => new()
	{
		new CharacterData {
			Name = "Cimarron", Description = "No hay papel...",
			Archetype = "Tirador ágil",
			ScenePath = CimarronScene, IconPath = CimarronIcon,
			Speed = 200f, MaxHp = 85f, DamageMultiplier = 1.05f,
			Dash = new DashProfile {
				Name = "Rodada", Description = "Esquiva rápida que se recarga pronto.",
				Style = DashStyle.Rodada, Charges = 2, Distance = 125f, Recharge = 5f,
				Color = new Color(0.55f, 0.85f, 1f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Armas a distancia", WeaponType = "ranged", Damage = 0.15f, ProjectileSpeed = 0.10f },
			},
		},
		new CharacterData {
			Name = "Abad", Description = "Muchachoos",
			Archetype = "Muro académico",
			ScenePath = "res://scenes/player/abad_char.tscn", IconPath = "res://sprites/Personajes/abadIcon.png",
			Speed = 125f, MaxHp = 150f, DamageMultiplier = 0.95f, BonusArmor = 10f,
			Dash = new DashProfile {
				Name = "Embestida", Description = "Carga corta que daña, empuja y aturde a lo que atraviesa.",
				Style = DashStyle.Embestida, Charges = 1, Distance = 95f, Duration = 0.3f, Recharge = 2.5f,
				Color = new Color(0.85f, 0.8f, 0.7f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Armas cuerpo a cuerpo", WeaponType = "melee", Damage = 0.15f, Size = 0.15f },
			},
		},
		new CharacterData {
			Name = "Edwin", Description = "Jovenes se me olvidaron los examenes",
			Archetype = "Erudito arcano",
			ScenePath = "res://scenes/player/edwin_char.tscn", IconPath = "res://sprites/Personajes/edwinIcon1.png",
			Speed = 150f, MaxHp = 100f, DamageMultiplier = 1f,
			Dash = new DashProfile {
				Name = "Parpadeo", Description = "Teletransporte corto; deja una explosión arcana donde estabas.",
				Style = DashStyle.Parpadeo, Charges = 2, Distance = 130f, Duration = 0.06f, Recharge = 7f,
				Color = new Color(0.75f, 0.45f, 1f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Armas mágicas", WeaponType = "magic", Damage = 0.15f, AttackSpeed = 0.10f },
			},
		},
		new CharacterData {
			Name = "Anguiano", Description = "Busquenlo en la pagina de ProfesorIvanAnguiano",
			Archetype = "Control de área",
			ScenePath = "res://scenes/player/anguiano_char.tscn", IconPath = "res://sprites/Personajes/anguianoIcon.png",
			Speed = 135f, MaxHp = 120f, DamageMultiplier = 1.1f,
			Dash = new DashProfile {
				Name = "Onda expansiva", Description = "Al terminar empuja y ralentiza a los enemigos alrededor.",
				Style = DashStyle.OndaExpansiva, Charges = 1, Distance = 110f, Recharge = 4f,
				Color = new Color(0.6f, 0.85f, 1f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Armas orbitales", Tag = WeaponTags.Orbital, Damage = 0.20f, Size = 0.10f },
				new WeaponAffinity { Label = "Armas de zona", Tag = WeaponTags.Zona, Damage = 0.20f, Size = 0.10f },
			},
		},
		new CharacterData {
			Name = "Bosch", Description = "Arrastren el lapiz cabezas",
			Archetype = "Crítico implacable",
			// Bosch todavía no tiene sprite propio: usa el de Cimarron con otro tinte.
			ScenePath = CimarronScene, IconPath = CimarronIcon, IsPlaceholder = true,
			Tint = new Color(0.8f, 0.75f, 1f),
			Speed = 180f, MaxHp = 90f, DamageMultiplier = 1f, BonusCritChance = 0.08f,
			Dash = new DashProfile {
				Name = "Paso sombrío", Description = "Tu siguiente golpe en 2 s es crítico seguro.",
				Style = DashStyle.PasoSombrio, Charges = 2, Distance = 130f, Recharge = 3f,
				Color = new Color(0.6f, 0.4f, 0.85f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Proyectiles", Tag = WeaponTags.Proyectil, AttackSpeed = 0.10f, Damage = 0.05f },
			},
		},

		// ── Personajes provisionales (sprite de Cimarron con tinte) ──────────
		new CharacterData {
			Name = "Monica", Description = "PROVISIONAL",
			Archetype = "Tanque inamovible",
			ScenePath = CimarronScene, IconPath = CimarronIcon, IsPlaceholder = true,
			Tint = new Color(0.7f, 0.75f, 0.85f),
			Speed = 115f, MaxHp = 180f, DamageMultiplier = 0.9f, BonusArmor = 15f,
			ExtraStats = s => s.HpRegen += 0.5f, ExtraStatsText = "Regen +0.5/s",
			Dash = new DashProfile {
				Name = "Bastión", Description = "Al terminar recibes 60% menos daño durante 1.5 s.",
				Style = DashStyle.Bastion, Charges = 1, Distance = 80f, Duration = 0.22f, Recharge = 3f,
				Color = new Color(0.8f, 0.8f, 0.85f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Armas orbitales", Tag = WeaponTags.Orbital, Size = 0.10f },
				new WeaponAffinity { Label = "Armas cuerpo a cuerpo", WeaponType = "melee", Damage = 0.10f },
			},
		},
		new CharacterData {
			Name = "OScar", Description = "PROVISIONAL",
			Archetype = "Velocista",
			ScenePath = CimarronScene, IconPath = CimarronIcon, IsPlaceholder = true,
			Tint = new Color(1f, 0.75f, 0.6f),
			Speed = 220f, MaxHp = 80f, DamageMultiplier = 0.95f,
			ExtraStats = s => s.AttackSpeedMultiplier += 0.05f, ExtraStatsText = "Vel. ataque +5%",
			Dash = new DashProfile {
				Name = "Sprint", Description = "Muchas cargas cortas; cada una da +30% de velocidad 1.5 s.",
				Style = DashStyle.Sprint, Charges = 3, Distance = 85f, Duration = 0.18f, Recharge = 2f, Cooldown = 0.15f,
				Color = new Color(1f, 0.7f, 0.4f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Armas sobre el objetivo", Tag = WeaponTags.Objetivo, AttackSpeed = 0.10f },
				new WeaponAffinity { Label = "Armas cuerpo a cuerpo", WeaponType = "melee", AttackSpeed = 0.05f },
			},
		},
		new CharacterData {
			Name = "PROVISIONAL", Description = "PROVISIONAL",
			Archetype = "Constructora",
			ScenePath = CimarronScene, IconPath = CimarronIcon, IsPlaceholder = true,
			Tint = new Color(1f, 0.9f, 0.55f),
			Speed = 145f, MaxHp = 105f, DamageMultiplier = 1f,
			ExtraStats = s => s.ActiveCooldownMultiplier *= 0.9f, ExtraStatsText = "Enfriamiento de activos -10%",
			Dash = new DashProfile {
				Name = "Propulsor", Description = "Deja una mina donde estabas que explota enseguida.",
				Style = DashStyle.Propulsor, Charges = 2, Distance = 120f, Recharge = 5f,
				Color = new Color(1f, 0.5f, 0.3f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Armas orbitales", Tag = WeaponTags.Orbital, Damage = 0.15f },
				new WeaponAffinity { Label = "Armas de zona", Tag = WeaponTags.Zona, Damage = 0.10f },
			},
		},
		new CharacterData {
			Name = "Memo", Description = "Vive de la suerte... y de la beca.",
			Archetype = "Afortunado",
			ScenePath = CimarronScene, IconPath = CimarronIcon, IsPlaceholder = true,
			Tint = new Color(0.65f, 1f, 0.65f),
			Speed = 160f, MaxHp = 95f, DamageMultiplier = 1f,
			ExtraStats = s => { s.Luck += 1f; s.GoldMultiplier += 0.20f; }, ExtraStatsText = "Suerte +1 · Monedas +20%",
			Dash = new DashProfile {
				Name = "Golpe de suerte", Description = "35% de probabilidad de no gastar la carga.",
				Style = DashStyle.GolpeDeSuerte, Charges = 2, Distance = 120f, Recharge = 2.5f,
				Color = new Color(1f, 0.85f, 0.3f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Proyectiles", Tag = WeaponTags.Proyectil, AttackSpeed = 0.10f },
			},
		},
		new CharacterData {
			Name = "PROVISIONAL", Description = "PROVISIONAL",
			Archetype = "Sanadora",
			ScenePath = CimarronScene, IconPath = CimarronIcon, IsPlaceholder = true,
			Tint = new Color(0.7f, 1f, 0.95f),
			Speed = 150f, MaxHp = 110f, DamageMultiplier = 0.95f,
			ExtraStats = s => { s.HpRegen += 1f; s.LifeSteal += 0.02f; }, ExtraStatsText = "Regen +1/s · Robo de vida 2%",
			Dash = new DashProfile {
				Name = "Paso vital", Description = "Al terminar te curas 4% de tu vida máxima.",
				Style = DashStyle.PasoVital, Charges = 2, Distance = 115f, Recharge = 4f,
				Color = new Color(0.45f, 1f, 0.55f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Armas mágicas", WeaponType = "magic", Damage = 0.10f },
				new WeaponAffinity { Label = "Armas de zona", Tag = WeaponTags.Zona, Size = 0.05f },
			},
		},
		new CharacterData {
			Name = "PROVISIONAL", Description = "PROVISIONAL",
			Archetype = "Alquimista de área",
			ScenePath = CimarronScene, IconPath = CimarronIcon, IsPlaceholder = true,
			Tint = new Color(0.75f, 1f, 0.45f),
			Speed = 140f, MaxHp = 100f, DamageMultiplier = 1.05f,
			ExtraStats = s => s.SizeMultiplier += 0.05f, ExtraStatsText = "Tamaño +5%",
			Dash = new DashProfile {
				Name = "Estela tóxica", Description = "Deja charcos tóxicos que dañan y ralentizan.",
				Style = DashStyle.EstelaToxica, Charges = 2, Distance = 120f, Recharge = 4f,
				Color = new Color(0.5f, 1f, 0.35f),
			},
			Affinities = {
				new WeaponAffinity { Label = "Armas de zona", Tag = WeaponTags.Zona, Damage = 0.15f },
				new WeaponAffinity { Label = "Armas sobre el objetivo", Tag = WeaponTags.Objetivo, Size = 0.10f },
			},
		},
	};
}
