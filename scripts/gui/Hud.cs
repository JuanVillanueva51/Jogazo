using Godot;
using System;

/// <summary>
/// HUD de la partida. La barra de vida viene de la escena (hud.tscn); el resto se
/// construye o se re-estiliza desde código:
///  - Vida con número, indicador de dash y objetos (arriba a la izquierda).
///  - Tiempo y nivel enemigo (arriba al centro).
///  - Monedas animadas y Créditos (arriba a la derecha).
///  - Insignia de nivel + barra de experiencia animada (abajo).
///  - Objeto activo (abajo a la izquierda) y aviso al recoger objetos.
/// </summary>
public partial class Hud : CanvasLayer
{
	private const string CoinSheet = "res://sprites/Currency/coins/goldCoinAnim.png";
	private static readonly Rect2[] CoinFrames =
	{
		new(204, 0, 204, 201), new(408, 0, 204, 201), new(612, 0, 204, 201), new(816, 0, 204, 201),
		new(0, 201, 204, 201), new(204, 201, 204, 201), new(408, 201, 204, 201), new(612, 201, 204, 201),
		new(816, 201, 204, 201),
	};
	private static readonly Color XpColor      = new Color(0.35f, 0.85f, 1f);
	private static readonly Color XpFlashColor = new Color(1f, 1f, 0.75f);
	private static readonly Color LevelGold    = new Color(1f, 0.82f, 0.3f);

	private TextureProgressBar _hpBar;
	private ProgressBar _xpBar;
	private Label _levelLabel;
	private Label _timerLabel;
	private Label _coinsLabel;
	private Player _player;

	// Elementos nuevos (creados desde código)
	private Label _hpText;
	private DashIndicator _dashIndicator;
	private HFlowContainer _itemStrip;
	private PanelContainer _activePanel;
	private Control _activeIconHolder;
	private Label _activeNameLabel;
	private Label _activeStatusLabel;
	private Label _creditsLabel;
	private Label _difficultyLabel;
	private Label _waveLabel;
	private Label _toastLabel;
	private Label _fpsLabel;
	private float _toastTimer;
	private PlayerInventory _inventory;
	private string _shownActiveId;

	// Monedas
	private PanelContainer _coinPanel;
	private Label _coinCount;
	private Label _coinGain;
	private AtlasTexture _coinTexture;
	private float _coinFrameTime;
	private int _coinFrame;
	private int _lastCoins = -1;
	private int _pendingGain;
	private float _gainTimer;

	// Nivel y experiencia
	private PanelContainer _levelBadge;
	private Label _levelNumber;
	private Label _xpText;
	private Label _levelUpLabel;
	private StyleBoxFlat _xpFill;
	private float _displayXp;
	private int _lastLevel = -1;
	private float _levelFlash;
	private float _levelUpTimer;

	public override void _Ready()
	{
		_hpBar = GetNode<TextureProgressBar>("HpBar");
		_xpBar = GetNode<ProgressBar>("XpBar");
		_levelLabel = GetNode<Label>("LevelLabel");
		_timerLabel = GetNode<Label>("TimerLabel");
		_coinsLabel = GetNode<Label>("CoinsLabel");

		BuildExtraUi();
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
	}

	public override void _ExitTree()
	{
		if (_inventory != null)
		{
			_inventory.ItemAdded -= OnItemAdded;
			_inventory.InventoryChanged -= RebuildItems;
		}
	}

	// ───────────────────────── Construcción ─────────────────────────

