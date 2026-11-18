using System;
using Godot;

public partial class Player : CharacterBody2D
{
	/// <summary>Velocidad final (personaje * estadísticas). Se recalcula en RecalculateStats().</summary>
	[Export] public float  Speed    = 150f;
	/// <summary>Vida máxima final. Se recalcula en RecalculateStats().</summary>
	[Export] public float  MaxHp    = 100f;
	[Export] public string AnimIdle = "idle";
	[Export] public string AnimWalk = "walk";
	public float       CurrentHp { get; private set; }

	/// <summary>Estadísticas finales (base + objetos + efectos temporales). Solo lectura para el resto del juego.</summary>
	public PlayerStats Stats     { get; private set; } = new();
	/// <summary>Mejoras permanentes de la partida (pasiva de clase, torres). Modificar y luego llamar RecalculateStats().</summary>
	public PlayerStats BaseStats { get; private set; } = new();

	public CharacterData   Character { get; private set; }
	public ClassData       ClassInfo { get; private set; }
	public PlayerInventory Inventory { get; private set; }

	//Separado de Stats.DamageMultiplier para no sobreescribir upgrades
	public float PassiveDamageBonus { get; private set; } = 1f;

	private PassiveType      _passive;
	private AnimatedSprite2D _sprite;
	private bool             _isDead = false;
	private bool             _statsInitialized = false;
	private float            _invulnerableTimer = 0f;
	private ulong            _lastPhysicsFrame;

	// Catalizador
	private float _catalystMultiplier = 1f;

	// Ogro
	private bool  _bloodMarkActive   = false;
	private float _bloodMarkTimer    = 0f;
	private float _bloodMarkCooldown = 0f;
	private const float BloodMarkRegen    = 0.05f;
	private const float BloodMarkDuration = 10f;
	private const float BloodMarkCooldownTime = 20f;

	// Caballero con Corcel
	private float _walkTimer     = 0f;
	private bool  _chargeActive  = false;
	private float _chargeCooldown = 0f;

	// Dash: los valores vienen del DashProfile del personaje (CharacterLibrary)
	private DashProfile _dash = DashProfile.Default();
	private readonly DashRunState _dashState = new();
	private int   MaxDash = 2;
	private float DashDistance = 120f;
	private float DashDuration = 0.25f;
	private float DashCooldown = 0.2f;
	private float DashRecharge = 1.5f;
	private Vector2 lastDirection = Vector2.Right;
	private bool isDashing = false;
	private int DashCount = 0;
	private float DashTimer = 0f;
	private float DashCooldownTimer = 0f;
	private float DashRechargeTimer = 0f;
	private Vector2 DashDirection = Vector2.Zero;
	private float _guaranteedCritTimer = 0f;

	// Lectura para el HUD
	public DashProfile DashInfo => _dash;
	public int   DashCharges    => DashCount;
	public int   DashMaxCharges => MaxDash;
	/// <summary>Progreso (0-1) de la carga que se está recuperando.</summary>
	public float DashRechargeProgress => DashCount >= MaxDash ? 1f : Mathf.Clamp(DashRechargeTimer / Mathf.Max(0.01f, DashRecharge), 0f, 1f);

	public override void _Ready()
	{
		EnsureInputActions();
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		AddToGroup("player");

		Character = CharacterLibrary.Get(GameManager.Instance.SelectedCharacterIndex);
		ClassInfo = ClassLibrary.Get(GameManager.Instance.SelectedClassIndex);
		_passive  = GameManager.Instance.SelectedPassive;
		ApplyDashProfile(Character?.Dash);

		// Personajes provisionales: mismo sprite con otro tinte
		if (Character != null && Character.Tint != Colors.White)
			_sprite.SelfModulate = Character.Tint;

		BaseStats = new PlayerStats();
		Character.ApplyBaseStats(BaseStats);
		ClassLibrary.ApplyPassiveStats(_passive, BaseStats);

		Inventory = new PlayerInventory { Name = "Inventory" };
		Inventory.Setup(this);
		AddChild(Inventory);
		// Pasiva de clase con lógica propia (se vuelve a crear en cada mapa)
		if (ClassInfo?.CreatePassiveEffect != null)
			Inventory.AddInnateEffect(ClassInfo.CreatePassiveEffect());

		AddChild(new DashPips { Name = "DashPips" });

		//restaurar estado del jugador si viene de otra escena
		bool restoring = GameManager.Instance.HasSavedState;
		if (restoring)
		{
			if (GameManager.Instance.SavedStats != null)
				BaseStats = GameManager.Instance.SavedStats.Clone();
			if (GameManager.Instance.SavedInventory != null)
				Inventory.Restore(GameManager.Instance.SavedInventory);
			PassiveDamageBonus = GameManager.Instance.SavedPassiveDamageBonus;
			if (_passive == PassiveType.Catalizador)
				_catalystMultiplier = Mathf.Max(1f, PassiveDamageBonus);
		}

		RecalculateStats(true);
		if (restoring && GameManager.Instance.SavedCurrentHp > 0f)
			CurrentHp = Mathf.Min(GameManager.Instance.SavedCurrentHp, MaxHp);

		EventManager.Instance.LevelUp += OnLevelUp;
		CombatEvents.EnemyHit += OnEnemyHit;
		CombatEvents.EnemyKilled += OnEnemyKilled;
	}

