using Godot;

/// <summary>
/// Dron de Vigilancia: orbita despacio alrededor del jugador y dispara
/// pequeños proyectiles al enemigo más cercano. "+1 proyectil" agrega otro dron.
/// </summary>
public partial class DroneOrbital : OrbitalWeapon
{
	private const float Range = 230f;
	private static readonly Color BodyColor  = new Color(0.25f, 0.27f, 0.33f);
	private static readonly Color EdgeColor  = new Color(0.75f, 0.8f, 0.9f);
	private static readonly Color LightColor = new Color(0.4f, 0.9f, 1f);

	private float _fireTimer;
	private float _rotor;
	private float _flash;

	public override void _Ready()
	{
		OrbitSpeed = 1.1f;
		ZIndex = 3;
		_fireTimer = (float)GD.RandRange(0.0, 0.5);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (Player == null || Data == null) return;
		float dt = (float)delta;
		_rotor += dt * 25f;
		_flash = Mathf.Max(0f, _flash - dt * 4f);
		QueueRedraw();

		AffinityBonus affinity = AffinityBonus.For(Player, Data);
		float attackSpeed = Mathf.Max(0.1f, Player.Stats.AttackSpeedMultiplier * (1f + affinity.AttackSpeed));
		float interval = Mathf.Max(0.12f, Data.FireRate / attackSpeed);
		_fireTimer -= dt;
		if (_fireTimer > 0f) return;

		IEnemy target = CombatEvents.NearestEnemy(GetTree(), GlobalPosition, Range);
		if (target == null)
		{
			_fireTimer = 0.15f;
			return;
		}
		_fireTimer = interval;
		Fire(target, affinity);
	}

	private void Fire(IEnemy target, AffinityBonus affinity)
	{
		Vector2 dir = (target.Body.GlobalPosition - GlobalPosition).Normalized();
		float damage = DamageCalculator.ForWeapon(Player, Data, Data.Damage).Amount;
		var bolt = new DroneBolt
		{
			Damage = damage,
			Speed = Data.Speed * Player.Stats.ProjectileSpeedMult * (1f + affinity.ProjectileSpeed),
			Pierce = Mathf.Max(1, Data.PierceCount + Player.Stats.BonusPierce),
			Direction = dir,
			Lifetime = Mathf.Max(0.5f, Data.Lifetime),
		};
		Player.GetParent().AddChild(bolt);
		bolt.GlobalPosition = GlobalPosition + dir * 8f;
		_flash = 1f;
	}

	protected override void UpdateOrbit()
	{
		base.UpdateOrbit();
		Rotation = 0f; // el dron siempre derecho
	}

	public override void _Draw()
	{
		// Cuerpo
		DrawCircle(Vector2.Zero, 6f, BodyColor);
		DrawArc(Vector2.Zero, 6f, 0f, Mathf.Tau, 16, EdgeColor, 1f, true);
		// Hélices
		for (int i = 0; i < 2; i++)
		{
			Vector2 c = new Vector2(i == 0 ? -7f : 7f, -5f);
			Vector2 blade = Vector2.FromAngle(_rotor + i) * 4f;
			DrawLine(c - blade, c + blade, EdgeColor, 1f);
		}
		// Luz (parpadea al disparar)
		Color light = LightColor;
		light.A = 0.7f + 0.3f * _flash;
		DrawCircle(new Vector2(0f, 1f), 2.2f + _flash, light);
	}
}
