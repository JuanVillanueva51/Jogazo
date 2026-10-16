using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Pantalla de configuración (se construye desde código). Se abre desde el menú principal
/// (escena configuration_menu.tscn) y desde el menú de pausa. Los cambios se aplican y
/// guardan al momento a través de SettingsManager. Se cierra con "Volver" o Escape.
/// </summary>
public partial class SettingsScreen : CanvasLayer
{
	private static readonly Color TitleColor = new Color(1f, 0.9f, 0.6f);
	private static readonly Color WaitingColor = new Color(1f, 0.85f, 0.3f);

	public static bool IsOpen { get; private set; }
	/// <summary>Frame en que se cerró (para que el mismo Escape no reanude la pausa).</summary>
	public static ulong LastClosedFrame { get; private set; }

	private Action _onClosed;
	private bool _dim = true;
	private bool _closing;

	private string _waitingAction;
	private ulong _escGuardFrame;
	private readonly Dictionary<string, Button> _keyButtons = new();

	// Controles que se refrescan
	private HSlider _master, _music, _sfx;
	private Label _masterValue, _musicValue, _sfxValue;
	private CheckBox _mute, _vsync, _dashPips, _toasts, _fps, _enemyLevel;
	private OptionButton _windowMode, _resolution, _fpsLimit;
	private Label _messageLabel;
	private bool _refreshing;

	/// <summary>Abre la configuración encima de todo. onClosed se llama al cerrar.</summary>
	public static SettingsScreen Open(Node context, Action onClosed = null, bool dimBackground = true)
	{
		if (context == null || !GodotObject.IsInstanceValid(context)) return null;
		var screen = new SettingsScreen { _onClosed = onClosed, _dim = dimBackground };
		context.GetTree().Root.AddChild(screen);
		return screen;
	}

	public override void _Ready()
	{
		Layer = 50;
		ProcessMode = ProcessModeEnum.Always;
		IsOpen = true;
		BuildUi();
		RefreshAll();
		if (SettingsManager.Instance != null) SettingsManager.Instance.Changed += RefreshAll;
	}

	public override void _ExitTree()
	{
		IsOpen = false;
		if (SettingsManager.Instance != null) SettingsManager.Instance.Changed -= RefreshAll;
	}

	public override void _Process(double delta)
	{
		if (_waitingAction == null && Engine.GetProcessFrames() != _escGuardFrame
			&& Input.IsActionJustPressed("pause"))
			Close();
	}

	public override void _Input(InputEvent @event)
	{
		if (_waitingAction == null) return;
		if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

		Key code = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
		string action = _waitingAction;
		_waitingAction = null;
		_escGuardFrame = Engine.GetProcessFrames();
		GetViewport().SetInputAsHandled();

		if (code == Key.Escape)
		{
			_messageLabel.Text = "Cambio cancelado.";
			RefreshControls();
			return;
		}
		SettingsManager.Instance?.SetKey(action, code);
		_messageLabel.Text = $"{SettingsManager.ActionDisplayName(action)}: {SettingsManager.KeyName(code)}";
	}

	private void Close()
	{
		if (_closing) return;
		_closing = true;
		IsOpen = false;
		LastClosedFrame = Engine.GetProcessFrames();
		Interactable.LockInteraction(0.3f);
		var callback = _onClosed;
		QueueFree();
		callback?.Invoke();
	}

	// ───────────────────────── Construcción ─────────────────────────

