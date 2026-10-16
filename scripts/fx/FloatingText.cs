using Godot;

/// <summary>Texto flotante en el mundo ("¡Bloqueado!", "Monedas insuficientes", etc.).</summary>
public partial class FloatingText : Node2D
{
	private const int MaxAlive = 25;
	private static int _alive;

	private float _duration = 0.9f;
	private float _time;
	private Label _label;
	private string _text = "";
	private Color _color = Colors.White;

	public static void Spawn(Vector2 globalPosition, string text, Color color, float duration = 0.9f)
	{
		if (_alive >= MaxAlive || string.IsNullOrEmpty(text)) return;
		var ft = new FloatingText
		{
			_text = text,
			_color = color,
			_duration = Mathf.Max(0.2f, duration),
			ZIndex = 20,
		};
		World.SpawnDeferred(ft, globalPosition);
	}

	public override void _Ready()
	{
		_label = UiFactory.MakeLabel(_text, 10, _color);
		_label.HorizontalAlignment = HorizontalAlignment.Center;
		_label.Size = new Vector2(160f, 16f);
		_label.Position = new Vector2(-80f, -24f);
		_label.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_label);
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
		float dt = (float)delta;
		_time += dt;
		Position += new Vector2(0f, -20f * dt);
		float k = Mathf.Clamp(_time / _duration, 0f, 1f);
		var m = Modulate;
		m.A = 1f - k * k;
		Modulate = m;
		if (_time >= _duration) QueueFree();
	}
}
