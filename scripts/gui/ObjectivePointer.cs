using Godot;

/// <summary>
/// Flecha que indica hacia dónde ir. Si el objetivo está fuera de la pantalla, aparece en el borde
/// apuntando hacia él (con su nombre y la distancia); si está en pantalla, rebota encima del objetivo.
/// Va dentro de un CanvasLayer. La usan la guía del tutorial (cofre, torre, salida) y el HUD (jefe, salida).
/// </summary>
public partial class ObjectivePointer : Control
{
	private static readonly Color Outline = new Color(0.05f, 0.04f, 0.07f, 0.95f);

	/// <summary>Posición global del objetivo. Null = no mostrar nada.</summary>
	public Vector2? Target { get; set; }
	public string TargetLabel { get; set; } = "";
	public Color PointerColor { get; set; } = new Color(1f, 0.85f, 0.3f);
	/// <summary>Altura (px de pantalla) a la que flota la flecha sobre el objetivo cuando se ve.</summary>
	public float HoverHeight { get; set; } = 34f;

	private float _time;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (Target == null) return;
		Viewport viewport = GetViewport();
		if (viewport == null) return;

		Vector2 size = GetViewportRect().Size;
		Vector2 screen = viewport.GetCanvasTransform() * Target.Value;
		Rect2 safe = new Rect2(Vector2.Zero, size).Grow(-46f);
		Font font = UiFactory.GameFont ?? ThemeDB.FallbackFont;
		const int fontSize = 13;

		if (safe.HasPoint(screen))
		{
			// En pantalla: flecha hacia abajo que rebota sobre el objetivo
			float bob = Mathf.Sin(_time * 5f) * 4f;
			Vector2 tip = screen + new Vector2(0f, -HoverHeight + bob);
			DrawArrow(tip, Vector2.Down);
			DrawLabel(font, fontSize, TargetLabel, tip + new Vector2(0f, -26f));
			return;
		}

		// Fuera de pantalla: flecha en el borde, apuntando hacia el objetivo
		Vector2 center = size * 0.5f;
		Vector2 dir = (screen - center).Normalized();
		if (dir == Vector2.Zero) dir = Vector2.Right;
		float t = DistanceToEdge(safe, center, dir);
		float pulse = 1f + 0.08f * Mathf.Sin(_time * 6f);
		Vector2 point = center + dir * t;
		DrawArrow(point, dir, pulse);

		// Nombre y distancia, del lado de adentro de la flecha
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		string text = TargetLabel;
		if (player != null)
		{
			int meters = Mathf.RoundToInt(player.GlobalPosition.DistanceTo(Target.Value) / 16f);
			text = string.IsNullOrEmpty(text) ? $"{meters} m" : $"{text} · {meters} m";
		}
		DrawLabel(font, fontSize, text, point - dir * 34f);
	}

	private void DrawArrow(Vector2 tip, Vector2 dir, float scale = 1f)
	{
		Vector2 perp = new Vector2(-dir.Y, dir.X);
		float length = 20f * scale, half = 11f * scale;
		Vector2 baseCenter = tip - dir * length;
		Vector2[] tri = { tip, baseCenter + perp * half, baseCenter - perp * half };
		Vector2[] outline = { tip + dir * 3f, baseCenter + perp * (half + 3f) - dir * 2f, baseCenter - perp * (half + 3f) - dir * 2f };
		DrawColoredPolygon(outline, Outline);
		DrawColoredPolygon(tri, PointerColor);
		// brillo
		Vector2[] shine = { tip - dir * 4f, baseCenter + perp * half * 0.45f, baseCenter };
		DrawColoredPolygon(shine, PointerColor.Lightened(0.45f));
	}

	private void DrawLabel(Font font, int fontSize, string text, Vector2 centerPos)
	{
		if (string.IsNullOrEmpty(text)) return;
		Vector2 textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
		Vector2 size = GetViewportRect().Size;
		Vector2 pos = centerPos - new Vector2(textSize.X * 0.5f, -fontSize * 0.35f);
		pos.X = Mathf.Clamp(pos.X, 6f, size.X - textSize.X - 6f);
		pos.Y = Mathf.Clamp(pos.Y, fontSize + 4f, size.Y - 6f);
		DrawStringOutline(font, pos, text, HorizontalAlignment.Left, -1, fontSize, 5, Outline);
		DrawString(font, pos, text, HorizontalAlignment.Left, -1, fontSize, PointerColor.Lightened(0.35f));
	}

	/// <summary>Punto de un área (su primer RectangleShape2D) más cercano a "from". Sirve para salidas largas.</summary>
	public static Vector2 ClosestPointInArea(Area2D area, Vector2 from)
	{
		if (area == null) return from;
		foreach (Node child in area.GetChildren())
		{
			if (child is CollisionShape2D shape && shape.Shape is RectangleShape2D rect)
			{
				Transform2D xf = shape.GlobalTransform;
				Vector2 local = xf.AffineInverse() * from;
				Vector2 half = rect.Size * 0.5f;
				local = new Vector2(Mathf.Clamp(local.X, -half.X, half.X), Mathf.Clamp(local.Y, -half.Y, half.Y));
				return xf * local;
			}
		}
		return area.GlobalPosition;
	}

	private static float DistanceToEdge(Rect2 rect, Vector2 from, Vector2 dir)
	{
		float tx = float.MaxValue, ty = float.MaxValue;
		if (dir.X > 0.0001f)       tx = (rect.End.X - from.X) / dir.X;
		else if (dir.X < -0.0001f) tx = (rect.Position.X - from.X) / dir.X;
		if (dir.Y > 0.0001f)       ty = (rect.End.Y - from.Y) / dir.Y;
		else if (dir.Y < -0.0001f) ty = (rect.Position.Y - from.Y) / dir.Y;
		return Mathf.Max(0f, Mathf.Min(tx, ty));
	}
}
