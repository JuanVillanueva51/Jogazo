using Godot;

/// <summary>
/// Utilidades para construir interfaces desde código con el estilo del juego
/// (fuente Peaberry, paneles oscuros con borde).
/// </summary>
public static class UiFactory
{
	public const string FontPath = "res://sprites/GUI/Fonts/Peaberry-Font-v2.0/Peaberry Font Family/Peaberry Base/PeaberryBase.ttf";

	public static readonly Color PanelBg     = new Color(0.08f, 0.07f, 0.12f, 0.94f);
	public static readonly Color PanelBorder = new Color(0.55f, 0.45f, 0.75f, 1f);
	public static readonly Color TextMuted   = new Color(0.75f, 0.75f, 0.8f);
	public static readonly Color CoinColor   = new Color(1f, 0.85f, 0.3f);
	public static readonly Color CreditColor = new Color(0.45f, 0.9f, 1f);
	public static readonly Color ErrorColor  = new Color(1f, 0.4f, 0.4f);

	private static Theme _theme;
	private static Font _font;

	public static Font GameFont
	{
		get
		{
			if (_font == null && ResourceLoader.Exists(FontPath))
				_font = GD.Load<Font>(FontPath);
			return _font;
		}
	}

	public static Theme GetTheme()
	{
		if (_theme != null) return _theme;
		_theme = new Theme();
		if (GameFont != null) _theme.DefaultFont = GameFont;
		_theme.DefaultFontSize = 16;
		return _theme;
	}

	public static Label MakeLabel(string text, int fontSize = 16, Color? color = null, bool outline = true)
	{
		var label = new Label { Text = text };
		if (GameFont != null) label.AddThemeFontOverride("font", GameFont);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", color ?? Colors.White);
		if (outline)
		{
			label.AddThemeConstantOverride("outline_size", Mathf.Max(2, fontSize / 4));
			label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
		}
		return label;
	}

	public static Label MakeWrappedLabel(string text, int fontSize, Color? color, float minWidth)
	{
		var label = MakeLabel(text, fontSize, color);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.CustomMinimumSize = new Vector2(minWidth, 0f);
		return label;
	}

	public static Button MakeButton(string text, int fontSize = 16, float minWidth = 0f, float minHeight = 36f)
	{
		var button = new Button { Text = text };
		if (GameFont != null) button.AddThemeFontOverride("font", GameFont);
		button.AddThemeFontSizeOverride("font_size", fontSize);
		button.CustomMinimumSize = new Vector2(minWidth, minHeight);
		return button;
	}

	/// <summary>
	/// Estilo de botón de menú (gris oscuro con borde, como el botón "Reanudar" original),
	/// con estados normal / encima / presionado / deshabilitado.
	/// </summary>
	public static void StyleMenuButton(Button button, int fontSize = -1)
	{
		if (button == null) return;
		var normal   = MakeBox(new Color(0.27f, 0.27f, 0.30f, 0.97f), new Color(0.10f, 0.10f, 0.12f), 3, 7, 10f);
		var hover    = MakeBox(new Color(0.36f, 0.36f, 0.41f, 0.98f), new Color(1f, 0.85f, 0.40f), 3, 7, 10f);
		var pressed  = MakeBox(new Color(0.18f, 0.18f, 0.21f, 0.98f), new Color(1f, 0.85f, 0.40f), 3, 7, 10f);
		var disabled = MakeBox(new Color(0.20f, 0.20f, 0.22f, 0.80f), new Color(0.10f, 0.10f, 0.12f), 3, 7, 10f);
		normal.ShadowColor = new Color(0f, 0f, 0f, 0.45f);
		normal.ShadowOffset = new Vector2(0f, 3f);
		normal.ShadowSize = 1;
		button.AddThemeStyleboxOverride("normal", normal);
		button.AddThemeStyleboxOverride("hover", hover);
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		button.AddThemeStyleboxOverride("disabled", disabled);
		if (GameFont != null) button.AddThemeFontOverride("font", GameFont);
		if (fontSize > 0) button.AddThemeFontSizeOverride("font_size", fontSize);
		button.AddThemeColorOverride("font_color", Colors.White);
		button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.92f, 0.6f));
		button.AddThemeColorOverride("font_pressed_color", new Color(1f, 0.85f, 0.4f));
		button.AddThemeColorOverride("font_disabled_color", new Color(0.55f, 0.55f, 0.6f));
		button.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
		button.AddThemeConstantOverride("outline_size", 4);
	}

	/// <summary>Imagen pixel-art escalada sin suavizado.</summary>
	public static TextureRect MakeIconRect(Texture2D texture, float size)
	{
		var rect = new TextureRect
		{
			Texture = texture,
			CustomMinimumSize = new Vector2(size, size),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		return rect;
	}

	public static StyleBoxFlat MakeBox(Color bg, Color border, int borderWidth = 2, int radius = 4, float margin = 8f)
	{
		var box = new StyleBoxFlat { BgColor = bg, BorderColor = border };
		box.SetBorderWidthAll(borderWidth);
		box.SetCornerRadiusAll(radius);
		box.SetContentMarginAll(margin);
		return box;
	}

	public static PanelContainer MakePanel(Color? bg = null, Color? border = null, int borderWidth = 2, float margin = 10f)
	{
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", MakeBox(bg ?? PanelBg, border ?? PanelBorder, borderWidth, 6, margin));
		return panel;
	}

	/// <summary>Ícono simple de objeto: cuadro con color de rareza e iniciales.</summary>
	public static Control MakeItemIcon(ItemDefinition item, float size = 28f, int stacks = 0)
	{
		var panel = new Panel { CustomMinimumSize = new Vector2(size, size) };
		Color c = item != null ? item.Color : Colors.Gray;
		var bg = new Color(c.R * 0.25f, c.G * 0.25f, c.B * 0.25f, 0.95f);
		panel.AddThemeStyleboxOverride("panel", MakeBox(bg, c, 2, 4, 0f));
		panel.MouseFilter = Control.MouseFilterEnum.Pass;
		if (item != null) panel.TooltipText = $"{item.Name}\n{item.FullDescription()}";

		if (item != null && !string.IsNullOrEmpty(item.IconPath) && ResourceLoader.Exists(item.IconPath))
		{
			var tex = new TextureRect
			{
				Texture = GD.Load<Texture2D>(item.IconPath),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			tex.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			panel.AddChild(tex);
		}
		else
		{
			var letters = MakeLabel(item != null ? item.Initials : "?", Mathf.RoundToInt(size * 0.42f), c);
			letters.HorizontalAlignment = HorizontalAlignment.Center;
			letters.VerticalAlignment = VerticalAlignment.Center;
			letters.MouseFilter = Control.MouseFilterEnum.Ignore;
			letters.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			panel.AddChild(letters);
		}

		if (stacks > 1)
		{
			var count = MakeLabel($"x{stacks}", Mathf.Max(8, Mathf.RoundToInt(size * 0.32f)), Colors.White);
			count.HorizontalAlignment = HorizontalAlignment.Right;
			count.VerticalAlignment = VerticalAlignment.Bottom;
			count.MouseFilter = Control.MouseFilterEnum.Ignore;
			count.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			panel.AddChild(count);
		}
		return panel;
	}
}
