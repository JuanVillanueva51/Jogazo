using System.Collections.Generic;
using Godot;

/// <summary>
/// Catálogo de todos los objetos del juego.
/// Para agregar uno nuevo: añade un ItemDefinition aquí (y, si necesita lógica propia,
/// una clase en ItemEffects.cs). Los Id deben ser únicos y no cambiar, porque se guardan
/// en el perfil del jugador.
/// </summary>
public static class ItemLibrary
{
	public const string SpentCredentialId = "credencial_vencida";

	private static List<ItemDefinition> _all;
	private static Dictionary<string, ItemDefinition> _byId;

	public static IReadOnlyList<ItemDefinition> All
	{
		get
		{
			EnsureBuilt();
			return _all;
		}
	}

	public static ItemDefinition Get(string id)
	{
		if (string.IsNullOrEmpty(id)) return null;
		EnsureBuilt();
		return _byId.TryGetValue(id, out var def) ? def : null;
	}

	/// <summary>Objetos que aparecen en el menú de desbloqueos.</summary>
	public static List<ItemDefinition> Unlockables()
	{
		var list = new List<ItemDefinition>();
		foreach (var def in All)
			if (!def.Hidden) list.Add(def);
		list.Sort((a, b) =>
		{
			int k = a.Kind.CompareTo(b.Kind);
			if (k != 0) return k;
			int r = a.Rarity.CompareTo(b.Rarity);
			return r != 0 ? r : string.Compare(a.Name, b.Name, System.StringComparison.CurrentCulture);
		});
		return list;
	}

	private static void EnsureBuilt()
	{
		if (_all != null) return;
		_all = Build();
		_byId = new Dictionary<string, ItemDefinition>();
		foreach (var def in _all)
		{
			if (_byId.ContainsKey(def.Id))
			{
				GD.PushError($"ItemLibrary: Id repetido '{def.Id}'");
				continue;
			}
			_byId[def.Id] = def;
		}
	}

	private static ItemDefinition Stat(string id, string name, string desc, ItemRarity rarity, bool unlocked,
		System.Action<PlayerStats, int> apply)
	{
		return new ItemDefinition
		{
			Id = id, Name = name, Description = desc, Rarity = rarity, UnlockedByDefault = unlocked,
			CreateEffect = () => new StatEffect(apply),
		};
	}

