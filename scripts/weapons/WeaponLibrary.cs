using Godot;
using System.Collections.Generic;

public class WeaponUpgrade
{
	public string               Name;
	public string               Description;
	public string               ForWeapon; // nombre del arma que mejora
	public System.Action<WeaponData> Apply;
}

public static class WeaponLibrary
{
	// Hojas de sprites usadas para íconos
	private const string FxSheet    = "res://sprites/Armas/simplefx-alpha.png";
	private const string SlashSheet = "res://sprites/Armas/Sprite-0001-Sheet-Sheet.png";
	private const string AxeSheet   = "res://sprites/Armas/AxeWeapon.png";
	private const string BoneSheet  = "res://sprites/Armas/boneWeaponAnim.png";

	private static readonly Dictionary<string, Texture2D> _iconCache = new();

	public static List<WeaponData> BuildAll()
	{
		//armas de la alfa
		var pencil   = Load("res://scenes/weapons/pencil_weapon.tscn");
		var revolver = Load("res://scenes/weapons/revolver_weapon.tscn");
		var bone     = Load("res://scenes/weapons/bone_weapon.tscn");
		//var axe      = Load("res://scenes/weapons/axe_weapon.tscn");

		//armas de las clases actuales
		var longSword    = Load("res://scenes/weapons/long_sword.tscn");
		var bow          = Load("res://scenes/weapons/bow.tscn");
		var dagger       = Load("res://scenes/weapons/dagger.tscn");
		var mallet       = Load("res://scenes/weapons/mallet.tscn");
		var catalystBeam = Load("res://scenes/weapons/catalyst_beam.tscn");
		var ironAxe      = Load("res://scenes/weapons/iron_axe.tscn");
		var magicBook    = Load("res://scenes/weapons/magic_book.tscn");
		var ironSpear    = Load("res://scenes/weapons/iron_spear.tscn");
		var lightning    = Load("res://scenes/weapons/lightning.tscn");
		var meteor       = Load("res://scenes/weapons/meteor.tscn");
		var laser        = Load("res://scenes/weapons/laser_orbital.tscn");
		var shield       = Load("res://scenes/weapons/shield_orbital.tscn");
		var fireWalker   = Load("res://scenes/weapons/fire_walker.tscn");
		var fireBall     = Load("res://scenes/weapons/fire_ball.tscn");

		// armas nuevas
		var scythe    = Load("res://scenes/weapons/scythe_sweep.tscn");
		var boomerang = Load("res://scenes/weapons/boomerang_axe.tscn");
		var tornado   = Load("res://scenes/weapons/tornado.tscn");
		var frostAura = Load("res://scenes/weapons/frost_aura.tscn");
		var drone     = Load("res://scenes/weapons/drone_orbital.tscn");

		return new List<WeaponData>
		{
			// ── Armas de clase ─────────────────────────────────────────
			new WeaponData {
				Name="Espada Larga", WeaponType="melee",
				Description="Tajo frontal que atraviesa a varios enemigos.",
				Damage=35f, Speed=0f, Lifetime=0.3f, FireRate=1.0f,
				BulletScale=1.5f, PierceCount=10, BulletsPerShot=1,
				SpawnOffset=40f,                        //aparece frente al jugador
				BulletScene=longSword,
				IconPath = "res://sprites/Sword.png"
			},
			new WeaponData {
				Name="Arco", WeaponType="ranged",
				Description="Flechas rápidas de largo alcance.",
				Damage=35f, Speed=500f, Lifetime=3f, FireRate=1.0f,
				BulletScale=0.9f, PierceCount=1, BulletsPerShot=1,
				SpawnOffset=16f,
				BulletScene=bow,
				IconPath = "res://sprites/Bow.png"
			},
			new WeaponData {
				Name="Dagas", WeaponType="melee",
				Description="Puñaladas rápidas directamente sobre el enemigo.",
				Damage=8f, Speed=0f, Lifetime=0.2f, FireRate=0.4f,
				BulletScale=1.0f, PierceCount=10, BulletsPerShot=2,
				SpawnOffset=35f,
				IsOnEnemyWeapon=true,
				BulletScene=dagger,
				IconPath = "res://sprites/Dagger.png"
			},
			new WeaponData {
				Name="Mazo", WeaponType="melee",
				Description="Golpe lento y pesado en área.",
				Damage=50f, Speed=0f, Lifetime=0.3f, FireRate=1.5f,
				BulletScale=2.0f, PierceCount=10, BulletsPerShot=1,
				SpawnOffset=45f,
				BulletScene=mallet,
				IconPath = "res://sprites/mace.png"
			},
			new WeaponData {
				Name="Catalizador de Energia", WeaponType="magic",
				Description="Rayo de energía que perfora.",
				Damage=20f, Speed=600f, Lifetime=4f, FireRate=0.6f,
				BulletScale=0.8f, PierceCount=5, BulletsPerShot=1,
				SpawnOffset=10f,
				BulletScene=catalystBeam,
				IconPath = "res://sprites/Catalizador.png"
			},
			new WeaponData {
				Name="Hacha de Acero", WeaponType="melee",
				Description="Hacha que gira a tu alrededor.",
				Damage=40f, Speed=0f, Lifetime=0f, FireRate=0f,
				BulletScale=1.2f, PierceCount=0, BulletsPerShot=1,
				IsOrbital=true,
				BulletScene=ironAxe,
				IconPath = "res://sprites/Battleaxe.png"
			},
			new WeaponData {
				Name="Libro Magico", WeaponType="magic",
				Description="Proyectil mágico que rebota entre enemigos.",
				Damage=22f, Speed=250f, Lifetime=5f, FireRate=0.8f,
				BulletScale=1.1f, PierceCount=1, BulletsPerShot=1,
				BounceCount=3, SpawnOffset=10f,
				BulletScene=magicBook,
				IconPath = "res://sprites/Magic_Tome.png"
			},
			new WeaponData {
				Name="Fireball", WeaponType="magic", UnlockCost=120,
				Description="Bola de fuego que rebota.",
				Damage=22f, Speed=250f, Lifetime=5f, FireRate=0.8f,
				BulletScale=1.1f, PierceCount=1, BulletsPerShot=1,
				BounceCount=3, SpawnOffset=10f,
				BulletScene=fireBall,
				IconPath = "res://sprites/baculo_magico.png"
			},
			new WeaponData {
				Name="Lanza de Acero", WeaponType="melee",
				Description="Estocada de largo alcance.",
				Damage=35f, Speed=0f, Lifetime=0.35f, FireRate=1.2f,
				BulletScale=1.3f, PierceCount=10, BulletsPerShot=1,
				SpawnOffset=50f,
				BulletScene=ironSpear,
				IconPath = "res://sprites/lance.png"
			},
			// ── Armas del pool (level up) ──────────────────────────────
			new WeaponData {
				Name="Lightning", UnlockCost=150,
				WeaponType="magic",
				Description="Rayo que salta entre enemigos cercanos.",
				Damage=30f,
				FireRate=0.8f,
				BounceCount=1,
				IsOnEnemyWeapon=true,
				BulletScene=lightning,
				IconPath = FxSheet, IconRegion = new Rect2(160, 0, 16, 16)
			},
			new WeaponData {
				Name="Meteor", UnlockCost=200,
				WeaponType="magic",
				Description="Meteoro que cae sobre un enemigo y explota en área.",
				Damage=30f,
				FireRate=0.8f,
				IsOnEnemyWeapon=true,
				BulletScene=meteor,
				IconPath = FxSheet, IconRegion = new Rect2(208, 16, 16, 16)
			},
			new WeaponData {
				Name="Laser", UnlockCost=200,
				WeaponType="magic",
				Description="Rayo continuo que gira a tu alrededor.",
				Damage=30f,
				FireRate=0.8f,
				IsOrbital=true,
				BulletScene=laser,
				IconPath = FxSheet, IconRegion = new Rect2(144, 64, 16, 16)
			},
			new WeaponData {
				Name="Shield", UnlockCost=150,
				WeaponType="magic",
				Description="Escudos orbitales que empujan y dañan al chocar, y bloquean golpes.",
				Damage=10f,
				IsOrbital=true,
				BulletScene=shield,
				IconPath = FxSheet, IconRegion = new Rect2(80, 0, 16, 16)
			},
			new WeaponData {
				Name="Fire Walker", UnlockCost=150,
				WeaponType="magic",
				Description="Deja charcos de fuego por donde caminas.",
				Damage=12f,
				FireRate=0.4f,
				Lifetime=3f,
				IsZoneWeapon=true,
				FollowPlayer=false,
				BulletScene=fireWalker,
				IconPath = FxSheet, IconRegion = new Rect2(176, 16, 16, 16)
			},
			new WeaponData {
				Name="Lapiz", WeaponType="ranged",
				Description="Ráfaga de tres lápices.",
				Damage=8f, Speed=300f, Lifetime=2f, FireRate=0.6f,
				BulletScale=1.0f, PierceCount=1, BulletsPerShot=3,
				SpawnOffset=10f, BulletScene=pencil,
				IconPath = "res://sprites/Armas/pencilproyectile.png"
			},
			new WeaponData {
				Name="Revolver", WeaponType="ranged",
				Description="Disparo lento y muy potente que perfora.",
				Damage=60f, Speed=600f, Lifetime=4f, FireRate=1.5f,
				BulletScale=1.0f, PierceCount=5, BulletsPerShot=1,
				SpawnOffset=10f, BulletScene=revolver,
				IconPath = "res://sprites/Armas/revolverWeapon.png"
			},
			//new WeaponData {
			//	Name="Hacha", WeaponType="melee",
			//	Damage=40f, Speed=0f, Lifetime=4f, FireRate=1.3f,
			//	BulletScale=1.2f, PierceCount=3, BulletsPerShot=1,
			//	IsOrbital=true, BulletScene=axeWeapon
			//},
			new WeaponData {
				Name="Hueso", WeaponType="ranged",
				Description="Hueso lento y pesado.",
				Damage=35f, Speed=100f, Lifetime=4f, FireRate=1.0f,
				BulletScale=1.0f, PierceCount=1, BulletsPerShot=1,
				SpawnOffset=10f, BulletScene=bone,
				IconPath = BoneSheet, IconRegion = new Rect2(0, 0, 32, 32)
			},
			// ── Armas nuevas ───────────────────────────────────────────
			new WeaponData {
				Name="Guadaña", WeaponType="melee", UnlockCost=120,
				Description="Da una vuelta completa a tu alrededor y golpea a todo lo que alcanza.",
				Damage=26f, Speed=0f, Lifetime=0.45f, FireRate=1.7f,
				BulletScale=1.0f, PierceCount=99, BulletsPerShot=1,
				SpawnOffset=0f, BulletScene=scythe,
				IconPath = SlashSheet, IconRegion = new Rect2(192, 0, 64, 64)
			},
			new WeaponData {
				Name="Hacha Bumerán", WeaponType="ranged", UnlockCost=100,
				Description="Sale disparada y regresa; golpea de ida y de vuelta.",
				Damage=22f, Speed=320f, Lifetime=2.0f, FireRate=1.6f,
				BulletScale=1.0f, PierceCount=99, BulletsPerShot=1,
				SpawnOffset=10f, BulletScene=boomerang,
				IconPath = AxeSheet, IconRegion = new Rect2(0, 0, 32, 32)
			},
			new WeaponData {
				Name="Tornado", WeaponType="magic", UnlockCost=150,
				Description="Remolino lento que atrapa enemigos y los daña varias veces.",
				Damage=7f, Speed=85f, Lifetime=3f, FireRate=2.6f,
				BulletScale=1.0f, PierceCount=99, BulletsPerShot=1,
				SpawnOffset=14f, BulletScene=tornado,
				IconPath = FxSheet, IconRegion = new Rect2(64, 16, 16, 16)
			},
			new WeaponData {
				Name="Aura de Escarcha", WeaponType="magic", UnlockCost=150,
				Description="Aura permanente que daña y ralentiza a los enemigos cercanos.",
				Damage=6f, FireRate=0.5f, Lifetime=99999f,
				BulletScale=1.0f, IsZoneWeapon=true, FollowPlayer=true,
				BulletScene=frostAura,
				IconPath = FxSheet, IconRegion = new Rect2(16, 80, 16, 16)
			},
			new WeaponData {
				Name="Dron de Vigilancia", WeaponType="ranged", UnlockCost=180,
				Description="Dron que te sigue y dispara al enemigo más cercano.",
				Damage=12f, Speed=420f, Lifetime=1.6f, FireRate=0.9f,
				BulletScale=1.0f, PierceCount=1, BulletsPerShot=1,
				IsOrbital=true, SpawnOffset=56f, BulletScene=drone,
				IconPath = FxSheet, IconRegion = new Rect2(144, 32, 16, 16)
			},
		};
	}