	private void BuildUi()
	{
		var root = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.Theme = UiFactory.GetTheme();
		AddChild(root);

		if (_dim)
		{
			var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.65f), MouseFilter = Control.MouseFilterEnum.Stop };
			dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			root.AddChild(dim);
		}

		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(center);

		var panel = UiFactory.MakePanel(null, UiFactory.PanelBorder, 3, 16f);
		panel.CustomMinimumSize = new Vector2(760f, 520f);
		center.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		panel.AddChild(vbox);

		var title = UiFactory.MakeLabel("CONFIGURACIÓN", 30, TitleColor);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		var tabs = new TabContainer { CustomMinimumSize = new Vector2(0f, 370f), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		if (UiFactory.GameFont != null) tabs.AddThemeFontOverride("font", UiFactory.GameFont);
		tabs.AddThemeFontSizeOverride("font_size", 16);
		vbox.AddChild(tabs);

		tabs.AddChild(BuildAudioTab());
		tabs.AddChild(BuildVideoTab());
		tabs.AddChild(BuildControlsTab());
		tabs.AddChild(BuildGameTab());

		_messageLabel = UiFactory.MakeLabel("", 13, UiFactory.TextMuted);
		_messageLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_messageLabel);

		var buttons = new HBoxContainer();
		buttons.AddThemeConstantOverride("separation", 16);
		buttons.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddChild(buttons);

		var reset = UiFactory.MakeButton("Restaurar todo", 16, 200f, 38f);
		UiFactory.StyleMenuButton(reset);
		reset.Pressed += () =>
		{
			SettingsManager.Instance?.ResetAll();
			_messageLabel.Text = "Se restauraron los valores predeterminados.";
		};
		buttons.AddChild(reset);

		var back = UiFactory.MakeButton("Volver [Esc]", 16, 200f, 38f);
		UiFactory.StyleMenuButton(back);
		back.Pressed += Close;
		buttons.AddChild(back);
	}

	private static VBoxContainer MakeTab(string name)
	{
		var tab = new VBoxContainer { Name = name };
		tab.AddThemeConstantOverride("separation", 12);
		return tab;
	}

	private static HBoxContainer MakeRow(VBoxContainer parent, string label, float labelWidth = 240f)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 12);
		var l = UiFactory.MakeLabel(label, 16, Colors.White);
		l.CustomMinimumSize = new Vector2(labelWidth, 0f);
		l.VerticalAlignment = VerticalAlignment.Center;
		row.AddChild(l);
		parent.AddChild(row);
		return row;
	}

	private HSlider MakeVolumeRow(VBoxContainer tab, string label, out Label valueLabel, Action<float> onChanged)
	{
		var row = MakeRow(tab, label);
		var slider = new HSlider
		{
			MinValue = 0, MaxValue = 100, Step = 1,
			CustomMinimumSize = new Vector2(320f, 24f),
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		};
		row.AddChild(slider);
		var value = UiFactory.MakeLabel("", 15, UiFactory.TextMuted);
		value.CustomMinimumSize = new Vector2(60f, 0f);
		row.AddChild(value);
		valueLabel = value;
		slider.ValueChanged += v =>
		{
			value.Text = $"{Mathf.RoundToInt((float)v)}%";
			if (!_refreshing) onChanged((float)v / 100f);
		};
		return slider;
	}

	private CheckBox MakeCheck(VBoxContainer tab, string text, Action<bool> onToggled)
	{
		var check = new CheckBox { Text = text };
		if (UiFactory.GameFont != null) check.AddThemeFontOverride("font", UiFactory.GameFont);
		check.AddThemeFontSizeOverride("font_size", 16);
		check.Toggled += on => { if (!_refreshing) onToggled(on); };
		tab.AddChild(check);
		return check;
	}

	private OptionButton MakeOption(VBoxContainer tab, string label, IEnumerable<string> items, Action<int> onSelected)
	{
		var row = MakeRow(tab, label);
		var option = new OptionButton { CustomMinimumSize = new Vector2(260f, 32f) };
		if (UiFactory.GameFont != null) option.AddThemeFontOverride("font", UiFactory.GameFont);
		option.AddThemeFontSizeOverride("font_size", 15);
		int i = 0;
		foreach (string item in items) option.AddItem(item, i++);
		option.ItemSelected += index => { if (!_refreshing) onSelected((int)index); };
		row.AddChild(option);
		return option;
	}

	private Control BuildAudioTab()
	{
		var tab = MakeTab("Audio");
		tab.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 6f) });
		_master = MakeVolumeRow(tab, "Volumen general", out _masterValue, v => SettingsManager.Instance?.SetMasterVolume(v));
		_music  = MakeVolumeRow(tab, "Música", out _musicValue, v => SettingsManager.Instance?.SetMusicVolume(v));
		_sfx    = MakeVolumeRow(tab, "Efectos", out _sfxValue, v => SettingsManager.Instance?.SetSfxVolume(v));
		_mute   = MakeCheck(tab, "Silenciar todo", on => SettingsManager.Instance?.SetMuted(on));
		return Wrap(tab);
	}

	private Control BuildVideoTab()
	{
		var tab = MakeTab("Video");
		tab.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 6f) });
		_windowMode = MakeOption(tab, "Modo de pantalla", SettingsManager.WindowModeNames,
			i => SettingsManager.Instance?.SetWindowMode(i));

		var resolutions = new List<string>();
		foreach (var r in SettingsManager.Resolutions) resolutions.Add($"{r.X} x {r.Y}");
		_resolution = MakeOption(tab, "Tamaño de ventana", resolutions, i => SettingsManager.Instance?.SetResolution(i));

		var fps = new List<string>();
		foreach (int f in SettingsManager.FpsLimits) fps.Add(f <= 0 ? "Sin límite" : $"{f} FPS");
		_fpsLimit = MakeOption(tab, "Límite de FPS", fps, i => SettingsManager.Instance?.SetFpsLimit(i));

		_vsync = MakeCheck(tab, "Sincronización vertical (VSync)", on => SettingsManager.Instance?.SetVSync(on));

		var note = UiFactory.MakeWrappedLabel("El tamaño de ventana solo se usa en modo Ventana. Si juegas desde el editor de Godot con la ventana integrada, algunos cambios de pantalla no se aplican.", 12, UiFactory.TextMuted, 680f);
		tab.AddChild(note);
		return Wrap(tab);
	}

	private Control BuildControlsTab()
	{
		var tab = MakeTab("Controles");
		tab.AddThemeConstantOverride("separation", 6);

		var list = new VBoxContainer();
		list.AddThemeConstantOverride("separation", 6);
		foreach (string action in SettingsManager.RebindableActions)
		{
			var row = MakeRow(list, SettingsManager.ActionDisplayName(action), 300f);
			var button = UiFactory.MakeButton("", 15, 220f, 32f);
			string captured = action;
			button.Pressed += () => StartRebind(captured);
			row.AddChild(button);
			_keyButtons[action] = button;
		}
		var scroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(0f, 250f),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
		};
		scroll.AddChild(list);
		tab.AddChild(scroll);

		tab.AddChild(UiFactory.MakeWrappedLabel("Haz clic en una tecla y presiona la nueva (Esc cancela). Las flechas, el clic izquierdo para interactuar y Esc para pausar siempre funcionan.", 12, UiFactory.TextMuted, 680f));

		var reset = UiFactory.MakeButton("Restaurar controles", 14, 220f, 32f);
		reset.Pressed += () =>
		{
			_waitingAction = null;
			SettingsManager.Instance?.ResetKeys();
			_messageLabel.Text = "Controles restaurados.";
		};
		tab.AddChild(reset);
		return Wrap(tab);
	}

	private Control BuildGameTab()
	{
		var tab = MakeTab("Juego");
		tab.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 6f) });
		_dashPips   = MakeCheck(tab, "Mostrar cargas de dash debajo del personaje", on => SettingsManager.Instance?.SetShowDashPips(on));
		_toasts     = MakeCheck(tab, "Mostrar aviso al recoger objetos", on => SettingsManager.Instance?.SetShowItemToasts(on));
		_enemyLevel = MakeCheck(tab, "Mostrar nivel de los enemigos", on => SettingsManager.Instance?.SetShowEnemyLevel(on));
		_fps        = MakeCheck(tab, "Mostrar FPS", on => SettingsManager.Instance?.SetShowFps(on));
		return Wrap(tab);
	}

	/// <summary>Margen interior para que el contenido no quede pegado al borde de la pestaña.</summary>
	private static Control Wrap(VBoxContainer tab)
	{
		var margin = new MarginContainer { Name = tab.Name };
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		tab.Name = "Contenido";
		margin.AddChild(tab);
		return margin;
	}

	// ───────────────────────── Refresco ─────────────────────────

	private void StartRebind(string action)
	{
		_waitingAction = action;
		RefreshControls();
		if (_keyButtons.TryGetValue(action, out var button))
		{
			button.Text = "Presiona una tecla...";
			button.AddThemeColorOverride("font_color", WaitingColor);
		}
		_messageLabel.Text = $"Nueva tecla para: {SettingsManager.ActionDisplayName(action)}";
	}

	private void RefreshAll()
	{
		var s = SettingsManager.Instance;
		if (s == null || _master == null) return;
		_refreshing = true;

		_master.Value = Mathf.RoundToInt(s.MasterVolume * 100f);
		_music.Value  = Mathf.RoundToInt(s.MusicVolume * 100f);
		_sfx.Value    = Mathf.RoundToInt(s.SfxVolume * 100f);
		_masterValue.Text = $"{Mathf.RoundToInt(s.MasterVolume * 100f)}%";
		_musicValue.Text  = $"{Mathf.RoundToInt(s.MusicVolume * 100f)}%";
		_sfxValue.Text    = $"{Mathf.RoundToInt(s.SfxVolume * 100f)}%";
		_mute.ButtonPressed = s.Muted;

		int mode = s.CurrentWindowModeIndex();
		_windowMode.Selected = mode;
		_resolution.Selected = s.CurrentResolutionIndex();
		_resolution.Disabled = mode != 0;
		_fpsLimit.Selected = s.FpsLimitIndex;
		_vsync.ButtonPressed = s.VSync;

		_dashPips.ButtonPressed   = s.ShowDashPips;
		_toasts.ButtonPressed     = s.ShowItemToasts;
		_enemyLevel.ButtonPressed = s.ShowEnemyLevel;
		_fps.ButtonPressed        = s.ShowFps;

		_refreshing = false;
		RefreshControls();
	}

	private void RefreshControls()
	{
		var s = SettingsManager.Instance;
		foreach (var kv in _keyButtons)
		{
			if (kv.Key == _waitingAction) continue;
			kv.Value.Text = s != null ? SettingsManager.KeyName(s.GetKey(kv.Key)) : "—";
			kv.Value.RemoveThemeColorOverride("font_color");
		}
	}
}