	private static List<ItemDefinition> Build()
	{
		var list = new List<ItemDefinition>
		{
			// ───────────── COMUNES ─────────────
			Stat("cafe_cafeteria", "Café de Cafetería", "+12% velocidad de ataque (+12% por copia).",
				ItemRarity.Comun, true, (s, n) => s.AttackSpeedMultiplier += 0.12f * n),
			Stat("tenis_gastados", "Tenis Gastados", "+10% velocidad de movimiento (+10% por copia).",
				ItemRarity.Comun, true, (s, n) => s.MovementSpeedMult += 0.10f * n),
			Stat("calculadora", "Calculadora Científica", "+8% probabilidad de crítico (+8% por copia).",
				ItemRarity.Comun, true, (s, n) => s.CritChance += 0.08f * n),
			Stat("curitas", "Curitas", "Regeneras +1 de vida por segundo (+1 por copia).",
				ItemRarity.Comun, true, (s, n) => s.HpRegen += 1f * n),
			Stat("mochila", "Mochila Pesada", "+20 de vida máxima (+20 por copia).",
				ItemRarity.Comun, true, (s, n) => s.MaxHpFlat += 20f * n),
			Stat("plumon", "Plumón Permanente", "+10% de daño (+10% por copia).",
				ItemRarity.Comun, true, (s, n) => s.DamageMultiplier += 0.10f * n),
			Stat("iman_refri", "Imán de Refri", "+30% rango para recoger experiencia y monedas (+30% por copia).",
				ItemRarity.Comun, false, (s, n) => s.PickupRangeMult += 0.30f * n),
			Stat("alcancia", "Alcancía de Cochinito", "+20% monedas obtenidas (+20% por copia).",
				ItemRarity.Comun, false, (s, n) => s.GoldMultiplier += 0.20f * n),
			Stat("apuntes", "Apuntes Subrayados", "+12% experiencia obtenida (+12% por copia).",
				ItemRarity.Comun, false, (s, n) => s.XpMultiplier += 0.12f * n),
			Stat("sudadera", "Sudadera UABC", "+8 de armadura (+8 por copia). Reduce el daño recibido.",
				ItemRarity.Comun, false, (s, n) => s.Armor += 8f * n),
			Stat("lupa", "Lupa", "+12% tamaño de ataques (+12% por copia).",
				ItemRarity.Comun, false, (s, n) => s.SizeMultiplier += 0.12f * n),
			new ItemDefinition {
				Id = "regla_metalica", Name = "Regla Metálica", Rarity = ItemRarity.Comun,
				Description = "+60% de daño a enemigos con más del 90% de vida (+60% por copia).",
				CreateEffect = () => new HighHealthBonusEffect(),
			},

			// ───────────── POCO COMUNES ─────────────
			new ItemDefinition {
				Id = "cable_pelado", Name = "Cable Pelado", Rarity = ItemRarity.PocoComun, UnlockedByDefault = true,
				Description = "20% de probabilidad al golpear de lanzar un rayo que salta a 2 enemigos (+1 por copia) por el 60% del daño.",
				CreateEffect = () => new ChainLightningEffect(),
			},
			new ItemDefinition {
				Id = "casco_obra", Name = "Casco de Obra", Rarity = ItemRarity.PocoComun, UnlockedByDefault = true,
				Description = "12% de probabilidad de bloquear un golpe por completo (se apila con rendimiento decreciente).",
				CreateEffect = () => new BlockChanceEffect(),
			},
			new ItemDefinition {
				Id = "encendedor", Name = "Encendedor", Rarity = ItemRarity.PocoComun,
				Description = "Al matar, el enemigo explota e inflige 120% de daño base en un radio de 60 (+15 por copia).",
				CreateEffect = () => new KillExplosionEffect(),
			},
			Stat("ligas", "Ligas de Hule", "Los proyectiles rebotan +1 vez (+1 por copia).",
				ItemRarity.PocoComun, false, (s, n) => s.BonusBounce += n),
			Stat("clip_doblado", "Clip Doblado", "Los proyectiles atraviesan +1 enemigo (+1 por copia).",
				ItemRarity.PocoComun, false, (s, n) => s.BonusPierce += n),
			new ItemDefinition {
				Id = "colmillo", Name = "Colmillo de Murciélago", Rarity = ItemRarity.PocoComun,
				Description = "Al matar a un enemigo te curas 3 de vida (+3 por copia).",
				CreateEffect = () => new HealOnKillEffect(),
			},
			Stat("pila_recargable", "Pila Recargable", "-15% enfriamiento del objeto activo (multiplicativo por copia).",
				ItemRarity.PocoComun, false, (s, n) => s.ActiveCooldownMultiplier *= Mathf.Pow(0.85f, n)),
			Stat("cactus", "Cactus de Escritorio", "Al recibir un golpe, devuelves 100% de daño base al atacante (+100% por copia).",
				ItemRarity.PocoComun, false, (s, n) => s.Thorns += 1f * n),
			new ItemDefinition {
				Id = "cronometro", Name = "Cronómetro", Rarity = ItemRarity.PocoComun,
				Description = "Tras 2 s sin recibir daño, +25% velocidad de movimiento (+15% por copia).",
				CreateEffect = () => new OutOfCombatSpeedEffect(),
			},

			// ───────────── RAROS ─────────────
			Stat("diploma", "Diploma Enmarcado", "+1 proyectil para todas las armas (+1 por copia).",
				ItemRarity.Raro, true, (s, n) => s.BonusProjectiles += n),
			Stat("tesis", "Tesis Doctoral", "+5% crítico y +50% de daño crítico (por copia).",
				ItemRarity.Raro, false, (s, n) => { s.CritChance += 0.05f * n; s.CritDamage += 0.5f * n; }),
			new ItemDefinition {
				Id = "termo_hielo", Name = "Termo de Hielo", Rarity = ItemRarity.Raro,
				Description = "10% de probabilidad al golpear (+5% por copia) de ralentizar al enemigo un 60% por 2 s.",
				CreateEffect = () => new SlowOnHitEffect(),
			},
			new ItemDefinition {
				Id = "sentencia_final", Name = "Sentencia Final", Rarity = ItemRarity.Raro,
				Description = "Los enemigos (no jefes) con menos del 11% de vida son ejecutados (sube por copia).",
				CreateEffect = () => new ExecuteEffect(),
			},
			new ItemDefinition {
				Id = "credencial_repuesto", Name = "Credencial de Repuesto", Rarity = ItemRarity.Raro,
				Description = "Al morir, revives con 50% de vida. Se consume.",
				CreateEffect = () => new ReviveEffect(),
			},
			Stat("termo_sangre", "Termo de Sangre", "+3% de robo de vida (+3% por copia).",
				ItemRarity.Raro, false, (s, n) => s.LifeSteal += 0.03f * n),

			// ───────────── LEGENDARIOS ─────────────
			new ItemDefinition {
				Id = "tormenta_examenes", Name = "Tormenta de Exámenes", Rarity = ItemRarity.Legendario,
				Description = "Cada 3 s cae un rayo sobre el enemigo más cercano: 300% de daño base (+150% y más frecuente por copia).",
				CreateEffect = () => new PeriodicLightningEffect(),
			},
			Stat("medalla_rector", "Medalla del Rector", "+25% daño, +15% velocidad de ataque y +10% velocidad de movimiento (por copia).",
				ItemRarity.Legendario, true, (s, n) => { s.DamageMultiplier += 0.25f * n; s.AttackSpeedMultiplier += 0.15f * n; s.MovementSpeedMult += 0.10f * n; }),
			new ItemDefinition {
				Id = "matraz_explosivo", Name = "Matraz Explosivo", Rarity = ItemRarity.Legendario,
				Description = "Los golpes de tus armas explotan e infligen 40% del daño (+40% por copia) a enemigos cercanos.",
				CreateEffect = () => new OnHitExplosionEffect(),
			},
			Stat("espiritu_cimarron", "Espíritu Cimarrón", "+1 proyectil, +1 rebote y +1 perforación (por copia).",
				ItemRarity.Legendario, false, (s, n) => { s.BonusProjectiles += n; s.BonusBounce += n; s.BonusPierce += n; }),

			// ───────────── MALDITOS (beneficio fuerte + desventaja) ─────────────
			Stat("pacto_sangre", "Pacto de Sangre", "+60% de daño, pero -35% de vida máxima (por copia).",
				ItemRarity.Maldito, true, (s, n) => { s.DamageMultiplier *= 1f + 0.6f * n; s.MaxHpMultiplier *= Mathf.Pow(0.65f, n); }),
			Stat("cafe_triple", "Café Triple", "+40% velocidad de ataque, pero recibes +25% de daño (por copia).",
				ItemRarity.Maldito, false, (s, n) => { s.AttackSpeedMultiplier += 0.4f * n; s.DamageTakenMultiplier *= 1f + 0.25f * n; }),
			Stat("moneda_trucada", "Moneda Trucada", "+75% monedas, pero -25% de experiencia (por copia).",
				ItemRarity.Maldito, false, (s, n) => { s.GoldMultiplier += 0.75f * n; s.XpMultiplier *= Mathf.Pow(0.75f, n); }),
			Stat("chaleco_plomo", "Chaleco de Plomo", "+80% vida máxima y +20 de armadura, pero -30% velocidad de movimiento (por copia).",
				ItemRarity.Maldito, false, (s, n) => { s.MaxHpMultiplier += 0.8f * n; s.Armor += 20f * n; s.MovementSpeedMult *= Mathf.Pow(0.7f, n); }),
			Stat("lentes_oscuros", "Lentes Oscuros", "+20% crítico y +30% daño crítico, pero -50% rango de recolección y -10% velocidad de ataque (por copia).",
				ItemRarity.Maldito, false, (s, n) => { s.CritChance += 0.2f * n; s.CritDamage += 0.3f * n; s.PickupRangeMult *= Mathf.Pow(0.5f, n); s.AttackSpeedMultiplier *= Mathf.Pow(0.9f, n); }),
			Stat("vidrio_templado", "Vidrio Templado", "Duplica tu daño, pero reduce tu vida máxima a la mitad (por copia).",
				ItemRarity.Maldito, false, (s, n) => { s.DamageMultiplier *= Mathf.Pow(2f, n); s.MaxHpMultiplier *= Mathf.Pow(0.5f, n); }),

			// ───────────── ACTIVOS ─────────────
			new ItemDefinition {
				Id = "botiquin_emergencia", Name = "Botiquín de Emergencia", Kind = ItemKind.Activo, Rarity = ItemRarity.Comun,
				Cooldown = 45f, UnlockedByDefault = true,
				Description = "Cura el 35% de tu vida máxima.",
				CreateEffect = () => new HealActive(0.35f),
			},
			new ItemDefinition {
				Id = "bebida_energetica", Name = "Bebida Energética", Kind = ItemKind.Activo, Rarity = ItemRarity.PocoComun,
				Cooldown = 40f, UnlockedByDefault = true,
				Description = "+50% velocidad de ataque y +30% velocidad de movimiento durante 8 s.",
				CreateEffect = () => new TimedBuffActive(8f, new Color(0.3f, 1f, 0.6f),
					s => { s.AttackSpeedMultiplier *= 1.5f; s.MovementSpeedMult *= 1.3f; }),
			},
			new ItemDefinition {
				Id = "silbato_prefecto", Name = "Silbato del Prefecto", Kind = ItemKind.Activo, Rarity = ItemRarity.PocoComun,
				Cooldown = 30f,
				Description = "Aturde 2.5 s y empuja a los enemigos cercanos.",
				CreateEffect = () => new StunNovaActive(),
			},
			new ItemDefinition {
				Id = "bomba_pintura", Name = "Bomba de Pintura", Kind = ItemKind.Activo, Rarity = ItemRarity.Raro,
				Cooldown = 35f,
				Description = "Inflige 500% de daño base a los enemigos a tu alrededor.",
				CreateEffect = () => new DamageNovaActive(150f, 5f),
			},
			new ItemDefinition {
				Id = "reloj_arena", Name = "Reloj de Arena", Kind = ItemKind.Activo, Rarity = ItemRarity.Raro,
				Cooldown = 60f,
				Description = "Ralentiza 70% a todos los enemigos durante 5 s.",
				CreateEffect = () => new GlobalSlowActive(),
			},
			new ItemDefinition {
				Id = "lluvia_meteoros", Name = "Lluvia de Meteoros", Kind = ItemKind.Activo, Rarity = ItemRarity.Legendario,
				Cooldown = 60f,
				Description = "Caen 8 meteoros sobre enemigos cercanos, cada uno con 400% de daño base.",
				CreateEffect = () => new MeteorShowerActive(),
			},
			new ItemDefinition {
				Id = "trato_diablo", Name = "Trato con el Diablo", Kind = ItemKind.Activo, Rarity = ItemRarity.Maldito,
				Cooldown = 50f,
				Description = "Obtienes monedas (escalan con la dificultad), pero pierdes 25% de tu vida actual.",
				CreateEffect = () => new BloodForGoldActive(),
			},

			// ───────────── OCULTOS ─────────────
			new ItemDefinition {
				Id = SpentCredentialId, Name = "Credencial Vencida", Rarity = ItemRarity.Comun, Hidden = true,
				Description = "Ya te salvó una vez. No hace nada.",
				CreateEffect = () => new NoEffect(),
			},
		};
		return list;
	}
}