	private static PackedScene Load(string path) => ResourceLoader.Exists(path) ? GD.Load<PackedScene>(path) : null;

	public static WeaponData Get(string name) =>
		BuildAll().Find(w => w.Name == name);

	/// <summary>Armas que se pueden ofrecer al subir de nivel (desbloqueadas en el perfil).</summary>
	public static bool IsUnlocked(WeaponData weapon)
	{
		if (weapon == null) return false;
		if (weapon.UnlockCost <= 0) return true;
		return ProfileManager.Instance != null && ProfileManager.Instance.IsWeaponUnlocked(weapon.Name);
	}

	/// <summary>Ícono del arma (recorta la hoja de sprites si tiene IconRegion). Puede ser null.</summary>
	public static Texture2D GetIcon(WeaponData weapon)
	{
		if (weapon == null || string.IsNullOrEmpty(weapon.IconPath)) return null;
		string key = $"{weapon.IconPath}|{weapon.IconRegion}";
		if (_iconCache.TryGetValue(key, out var cached) && cached != null) return cached;
		if (!ResourceLoader.Exists(weapon.IconPath)) return null;

		var texture = GD.Load<Texture2D>(weapon.IconPath);
		Texture2D icon = texture;
		if (texture != null && weapon.IconRegion.Size != Vector2.Zero)
			icon = new AtlasTexture { Atlas = texture, Region = weapon.IconRegion };
		_iconCache[key] = icon;
		return icon;
	}

