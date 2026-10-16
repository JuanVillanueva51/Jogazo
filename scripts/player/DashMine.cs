using Godot;

/// <summary>Mina que deja el dash "Propulsor": explota poco después de colocarse.</summary>
public partial class DashMine : Node2D
{
	private static readonly Color MineColor  = new Color(0.3f, 0.3f, 0.35f);
	private static readonly Color LightColor = new Color(1f, 0.35f, 0.25f);
	private static readonly Color BlastColor = new Color(1f, 0.6f, 0.25f);

	public float Damage = 20f;
	public float Radius = 70f;
	public float Fuse = 0.6f;

	private float _time;
	private bool _exploded;

	public override void _Ready()
	{
		ZIndex = 1;
	}

	public override void _Process(double delta)
	{
		if (_exploded) return;
		_time += (float)delta;
		QueueRedraw();
		if (_time >= Fuse) Explode();
	}

	private void Explode()
	{
		_exploded = true;
		FxBurst.Spawn(GlobalPosition, Radius, BlastColor, 0.35f);
		foreach (var enemy in CombatEvents.EnemiesInRadius(GetTree(), GlobalPosition, Radius))
			enemy.TakeDamage(Damage);
		QueueFree();
	}

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, 5f, MineColor);
		// Parpadea cada vez más rápido
		float blink = Mathf.Sin(_time * (10f + _time * 30f));
		if (blink > 0f) DrawCircle(Vector2.Zero, 2.2f, LightColor);
		Color ring = LightColor;
		ring.A = 0.25f;
		DrawArc(Vector2.Zero, Radius * Mathf.Clamp(_time / Fuse, 0f, 1f), 0f, Mathf.Tau, 32, ring, 1f, true);
	}
}
