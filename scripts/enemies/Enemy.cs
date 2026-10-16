using Godot;
using System;

public partial class Enemy : CharacterBody2D, IEnemy
{
	[Export] public float MaxHp = 30f; //30
	[Export] public float Damage = 10f; //10
	[Export] public float Speed = 60f; //60
	[Export] public float DamageCooldown = 3f; //3
	[Export] public float XpValue = 10f; //10

	//Drops
	[Export] public PackedScene ExpOrbScene;
	[Export] public PackedScene GoldCoinScene;
	[Export] public int CoinValue = 1;

	private float _currentHp;
	private Player _player;
	private AnimatedSprite2D _sprite;
	private float _damageCooldownTimer = 0f;
	private bool _isDead = false;

	// Estados alterados
	private float _slowStrength;
	private float _slowTimer;
	private Color _baseModulate = Colors.White;

	// IEnemy
	public Node2D Body => this;
	public float CurrentHealth => _currentHp;
	public float MaxHealth => MaxHp;
	public bool IsElite { get; private set; }
	public virtual bool IsBoss => false;
	public bool IsDead => _isDead;

	public override void _Ready()
	{
		_currentHp = MaxHp;
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		_baseModulate = Modulate;
		AddToGroup("enemies");
	}

	/// <summary>Convierte al enemigo en élite. Llamar ANTES de agregarlo a la escena.</summary>
	public void MakeElite()
	{
		if (IsElite) return;
		IsElite = true;
		MaxHp    *= Balance.EliteHpMultiplier;
		Damage   *= Balance.EliteDamageMultiplier;
		Speed    *= Balance.EliteSpeedMultiplier;
		XpValue  *= Balance.EliteXpMultiplier;
		CoinValue *= Balance.EliteCoinMultiplier;
		Modulate = Balance.EliteTint;
		Scale   *= Balance.EliteVisualScale;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead || _player == null) return;
		float dt = (float)delta;

		if (_damageCooldownTimer > 0)
			_damageCooldownTimer -= dt;
		UpdateSlow(dt);

		//Seguimiento
		Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * Speed * (1f - _slowStrength);
		if (direction.X != 0)
			_sprite.FlipH = direction.X < 0;
		MoveAndSlide();

		float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
		if (distanceToPlayer < 25f && _damageCooldownTimer <= 0f && _slowStrength < 1f)
		{
			_player.TakeDamageFrom(Damage, this);
			_damageCooldownTimer = DamageCooldown;
		}
	}

	public void TakeDamage(float amount)
	{
		if (_isDead || amount <= 0f) return;
		amount = CombatEvents.ModifyDamageToEnemy(this, amount);
		_currentHp -= amount;
		CombatEvents.RaiseEnemyHit(this, amount);
		if (_currentHp <= 0)
			Die();
	}

	public void ApplySlow(float strength, float duration)
	{
		if (_isDead) return;
		strength = Mathf.Clamp(strength, 0f, 1f);
		// El efecto más fuerte manda; si es igual, se renueva la duración
		if (strength > _slowStrength || _slowTimer <= 0f)
			_slowStrength = strength;
		_slowTimer = Mathf.Max(_slowTimer, duration);
		RefreshTint();
	}

	public void Knockback(Vector2 direction, float distance)
	{
		if (_isDead || IsBoss) return;
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

	public void Die()
	{
		if (_isDead) return;
		_isDead = true;
		// Primero se notifica la muerte (créditos, cofres, objetos) y después el gancho
		// de la subclase: así la victoria del jefe final ya incluye su recompensa.
		CombatEvents.RaiseEnemyKilled(this);
		OnDeath();
		CallDeferred(nameof(SpawnDrops));
		CallDeferred("queue_free");
	}

	/// <summary>Gancho para subclases (p. ej. BossEnemy).</summary>
	protected virtual void OnDeath() { }

	private void SpawnDrops()
	{
		// Soltar orbe de XP
		if (ExpOrbScene != null)
		{
			ExpOrb orb    = ExpOrbScene.Instantiate<ExpOrb>();
			orb.XpValue   = XpValue;
			GetParent().AddChild(orb);
			orb.GlobalPosition = GlobalPosition;
		}

		// Soltar moneda
		if (GoldCoinScene != null)
		{
			GoldCoin coin   = GoldCoinScene.Instantiate<GoldCoin>();
			coin.CoinValue  = CoinValue;
			GetParent().AddChild(coin);
			coin.GlobalPosition = GlobalPosition;
		}
	}
}