	/// <summary>Texto corto del tipo y etiquetas del arma (para tarjetas).</summary>
	public static string TagsText(WeaponData weapon)
	{
		if (weapon == null) return "";
		string tags = WeaponTags.TypeDisplayName(weapon.WeaponType);
		if (weapon.IsOrbital) tags += " · orbital";
		if (weapon.IsZoneWeapon) tags += " · de zona";
		if (weapon.IsOnEnemyWeapon) tags += " · sobre el objetivo";
		if (WeaponTags.Has(weapon, WeaponTags.Proyectil)) tags += " · proyectil";
		return tags;
	}

	// ── Upgrades por arma ──────────────────────────────────────────────
	public static List<WeaponUpgrade> GetUpgradesForWeapon(WeaponData weapon)
	{
		var upgrades = new List<WeaponUpgrade>
		{
			new WeaponUpgrade { Name="Dano +10",     Description=$"{weapon.Name}: +10 dano",               ForWeapon=weapon.Name, Apply=w => w.Damage       += 10f },
			new WeaponUpgrade { Name="Vel. ataque",  Description=$"{weapon.Name}: dispara mas rapido",      ForWeapon=weapon.Name, Apply=w => w.FireRate      = Mathf.Max(0.05f, w.FireRate - 0.1f) },
			new WeaponUpgrade { Name="Tamaño +20%", Description=$"{weapon.Name}: proyectiles mas grandes", ForWeapon=weapon.Name, Apply=w => w.BulletScale  *= 1.2f },
		};

		// +1 proyectil no hace nada en las armas de zona: se cambia por otra mejora
		if (weapon.IsZoneWeapon)
		{
			if (weapon.FollowPlayer)
				upgrades.Add(new WeaponUpgrade { Name="Dano +6", Description=$"{weapon.Name}: +6 dano por pulso", ForWeapon=weapon.Name, Apply=w => w.Damage += 6f });
			else
				upgrades.Add(new WeaponUpgrade { Name="Duracion +25%", Description=$"{weapon.Name}: la zona dura mas", ForWeapon=weapon.Name, Apply=w => w.Lifetime *= 1.25f });
		}
		else
		{
			upgrades.Add(new WeaponUpgrade { Name="+1 proyectil", Description=$"{weapon.Name}: +1 bala por disparo", ForWeapon=weapon.Name, Apply=w => w.BulletsPerShot++ });
		}

		// Upgrades adicionales según tipo
		if ((weapon.WeaponType == "ranged" || weapon.WeaponType == "magic") && !weapon.IsZoneWeapon)
		{
			upgrades.Add(new WeaponUpgrade { Name="Vel. proyectil", Description=$"{weapon.Name}: +20% velocidad", ForWeapon=weapon.Name, Apply=w => w.Speed *= 1.2f });
			upgrades.Add(new WeaponUpgrade { Name="+1 Perforar",   Description=$"{weapon.Name}: perfora un enemigo mas", ForWeapon=weapon.Name, Apply=w => w.PierceCount++ });
		}

		if (weapon.WeaponType == "magic" && !weapon.IsZoneWeapon)
			upgrades.Add(new WeaponUpgrade { Name="+1 Rebote", Description=$"{weapon.Name}: rebota una vez mas", ForWeapon=weapon.Name, Apply=w => w.BounceCount++ });

		return upgrades;
	}
}
