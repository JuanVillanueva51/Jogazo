using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Configuración del juego (autoload). Guarda en user://settings.cfg y aplica:
///  - Audio: volumen general, música y efectos (crea los buses "Musica" y "Efectos").
///  - Video: modo de ventana, resolución, VSync, límite de FPS.
///  - Controles: tecla principal de cada acción (las teclas secundarias, como flechas, se conservan).
///  - Juego: dificultad de las partidas nuevas y opciones de interfaz (cargas de dash, avisos, FPS, nivel enemigo).
/// </summary>
public partial class SettingsManager : Node
{
	public static SettingsManager Instance { get; private set; }

	public const string FilePath = "user://settings.cfg";
	public const string MusicBus = "Musica";
	public const string SfxBus = "Efectos";

	/// <summary>Acciones que se pueden reasignar (Escape/pausa queda fija para no bloquear los menús).</summary>
	public static readonly string[] RebindableActions =
	{
		"move_up", "move_down", "move_left", "move_right", "dash", "interact", "use_item",
	};

	public static readonly string[] WindowModeNames = { "Ventana", "Pantalla completa (sin bordes)", "Pantalla completa (exclusiva)" };
	public static readonly Vector2I[] Resolutions =
	{
		new(1152, 648), new(1280, 720), new(1366, 768), new(1600, 900), new(1920, 1080), new(2560, 1440),
	};
	public static readonly int[] FpsLimits = { 0, 30, 60, 120, 144, 240 };

	// ── Audio ──
	public float MasterVolume { get; private set; } = 1f;
	public float MusicVolume  { get; private set; } = 0.8f;
	public float SfxVolume    { get; private set; } = 0.8f;
	public bool  Muted        { get; private set; }

	// ── Video ── (-1 = no tocar lo que tenga el proyecto)
	public int  WindowMode      { get; private set; } = -1;
	public int  ResolutionIndex { get; private set; } = -1;
	public bool VSync           { get; private set; } = true;
	public int  FpsLimitIndex   { get; private set; } = 0;

	// ── Juego ──
	public bool ShowDashPips    { get; private set; } = true;
	public bool ShowItemToasts  { get; private set; } = true;
	public bool ShowFps         { get; private set; }
	public bool ShowEnemyLevel  { get; private set; } = true;
	/// <summary>Dificultad de las partidas nuevas (índice de Balance.Difficulties).</summary>
	public int  DifficultyIndex { get; private set; } = 0;

	/// <summary>Se dispara cuando cambia cualquier opción.</summary>
	public event Action Changed;

	private readonly Dictionary<string, Key> _defaultKeys = new();
	private readonly Dictionary<string, Key> _keys = new();
	private readonly Dictionary<string, InputEventKey> _managedEvents = new();

	public override void _EnterTree() => Instance = this;

