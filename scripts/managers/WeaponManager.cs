using Godot;
using System.Collections.Generic;

//Datos de un arma individual
public class WeaponData
{
	public string Name;
	public string WeaponType    = "ranged"; // "melee", "ranged", "magic"

	public float Damage         = 20f;
	public float Speed          = 350f;
	public float Lifetime       = 3f;
	public float FireRate       = 2.8f;
	public float BulletScale    = 1f;
	public int   PierceCount    = 1;
	public int   BulletsPerShot = 1;
	public float SpawnOffset    = 0;
	public bool IsOrbital       = false;
	public bool IsOnEnemyWeapon = false;
	public int   BounceCount    = 0;
	public bool IsZoneWeapon = false;
	public bool FollowPlayer = false;

	/// <summary>Créditos para desbloquearla en el menú. 0 = desbloqueada desde el inicio.</summary>
	public int UnlockCost = 0;
	/// <summary>Descripción corta para la subida de nivel y el menú de desbloqueos.</summary>
	public string Description = "";

	public PackedScene BulletScene;
	public string IconPath;
	/// <summary>Región del ícono dentro de IconPath si es una hoja de sprites (vacío = imagen completa).</summary>
	public Rect2 IconRegion = new Rect2();
}

//Instancia activa de un arma (tiene su propio timer)
public class WeaponInstance
{
	public WeaponData Data;
	public float      FireTimer = 0f;
	public bool isBursting = false;
	public int shotsLeft = 0;
	public float betweenBulletsTimer = 0f;
	public float timeBetweenBullets = 0f;
	public float BurstCooldownTimer = 0f;
}

public partial class WeaponManager : Node
{
	[Export] public float DetectionRange = 200f;
	[Export] public PackedScene DefaultBulletScene;
	private Dictionary<string, ZoneWeapon> _activeZones = new();
	//|--------------------------------------|
	public const int MaxWeapons = 3; // !!!  | valor original = 3
	//|--------------------------------------|

	private List<WeaponInstance> _activeWeapons = new();
	private Dictionary<string, List<OrbitalWeapon>> _orbitalsByWeapon = new();
	private Player _player;

	private WeaponData _pendingStartWeapon;

	public override void _Ready()
	{
		_player = GetParent<Player>();

		ClassData selectedClass = ClassLibrary.Get(GameManager.Instance.SelectedClassIndex);
		if (selectedClass != null)
		{
			_pendingStartWeapon = WeaponLibrary.Get(selectedClass.StartingWeaponName);
			//Diferir para que Player._Ready() corra primero
			CallDeferred(nameof(InitStartWeapon));
		}
	}

	public override void _Process(double delta)
	{
		float shootTime = (float)delta;
		foreach (var weapon in _activeWeapons)
		{
			if (weapon.Data.IsOrbital)
			{
				CheckOrbitalCount(weapon.Data);
				continue;
			}
			WeaponProccess(weapon, shootTime);
		}
	}

	/// <summary>Cadencia final del arma (estadísticas + afinidad de personaje/clase).</summary>
	private float FinalFireRate(WeaponData data)
	{
		PlayerStats stats = _player.Stats;
		AffinityBonus affinity = AffinityBonus.For(_player, data);
		float attackSpeed = Mathf.Max(0.01f, stats.AttackSpeedMultiplier * (1f + affinity.AttackSpeed));
		return data.FireRate / attackSpeed;
	}

	private void WeaponProccess(WeaponInstance weapon, float shootTime)
	{
		float finalFireRate = FinalFireRate(weapon.Data);
		if (weapon.Data.IsZoneWeapon)
		{
			weapon.FireTimer += shootTime;
			if (weapon.FireTimer >= finalFireRate)
			{
				weapon.FireTimer = 0f;
				SpawnZoneWeapon(weapon.Data);
			}
			return;
		}
		if (weapon.BurstCooldownTimer > 0f)
		{
			weapon.BurstCooldownTimer -= shootTime;
			return;
		}
		if (weapon.isBursting)
		{
			weapon.betweenBulletsTimer -= shootTime;
			if (weapon.betweenBulletsTimer <= 0f)
			{
				Node2D target = GetNearestEnemy();
				if (target != null)
				{
					Shoot(weapon.Data, target);
				}
				weapon.shotsLeft--;
				if (weapon.shotsLeft <= 0)
				{
					weapon.isBursting = false;
					weapon.BurstCooldownTimer = finalFireRate;
				}
				else
				{
					weapon.betweenBulletsTimer = weapon.timeBetweenBullets;
				}
			}
			return;
		}
		weapon.FireTimer += shootTime;
		if (weapon.FireTimer >= finalFireRate)
		{
			weapon.FireTimer = 0f;
			Burst(weapon, finalFireRate);
		}
	}

