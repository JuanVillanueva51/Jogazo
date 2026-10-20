using Godot;
using System.Collections.Generic;

/// <summary>
/// Pantalla "Historia" del menú principal: reproduce la narración de introducción y muestra
/// su texto sincronizado (subtítulos). Se puede pausar, reiniciar, adelantar con la barra,
/// ver el texto completo (clic en una línea para saltar ahí) y volver con Escape.
/// El texto y los tiempos están en HistoryText.cs.
/// </summary>
public partial class HistoryScreen : Control
{
	public const string ScenePath = "res://scenes/ui/historia.tscn";
	private const string BackgroundPath = "res://sprites/GUI/background1.png";

	private static readonly Color TitleColor   = new Color(1f, 0.9f, 0.6f);
	private static readonly Color CurrentColor = new Color(1f, 0.97f, 0.9f);
	private static readonly Color PastColor    = new Color(0.62f, 0.6f, 0.68f);

	private AudioStreamPlayer _audio;
	private float _length;
	private float _fallbackTime;      // si no hay audio, el texto avanza solo
	private bool _paused;
	private bool _finished;
	private bool _seeking;
	private int _shownLine = -2;

	private Label _previousLabel;
	private Label _currentLabel;
	private Label _timeLabel;
	private HSlider _progress;
	private Button _playButton;
	private Button _fullTextButton;
	private Control _subtitleBox;
	private ScrollContainer _fullText;
	private readonly List<Button> _lineButtons = new();
	private bool _updatingSlider;

	public override void _Ready()
	{
		Theme = UiFactory.GetTheme();
		BuildUi();
		SetupAudio();
		Play();
	}

	// ───────────────────────── Construcción ─────────────────────────

	private void BuildUi()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		if (ResourceLoader.Exists(BackgroundPath))
		{
			var bg = new TextureRect
			{
				Texture = GD.Load<Texture2D>(BackgroundPath),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			bg.SetAnchorsPreset(LayoutPreset.FullRect);
			AddChild(bg);
		}
		var dim = new ColorRect { Color = new Color(0.02f, 0.01f, 0.05f, 0.72f), MouseFilter = MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(dim);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 70);
		margin.AddThemeConstantOverride("margin_right", 70);
		margin.AddThemeConstantOverride("margin_top", 28);
		margin.AddThemeConstantOverride("margin_bottom", 24);
		AddChild(margin);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 12);
		margin.AddChild(root);

		var title = UiFactory.MakeLabel("HISTORIA", 40, TitleColor);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(title);

		// Subtítulos (línea anterior tenue + línea actual grande)
		var center = new CenterContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		root.AddChild(center);
		var subtitleBox = new VBoxContainer { CustomMinimumSize = new Vector2(860f, 0f) };
		subtitleBox.AddThemeConstantOverride("separation", 18);
		center.AddChild(subtitleBox);
		_subtitleBox = subtitleBox;

		_previousLabel = UiFactory.MakeWrappedLabel("", 17, PastColor, 860f);
		_previousLabel.HorizontalAlignment = HorizontalAlignment.Center;
		subtitleBox.AddChild(_previousLabel);
		_currentLabel = UiFactory.MakeWrappedLabel("", 26, CurrentColor, 860f);
		_currentLabel.HorizontalAlignment = HorizontalAlignment.Center;
		subtitleBox.AddChild(_currentLabel);

		// Texto completo (oculto al inicio): ocupa el mismo lugar que los subtítulos
		_fullText = new ScrollContainer
		{
			Visible = false,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
		};
		root.AddChild(_fullText);
		root.MoveChild(_fullText, center.GetIndex() + 1);
		var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		list.AddThemeConstantOverride("separation", 2);
		_fullText.AddChild(list);
		for (int i = 0; i < HistoryText.Lines.Length; i++)
		{
			int index = i;
			var line = new Button
			{
				Text = HistoryText.Lines[i].Text,
				Flat = true,
				Alignment = HorizontalAlignment.Left,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				TooltipText = "Escuchar desde aquí",
			};
			line.AddThemeFontSizeOverride("font_size", 15);
			line.Pressed += () => SeekTo(HistoryText.Lines[index].Start);
			list.AddChild(line);
			_lineButtons.Add(line);
		}

		// Barra de progreso (se puede arrastrar) y tiempo
		var progressRow = new HBoxContainer();
		progressRow.AddThemeConstantOverride("separation", 12);
		root.AddChild(progressRow);
		_progress = new HSlider
		{
			MinValue = 0, MaxValue = 1, Step = 0.001,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			FocusMode = FocusModeEnum.None,
		};
		_progress.DragStarted += () => _seeking = true;
		_progress.DragEnded += changed => { _seeking = false; SeekTo((float)_progress.Value * TotalLength()); };
		_progress.ValueChanged += v => { if (!_updatingSlider && !_seeking) SeekTo((float)v * TotalLength()); };
		progressRow.AddChild(_progress);
		_timeLabel = UiFactory.MakeLabel("0:00 / 0:00", 14, UiFactory.TextMuted);
		_timeLabel.CustomMinimumSize = new Vector2(110f, 0f);
		_timeLabel.HorizontalAlignment = HorizontalAlignment.Right;
		progressRow.AddChild(_timeLabel);

