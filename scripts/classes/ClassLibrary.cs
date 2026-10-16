using Godot;
using System.Collections.Generic;

/// <summary>
/// Catálogo de clases. Cada clase tiene un arma inicial, una pasiva y un arquetipo:
/// sus afinidades dan un bonus pequeño con ciertos tipos de arma (y hacen que salgan
/// un poco más seguido al subir de nivel), pero cualquier arma se puede usar.
/// El orden importa: GameManager guarda el índice de la clase elegida.
/// </summary>
public static class ClassLibrary
{
	private static List<ClassData> _all;

	public static List<ClassData> All => _all ??= Build();

	/// <summary>Se mantiene por compatibilidad: devuelve la lista (cacheada).</summary>
	public static List<ClassData> BuildAll() => All;

	public static ClassData Get(int index)
	{
		var all = All;
		return index >= 0 && index < all.Count ? all[index] : null;
	}

	private static List<ClassData> Build()
	{
		return new List<ClassData>
		{
			// ── Clases originales ─────────────────────────────────────────────
			new ClassData {
				Name               = "Caballero",
				Archetype          = "Defensor cuerpo a cuerpo",
				Description        = "Un guerrero blindado experto en combate cuerpo a cuerpo.",
				PassiveName        = "Maestria Melee",
				PassiveDescription = "+10% de vida máxima y +5 de armadura.",
				StartingWeaponName = "Espada Larga",
				Passive            = PassiveType.Caballero,
				PortraitPath       = "res://sprites/Classes/Caballero.png",
				Affinities = {
					new WeaponAffinity { Label = "Cuerpo a cuerpo", WeaponType = "melee", Damage = 0.10f, Size = 0.10f },
				},
			},
			new ClassData {
				Name               = "Arquero",
				Archetype          = "Tirador a distancia",
				Description        = "Un tirador experto letal a distancia.",
				PassiveName        = "Ojo de Aguila",
				PassiveDescription = "+5% de probabilidad de crítico.",
				StartingWeaponName = "Arco",
				Passive            = PassiveType.Arquero,
				PortraitPath       = "res://sprites/Classes/Arco.png",
				Affinities = {
					new WeaponAffinity { Label = "A distancia", WeaponType = "ranged", Damage = 0.10f, ProjectileSpeed = 0.10f },
				},
			},
			new ClassData {
				Name               = "Asesino",
				Archetype          = "Golpes rápidos",
				Description        = "Rapido y letal, golpea antes de que te vean.",
				PassiveName        = "Sombra Veloz",
				PassiveDescription = "+10% velocidad de movimiento, +10% velocidad de ataque y +5% de crítico.",
				StartingWeaponName = "Dagas",
				Passive            = PassiveType.Asesino,
				PortraitPath       = "res://sprites/Classes/Asesino.png",
				Affinities = {
					new WeaponAffinity { Label = "Armas sobre el objetivo", Tag = WeaponTags.Objetivo, Damage = 0.10f, AttackSpeed = 0.10f },
				},
			},
			new ClassData {
				Name               = "Berserker",
				Archetype          = "Peleador de multitudes",
				Description        = "Cuantos mas enemigos rodean al berserker, mas peligroso se vuelve.",
				PassiveName        = "Furia de Masa",
				PassiveDescription = "+5% de daño por cada enemigo cercano (máx. +50%). +5 de armadura.",
				StartingWeaponName = "Mazo",
				Passive            = PassiveType.Berserker,
				PortraitPath       = "res://sprites/Classes/Mazo-Berseker.png",
				Affinities = {
					new WeaponAffinity { Label = "Cuerpo a cuerpo", WeaponType = "melee", AttackSpeed = 0.10f },
				},
			},
			new ClassData {
				Name               = "Catalizador",
				Archetype          = "Bola de nieve",
				Description        = "Un ser de energia pura que escala su poder con cada kill.",
				PassiveName        = "Cascada de Poder",
				PassiveDescription = "Cada kill acumula +2% de daño (máx. x3). Recibes 15% más de daño.",
				StartingWeaponName = "Catalizador de Energia",
				Passive            = PassiveType.Catalizador,
				PortraitPath       = "res://sprites/Classes/Catalizador de energia.png",
				Affinities = {
					new WeaponAffinity { Label = "Mágicas", WeaponType = "magic", Damage = 0.05f },
					new WeaponAffinity { Label = "Proyectiles", Tag = WeaponTags.Proyectil, ProjectileSpeed = 0.10f },
				},
			},
			new ClassData {
				Name               = "Ogro",
				Archetype          = "Coloso furioso",
				Description        = "Un coloso que se vuelve mas peligroso conforme recibe daño.",
				PassiveName        = "Marca de Sangre",
				PassiveDescription = "Más daño según la vida que falte (hasta +75%). +15% de vida máxima. Si un golpe te quita más del 50% de vida, te regeneras rápidamente.",
				StartingWeaponName = "Hacha de Acero",
				Passive            = PassiveType.Ogro,
				PortraitPath       = "res://sprites/Classes/Hacha de acero.png",
				Affinities = {
					new WeaponAffinity { Label = "Orbitales", Tag = WeaponTags.Orbital, Damage = 0.10f },
					new WeaponAffinity { Label = "Cuerpo a cuerpo", WeaponType = "melee", Size = 0.10f },
				},
			},
			new ClassData {
				Name               = "Mago Maestro",
				Archetype          = "Hechicero",
				Description        = "Un maestro de las artes arcanas.",
				PassiveName        = "Dominio Arcano",
				PassiveDescription = "-10% de enfriamiento de objetos activos.",
				StartingWeaponName = "Libro Magico",
				Passive            = PassiveType.MagoMaestro,
				PortraitPath       = "res://sprites/Classes/Libro Magico.png",
				Affinities = {
					new WeaponAffinity { Label = "Mágicas", WeaponType = "magic", Damage = 0.10f, Size = 0.10f },
				},
			},
			new ClassData {
				Name               = "Caballero con Corcel",
				Archetype          = "Jinete en movimiento",
				Description        = "Un jinete que gana poder al moverse sin detenerse.",
				PassiveName        = "Carga de Caballeria",
				PassiveDescription = "Caminar 10s sin detenerse activa una carga: +15% velocidad y +30% daño al primer golpe. Cooldown 25s.",
				StartingWeaponName = "Lanza de Acero",
				Passive            = PassiveType.CaballeroCorcel,
				PortraitPath       = "res://sprites/Classes/Lanza de Acero.png",
				Affinities = {
					new WeaponAffinity { Label = "Cuerpo a cuerpo", WeaponType = "melee", Damage = 0.10f },
				},
			},

			// ── Clases nuevas (una por cada arma que no tenía clase) ───────────
			new ClassData {
				Name               = "Piromante",
				Archetype          = "Fuego persistente",
				Description        = "Prende fuego a todo lo que toca.",
				PassiveName        = "Brasas",
				PassiveDescription = "15% de probabilidad de quemar al enemigo (3 pulsos de daño). +5% de daño mágico.",
				StartingWeaponName = "Fireball",
				Passive            = PassiveType.Piromante,
				CreatePassiveEffect = () => new BurnOnHitPassive(),
				Affinities = {
					new WeaponAffinity { Label = "Mágicas", WeaponType = "magic", Damage = 0.10f },
					new WeaponAffinity { Label = "De zona", Tag = WeaponTags.Zona, Damage = 0.10f },
				},
			},
			new ClassData {
				Name               = "Electromante",
				Archetype          = "Cadenas eléctricas",
				Description        = "La electricidad salta de un enemigo a otro.",
				PassiveName        = "Sobrecarga",
				PassiveDescription = "+1 rebote en todas las armas y +10% de velocidad de ataque.",
				StartingWeaponName = "Lightning",
				Passive            = PassiveType.Electromante,
				Affinities = {
					new WeaponAffinity { Label = "Sobre el objetivo", Tag = WeaponTags.Objetivo, Damage = 0.10f },
					new WeaponAffinity { Label = "Mágicas", WeaponType = "magic", AttackSpeed = 0.05f },
				},
			},
			new ClassData {
				Name               = "Astrónomo",
				Archetype          = "Artillería celeste",
				Description        = "Estudia el cielo... y lo deja caer sobre sus enemigos.",
				PassiveName        = "Lluvia de Estrellas",
				PassiveDescription = "Cada 7 s cae una estrella sobre un enemigo cercano (150% de daño base en área).",
				StartingWeaponName = "Meteor",
				Passive            = PassiveType.Astronomo,
				CreatePassiveEffect = () => new StarfallPassive(),
				Affinities = {
					new WeaponAffinity { Label = "Mágicas", WeaponType = "magic", Damage = 0.10f },
					new WeaponAffinity { Label = "Sobre el objetivo", Tag = WeaponTags.Objetivo, Size = 0.10f },
				},
			},
			new ClassData {
				Name               = "Técnico Láser",
				Archetype          = "Control rotatorio",
				Description        = "Calibra sus lentes para cubrir más terreno.",
				PassiveName        = "Lente de Aumento",
				PassiveDescription = "+15% de tamaño en todas las armas.",
				StartingWeaponName = "Laser",
				Passive            = PassiveType.TecnicoLaser,
				Affinities = {
					new WeaponAffinity { Label = "Orbitales", Tag = WeaponTags.Orbital, Damage = 0.10f, AttackSpeed = 0.05f },
				},
			},
			new ClassData {
				Name               = "Guardián",
				Archetype          = "Tanque defensivo",
				Description        = "Nada pasa sin su permiso.",
				PassiveName        = "Baluarte",
				PassiveDescription = "+12 de armadura y +1 de vida por segundo, pero -10% de velocidad de movimiento.",
				StartingWeaponName = "Shield",
				Passive            = PassiveType.Guardian,
				Affinities = {
					new WeaponAffinity { Label = "Orbitales", Tag = WeaponTags.Orbital, Size = 0.10f },
					new WeaponAffinity { Label = "Cuerpo a cuerpo", WeaponType = "melee", Damage = 0.10f },
				},
			},
			new ClassData {
				Name               = "Caminante Ígneo",
				Archetype          = "Hostigador móvil",
				Description        = "Nunca deja de moverse y deja fuego a su paso.",
				PassiveName        = "Paso Ardiente",
				PassiveDescription = "+10% de velocidad de movimiento y +10% de vida máxima.",
				StartingWeaponName = "Fire Walker",
				Passive            = PassiveType.CaminanteIgneo,
				Affinities = {
					new WeaponAffinity { Label = "De zona", Tag = WeaponTags.Zona, Damage = 0.15f, Size = 0.10f },
				},
			},
			new ClassData {
				Name               = "Estudiante",
				Archetype          = "Aprendiz versátil",
				Description        = "Aprende rápido... sobre todo la noche antes del examen.",
				PassiveName        = "Desvelado",
				PassiveDescription = "+20% de experiencia y +10% de velocidad de ataque.",
				StartingWeaponName = "Lapiz",
				Passive            = PassiveType.Estudiante,
				Affinities = {
					new WeaponAffinity { Label = "A distancia", WeaponType = "ranged", AttackSpeed = 0.10f },
				},
			},
			new ClassData {
				Name               = "Pistolero",
				Archetype          = "Francotirador crítico",
				Description        = "Un disparo, un problema menos.",
				PassiveName        = "Mano Rápida",
				PassiveDescription = "+10% de probabilidad de crítico y +25% de daño crítico.",
				StartingWeaponName = "Revolver",
				Passive            = PassiveType.Pistolero,
				Affinities = {
					new WeaponAffinity { Label = "A distancia", WeaponType = "ranged", Damage = 0.10f },
					new WeaponAffinity { Label = "Proyectiles", Tag = WeaponTags.Proyectil, ProjectileSpeed = 0.10f },
				},
			},
			new ClassData {
				Name               = "Nigromante",
				Archetype          = "Drenaje de vida",
				Description        = "Se alimenta de lo que derrota.",
				PassiveName        = "Cosecha de Almas",
				PassiveDescription = "3% de robo de vida. Cada enemigo derrotado te cura un poco.",
				StartingWeaponName = "Hueso",
				Passive            = PassiveType.Nigromante,
				CreatePassiveEffect = () => new SoulHarvestPassive(),
				Affinities = {
					new WeaponAffinity { Label = "A distancia", WeaponType = "ranged", Damage = 0.10f },
					new WeaponAffinity { Label = "Mágicas", WeaponType = "magic", Damage = 0.05f },
				},
			},
			new ClassData {
				Name               = "Segador",
				Archetype          = "Ejecutor",
				Description        = "Termina lo que otros empiezan.",
				PassiveName        = "Siega",
				PassiveDescription = "+30% de daño contra enemigos con menos del 35% de vida.",
				StartingWeaponName = "Guadaña",
				Passive            = PassiveType.Segador,
				CreatePassiveEffect = () => new ReaperPassive(),
				Affinities = {
					new WeaponAffinity { Label = "Cuerpo a cuerpo", WeaponType = "melee", Damage = 0.10f, Size = 0.10f },
				},
			},
			new ClassData {
				Name               = "Cazador",
				Archetype          = "Perforación",
				Description        = "Sus armas siempre vuelven a su mano.",
				PassiveName        = "Brazo Fuerte",
				PassiveDescription = "+1 de perforación y +10% de velocidad de proyectil.",
				StartingWeaponName = "Hacha Bumerán",
				Passive            = PassiveType.Cazador,
				Affinities = {
					new WeaponAffinity { Label = "Proyectiles", Tag = WeaponTags.Proyectil, Damage = 0.10f },
					new WeaponAffinity { Label = "A distancia", WeaponType = "ranged", AttackSpeed = 0.05f },
				},
			},
			new ClassData {
				Name               = "Aeromante",
				Archetype          = "Control de masas",
				Description        = "Maneja el viento para mantener a raya a la horda.",
				PassiveName        = "Vendaval",
				PassiveDescription = "10% de probabilidad de empujar al enemigo al golpearlo. +5% de tamaño.",
				StartingWeaponName = "Tornado",
				Passive            = PassiveType.Aeromante,
				CreatePassiveEffect = () => new GalePassive(),
				Affinities = {
					new WeaponAffinity { Label = "Mágicas", WeaponType = "magic", Size = 0.10f },
					new WeaponAffinity { Label = "Proyectiles", Tag = WeaponTags.Proyectil, Damage = 0.10f },
				},
			},
			new ClassData {
				Name               = "Criomante",
				Archetype          = "Ralentización",
				Description        = "Congela el campo de batalla.",
				PassiveName        = "Frío Glacial",
				PassiveDescription = "Todos tus golpes ralentizan 20% durante 1.5 s. +5 de armadura.",
				StartingWeaponName = "Aura de Escarcha",
				Passive            = PassiveType.Criomante,
				CreatePassiveEffect = () => new FrostbitePassive(),
				Affinities = {
					new WeaponAffinity { Label = "De zona", Tag = WeaponTags.Zona, Damage = 0.10f, Size = 0.15f },
				},
			},
			new ClassData {
				Name               = "Ingeniero",
				Archetype          = "Torretas y artefactos",
				Description        = "Deja que sus inventos hagan el trabajo sucio.",
				PassiveName        = "Mantenimiento",
				PassiveDescription = "-15% de enfriamiento de objetos activos y +10% de velocidad de ataque.",
				StartingWeaponName = "Dron de Vigilancia",
				Passive            = PassiveType.Ingeniero,
				Affinities = {
					new WeaponAffinity { Label = "Orbitales", Tag = WeaponTags.Orbital, Damage = 0.15f },
					new WeaponAffinity { Label = "A distancia", WeaponType = "ranged", AttackSpeed = 0.05f },
				},
			},
		};
	}

