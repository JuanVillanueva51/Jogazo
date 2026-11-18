using Godot;
using System;

public partial class RangedEnemy : CharacterBody2D, IEnemy
{
	// ── Stats base exportables ─────────────────────────────────────────────
	[Export] public float MaxHp        = 25f;
	[Export] public float Damage       = 8f;   // se suma al daño del proyectil
	[Export] public float Speed        = 45f;
	[Export] public float XpValue      = 15f;

	// ── Comportamiento de disparo ──────────────────────────────────────────
	[Export] public float ShootRange   = 200f; // distancia a la que empieza a disparar
	[Export] public float FireRate     = 2.0f; // segundos entre disparos (antes 2.5)
	[Export] public PackedScene ProjectileScene; // SlimeThornWeapon.tscn

	// ── Stats del proyectil ────────────────────────────────────────────────
	[Export] public float ProjectileSpeed    = 150f; // antes 120
	[Export] public float ProjectileLifetime = 4f;

	//Drops
	[Export] public PackedScene ExpOrbScene;
	[Export] public PackedScene GoldCoinScene;
	[Export] public int         CoinValue = 1;

	private float            _currentHp;
	private Player           _player;
	private AnimatedSprite2D _sprite;
	private float            _fireTimer = 0f;
	private bool             _isDead    = false;

	// Estados alterados
	private float _slowStrength;
	private float _slowTimer;
	private Color _baseModulate = Colors.White;

	// IEnemy
	public Node2D Body => this;
	public float CurrentHealth => _currentHp;
	public float MaxHealth => MaxHp;
	public bool IsElite { get; private set; }
	public bool IsBoss => false;
	public bool IsDead => _isDead;

	public override void _Ready()
	{
		_currentHp = MaxHp;
		CollisionMask = Enemy.EnemyPhysicsLayer; // los muros no los detienen (antes se atoraban)
		_sprite    = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_player    = GetTree().GetFirstNodeInGroup("player") as Player;
		_baseModulate = Modulate;
		AddToGroup("enemies");
	}

	/// <summary>Convierte al enemigo en élite. Llamar ANTES de agregarlo a la escena.</summary>
	public void MakeElite()
	{
		if (IsElite) return;
		IsElite = true;
		MaxHp     *= Balance.EliteHpMultiplier;
		Damage    *= Balance.EliteDamageMultiplier;
		Speed     *= Balance.EliteSpeedMultiplier;
		XpValue   *= Balance.EliteXpMultiplier;
		CoinValue *= Balance.EliteCoinMultiplier;
		Modulate = Balance.EliteTint;
		Scale   *= Balance.EliteVisualScale;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead || _player == null) return;
		float dt = (float)delta;
		UpdateSlow(dt);

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();

		// Flip sprite
		if (direction.X != 0)
			_sprite.FlipH = direction.X < 0;

		if (distance > ShootRange)
		{
			// Acercarse al jugador
			Velocity = direction * Speed * (1f - _slowStrength);
			_sprite.Play("walk");
		}
		else
		{
			// Dentro del rango: detenerse y disparar
			Velocity = Vector2.Zero;
			_sprite.Play("idle");

			// Aturdido = no dispara; ralentizado = dispara más lento
			_fireTimer += dt * (1f - _slowStrength);
			if (_fireTimer >= FireRate)
			{
				_fireTimer = 0f;
				Shoot(direction);
			}
		}

		MoveAndSlide();
	}

	private void Shoot(Vector2 direction)
	{
		if (ProjectileScene == null) return;

		EnemyProjectile projectile = ProjectileScene.Instantiate<EnemyProjectile>();

		projectile.Damage   = projectile.Damage + Damage;
		projectile.Speed    = ProjectileSpeed;
		projectile.Lifetime = ProjectileLifetime;

		GetParent().AddChild(projectile);
		projectile.GlobalPosition = GlobalPosition;
		projectile.Initialize(direction);
	}

	public void TakeDamage(float amount)
	{
		if (_isDead || amount <= 0f) return;
		amount = CombatEvents.ModifyDamageToEnemy(this, amount);
		_currentHp -= amount;
		CombatEvents.RaiseEnemyHit(this, amount);
		if (_currentHp <= 0) Die();
	}

	public void ApplySlow(float strength, float duration)
	{
		if (_isDead) return;
		strength = Mathf.Clamp(strength, 0f, 1f);
		if (strength > _slowStrength || _slowTimer <= 0f)
			_slowStrength = strength;
		_slowTimer = Mathf.Max(_slowTimer, duration);
		RefreshTint();
	}

	public void Knockback(Vector2 direction, float distance)
	{
		if (_isDead) return;
		GlobalPosition += direction.Normalized() * distance;
	}

	private void UpdateSlow(float dt)
	{
		if (_slowTimer <= 0f) return;
		_slowTimer -= dt;
		if (_slowTimer <= 0f)
		{
			_slowTimer = 0f;
			_slowStrength = 0f;
			RefreshTint();
		}
	}

	private void RefreshTint()
	{
		Modulate = _slowStrength > 0f ? _baseModulate * new Color(0.6f, 0.8f, 1.2f) : _baseModulate;
	}

	private void Die()
	{
		if (_isDead) return;
		_isDead = true;
		CombatEvents.RaiseEnemyKilled(this);
		CallDeferred(nameof(SpawnDrops));
		CallDeferred("queue_free");
	}

	private void SpawnDrops()
	{
		if (ExpOrbScene != null)
		{
			ExpOrb orb  = ExpOrbScene.Instantiate<ExpOrb>();
			orb.XpValue = XpValue;
			GetParent().AddChild(orb);
			orb.GlobalPosition = GlobalPosition;
		}

		if (GoldCoinScene != null)
		{
			GoldCoin coin  = GoldCoinScene.Instantiate<GoldCoin>();
			coin.CoinValue = CoinValue;
			GetParent().AddChild(coin);
			coin.GlobalPosition = GlobalPosition;
		}
	}
}