		// Botones
		var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		buttons.AddThemeConstantOverride("separation", 14);
		root.AddChild(buttons);
		_playButton = AddButton(buttons, "Pausa [Espacio]", TogglePause);
		AddButton(buttons, "Reiniciar", Restart);
		_fullTextButton = AddButton(buttons, "Texto completo", ToggleFullText);
		AddButton(buttons, "Volver [Esc]", Back);
	}

	private static Button AddButton(Container parent, string text, System.Action onPressed)
	{
		var button = UiFactory.MakeButton(text, 16, 190f, 40f);
		UiFactory.StyleMenuButton(button);
		button.FocusMode = FocusModeEnum.None;
		button.Pressed += onPressed;
		parent.AddChild(button);
		return button;
	}

	private void SetupAudio()
	{
		_audio = new AudioStreamPlayer { Name = "Narracion" };
		AddChild(_audio);
		// Narración: va al bus general (no al de música ni al de efectos)
		_audio.Bus = "Master";
		if (ResourceLoader.Exists(HistoryText.AudioPath))
		{
			_audio.Stream = GD.Load<AudioStream>(HistoryText.AudioPath);
			_length = _audio.Stream != null ? (float)_audio.Stream.GetLength() : 0f;
		}
		if (_audio.Stream == null)
			GD.PushWarning($"HistoryScreen: no se encontró {HistoryText.AudioPath}; se muestra solo el texto.");
		_audio.Finished += () => { _finished = true; RefreshPlayButton(); };
	}

	// ───────────────────────── Reproducción ─────────────────────────

	private float TotalLength()
	{
		if (_length > 0f) return _length;
		var lines = HistoryText.Lines;
		return lines.Length > 0 ? lines[lines.Length - 1].Start + 8f : 1f;
	}

	private float CurrentTime()
	{
		if (_audio.Stream == null) return _fallbackTime;
		if (_finished) return TotalLength();
		if (!_audio.Playing) return _audio.GetPlaybackPosition();
		return (float)(_audio.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix());
	}

	private void Play(float from = 0f)
	{
		_finished = false;
		_paused = false;
		_fallbackTime = from;
		if (_audio.Stream != null) _audio.Play(from);
		RefreshPlayButton();
	}

	private void TogglePause()
	{
		if (_finished) { Restart(); return; }
		_paused = !_paused;
		if (_audio.Stream != null) _audio.StreamPaused = _paused;
		RefreshPlayButton();
	}

	private void Restart() => Play(0f);

	private void SeekTo(float seconds)
	{
		seconds = Mathf.Clamp(seconds, 0f, Mathf.Max(0f, TotalLength() - 0.1f));
		bool wasPaused = _paused;
		Play(seconds);
		if (wasPaused) TogglePause();
	}

	private void RefreshPlayButton()
	{
		if (_playButton == null) return;
		_playButton.Text = _finished ? "Escuchar de nuevo" : (_paused ? "Reanudar [Espacio]" : "Pausa [Espacio]");
	}

	private void ToggleFullText()
	{
		bool show = !_fullText.Visible;
		_fullText.Visible = show;
		_subtitleBox.GetParent<Control>().Visible = !show;
		_fullTextButton.Text = show ? "Ver subtítulos" : "Texto completo";
	}

	private void Back()
	{
		_audio?.Stop();
		GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
	}

	// ───────────────────────── Actualización ─────────────────────────

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("pause")) { Back(); return; }
		if (Input.IsKeyPressed(Key.Space) && !_spaceHeld) TogglePause();
		_spaceHeld = Input.IsKeyPressed(Key.Space);

		if (_audio.Stream == null && !_paused && !_finished)
		{
			_fallbackTime += (float)delta;
			if (_fallbackTime >= TotalLength()) { _finished = true; RefreshPlayButton(); }
		}

		float t = CurrentTime();
		float total = TotalLength();
		if (!_seeking)
		{
			_updatingSlider = true;
			_progress.Value = Mathf.Clamp(t / total, 0f, 1f);
			_updatingSlider = false;
		}
		_timeLabel.Text = $"{Format(t)} / {Format(total)}";

		int index = HistoryText.LineAt(t);
		if (index != _shownLine)
		{
			_shownLine = index;
			_previousLabel.Text = index > 0 ? HistoryText.Lines[index - 1].Text : "";
			_currentLabel.Text = index >= 0 ? HistoryText.Lines[index].Text : "";
			for (int i = 0; i < _lineButtons.Count; i++)
			{
				Color c = i == index ? TitleColor : (i < index ? PastColor : UiFactory.TextMuted);
				_lineButtons[i].AddThemeColorOverride("font_color", c);
			}
		}

		// Efecto de máquina de escribir en la línea actual
		if (index >= 0)
		{
			var line = HistoryText.Lines[index];
			float revealTime = Mathf.Clamp(line.Text.Length * 0.035f, 0.6f, 3.5f);
			_currentLabel.VisibleRatio = Mathf.Clamp((t - line.Start) / revealTime, 0f, 1f);
		}
	}

	private bool _spaceHeld;

	private static string Format(float seconds)
	{
		int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
		return $"{s / 60}:{s % 60:D2}";
	}
}