	/// <summary>Estadísticas base de la pasiva al inicio de la partida (se aplican a Player.BaseStats).</summary>
	public static void ApplyPassiveStats(PassiveType passive, PlayerStats stats)
	{
		switch (passive)
		{
			case PassiveType.Caballero:
				stats.MaxHpMultiplier += 0.10f;
				stats.Armor           += 5f;
				break;
			case PassiveType.Arquero:
				stats.CritChance += 0.05f;
				break;
			case PassiveType.Asesino:
				stats.MovementSpeedMult     += 0.10f;
				stats.AttackSpeedMultiplier += 0.10f;
				stats.CritChance            += 0.05f;
				break;
			case PassiveType.Berserker:
				stats.Armor += 5f;
				break;
			case PassiveType.Catalizador:
				stats.DamageTakenMultiplier *= 1.15f;
				break;
			case PassiveType.Ogro:
				stats.MaxHpMultiplier += 0.15f;
				break;
			case PassiveType.MagoMaestro:
				stats.ActiveCooldownMultiplier *= 0.9f;
				break;
			case PassiveType.CaballeroCorcel:
				// Lógica en Player._PhysicsProcess / UpdatePassive
				break;

			// ── Clases nuevas ──
			case PassiveType.Piromante:
				stats.MagicDamageBonus += 0.05f;   // + quemadura (BurnOnHitPassive)
				break;
			case PassiveType.Electromante:
				stats.BonusBounce           += 1;
				stats.AttackSpeedMultiplier += 0.10f;
				break;
			case PassiveType.Astronomo:
				break;                             // StarfallPassive
			case PassiveType.TecnicoLaser:
				stats.SizeMultiplier += 0.15f;
				break;
			case PassiveType.Guardian:
				stats.Armor             += 12f;
				stats.HpRegen           += 1f;
				stats.MovementSpeedMult -= 0.10f;
				break;
			case PassiveType.CaminanteIgneo:
				stats.MovementSpeedMult += 0.10f;
				stats.MaxHpMultiplier   += 0.10f;
				break;
			case PassiveType.Estudiante:
				stats.XpMultiplier          += 0.20f;
				stats.AttackSpeedMultiplier += 0.10f;
				break;
			case PassiveType.Pistolero:
				stats.CritChance += 0.10f;
				stats.CritDamage += 0.25f;
				break;
			case PassiveType.Nigromante:
				stats.LifeSteal += 0.03f;          // + curación al matar (SoulHarvestPassive)
				break;
			case PassiveType.Segador:
				break;                             // ReaperPassive
			case PassiveType.Cazador:
				stats.BonusPierce         += 1;
				stats.ProjectileSpeedMult += 0.10f;
				break;
			case PassiveType.Aeromante:
				stats.SizeMultiplier += 0.05f;     // + empuje (GalePassive)
				break;
			case PassiveType.Criomante:
				stats.Armor += 5f;                 // + ralentizar (FrostbitePassive)
				break;
			case PassiveType.Ingeniero:
				stats.ActiveCooldownMultiplier *= 0.85f;
				stats.AttackSpeedMultiplier    += 0.10f;
				break;
		}
	}

	/// <summary>Retrato de la clase (o el ícono de su arma inicial si no tiene).</summary>
	public static Texture2D GetPortrait(ClassData cls)
	{
		if (cls == null) return null;
		if (!string.IsNullOrEmpty(cls.PortraitPath) && ResourceLoader.Exists(cls.PortraitPath))
			return GD.Load<Texture2D>(cls.PortraitPath);
		return WeaponLibrary.GetIcon(WeaponLibrary.Get(cls.StartingWeaponName));
	}
}