	public override void _ExitTree()
	{
		if (EventManager.Instance != null)
			EventManager.Instance.LevelUp -= OnLevelUp;
		CombatEvents.EnemyHit -= OnEnemyHit;
		CombatEvents.EnemyKilled -= OnEnemyKilled;
	}

	/// <summary>Crea la acción "use_item" (Q) si el proyecto no la tiene.</summary>
	private static void EnsureInputActions()
	{
		if (!InputMap.HasAction("use_item"))
		{
			InputMap.AddAction("use_item");
			InputMap.ActionAddEvent("use_item", new InputEventKey { PhysicalKeycode = Key.Q });
		}
	}

	// ───────────────────────── Estadísticas ─────────────────────────

	/// <summary>
	/// Recalcula Stats = BaseStats + objetos + efectos temporales, y con ello la vida máxima y la velocidad.
	/// Llamar después de modificar BaseStats o el inventario.
	/// </summary>
	public void RecalculateStats(bool fullHeal = false)
	{
		var stats = BaseStats.Clone();
		Inventory?.ApplyStatModifiers(stats);
		Stats = stats;

		float oldMax = MaxHp;
		int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
		float baseHp = (Character != null ? Character.MaxHp : 100f) + Balance.MaxHpPerLevel * (level - 1);
		float newMax = (baseHp + stats.MaxHpFlat) * Mathf.Max(0.05f, stats.MaxHpMultiplier);
		MaxHp = Mathf.Max(1f, newMax);

		float baseSpeed = Character != null ? Character.Speed : 150f;
		Speed = baseSpeed * Mathf.Max(0.2f, stats.MovementSpeedMult);

		if (fullHeal || !_statsInitialized)
			CurrentHp = MaxHp;
		else if (MaxHp > oldMax)
			CurrentHp = Mathf.Min(CurrentHp + (MaxHp - oldMax), MaxHp);
		else
			CurrentHp = Mathf.Min(CurrentHp, MaxHp);
		_statsInitialized = true;
	}

	private void OnLevelUp(int level)
	{
		RecalculateStats();
	}