	private void SpawnZoneWeapon(WeaponData data)
	{
		if (data.BulletScene == null)
			return;
		if (data.FollowPlayer && _activeZones.TryGetValue(data.Name, out var existing))
		{
			if (GodotObject.IsInstanceValid(existing))
				return;
			_activeZones.Remove(data.Name);
		}

		PlayerStats stats = _player.Stats;
		AffinityBonus affinity = AffinityBonus.For(_player, data);
		DamageRoll roll = DamageCalculator.ForWeapon(_player, data, data.Damage);
		float finalScale = data.BulletScale * stats.SizeMultiplier * (1f + affinity.Size);

		var zone = data.BulletScene.Instantiate<ZoneWeapon>();
		zone.Initialize(data);
		zone.FinalScale = finalScale;
		zone.FinalDamage = roll.Amount;
		zone.GlobalPosition = _player.GlobalPosition;
		_player.GetParent().AddChild(zone);

		if (data.FollowPlayer)
			_activeZones[data.Name] = zone;
	}

	private void Burst(WeaponInstance weapon, float finalFireRate)
	{
		PlayerStats stats = _player.Stats;
		int totalBullets = weapon.Data.BulletsPerShot + stats.BonusProjectiles;
		totalBullets = Mathf.Max(1, totalBullets);
		weapon.isBursting = true;
		weapon.shotsLeft = totalBullets;
		weapon.timeBetweenBullets = Mathf.Max(0.03f, (finalFireRate * 0.35f) / totalBullets);
		weapon.betweenBulletsTimer = 0f;
	}

	private void Shoot(WeaponData data, Node2D target)
	{
		PlayerStats stats     = _player.Stats;
		Vector2     direction = (target.GlobalPosition - _player.GlobalPosition).Normalized();
		AffinityBonus affinity = AffinityBonus.For(_player, data);

		DamageRoll roll = DamageCalculator.ForWeapon(_player, data, data.Damage);
		float finalDamage = roll.Amount;
		if (_player.IsChargeActive()) { finalDamage *= 1.3f; _player.ConsumeCharge(); }

		float finalSpeed  = data.Speed       * stats.ProjectileSpeedMult * (1f + affinity.ProjectileSpeed);
		float finalScale  = data.BulletScale * stats.SizeMultiplier * (1f + affinity.Size);
		int   finalPierce = data.PierceCount + stats.BonusPierce;
		int   finalBounce = data.BounceCount + stats.BonusBounce;

		PackedScene scene = data.BulletScene ?? DefaultBulletScene;
		if (scene == null) return;

		if (data.IsOnEnemyWeapon)
		{
			var spell = scene.Instantiate<OnEnemyWeapon>();
			spell.Initialize(target);
			spell.Damage = finalDamage;
			spell.BulletScale = finalScale;
			spell.duration = data.Lifetime;
			if (spell is Lightning lightning)
			{
				lightning.MaxJumps = 1 + finalBounce;
			}
			_player.GetParent().AddChild(spell);
			return;
		}

		if (scene.CanInstantiate())
		{
			Node instance = scene.Instantiate();
			// Armas nuevas (guadaña, hacha bumerán, tornado...): reciben todo ya calculado
			if (instance is IWeaponSpawn spawn && instance is Node2D spawnNode)
			{
				spawn.Launch(new WeaponLaunch
				{
					Player    = _player,
					Data      = data,
					Target    = target,
					Origin    = _player.GlobalPosition + direction * data.SpawnOffset,
					Direction = direction,
					Damage    = finalDamage,
					IsCrit    = roll.IsCrit,
					Speed     = finalSpeed,
					Scale     = finalScale,
					Pierce    = finalPierce,
					Bounce    = finalBounce,
					Lifetime  = data.Lifetime,
				});
				_player.GetParent().AddChild(spawnNode);
				return;
			}
			if (instance is MeleeWeapon melee)
			{
				melee.Damage = finalDamage;
				melee.BulletScale = finalScale;
				melee.Lifetime = data.Lifetime;
				melee.GlobalPosition = _player.GlobalPosition + direction * data.SpawnOffset;
				melee.Rotation = direction.Angle();
				_player.GetParent().AddChild(melee);
				return;
			}
			if (instance is Bullet bullet)
			{
				bullet.Damage      = finalDamage;
				bullet.Speed       = finalSpeed;
				bullet.Lifetime    = data.Lifetime;
				bullet.BulletScale = finalScale;
				bullet.PierceCount = finalPierce;
				bullet.BounceCount = finalBounce;

				_player.GetParent().AddChild(bullet);
				bullet.GlobalPosition = _player.GlobalPosition + direction * data.SpawnOffset;
				bullet.Initialize(direction);
				return;
			}
			// La escena no es ni melee ni bala: se descarta para no dejar nodos huérfanos
			instance.QueueFree();
		}
	}

