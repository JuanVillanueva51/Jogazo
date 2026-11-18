using Godot;

/// <summary>
/// Guía del nivel inicial (nodo "GuiaTutorial" en forest_map.tscn).
/// Muestra el objetivo actual arriba de la pantalla y una flecha hacia dónde ir:
/// moverse → abrir el cofre → recoger el objeto → dash → torre de mejora (opcional) → salida.
/// En la salida dibuja flechas en el suelo para que se vea hacia dónde continuar.
/// </summary>
public partial class TutorialGuide : Node2D
{
	[Export] public NodePath ChestPath = "../Botin/CofreTutorial";
	[Export] public NodePath TowerPath = "../LevelUpTower";
	[Export] public NodePath ExitPath  = "../AreaTransicion";

	private enum Step { Move, Chest, Item, Dash, Tower, Exit }
	private const int StepCount = 6;

	private static readonly Color PanelBg     = new Color(0.06f, 0.05f, 0.1f, 0.86f);
	private static readonly Color GuideColor  = new Color(1f, 0.85f, 0.3f);
	private static readonly Color ExitColor   = new Color(0.45f, 1f, 0.55f);
	private static readonly Color DoneColor   = new Color(0.5f, 1f, 0.5f);

	private Step _step = Step.Move;
	private Player _player;
	private Chest _chest;
	private LevelUpTower _tower;
	private Area2D _exit;
	private Vector2 _spawnPosition;
	private bool _hasSpawn;
	private float _doneTimer;
	private float _time;

	private CanvasLayer _layer;
	private Label _header;
	private Label _text;
	private ObjectivePointer _pointer;

	public override void _Ready()
	{
		ZIndex = 4;
		// Sigue corriendo en pausa solo para esconderse detrás de los menús (torre, pausa...)
		ProcessMode = ProcessModeEnum.Always;
		_chest = GetNodeOrNull<Chest>(ChestPath);
		_tower = GetNodeOrNull<LevelUpTower>(TowerPath);
		_exit  = GetNodeOrNull<Area2D>(ExitPath);
		BuildUi();
	}