	// ───────────────────────── Bucle principal ─────────────────────────

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead) return;
		float dt = (float)delta;
		if (_invulnerableTimer > 0f) _invulnerableTimer -= dt;
		if (_guaranteedCritTimer > 0f) _guaranteedCritTimer -= dt;
		if (DashCooldownTimer > 0)
			DashCooldownTimer -= dt;
		RechargeDash(dt);

		// Si el juego estaba en pausa (menús), ignorar el clic/tecla que cerró el menú
		ulong frame = Engine.GetPhysicsFrames();
		bool resumedThisFrame = frame > _lastPhysicsFrame + 1;
		_lastPhysicsFrame = frame;
		if (!resumedThisFrame)
		{
			if (Input.IsActionJustPressed("interact"))
				Interactable.FindBest(this)?.Interact(this);
			if (Input.IsActionJustPressed("use_item"))
				Inventory?.TryActivate();
		}

		if (isDashing)
		{
			DashTimer -= dt;
			float DashSpeed = DashDistance / Mathf.Max(0.01f, DashDuration);
			Velocity = DashDirection * DashSpeed;
			MoveAndSlide();
			DashEffects.OnTick(this, _dash, _dashState);
			if (DashTimer <= 0)
			{
				isDashing = false;
				Velocity = Vector2.Zero;
				DashEffects.OnEnd(this, _dash, _dashState);
			}

			return;
		}
		Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		bool    isMoving  = direction != Vector2.Zero;

		if (isMoving)
		{
			direction = direction.Normalized();
			lastDirection = direction;
			float chargeBonus = _chargeActive ? 1.15f : 1f;
			Velocity = direction * Speed * chargeBonus;
			_sprite.Play(AnimWalk);
			if (direction.X != 0) _sprite.FlipH = direction.X < 0;
		}
		else
		{
			Velocity = Vector2.Zero;
			_sprite.Play(AnimIdle);
		}
		if (Input.IsActionJustPressed("dash"))
		{
			Dash();
		}
		MoveAndSlide();
		UpdatePassive(dt, isMoving);
	}

	private void UpdatePassive(float delta, bool isMoving)
	{
		switch (_passive)
		{
			case PassiveType.Berserker:
				// Modifica PassiveDamageBonus
				int nearby = CountNearbyEnemies(150f);
				PassiveDamageBonus = 1f + Mathf.Min(nearby * Balance.BerserkerPerEnemy, Balance.BerserkerMaxBonus);
				break;

			case PassiveType.Ogro:
				// Daño escala con HP faltante
				float missing = 1f - (CurrentHp / MaxHp);
				PassiveDamageBonus = 1f + missing * Balance.OgreMaxMissingHpBonus;

				// Regeneración de Marca de Sangre
				if (_bloodMarkActive)
				{
					Heal(MaxHp * BloodMarkRegen * delta);
					_bloodMarkTimer -= delta;
					if (_bloodMarkTimer <= 0f) _bloodMarkActive = false;
				}
				if (_bloodMarkCooldown > 0f) _bloodMarkCooldown -= delta;
				break;

			case PassiveType.CaballeroCorcel:
				if (_chargeCooldown > 0f) _chargeCooldown -= delta;

				if (isMoving)
				{
					_walkTimer += delta;
					if (_walkTimer >= 10f && _chargeCooldown <= 0f && !_chargeActive)
					{
						_chargeActive = true;
						GD.Print("Carga activada!");
					}
				}
				else
				{
					// Detenerse cancela la carga sin cooldown
					if (_chargeActive) GD.Print("Carga cancelada.");
					_walkTimer    = 0f;
					_chargeActive = false;
				}
				break;
		}

		// HP Regen pasiva
		if (Stats.HpRegen > 0f)
			Heal(Stats.HpRegen * delta);
	}

	// ───────────────────────── Daño y curación ─────────────────────────

	public void TakeDamage(float amount) => TakeDamageFrom(amount, null);

	/// <summary>source: quien hizo el daño (para espinas). Puede ser null.</summary>
	public void TakeDamageFrom(float amount, Node2D source)
	{
		if (_isDead || amount <= 0f) return;

		// Ogro: golpe mayor al 50% de HP max activa Marca de Sangre
		if (_passive == PassiveType.Ogro
			&& amount > MaxHp * 0.5f
			&& _bloodMarkCooldown <= 0f)
		{
			_bloodMarkActive   = true;
			_bloodMarkTimer    = BloodMarkDuration;
			_bloodMarkCooldown = BloodMarkCooldownTime;
			GD.Print("Marca de Sangre activada!");
		}

		if (isDashing || _invulnerableTimer > 0f) return;
		if (ConsumeOrbitalShield())
		{
			amount = 1f;
		}

		// Objetos (bloqueo, etc.)
		if (Inventory != null)
		{
			amount = Inventory.ModifyIncomingDamage(amount);
			if (amount <= 0f) return;
		}

		amount *= Mathf.Max(0f, Stats.DamageTakenMultiplier) * Balance.ArmorMultiplier(Stats.Armor);
		CurrentHp -= amount;
		CurrentHp  = Mathf.Clamp(CurrentHp, 0f, MaxHp);

		// Espinas: devolver daño al atacante
		if (Stats.Thorns > 0f && source is IEnemy attacker && !attacker.IsDead)
			CombatEvents.DealProcDamage(attacker, Stats.Thorns * DamageCalculator.ItemBaseDamage(this));

		Inventory?.NotifyDamaged(amount, source);

		if (CurrentHp <= 0f)
		{
			if (Inventory != null && Inventory.TryPreventDeath()) return;
			Die();
		}
	}

	public void Heal(float amount)
	{
		if (_isDead || amount <= 0f) return;
		CurrentHp = Mathf.Clamp(CurrentHp + amount, 0f, MaxHp);
	}

	/// <summary>Pierde un % de la vida actual (nunca mata).</summary>
	public void LoseHealthFraction(float fraction)
	{
		if (_isDead) return;
		CurrentHp = Mathf.Max(1f, CurrentHp * (1f - Mathf.Clamp(fraction, 0f, 1f)));
	}

	/// <summary>Revive con un % de vida y unos segundos de invulnerabilidad.</summary>
	public void Revive(float hpFraction, float invulnerableSeconds)
	{
		_isDead = false;
		CurrentHp = Mathf.Max(1f, MaxHp * Mathf.Clamp(hpFraction, 0.01f, 1f));
		_invulnerableTimer = invulnerableSeconds;
	}

	public bool IsInvulnerable => isDashing || _invulnerableTimer > 0f;

	//Golpes del jugador a enemigos (todas las armas y objetos)
	private void OnEnemyHit(HitInfo hit)
	{
		if (hit.FromProc || _isDead) return;
		OnDamageDealt(hit.Damage);
	}

	private void OnEnemyKilled(KillInfo kill)
	{
		// Catalizador: acumula multiplicador por cada kill
		if (_passive == PassiveType.Catalizador && !_isDead)
		{
			_catalystMultiplier = Mathf.Min(_catalystMultiplier + Balance.CatalystPerKill, Balance.CatalystMax);
			PassiveDamageBonus  = _catalystMultiplier;
		}
	}

	//Robo de vida (con límite por golpe para que no sea infinito con armas rápidas)
	public void OnDamageDealt(float amount)
	{
		if (Stats.LifeSteal > 0f)
			Heal(Mathf.Min(amount * Stats.LifeSteal, MaxHp * Balance.LifeStealCapPerHit));
	}

	public bool IsChargeActive()  => _chargeActive;
	public void ConsumeCharge()
	{
		_chargeActive  = false;
		_walkTimer     = 0f;
		_chargeCooldown = 25f;
		GD.Print("Carga consumida! Cooldown 25s");
	}

	private int CountNearbyEnemies(float radius)
	{
		int count = 0;
		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
			if (node is Node2D e && GlobalPosition.DistanceTo(e.GlobalPosition) < radius)
				count++;
		return count;
	}

	public void Dash(){
		if (DashCooldownTimer > 0 || DashCount <= 0)
			return;

		isDashing = true;
		DashTimer = DashDuration;
		DashCooldownTimer = DashCooldown;
		DashDirection = lastDirection;

		_dashState.Hit.Clear();
		_dashState.From = GlobalPosition;
		_dashState.Direction = DashDirection;
		_dashState.LastTrailPoint = GlobalPosition;
		bool consume = DashEffects.OnStart(this, _dash, _dashState);
		if (consume) DashCount--;
	}

	/// <summary>Configura el dash según el personaje (cargas, distancia, recarga...).</summary>
	private void ApplyDashProfile(DashProfile profile)
	{
		_dash = profile ?? DashProfile.Default();
		MaxDash      = Mathf.Max(1, _dash.Charges);
		DashDistance = Mathf.Max(10f, _dash.Distance);
		DashDuration = Mathf.Max(0.03f, _dash.Duration);
		DashCooldown = Mathf.Max(0f, _dash.Cooldown);
		DashRecharge = Mathf.Max(0.1f, _dash.Recharge);
		DashCount    = MaxDash;
	}

	/// <summary>Invulnerabilidad extra (se queda con la más larga).</summary>
	public void AddInvulnerability(float seconds)
	{
		_invulnerableTimer = Mathf.Max(_invulnerableTimer, seconds);
	}

	/// <summary>El siguiente golpe dentro de este tiempo será crítico (Paso sombrío).</summary>
	public void GrantGuaranteedCrit(float seconds)
	{
		_guaranteedCritTimer = Mathf.Max(_guaranteedCritTimer, seconds);
	}

	/// <summary>Gasta el crítico garantizado si hay uno activo.</summary>
	public bool ConsumeGuaranteedCrit()
	{
		if (_guaranteedCritTimer <= 0f) return false;
		_guaranteedCritTimer = 0f;
		return true;
	}

	private void Die()
	{
		_isDead = true;
		EventManager.Instance.EmitPlayerDied();
	}

	private void RechargeDash(float dt)
	{
		if (DashCount >= MaxDash)
		{
			DashRechargeTimer = 0f;
			return;
		}
		DashRechargeTimer += dt;
		if (DashRechargeTimer >= DashRecharge)
		{
			DashCount++;
			DashRechargeTimer = 0f;
		}
	}

	private bool ConsumeOrbitalShield()
	{
		foreach(Node node in GetTree().GetNodesInGroup("shields"))
		{
			if(node is ShieldOrbital shield &&
			   shield.IsActive)
			{
				shield.ConsumeShield();
				return true;
			}
		}

		return false;
	}
}