	//Agregar un arma nueva (llamado desde LevelUpScreen)
	public bool TryAddWeapon(WeaponData data)
	{
		if (_activeWeapons.Count >= MaxWeapons) return false;
		_activeWeapons.Add(new WeaponInstance { Data = data });

		if (data.IsOrbital)
			SpawnOrbital(data);
		EventManager.Instance.EmitWeaponEquipped(data);
		return true;
	}

	public int WeaponCount => _activeWeapons.Count;

	public List<string> GetActiveWeaponNames()
	{
		var names = new List<string>();
		foreach (var w in _activeWeapons)
			names.Add(w.Data.Name);
		return names;
	}

	public List<WeaponData> GetActiveWeapons()
	{
		var list = new List<WeaponData>();
		foreach (var w in _activeWeapons)
			list.Add(w.Data);
		return list;
	}

	// Upgrades del pool de mejoras (afectan TODAS las armas)
	public void UpgradeDamage(float amount)
	{
		foreach (var w in _activeWeapons)
			w.Data.Damage += amount;
	}
	public void UpgradeFireRate(float amount)
	{
		foreach (var w in _activeWeapons)
			w.Data.FireRate = Mathf.Max(0.1f, w.Data.FireRate - amount);
	}
	public void UpgradeBulletCount()
	{
		foreach (var w in _activeWeapons)
			w.Data.BulletsPerShot++;
	}

	private Node2D GetNearestEnemy()
	{
		var enemies = GetTree().GetNodesInGroup("enemies");
		Node2D nearest = null;
		float minDist = float.MaxValue;

		foreach (Node node in enemies)
		{
			if (node is Node2D enemyNode)
			{
				if (enemyNode is IEnemy e && e.IsDead) continue;
				float d = _player.GlobalPosition.DistanceTo(enemyNode.GlobalPosition);
				if (d < DetectionRange && d < minDist)
				{
					minDist = d;
					nearest = enemyNode;
				}
			}
		}
		return nearest;
	}

	//Spawnea orbitales faltantes si BulletsPerShot aumentó
	private void CheckOrbitalCount(WeaponData data)
	{
		if (!_orbitalsByWeapon.ContainsKey(data.Name))
			_orbitalsByWeapon[data.Name] = new List<OrbitalWeapon>();

		var list    = _orbitalsByWeapon[data.Name];
		int target  = Mathf.Max(1, data.BulletsPerShot + _player.Stats.BonusProjectiles);
		list.RemoveAll(orbital => !GodotObject.IsInstanceValid(orbital));
		while (list.Count < target)
			SpawnSingleOrbital(data, list);
		// Si se perdieron proyectiles extra (p. ej. se consumió un objeto), quitar orbitales sobrantes
		while (list.Count > target)
		{
			var extra = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			if (GodotObject.IsInstanceValid(extra)) extra.QueueFree();
		}
		updateOrbitalPositions(data, list);
	}

	private void SpawnSingleOrbital(WeaponData data, List<OrbitalWeapon> list)
	{
		if (data.BulletScene == null) return;
		OrbitalWeapon orbital = data.BulletScene.Instantiate<OrbitalWeapon>();
		_player.GetParent().CallDeferred("add_child", orbital);
		list.Add(orbital);
	}

	private void updateOrbitalPositions(WeaponData data, List<OrbitalWeapon> list)
	{
		int total = list.Count;
		if (total <= 0) { return; }

		float angle = Mathf.Tau / total;
		float radious = Mathf.Max(70f, data.SpawnOffset);

		for (int i = 0; i < total; i++)
		{
			float angleOffset = i * angle;
			if (GodotObject.IsInstanceValid(list[i]))
			{
				list[i].SetOrbitData(angleOffset, radious, data);
				if (list[i] is ShieldOrbital shield)
					shield.ShieldIndex = i;
			}
		}
	}

	private void SpawnOrbital(WeaponData data)
	{
		CheckOrbitalCount(data);
	}

	private void InitStartWeapon()
	{
		//si hay estado guardado, restaurar armas con mejoras
		if (GameManager.Instance.HasSavedState
		&& GameManager.Instance.SavedWeapons != null
		&& GameManager.Instance.SavedWeapons.Count > 0)
		{
			foreach (WeaponData weapon in GameManager.Instance.SavedWeapons)
				TryAddWeapon(weapon);
			return;
		}
		//sino solo el arma inicial de la clase
		if (_pendingStartWeapon != null)
			TryAddWeapon(_pendingStartWeapon);
	}
}