	private void BuildExtraUi()
	{
		Vector2 screen = GetViewport().GetVisibleRect().Size;

		// ── Vida ──
		_hpBar.Position = new Vector2(4f, 4f);
		_hpText = UiFactory.MakeLabel("", 15, Colors.White);
		_hpText.Position = new Vector2(310f, 13f);
		_hpText.Size = new Vector2(120f, 20f);
		AddChild(_hpText);

		// ── Dash ──
		_dashIndicator = new DashIndicator { Position = new Vector2(10f, 48f) };
		AddChild(_dashIndicator);

		// ── Objetos ──
		_itemStrip = new HFlowContainer
		{
			Position = new Vector2(6f, 70f),
			Size = new Vector2(440f, 60f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_itemStrip.AddThemeConstantOverride("h_separation", 3);
		_itemStrip.AddThemeConstantOverride("v_separation", 3);
		AddChild(_itemStrip);

		// ── Tiempo y nivel enemigo ──
		_timerLabel.Visible = true;
		if (UiFactory.GameFont != null) _timerLabel.AddThemeFontOverride("font", UiFactory.GameFont);
		_timerLabel.AddThemeFontSizeOverride("font_size", 24);
		_timerLabel.AddThemeConstantOverride("outline_size", 6);
		_timerLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
		_timerLabel.OffsetTop = 2f;
		_timerLabel.OffsetBottom = 30f;

		_difficultyLabel = UiFactory.MakeLabel("", 11, new Color(1f, 0.7f, 0.6f));
		_difficultyLabel.Position = new Vector2(screen.X * 0.5f - 140f, 30f);
		_difficultyLabel.Size = new Vector2(280f, 14f);
		_difficultyLabel.HorizontalAlignment = HorizontalAlignment.Center;
		AddChild(_difficultyLabel);

		// Aviso de oleada (EnemySpawner.WaveActive)
		_waveLabel = UiFactory.MakeLabel("¡OLEADA!", 16, UiFactory.ErrorColor);
		_waveLabel.Position = new Vector2(screen.X * 0.5f - 100f, 44f);
		_waveLabel.Size = new Vector2(200f, 20f);
		_waveLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_waveLabel.Visible = false;
		AddChild(_waveLabel);

		BuildCoinPanel(screen);
		BuildXpBar(screen);

		// ── Objeto activo ──
		_activePanel = UiFactory.MakePanel(new Color(0.06f, 0.05f, 0.1f, 0.85f), UiFactory.PanelBorder, 2, 6f);
		_activePanel.Position = new Vector2(8f, screen.Y - 46f - 54f);
		_activePanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_activePanel.Visible = false;
		AddChild(_activePanel);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		_activePanel.AddChild(row);
		_activeIconHolder = new Control { CustomMinimumSize = new Vector2(36f, 36f) };
		row.AddChild(_activeIconHolder);
		var col = new VBoxContainer();
		row.AddChild(col);
		_activeNameLabel = UiFactory.MakeLabel("", 13, Colors.White);
		col.AddChild(_activeNameLabel);
		_activeStatusLabel = UiFactory.MakeLabel("", 12, UiFactory.TextMuted);
		col.AddChild(_activeStatusLabel);

		// ── Aviso de objeto ──
		_toastLabel = UiFactory.MakeLabel("", 16, Colors.White);
		_toastLabel.Position = new Vector2(screen.X * 0.5f - 300f, 74f);
		_toastLabel.Size = new Vector2(600f, 40f);
		_toastLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_toastLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_toastLabel.Visible = false;
		AddChild(_toastLabel);

		// ── FPS (opcional) ──
		_fpsLabel = UiFactory.MakeLabel("", 11, UiFactory.TextMuted);
		_fpsLabel.Position = new Vector2(screen.X - 90f, screen.Y - 44f);
		_fpsLabel.Size = new Vector2(82f, 14f);
		_fpsLabel.HorizontalAlignment = HorizontalAlignment.Right;
		AddChild(_fpsLabel);
	}

	private void BuildCoinPanel(Vector2 screen)
	{
		// Se reemplazan la moneda y el número originales por un panel más legible
		_coinsLabel.Visible = false;
		var oldCoin = GetNodeOrNull<CanvasItem>("AnimatedSprite2D");
		if (oldCoin != null) oldCoin.Visible = false;
		_levelLabel.Visible = false; // el nivel ahora va junto a la barra de experiencia

		_coinPanel = UiFactory.MakePanel(new Color(0.07f, 0.06f, 0.1f, 0.82f), new Color(0.85f, 0.65f, 0.2f), 2, 6f);
		_coinPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_coinPanel.CustomMinimumSize = new Vector2(176f, 0f);
		AddChild(_coinPanel);
		// Anclado arriba a la derecha: si el número crece, el panel crece hacia la izquierda
		_coinPanel.AnchorLeft = 1f;
		_coinPanel.AnchorRight = 1f;
		_coinPanel.AnchorTop = 0f;
		_coinPanel.AnchorBottom = 0f;
		_coinPanel.GrowHorizontal = Control.GrowDirection.Begin;
		_coinPanel.OffsetLeft = -184f;
		_coinPanel.OffsetRight = -8f;
		_coinPanel.OffsetTop = 6f;
		_coinPanel.OffsetBottom = 6f;

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 0);
		_coinPanel.AddChild(vbox);

		var coinRow = new HBoxContainer();
		coinRow.AddThemeConstantOverride("separation", 6);
		vbox.AddChild(coinRow);

		if (ResourceLoader.Exists(CoinSheet))
		{
			_coinTexture = new AtlasTexture { Atlas = GD.Load<Texture2D>(CoinSheet), Region = CoinFrames[0] };
			var icon = UiFactory.MakeIconRect(_coinTexture, 26f);
			icon.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
			coinRow.AddChild(icon);
		}

		_coinCount = UiFactory.MakeLabel("", 24, UiFactory.CoinColor);
		_coinCount.VerticalAlignment = VerticalAlignment.Center;
		coinRow.AddChild(_coinCount);

		_coinGain = UiFactory.MakeLabel("", 14, new Color(1f, 0.95f, 0.6f));
		_coinGain.VerticalAlignment = VerticalAlignment.Center;
		_coinGain.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		_coinGain.HorizontalAlignment = HorizontalAlignment.Right;
		coinRow.AddChild(_coinGain);

		_creditsLabel = UiFactory.MakeLabel("", 12, UiFactory.CreditColor);
		vbox.AddChild(_creditsLabel);
	}

	private void BuildXpBar(Vector2 screen)
	{
		// Barra de experiencia: más delgada, con estilo propio y espacio para la insignia de nivel
		_xpBar.OffsetLeft = 74f;
		_xpBar.OffsetRight = -10f;
		_xpBar.OffsetTop = -26f;
		_xpBar.OffsetBottom = -10f;
		_xpBar.ShowPercentage = false;
		_xpBar.MinValue = 0;
		_xpBar.MaxValue = 1;
		_xpBar.Step = 0.0001;
		_xpBar.AddThemeStyleboxOverride("background",
			UiFactory.MakeBox(new Color(0.05f, 0.04f, 0.09f, 0.88f), new Color(0.55f, 0.45f, 0.75f), 2, 5, 0f));
		_xpFill = UiFactory.MakeBox(XpColor, XpColor.Darkened(0.35f), 1, 4, 0f);
		_xpBar.AddThemeStyleboxOverride("fill", _xpFill);

		_xpText = UiFactory.MakeLabel("", 11, Colors.White);
		_xpText.HorizontalAlignment = HorizontalAlignment.Center;
		_xpText.VerticalAlignment = VerticalAlignment.Center;
		_xpText.MouseFilter = Control.MouseFilterEnum.Ignore;
		_xpText.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_xpBar.AddChild(_xpText);

		// Insignia de nivel
		_levelBadge = UiFactory.MakePanel(new Color(0.12f, 0.08f, 0.02f, 0.95f), LevelGold, 2, 2f);
		_levelBadge.Position = new Vector2(8f, screen.Y - 46f);
		_levelBadge.CustomMinimumSize = new Vector2(60f, 38f);
		_levelBadge.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_levelBadge);

		var badgeBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		badgeBox.AddThemeConstantOverride("separation", -4);
		_levelBadge.AddChild(badgeBox);
		var caption = UiFactory.MakeLabel("NIVEL", 9, LevelGold);
		caption.HorizontalAlignment = HorizontalAlignment.Center;
		badgeBox.AddChild(caption);
		_levelNumber = UiFactory.MakeLabel("1", 20, Colors.White);
		_levelNumber.HorizontalAlignment = HorizontalAlignment.Center;
		badgeBox.AddChild(_levelNumber);

		_levelUpLabel = UiFactory.MakeLabel("", 18, LevelGold);
		_levelUpLabel.Position = new Vector2(screen.X * 0.5f - 150f, screen.Y - 56f);
		_levelUpLabel.Size = new Vector2(300f, 24f);
		_levelUpLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_levelUpLabel.Visible = false;
		AddChild(_levelUpLabel);
	}

