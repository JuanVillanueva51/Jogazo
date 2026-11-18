using Godot;

/// <summary>Rayo en zigzag entre dos puntos (cadenas eléctricas, rayos del cielo).</summary>
public partial class FxBolt : Line2D
{
	private const int MaxAlive = 40;
	private static int _alive;

	private float _duration = 0.2f;
	private float _time;

	public static void Spawn(Vector2 from, Vector2 to, Color color, float width = 2f, float duration = 0.2f)
	{
		if (_alive >= MaxAlive) return;
		var bolt = new FxBolt
		{
			_duration = Mathf.Max(0.05f, duration),
			Width = width,
			DefaultColor = color,
			ZIndex = 6,
		};
		// Puntos relativos al origen del rayo
		Vector2 delta = to - from;
		const int segments = 6;
		bolt.AddPoint(Vector2.Zero);
		for (int i = 1; i < segments; i++)
		{
			float t = (float)i / segments;
			Vector2 p = delta * t;
			p += new Vector2((float)GD.RandRange(-8.0, 8.0), (float)GD.RandRange(-8.0, 8.0));
			bolt.AddPoint(p);
		}
		bolt.AddPoint(delta);
		World.SpawnDeferred(bolt, from);
	}

	public override void _EnterTree()
	{
		_alive++;
	}

	public override void _ExitTree()
	{
		_alive = Mathf.Max(0, _alive - 1);
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		float k = Mathf.Clamp(_time / _duration, 0f, 1f);
		var m = Modulate;
		m.A = 1f - k;
		Modulate = m;
		if (_time >= _duration) QueueFree();
	}
}
