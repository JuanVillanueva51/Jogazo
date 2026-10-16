using Godot;

/// <summary>Círculo que se expande y desvanece. Efecto visual para explosiones y áreas.</summary>
public partial class FxBurst : Node2D
{
	private const int MaxAlive = 40;
	private static int _alive;

	private float _radius = 40f;
	private Color _color = Colors.White;
	private float _duration = 0.35f;
	private float _time;

	public static void Spawn(Vector2 globalPosition, float radius, Color color, float duration = 0.35f)
	{
		if (_alive >= MaxAlive) return;
		var fx = new FxBurst
		{
			_radius = radius,
			_color = color,
			_duration = Mathf.Max(0.05f, duration),
			ZIndex = 5,
		};
		World.SpawnDeferred(fx, globalPosition);
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
		if (_time >= _duration)
		{
			QueueFree();
			return;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		float k = Mathf.Clamp(_time / _duration, 0f, 1f);
		float r = Mathf.Lerp(_radius * 0.3f, _radius, k);
		var fill = _color;
		fill.A = 0.35f * (1f - k);
		DrawCircle(Vector2.Zero, r, fill);
		var edge = _color;
		edge.A = 0.9f * (1f - k);
		DrawArc(Vector2.Zero, r, 0f, Mathf.Tau, 32, edge, 2f);
	}
}