	// ───────────────────────── Actualización ─────────────────────────

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		//buscar al jugador aun si no esta
		if (_player == null || !IsInstanceValid(_player))
		{
			_player = GetTree().GetFirstNodeInGroup("player") as Player;
			return;
		}
		if (_dashIndicator.Player != _player) _dashIndicator.Player = _player;
		if (_inventory == null && _player.Inventory != null)
		{
			_inventory = _player.Inventory;
			_inventory.ItemAdded += OnItemAdded;
			_inventory.InventoryChanged += RebuildItems;
			RebuildItems();
		}

		var gm = GameManager.Instance;
		var settings = SettingsManager.Instance;

		UpdateHealth();
		UpdateCoins(gm.Coins, dt);
		UpdateXp(gm.CurrentLevel, gm.CurrentXp, gm.XpToNextLevel, dt);

		int seconds      = (int)gm.ElapsedTime;
		_timerLabel.Text = $"{seconds / 60:D2}:{seconds % 60:D2}";

		bool showLevel = settings == null || settings.ShowEnemyLevel;
		_difficultyLabel.Text = showLevel && gm.currentMapLevel > 0 ? $"Nivel enemigo {gm.EnemyLevel:0.0} · {gm.Difficulty.Name}" : "";

		bool wave = EnemySpawner.WaveActive && gm.currentMapLevel > 0;
		_waveLabel.Visible = wave;
		if (wave)
		{
			var wm = _waveLabel.Modulate;
			wm.A = 0.55f + 0.45f * Mathf.Sin(Time.GetTicksMsec() / 140f);
			_waveLabel.Modulate = wm;
		}