	private void BuildUi()
	{
		var layer = new CanvasLayer { Name = "GuiaUI", Layer = 1 };
		AddChild(layer);
		_layer = layer;

		_pointer = new ObjectivePointer { Name = "Flecha" };
		layer.AddChild(_pointer);

		var panel = UiFactory.MakePanel(PanelBg, GuideColor.Darkened(0.3f), 2, 8f);
		panel.MouseFilter = Control.MouseFilterEnum.Ignore;
		panel.CustomMinimumSize = new Vector2(480f, 0f);
		layer.AddChild(panel);
		// Centrado abajo, encima de la barra de experiencia (arriba van los avisos de objetos)
		panel.AnchorLeft = 0.5f;
		panel.AnchorRight = 0.5f;
		panel.AnchorTop = 1f;
		panel.AnchorBottom = 1f;
		panel.GrowHorizontal = Control.GrowDirection.Both;
		panel.GrowVertical = Control.GrowDirection.Begin;
		panel.OffsetLeft = -240f;
		panel.OffsetRight = 240f;
		panel.OffsetTop = -74f;
		panel.OffsetBottom = -74f;

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 2);
		panel.AddChild(box);
		_header = UiFactory.MakeLabel("", 11, GuideColor);
		_header.HorizontalAlignment = HorizontalAlignment.Center;
		box.AddChild(_header);
		_text = UiFactory.MakeWrappedLabel("", 15, Colors.White, 460f);
		_text.HorizontalAlignment = HorizontalAlignment.Center;
		box.AddChild(_text);
	}

	public override void _Process(double delta)
	{
		bool paused = GetTree().Paused;
		if (_layer != null) _layer.Visible = !paused;
		if (paused) return;

		float dt = (float)delta;
		_time += dt;
		if (_player == null || !IsInstanceValid(_player))
		{
			_player = GetTree().GetFirstNodeInGroup("player") as Player;
			_hasSpawn = false;
			if (_player == null) return;
		}
		if (!_hasSpawn)
		{
			_spawnPosition = _player.GlobalPosition;
			_hasSpawn = true;
		}

		if (_doneTimer > 0f)
		{
			// "¡Bien!" un momento antes de pasar al siguiente paso
			_doneTimer -= dt;
			if (_doneTimer <= 0f) Advance();
		}
		else if (_step != Step.Exit && IsStepDone(_step))
		{
			_doneTimer = 0.8f;
		}

		UpdateUi();
		QueueRedraw();
	}

	private bool IsStepDone(Step step)
	{
		switch (step)
		{
			case Step.Move:
				return _player.GlobalPosition.DistanceTo(_spawnPosition) > 70f;
			case Step.Chest:
				return _chest == null || !IsInstanceValid(_chest) || _chest.IsOpened;
			case Step.Item:
				var inv = _player.Inventory;
				return inv == null || inv.ActiveItem != null || inv.GetPassiveItems().Count > 0;
			case Step.Dash:
				return _player.DashCharges < _player.DashMaxCharges;
			case Step.Tower:
				// Opcional: también se da por hecho si el jugador ya va hacia la salida
				return _tower == null || !IsInstanceValid(_tower) || _tower.IsUsed
					|| (_exit != null && _player.GlobalPosition.DistanceTo(ExitPoint()) < 260f);
			default:
				return false;
		}
	}

	private void Advance()
	{
		if (_step < Step.Exit) _step++;
	}

	private void UpdateUi()
	{
		bool done = _doneTimer > 0f;
		_header.Text = $"TUTORIAL · PASO {(int)_step + 1}/{StepCount}";
		_text.Text = done ? "¡Bien!" : StepText(_step);
		_text.AddThemeColorOverride("font_color", done ? DoneColor : Colors.White);

		// Flecha hacia el objetivo del paso
		switch (_step)
		{
			case Step.Chest when _chest != null && IsInstanceValid(_chest) && !_chest.IsOpened:
				SetPointer(_chest.GlobalPosition, "COFRE", GuideColor);
				break;
			case Step.Item:
				ItemPickup pickup = FindNearestPickup();
				if (pickup != null) SetPointer(pickup.GlobalPosition, "OBJETO", GuideColor);
				else _pointer.Target = null;
				break;
			case Step.Tower when _tower != null && IsInstanceValid(_tower):
				SetPointer(_tower.GlobalPosition, "TORRE", GuideColor, 60f);
				break;
			case Step.Exit:
			case Step.Move:
			case Step.Dash:
				// Mientras aprende a moverse y a hacer dash, ya se señala la salida
				if (_exit != null && IsInstanceValid(_exit)) SetPointer(ExitPoint(), "SALIDA", ExitColor);
				else _pointer.Target = null;
				break;
			default:
				_pointer.Target = null;
				break;
		}
	}

	private void SetPointer(Vector2 target, string label, Color color, float hover = 34f)
	{
		_pointer.Target = target;
		_pointer.TargetLabel = label;
		_pointer.PointerColor = color;
		_pointer.HoverHeight = hover;
	}

	private string StepText(Step step)
	{
		string interact = Key("interact", "E");
		string dash = Key("dash", "Espacio");
		bool demo = GameManager.Instance != null && GameManager.Instance.IsDemo;
		return step switch
		{
			Step.Move  => $"Muévete con {Key("move_up", "W")} {Key("move_left", "A")} {Key("move_down", "S")} {Key("move_right", "D")} o con las flechas.",
			Step.Chest => $"Abre el cofre gratis: acércate y presiona [{interact}].",
			Step.Item  => "Recoge el objeto que salió del cofre pasando encima de él.",
			Step.Dash  => $"Haz un dash con [{dash}]. Mientras dura, no recibes daño.",
			Step.Tower => "Opcional: párate dentro de la torre de mejora hasta llenar la barra.",
			_ => demo
				? "¡Listo! Sigue la flecha hasta la salida para empezar el demo."
				: "¡Listo! Sigue la flecha hasta la salida para empezar la partida.",
		};
	}

	private static string Key(string action, string fallback)
	{
		if (SettingsManager.Instance == null) return fallback;
		Key key = SettingsManager.Instance.GetKey(action);
		return key == Godot.Key.None ? fallback : SettingsManager.KeyName(key);
	}

	private Vector2 ExitPoint()
	{
		Vector2 from = _player != null ? _player.GlobalPosition : GlobalPosition;
		return ObjectivePointer.ClosestPointInArea(_exit, from);
	}

	private ItemPickup FindNearestPickup()
	{
		Node scene = GetTree().CurrentScene;
		if (scene == null || _player == null) return null;
		ItemPickup best = null;
		float bestDist = float.MaxValue;
		foreach (Node child in scene.GetChildren())
		{
			if (child is ItemPickup pickup && IsInstanceValid(pickup))
			{
				float d = pickup.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);
				if (d < bestDist) { bestDist = d; best = pickup; }
			}
		}
		return best;
	}

	/// <summary>Flechas en el suelo junto a la salida, apuntando hacia afuera del mapa.</summary>
	public override void _Draw()
	{
		if (_exit == null || !IsInstanceValid(_exit) || _player == null) return;
		Vector2 point = ExitPoint();
		Vector2 dir = point.X >= _player.GlobalPosition.X ? Vector2.Right : Vector2.Left;
		Vector2 perp = new Vector2(-dir.Y, dir.X);
		bool highlight = _step == Step.Exit;
		for (int row = -2; row <= 2; row++)
		{
			for (int i = 0; i < 3; i++)
			{
				float wave = Mathf.PosMod(_time * 2.2f - i * 0.35f, 1f);
				float alpha = (highlight ? 0.9f : 0.45f) * (0.35f + 0.65f * (1f - wave));
				Vector2 c = ToLocal(point + perp * row * 46f + dir * (i * 16f - 10f));
				Color color = new Color(ExitColor, alpha);
				Vector2 tip = c + dir * 7f;
				DrawPolyline(new[] { tip - dir * 9f + perp * 8f, tip, tip - dir * 9f - perp * 8f }, color, 3f, true);
			}
		}
	}
}