	public override void _ExitTree()
	{
		if (Instance == this) Instance = null;
	}

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		EnsureBus(MusicBus);
		EnsureBus(SfxBus);
		CaptureDefaultKeys();
		Load();
		ApplyAll();
		GetTree().NodeAdded += OnNodeAdded;
	}

	// ───────────────────────── Audio ─────────────────────────

	public void SetMasterVolume(float v) { MasterVolume = Mathf.Clamp(v, 0f, 1f); ApplyAudio(); SaveAndNotify(); }
	public void SetMusicVolume(float v)  { MusicVolume  = Mathf.Clamp(v, 0f, 1f); ApplyAudio(); SaveAndNotify(); }
	public void SetSfxVolume(float v)    { SfxVolume    = Mathf.Clamp(v, 0f, 1f); ApplyAudio(); SaveAndNotify(); }
	public void SetMuted(bool muted)     { Muted = muted; ApplyAudio(); SaveAndNotify(); }

	private static int EnsureBus(string name)
	{
		int index = AudioServer.GetBusIndex(name);
		if (index >= 0) return index;
		AudioServer.AddBus();
		index = AudioServer.BusCount - 1;
		AudioServer.SetBusName(index, name);
		AudioServer.SetBusSend(index, "Master");
		return index;
	}

	private void ApplyAudio()
	{
		SetBus("Master", Muted ? 0f : MasterVolume);
		SetBus(MusicBus, MusicVolume);
		SetBus(SfxBus, SfxVolume);
	}

	private static void SetBus(string name, float linear)
	{
		int index = AudioServer.GetBusIndex(name);
		if (index < 0) return;
		AudioServer.SetBusMute(index, linear <= 0.001f);
		AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Mathf.Max(linear, 0.0001f)));
	}

	/// <summary>Manda cada reproductor nuevo al bus que le toca (música o efectos).</summary>
	private void OnNodeAdded(Node node)
	{
		switch (node)
		{
			case AudioStreamPlayer player when player.Bus.ToString() == "Master":
				player.Bus = IsMusic(player) ? MusicBus : SfxBus;
				break;
			case AudioStreamPlayer2D player2D when player2D.Bus.ToString() == "Master":
				player2D.Bus = SfxBus;
				break;
			case VideoStreamPlayer video when video.Bus.ToString() == "Master":
				video.Bus = MusicBus;
				break;
		}
	}

	private static bool IsMusic(AudioStreamPlayer player)
	{
		string name = player.Name.ToString().ToLowerInvariant();
		if (name.Contains("music") || name.Contains("musica") || name.Contains("música")) return true;
		return player.Stream != null && player.Stream.GetLength() > 30.0;
	}

	// ───────────────────────── Video ─────────────────────────

	public void SetWindowMode(int mode) { WindowMode = Mathf.Clamp(mode, 0, WindowModeNames.Length - 1); ApplyVideo(); SaveAndNotify(); }
	public void SetResolution(int index) { ResolutionIndex = Mathf.Clamp(index, 0, Resolutions.Length - 1); if (WindowMode < 0) WindowMode = 0; ApplyVideo(); SaveAndNotify(); }
	public void SetVSync(bool on) { VSync = on; ApplyVideo(); SaveAndNotify(); }
	public void SetFpsLimit(int index) { FpsLimitIndex = Mathf.Clamp(index, 0, FpsLimits.Length - 1); ApplyVideo(); SaveAndNotify(); }

	/// <summary>Modo de ventana actual (para mostrar en el menú aunque no se haya elegido).</summary>
	public int CurrentWindowModeIndex()
	{
		if (WindowMode >= 0) return WindowMode;
		return DisplayServer.WindowGetMode() switch
		{
			DisplayServer.WindowMode.ExclusiveFullscreen => 2,
			DisplayServer.WindowMode.Fullscreen => 1,
			_ => 0,
		};
	}

	public int CurrentResolutionIndex()
	{
		if (ResolutionIndex >= 0) return ResolutionIndex;
		Vector2I size = DisplayServer.WindowGetSize();
		for (int i = 0; i < Resolutions.Length; i++)
			if (Resolutions[i] == size) return i;
		return 0;
	}

	private void ApplyVideo()
	{
		try
		{
			DisplayServer.WindowSetVsyncMode(VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
			Engine.MaxFps = FpsLimits[Mathf.Clamp(FpsLimitIndex, 0, FpsLimits.Length - 1)];

			if (WindowMode < 0) return; // el jugador nunca cambió la ventana: respetar el proyecto
			switch (WindowMode)
			{
				case 1:
					DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
					break;
				case 2:
					DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
					break;
				default:
					DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
					if (ResolutionIndex >= 0) ResizeAndCenter(Resolutions[ResolutionIndex]);
					break;
			}
		}
		catch (Exception e)
		{
			GD.PushWarning($"SettingsManager: no se pudo aplicar el video ({e.Message})");
		}
	}

	private static void ResizeAndCenter(Vector2I size)
	{
		int screen = DisplayServer.WindowGetCurrentScreen();
		Rect2I usable = DisplayServer.ScreenGetUsableRect(screen);
		size = new Vector2I(Mathf.Min(size.X, usable.Size.X), Mathf.Min(size.Y, usable.Size.Y));
		DisplayServer.WindowSetSize(size);
		DisplayServer.WindowSetPosition(usable.Position + (usable.Size - size) / 2);
	}

	// ───────────────────────── Juego ─────────────────────────

	public void SetShowDashPips(bool v)   { ShowDashPips = v; SaveAndNotify(); }
	public void SetShowItemToasts(bool v) { ShowItemToasts = v; SaveAndNotify(); }
	public void SetShowFps(bool v)        { ShowFps = v; SaveAndNotify(); }
	public void SetShowEnemyLevel(bool v) { ShowEnemyLevel = v; SaveAndNotify(); }
	public void SetDifficulty(int index)  { DifficultyIndex = Mathf.Clamp(index, 0, Balance.Difficulties.Length - 1); SaveAndNotify(); }

	// ───────────────────────── Controles ─────────────────────────

	public static string ActionDisplayName(string action) => action switch
	{
		"move_up"    => "Mover arriba",
		"move_down"  => "Mover abajo",
		"move_left"  => "Mover izquierda",
		"move_right" => "Mover derecha",
		"dash"       => "Dash",
		"interact"   => "Interactuar (cofres, tienda)",
		"use_item"   => "Usar objeto activo",
		"pause"      => "Pausa",
		_ => action,
	};

	public Key GetKey(string action) => _keys.TryGetValue(action, out var key) ? key : Key.None;

	public static string KeyName(Key key)
	{
		if (key == Key.None) return "—";
		return key switch
		{
			Key.Space  => "Espacio",
			Key.Escape => "Esc",
			Key.Enter  => "Enter",
			Key.Shift  => "Shift",
			Key.Ctrl   => "Ctrl",
			Key.Alt    => "Alt",
			Key.Tab    => "Tab",
			Key.Up     => "Flecha arriba",
			Key.Down   => "Flecha abajo",
			Key.Left   => "Flecha izquierda",
			Key.Right  => "Flecha derecha",
			_ => OS.GetKeycodeString(key),
		};
	}

	/// <summary>
	/// Cambia la tecla principal de una acción. Si otra acción usaba esa tecla,
	/// intercambian teclas para no dejar dos acciones con la misma.
	/// </summary>
	public void SetKey(string action, Key key)
	{
		if (key == Key.None || Array.IndexOf(RebindableActions, action) < 0) return;
		Key previous = GetKey(action);
		foreach (string other in RebindableActions)
		{
			if (other != action && GetKey(other) == key)
			{
				ApplyKey(other, previous);
				break;
			}
		}
		ApplyKey(action, key);
		SaveAndNotify();
	}

	public void ResetKeys()
	{
		foreach (var kv in _defaultKeys) ApplyKey(kv.Key, kv.Value);
		SaveAndNotify();
	}

	/// <summary>Guarda la tecla principal original de cada acción (la primera tecla del Mapa de entrada).</summary>
	private void CaptureDefaultKeys()
	{
		foreach (string action in RebindableActions)
		{
			if (!InputMap.HasAction(action)) InputMap.AddAction(action);
			InputEventKey primary = null;
			foreach (InputEvent ev in InputMap.ActionGetEvents(action))
			{
				if (ev is InputEventKey k) { primary = k; break; }
			}
			if (primary == null && action == "use_item")
			{
				primary = new InputEventKey { PhysicalKeycode = Key.Q };
				InputMap.ActionAddEvent(action, primary);
			}
			Key key = primary == null ? Key.None : (primary.PhysicalKeycode != Key.None ? primary.PhysicalKeycode : primary.Keycode);
			_defaultKeys[action] = key;
			_keys[action] = key;
			if (primary != null) _managedEvents[action] = primary;
		}
	}

	private void ApplyKey(string action, Key key)
	{
		if (!InputMap.HasAction(action)) return;
		if (_managedEvents.TryGetValue(action, out var old) && old != null)
			InputMap.ActionEraseEvent(action, old);
		if (key == Key.None)
		{
			_managedEvents.Remove(action);
			_keys[action] = Key.None;
			return;
		}
		var ev = new InputEventKey { PhysicalKeycode = key };
		InputMap.ActionAddEvent(action, ev);
		_managedEvents[action] = ev;
		_keys[action] = key;
	}

	// ───────────────────────── Guardar / cargar ─────────────────────────

	public void ResetAll()
	{
		MasterVolume = 1f; MusicVolume = 0.8f; SfxVolume = 0.8f; Muted = false;
		VSync = true; FpsLimitIndex = 0;
		ShowDashPips = true; ShowItemToasts = true; ShowFps = false; ShowEnemyLevel = true;
		DifficultyIndex = 0;
		foreach (var kv in _defaultKeys) ApplyKey(kv.Key, kv.Value);
		ApplyAll();
		SaveAndNotify();
	}

	private void ApplyAll()
	{
		ApplyAudio();
		ApplyVideo();
	}

	private void SaveAndNotify()
	{
		Save();
		Changed?.Invoke();
	}

	private void Load()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(FilePath) != Error.Ok) return;

		MasterVolume = (float)cfg.GetValue("audio", "master", MasterVolume);
		MusicVolume  = (float)cfg.GetValue("audio", "music", MusicVolume);
		SfxVolume    = (float)cfg.GetValue("audio", "sfx", SfxVolume);
		Muted        = (bool)cfg.GetValue("audio", "muted", Muted);

		WindowMode      = (int)cfg.GetValue("video", "window_mode", WindowMode);
		ResolutionIndex = (int)cfg.GetValue("video", "resolution", ResolutionIndex);
		VSync           = (bool)cfg.GetValue("video", "vsync", VSync);
		FpsLimitIndex   = (int)cfg.GetValue("video", "fps_limit", FpsLimitIndex);

		ShowDashPips   = (bool)cfg.GetValue("game", "dash_pips", ShowDashPips);
		ShowItemToasts = (bool)cfg.GetValue("game", "item_toasts", ShowItemToasts);
		ShowFps        = (bool)cfg.GetValue("game", "show_fps", ShowFps);
		ShowEnemyLevel = (bool)cfg.GetValue("game", "enemy_level", ShowEnemyLevel);
		DifficultyIndex = Mathf.Clamp((int)cfg.GetValue("game", "difficulty", DifficultyIndex), 0, Balance.Difficulties.Length - 1);

		foreach (string action in RebindableActions)
		{
			if (!cfg.HasSectionKey("controls", action)) continue;
			long code = (long)cfg.GetValue("controls", action, 0L);
			if (code > 0 && (Key)code != GetKey(action)) ApplyKey(action, (Key)code);
		}
	}

	private void Save()
	{
		var cfg = new ConfigFile();
		cfg.SetValue("audio", "master", MasterVolume);
		cfg.SetValue("audio", "music", MusicVolume);
		cfg.SetValue("audio", "sfx", SfxVolume);
		cfg.SetValue("audio", "muted", Muted);

		cfg.SetValue("video", "window_mode", WindowMode);
		cfg.SetValue("video", "resolution", ResolutionIndex);
		cfg.SetValue("video", "vsync", VSync);
		cfg.SetValue("video", "fps_limit", FpsLimitIndex);

		cfg.SetValue("game", "dash_pips", ShowDashPips);
		cfg.SetValue("game", "item_toasts", ShowItemToasts);
		cfg.SetValue("game", "show_fps", ShowFps);
		cfg.SetValue("game", "enemy_level", ShowEnemyLevel);
		cfg.SetValue("game", "difficulty", DifficultyIndex);

		foreach (string action in RebindableActions)
			cfg.SetValue("controls", action, (long)GetKey(action));

		Error err = cfg.Save(FilePath);
		if (err != Error.Ok) GD.PushWarning($"SettingsManager: no se pudo guardar ({err})");
	}
}