		int total = ProfileManager.Instance != null ? ProfileManager.Instance.Credits : 0;
		_creditsLabel.Text = $"{Balance.MetaCurrencyName}: {total}  (+{gm.Run.CreditsEarned})";

		bool showFps = settings != null && settings.ShowFps;
		_fpsLabel.Visible = showFps;
		if (showFps) _fpsLabel.Text = $"{Engine.GetFramesPerSecond():0} FPS";

		UpdateActivePanel();

		if (_toastTimer > 0f)
		{
			_toastTimer -= dt;
			var m = _toastLabel.Modulate;
			m.A = Mathf.Clamp(_toastTimer, 0f, 1f);
			_toastLabel.Modulate = m;
			if (_toastTimer <= 0f) _toastLabel.Visible = false;
		}
	}

	private void UpdateHealth()
	{
		_hpBar.MaxValue = _player.MaxHp;
		_hpBar.Value    = _player.CurrentHp;
		_hpText.Text = $"{Mathf.CeilToInt(_player.CurrentHp)} / {Mathf.RoundToInt(_player.MaxHp)}";

		// Parpadeo rojo con poca vida
		float ratio = _player.MaxHp > 0f ? _player.CurrentHp / _player.MaxHp : 1f;
		if (ratio < 0.3f)
		{
			float pulse = 0.5f + 0.5f * Mathf.Sin(Time.GetTicksMsec() / 120f);
			_hpText.AddThemeColorOverride("font_color", Colors.White.Lerp(UiFactory.ErrorColor, pulse));
		}
		else
		{
			_hpText.AddThemeColorOverride("font_color", Colors.White);
		}
	}

	private void UpdateCoins(int coins, float dt)
	{
		// Moneda girando
		if (_coinTexture != null)
		{
			_coinFrameTime += dt;
			if (_coinFrameTime >= 0.11f)
			{
				_coinFrameTime = 0f;
				_coinFrame = (_coinFrame + 1) % CoinFrames.Length;
				_coinTexture.Region = CoinFrames[_coinFrame];
			}
		}

		if (_lastCoins < 0) _lastCoins = coins;
		if (coins != _lastCoins)
		{
			int diff = coins - _lastCoins;
			_lastCoins = coins;
			_coinCount.Text = coins.ToString("N0");
			Pop(_coinCount, diff > 0 ? 1.25f : 0.85f);
			if (diff > 0)
			{
				_pendingGain += diff;
				_gainTimer = 1.2f;
				_coinGain.Text = $"+{_pendingGain}";
				_coinGain.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.6f));
			}
			else
			{
				_pendingGain = 0;
				_gainTimer = 1.2f;
				_coinGain.Text = $"{diff}";
				_coinGain.AddThemeColorOverride("font_color", UiFactory.ErrorColor);
			}
		}
		else if (_coinCount.Text == "")
		{
			_coinCount.Text = coins.ToString("N0");
		}

		if (_gainTimer > 0f)
		{
			_gainTimer -= dt;
			var m = _coinGain.Modulate;
			m.A = Mathf.Clamp(_gainTimer / 0.4f, 0f, 1f);
			_coinGain.Modulate = m;
			if (_gainTimer <= 0f)
			{
				_pendingGain = 0;
				_coinGain.Text = "";
			}
		}
	}

	private void UpdateXp(int level, float xp, float required, float dt)
	{
		if (_lastLevel < 0) _lastLevel = level;
		if (level > _lastLevel)
		{
			_lastLevel = level;
			_displayXp = 0f;
			_levelFlash = 1f;
			_levelUpTimer = 1.6f;
			_levelUpLabel.Text = $"¡NIVEL {level}!";
			_levelUpLabel.Visible = true;
			Pop(_levelBadge, 1.3f);
		}
		else if (level < _lastLevel)
		{
			_lastLevel = level; // partida nueva
			_displayXp = 0f;
		}

		float target = required > 0f ? Mathf.Clamp(xp / required, 0f, 1f) : 0f;
		_displayXp = Mathf.Lerp(_displayXp, target, 1f - Mathf.Exp(-10f * dt));
		_xpBar.Value = _displayXp;
		_xpText.Text = $"{Mathf.FloorToInt(xp)} / {Mathf.CeilToInt(required)} XP";
		_levelNumber.Text = level.ToString();

		// Destello al subir de nivel
		_levelFlash = Mathf.Max(0f, _levelFlash - dt * 1.5f);
		_xpFill.BgColor = XpColor.Lerp(XpFlashColor, _levelFlash);

		if (_levelUpTimer > 0f)
		{
			_levelUpTimer -= dt;
			var m = _levelUpLabel.Modulate;
			m.A = Mathf.Clamp(_levelUpTimer / 0.5f, 0f, 1f);
			_levelUpLabel.Modulate = m;
			if (_levelUpTimer <= 0f) _levelUpLabel.Visible = false;
		}
	}

	/// <summary>Pequeño "salto" de escala (las etiquetas siguen funcionando aunque el juego esté en pausa).</summary>
	private void Pop(Control control, float scale)
	{
		if (control == null) return;
		control.PivotOffset = control.Size * 0.5f;
		var tween = CreateTween();
		tween.TweenProperty(control, "scale", new Vector2(scale, scale), 0.07f);
		tween.TweenProperty(control, "scale", Vector2.One, 0.15f);
	}

	private void UpdateActivePanel()
	{
		if (_inventory == null) return;
		ItemDefinition active = _inventory.ActiveItem;
		_activePanel.Visible = active != null;
		if (active == null) { _shownActiveId = null; return; }

		if (_shownActiveId != active.Id)
		{
			_shownActiveId = active.Id;
			foreach (Node child in _activeIconHolder.GetChildren())
			{
				_activeIconHolder.RemoveChild(child);
				child.QueueFree();
			}
			_activeIconHolder.AddChild(UiFactory.MakeItemIcon(active, 36f));
			_activeNameLabel.Text = active.Name;
			_activeNameLabel.AddThemeColorOverride("font_color", active.Color);
		}

		string key = SettingsManager.Instance != null
			? SettingsManager.KeyName(SettingsManager.Instance.GetKey("use_item"))
			: "Q";
		if (_inventory.ActiveReady)
		{
			_activeStatusLabel.Text = $"[{key}] ¡Listo!";
			_activeStatusLabel.AddThemeColorOverride("font_color", new Color(0.5f, 1f, 0.5f));
		}
		else
		{
			_activeStatusLabel.Text = $"[{key}] {Mathf.CeilToInt(_inventory.ActiveCooldownRemaining)} s";
			_activeStatusLabel.AddThemeColorOverride("font_color", UiFactory.TextMuted);
		}
	}

	private void RebuildItems()
	{
		if (_inventory == null || _itemStrip == null) return;
		foreach (Node child in _itemStrip.GetChildren())
		{
			_itemStrip.RemoveChild(child);
			child.QueueFree();
		}
		foreach (var stack in _inventory.GetPassiveItems())
			_itemStrip.AddChild(UiFactory.MakeItemIcon(stack.Item, 24f, stack.Count));
	}

	private void OnItemAdded(ItemDefinition item, int stacks)
	{
		if (item == null) return;
		if (SettingsManager.Instance != null && !SettingsManager.Instance.ShowItemToasts) return;
		string count = stacks > 1 ? $" (x{stacks})" : "";
		_toastLabel.Text = $"{item.Name}{count}\n{item.Description}";
		_toastLabel.AddThemeColorOverride("font_color", item.Color);
		_toastLabel.Visible = true;
		_toastLabel.Modulate = Colors.White;
		_toastTimer = 3.5f;
	}
}
