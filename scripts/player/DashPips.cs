using Godot;

/// <summary>
/// Indicador pequeño de cargas de dash debajo del personaje.
/// Solo aparece mientras alguna carga se está recargando (se puede apagar en Configuración).
/// </summary>
public partial class DashPips : Node2D
{
	private static readonly Color EmptyColor = new Color(0f, 0f, 0f, 0.55f);
	private const float PipWidth = 7f;
	private const float PipHeight = 2.5f;
	private const float Gap = 2f;

	private Player _player;
	private float _alpha;

	public override void _Ready()
	{
		_player = GetParent() as Player;
		Position = new Vector2(0f, 9f);
		ZIndex = 5;
	}

	public override void _Process(double delta)
	{
		if (_player == null) return;
		bool enabled = SettingsManager.Instance == null || SettingsManager.Instance.ShowDashPips;
		bool recharging = _player.DashCharges < _player.DashMaxCharges;
		float target = enabled && recharging ? 1f : 0f;
		_alpha = Mathf.MoveToward(_alpha, target, (float)delta * (target > _alpha ? 8f : 2.5f));
		Visible = _alpha > 0.01f;
		if (Visible) QueueRedraw();
	}

	public override void _Draw()
	{
		if (_player == null) return;
		int max = Mathf.Max(1, _player.DashMaxCharges);
		int charges = _player.DashCharges;
		float progress = _player.DashRechargeProgress;
		Color full = _player.DashInfo != null ? _player.DashInfo.Color : Colors.White;

		float total = max * PipWidth + (max - 1) * Gap;
		float x = -total * 0.5f;
		for (int i = 0; i < max; i++)
		{
			var rect = new Rect2(x, 0f, PipWidth, PipHeight);
			DrawRect(rect.Grow(0.6f), WithAlpha(EmptyColor, _alpha));
			float fill = i < charges ? 1f : (i == charges ? progress : 0f);
			if (fill > 0f)
			{
				Color c = i < charges ? full : full.Darkened(0.35f);
				DrawRect(new Rect2(x, 0f, PipWidth * fill, PipHeight), WithAlpha(c, _alpha));
			}
			x += PipWidth + Gap;
		}
	}

	private static Color WithAlpha(Color c, float a)
	{
		c.A *= a;
		return c;
	}
}
