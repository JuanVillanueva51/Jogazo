using Godot;

/// <summary>
/// Ventana de la tienda. Se construye desde código. Pausa el juego mientras está abierta.
/// Se cierra con el botón "Salir" o con Escape ("pause").
/// </summary>
public partial class ShopScreen : CanvasLayer
{
	private ShopTerminal _terminal;
	private Player _player;

	private Label _coinsLabel;
	private HBoxContainer _cards;
	private Button _rerollButton;
	private Label _messageLabel;
	private bool _closing;

	public static void Open(ShopTerminal terminal, Player player)
	{
		if (terminal == null || player == null) return;
		var tree = terminal.GetTree();
		var screen = new ShopScreen { _terminal = terminal, _player = player };
		tree.Root.AddChild(screen);
	}

	public override void _Ready()
	{
		Layer = 20;
		ProcessMode = ProcessModeEnum.Always;
		if (GameManager.Instance != null) GameManager.Instance.IsModalOpen = true;
		GetTree().Paused = true;
		BuildUi();
		Refresh();
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null) GameManager.Instance.IsModalOpen = false;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("pause")) Close();
	}

	private void BuildUi()
	{
		var root = new Control();
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.Theme = UiFactory.GetTheme();
		AddChild(root);

		var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.6f) };
		dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(dim);

		var center = new CenterContainer();
		center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(center);

		var panel = UiFactory.MakePanel(null, RarityInfo.GetColor(ItemRarity.Maldito), 3, 16f);
		center.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		panel.AddChild(vbox);

		var title = UiFactory.MakeLabel("TIENDA", 32, new Color(1f, 0.85f, 1f));
		title.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(title);

		_coinsLabel = UiFactory.MakeLabel("", 18, UiFactory.CoinColor);
		_coinsLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_coinsLabel);

		_cards = new HBoxContainer();
		_cards.AddThemeConstantOverride("separation", 12);
		vbox.AddChild(_cards);

		_messageLabel = UiFactory.MakeLabel("", 14, UiFactory.ErrorColor);
		_messageLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_messageLabel);

		var buttons = new HBoxContainer();
		buttons.Alignment = BoxContainer.AlignmentMode.Center;
		buttons.AddThemeConstantOverride("separation", 16);
		vbox.AddChild(buttons);

		_rerollButton = UiFactory.MakeButton("", 16, 220f);
		_rerollButton.Pressed += OnReroll;
		buttons.AddChild(_rerollButton);

		var close = UiFactory.MakeButton("Salir [Esc]", 16, 160f);
		close.Pressed += Close;
		buttons.AddChild(close);
	}

	private void Refresh()
	{
		if (_terminal == null || !IsInstanceValid(_terminal)) { Close(); return; }
		int coins = GameManager.Instance != null ? GameManager.Instance.Coins : 0;
		_coinsLabel.Text = $"Monedas: {coins}";
		_rerollButton.Text = $"Cambiar ofertas ({_terminal.RerollCost})";
		_rerollButton.Disabled = coins < _terminal.RerollCost;

		foreach (Node child in _cards.GetChildren())
		{
			_cards.RemoveChild(child);
			child.QueueFree();
		}

		foreach (var offer in _terminal.Offers)
			_cards.AddChild(MakeCard(offer, coins));

		if (_terminal.Offers.Count == 0)
			_cards.AddChild(UiFactory.MakeLabel("No hay objetos disponibles.\nDesbloquea más en el menú principal.", 16, UiFactory.TextMuted));
	}

	private Control MakeCard(ShopOffer offer, int coins)
	{
		ItemDefinition item = offer.Item;
		var card = UiFactory.MakePanel(new Color(0.12f, 0.1f, 0.16f, 0.97f), item.Color, 2, 10f);
		card.CustomMinimumSize = new Vector2(220f, 260f);

		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 6);
		card.AddChild(v);

		var header = new HBoxContainer();
		header.AddThemeConstantOverride("separation", 8);
		header.AddChild(UiFactory.MakeItemIcon(item, 36f));
		var name = UiFactory.MakeWrappedLabel(item.Name, 16, item.Color, 160f);
		name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		header.AddChild(name);
		v.AddChild(header);

		v.AddChild(UiFactory.MakeLabel($"{RarityInfo.GetName(item.Rarity)} · {RarityInfo.GetKindName(item.Kind)}", 12, UiFactory.TextMuted));

		var desc = UiFactory.MakeWrappedLabel(item.Description, 13, Colors.White, 196f);
		desc.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		v.AddChild(desc);

		if (item.IsActive)
		{
			string extra = $"Enfriamiento: {item.Cooldown:0}s";
			if (_player != null && _player.Inventory?.ActiveItem != null)
				extra += $"\nReemplaza: {_player.Inventory.ActiveItem.Name}";
			v.AddChild(UiFactory.MakeWrappedLabel(extra, 12, new Color(1f, 0.7f, 0.4f), 196f));
		}

		var buy = UiFactory.MakeButton(offer.Sold ? "VENDIDO" : $"Comprar ({offer.Price})", 15);
		buy.Disabled = offer.Sold || coins < offer.Price;
		buy.Pressed += () => OnBuy(offer);
		v.AddChild(buy);
		return card;
	}

	private void OnBuy(ShopOffer offer)
	{
		if (_terminal == null || !IsInstanceValid(_terminal)) return;
		if (_terminal.TryBuy(offer, _player))
		{
			_messageLabel.Text = "";
			_messageLabel.AddThemeColorOverride("font_color", new Color(0.5f, 1f, 0.5f));
			_messageLabel.Text = $"¡Compraste {offer.Item.Name}!";
		}
		else
		{
			_messageLabel.AddThemeColorOverride("font_color", UiFactory.ErrorColor);
			_messageLabel.Text = "Monedas insuficientes.";
		}
		Refresh();
	}

	private void OnReroll()
	{
		if (_terminal == null || !IsInstanceValid(_terminal)) return;
		if (!_terminal.TryReroll(_player))
		{
			_messageLabel.AddThemeColorOverride("font_color", UiFactory.ErrorColor);
			_messageLabel.Text = "Monedas insuficientes.";
		}
		else
		{
			_messageLabel.Text = "";
		}
		Refresh();
	}

	private void Close()
	{
		if (_closing) return;
		_closing = true;
		Interactable.LockInteraction(0.35f);
		GetTree().Paused = false;
		QueueFree();
	}
}
