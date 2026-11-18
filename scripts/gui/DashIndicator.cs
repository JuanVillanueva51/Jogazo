using Godot;

/// <summary>
/// Indicador de dash del HUD: tecla, nombre del dash y una barrita por carga.
/// La carga que se está recuperando se va llenando; al recuperarse parpadea.
/// </summary>
public partial class DashIndicator : Control
{
	private static readonly Color EmptyColor  = new Color(0.05f, 0.05f, 0.08f, 0.85f);
	private static readonly Color BorderColor = new Color(0f, 0f, 0f, 0.9f);
	private static readonly Color TextColor   = new Color(0.9f, 0.92f, 1f);
	private const float PipWidth = 22f;
	private const float PipHeight = 9f;
	private const float Gap = 4f;

	public Player Player { get; set; }

	private int _lastCharges = -1;
	private float _flash;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(260f, 20f);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		if (Player == null || !IsInstanceValid(Player)) return;
		int charges = Player.DashCharges;
		if (_lastCharges >= 0 && charges > _lastCharges) _flash = 1f;
		_lastCharges = charges;
		_flash = Mathf.Max(0f, _flash - (float)delta * 3f);
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (Player == null || !IsInstanceValid(Player)) return;
		Font font = UiFactory.GameFont ?? ThemeDB.FallbackFont;
		DashProfile dash = Player.DashInfo;
		Color color = dash != null ? dash.Color : Colors.White;

		string key = SettingsManager.Instance != null
			? SettingsManager.KeyName(SettingsManager.Instance.GetKey("dash"))
			: "Espacio";
		string label = $"[{key}]";
		const int fontSize = 11;
		float y = PipHeight + 1f;
		DrawStringOutline(font, new Vector2(0f, y), label, HorizontalAlignment.Left, -1, fontSize, 4, BorderColor);
		DrawString(font, new Vector2(0f, y), label, HorizontalAlignment.Left, -1, fontSize, TextColor);
		float x = font.GetStringSize(label, HorizontalAlignment.Left, -1, fontSize).X + 6f;

		int max = Mathf.Max(1, Player.DashMaxCharges);
		int charges = Player.DashCharges;
		float progress = Player.DashRechargeProgress;
		for (int i = 0; i < max; i++)
		{
			var outer = new Rect2(x, 1f, PipWidth, PipHeight);
			DrawRect(outer.Grow(1.5f), BorderColor);
			DrawRect(outer, EmptyColor);
			float fill = i < charges ? 1f : (i == charges ? progress : 0f);
			if (fill > 0f)
			{
				Color c = i < charges ? color : color.Darkened(0.45f);
				if (i == charges - 1 && _flash > 0f) c = c.Lerp(Colors.White, _flash);
				DrawRect(new Rect2(x, 1f, PipWidth * fill, PipHeight), c);
				if (i < charges) DrawRect(new Rect2(x, 1f, PipWidth * fill, 2f), c.Lightened(0.4f));
			}
			x += PipWidth + Gap;
		}

		if (dash != null)
		{
			x += 4f;
			DrawStringOutline(font, new Vector2(x, y), dash.Name, HorizontalAlignment.Left, -1, fontSize, 4, BorderColor);
			DrawString(font, new Vector2(x, y), dash.Name, HorizontalAlignment.Left, -1, fontSize, color.Lightened(0.3f));
		}
	}
}
