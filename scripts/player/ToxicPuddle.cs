using Godot;

/// <summary>Charco tóxico que deja el dash "Estela tóxica": daña y ralentiza un poco.</summary>
public partial class ToxicPuddle : Node2D
{
	private static readonly Color PuddleColor = new Color(0.45f, 0.95f, 0.3f);
	private const float TickInterval = 0.5f;

	public float Damage = 5f;
	public float Radius = 22f;
	public float Duration = 2.5f;

	private float _time;
	private float _tick;

	public override void _Ready()
	{
		ZIndex = 0;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;
		_tick -= dt;
		if (_tick <= 0f)
		{
			_tick = TickInterval;
			foreach (var enemy in CombatEvents.EnemiesInRadius(GetTree(), GlobalPosition, Radius))
			{
				enemy.TakeDamage(Damage);
				if (!enemy.IsDead) enemy.ApplySlow(0.25f, 0.6f);
			}
		}
		QueueRedraw();
		if (_time >= Duration) QueueFree();
	}

	public override void _Draw()
	{
		float life = 1f - Mathf.Clamp(_time / Duration, 0f, 1f);
		Color fill = PuddleColor;
		fill.A = 0.28f * life;
		DrawCircle(Vector2.Zero, Radius, fill);
		Color edge = PuddleColor;
		edge.A = 0.5f * life;
		DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 24, edge, 1f, true);
		// Burbujas
		for (int i = 0; i < 3; i++)
		{
			float a = i * 2.1f + _time * 1.5f;
			Vector2 p = Vector2.FromAngle(a) * Radius * 0.5f;
			DrawCircle(p, 1.5f + Mathf.Sin(_time * 6f + i) * 0.7f, edge);
		}
	}
}
