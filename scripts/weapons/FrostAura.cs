using System.Linq;
using Godot;

/// <summary>
/// Aura de Escarcha: zona permanente alrededor del jugador que daña y ralentiza
/// a los enemigos dentro. Usa el sistema de armas de zona (FollowPlayer = true)
/// pero recalcula daño y tamaño en cada pulso, así las mejoras se aplican al momento.
/// </summary>
public partial class FrostAura : ZoneWeapon
{
	private const float BaseRadius = 52f;
	private static readonly Color FillColor = new Color(0.55f, 0.85f, 1f, 0.10f);
	private static readonly Color RingColor = new Color(0.7f, 0.92f, 1f, 0.45f);
	private static readonly Color CrystalColor = new Color(0.85f, 0.97f, 1f, 0.85f);

	private Player _player;
	private float _tickTimer;
	private float _time;
	private float _pulse;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 2;
		Monitorable = false;
		ZIndex = 0;
		bool hasShape = false;
		foreach (Node child in GetChildren())
			if (child is CollisionShape2D) { hasShape = true; break; }
		if (!hasShape)
			AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = BaseRadius } });
		base._Ready();
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
	}

	public override void _Process(double delta)
	{
		if (_player == null || !IsInstanceValid(_player) || Data == null)
		{
			_player = GetTree().GetFirstNodeInGroup("player") as Player;
			if (_player == null) return;
		}
		float dt = (float)delta;
		_time += dt;
		_pulse = Mathf.Max(0f, _pulse - dt * 3f);

		GlobalPosition = _player.GlobalPosition;
		AffinityBonus affinity = AffinityBonus.For(_player, Data);
		Scale = Vector2.One * Data.BulletScale * _player.Stats.SizeMultiplier * (1f + affinity.Size);

		float attackSpeed = Mathf.Max(0.1f, _player.Stats.AttackSpeedMultiplier * (1f + affinity.AttackSpeed));
		float interval = Mathf.Max(0.1f, Data.FireRate / attackSpeed);
		_tickTimer += dt;
		if (_tickTimer >= interval)
		{
			_tickTimer = 0f;
			Pulse();
		}
		QueueRedraw();
	}

	private void Pulse()
	{
		bool hitSomething = false;
		foreach (var body in enemiesInside.ToArray())
		{
			if (!IsInstanceValid(body))
			{
				enemiesInside.Remove(body);
				continue;
			}
			if (body is IEnemy enemy)
			{
				if (enemy.IsDead) continue;
				enemy.ApplySlow(0.35f, 0.8f);
			}
			if (body is IDamageable damageable)
			{
				damageable.TakeDamage(DamageCalculator.ForWeapon(_player, Data, Data.Damage).Amount);
				hitSomething = true;
			}
		}
		if (hitSomething) _pulse = 1f;
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, BaseRadius, FillColor);
		Color ring = RingColor;
		ring.A += 0.35f * _pulse;
		DrawArc(Vector2.Zero, BaseRadius, 0f, Mathf.Tau, 48, ring, 1.5f + _pulse, true);

		// Cristales de hielo girando
		const int crystals = 6;
		for (int i = 0; i < crystals; i++)
		{
			float a = _time * 0.8f + Mathf.Tau * i / crystals;
			Vector2 p = Vector2.FromAngle(a) * (BaseRadius - 6f);
			var diamond = new Vector2[]
			{
				p + new Vector2(0f, -4f), p + new Vector2(2.5f, 0f),
				p + new Vector2(0f, 4f),  p + new Vector2(-2.5f, 0f),
			};
			DrawColoredPolygon(diamond, CrystalColor);
		}
	}
}
