using Godot;

/// <summary>
/// Menú de desbloqueos (desde el menú principal). Se gastan Créditos — la moneda global
/// que se gana jugando — para agregar objetos y armas al botín de futuras partidas.
/// La interfaz se construye desde código.
/// </summary>
public partial class UnlocksMenu : Control
{
	private Label _creditsLabel;
	private Label _messageLabel;
	private VBoxContainer _itemsList;
	private VBoxContainer _weaponsList;
	private Label _summaryLabel;

	public override void _Ready()
	{
		Theme = UiFactory.GetTheme();
		SetAnchorsPreset(LayoutPreset.FullRect);
		BuildUi();
		if (ProfileManager.Instance != null)
		{
			ProfileManager.Instance.CreditsChanged += OnCreditsChanged;
			ProfileManager.Instance.UnlocksChanged += RebuildLists;
		}
		RebuildLists();
	}

	public override void _ExitTree()
	{
		if (ProfileManager.Instance != null)
		{
			ProfileManager.Instance.CreditsChanged -= OnCreditsChanged;
			ProfileManager.Instance.UnlocksChanged -= RebuildLists;
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("pause")) OnBack();
	}

	private void BuildUi()
	{
		const string backgroundPath = "res://sprites/GUI/background1.png";
		if (ResourceLoader.Exists(backgroundPath))
		{
			var bg = new TextureRect
			{
				Texture = GD.Load<Texture2D>(backgroundPath),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			bg.SetAnchorsPreset(LayoutPreset.FullRect);
			AddChild(bg);
		}
		var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.45f), MouseFilter = MouseFilterEnum.Ignore };
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(dim);

		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 40);
		margin.AddThemeConstantOverride("margin_right", 40);
		margin.AddThemeConstantOverride("margin_top", 20);
		margin.AddThemeConstantOverride("margin_bottom", 20);
		AddChild(margin);

		var panel = UiFactory.MakePanel(null, null, 2, 14f);
		margin.AddChild(panel);

		var root = new VBoxContainer();
		root.AddThemeConstantOverride("separation", 8);
		panel.AddChild(root);

		var header = new HBoxContainer();
		header.AddThemeConstantOverride("separation", 16);
		root.AddChild(header);

		var title = UiFactory.MakeLabel("DESBLOQUEOS", 30, new Color(1f, 0.9f, 0.6f));
		title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(title);

		_creditsLabel = UiFactory.MakeLabel("", 22, UiFactory.CreditColor);
		header.AddChild(_creditsLabel);

		var back = UiFactory.MakeButton("Regresar", 16, 140f);
		back.Pressed += OnBack;
		header.AddChild(back);

		_summaryLabel = UiFactory.MakeLabel("", 13, UiFactory.TextMuted);
		root.AddChild(_summaryLabel);

		_messageLabel = UiFactory.MakeLabel("", 14, new Color(0.5f, 1f, 0.5f));
		root.AddChild(_messageLabel);

		var tabs = new TabContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		root.AddChild(tabs);

		_itemsList = CreateTab(tabs, "Objetos");
		_weaponsList = CreateTab(tabs, "Armas");

		string location = ProfileManager.Instance != null ? ProfileManager.Instance.SaveLocation : "";
		root.AddChild(UiFactory.MakeLabel($"Datos guardados en: {location}", 10, UiFactory.TextMuted, false));
	}

	private static VBoxContainer CreateTab(TabContainer tabs, string name)
	{
		var scroll = new ScrollContainer
		{
			Name = name,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		tabs.AddChild(scroll);
		var list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		list.AddThemeConstantOverride("separation", 6);
		scroll.AddChild(list);
		return list;
	}

	private void OnCreditsChanged(int total, int delta)
	{
		UpdateHeader();
	}

	private void UpdateHeader()
	{
		var pm = ProfileManager.Instance;
		int credits = pm != null ? pm.Credits : 0;
		_creditsLabel.Text = $"{Balance.MetaCurrencyName}: {credits}";
		if (pm != null && pm.Profile != null)
		{
			var p = pm.Profile;
			int best = Mathf.RoundToInt(p.BestRunSeconds);
			_summaryLabel.Text =
				$"Partidas: {p.RunsPlayed}   Victorias: {p.RunsWon}   Enemigos derrotados: {p.TotalKills}   " +
				$"Mejor tiempo: {best / 60:D2}:{best % 60:D2}   Créditos ganados en total: {p.LifetimeCredits}";
		}
	}

	private void RebuildLists()
	{
		UpdateHeader();
		Clear(_itemsList);
		Clear(_weaponsList);

		foreach (var item in ItemLibrary.Unlockables())
			_itemsList.AddChild(MakeItemRow(item));

		foreach (var weapon in WeaponLibrary.BuildAll())
			_weaponsList.AddChild(MakeWeaponRow(weapon));
	}

	private static void Clear(Container container)
	{
		foreach (Node child in container.GetChildren())
		{
			container.RemoveChild(child);
			child.QueueFree();
		}
	}

	private Control MakeItemRow(ItemDefinition item)
	{
		var pm = ProfileManager.Instance;
		bool unlocked = pm != null ? pm.IsItemUnlocked(item.Id) : item.UnlockedByDefault;
		int credits = pm != null ? pm.Credits : 0;

		var row = UiFactory.MakePanel(new Color(0.1f, 0.09f, 0.14f, 0.95f), unlocked ? item.Color : new Color(0.35f, 0.35f, 0.4f), 2, 8f);
		var h = new HBoxContainer();
		h.AddThemeConstantOverride("separation", 12);
		row.AddChild(h);

		var icon = UiFactory.MakeItemIcon(item, 40f);
		if (!unlocked) icon.Modulate = new Color(0.5f, 0.5f, 0.5f);
		h.AddChild(icon);

		var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		h.AddChild(text);
		text.AddChild(UiFactory.MakeLabel(item.Name, 16, unlocked ? item.Color : UiFactory.TextMuted));
		string header = $"{RarityInfo.GetName(item.Rarity)} · {RarityInfo.GetKindName(item.Kind)}";
		if (item.IsActive) header += $" · Enfriamiento {item.Cooldown:0}s";
		text.AddChild(UiFactory.MakeLabel(header, 11, UiFactory.TextMuted));
		var desc = UiFactory.MakeWrappedLabel(item.Description, 13, Colors.White, 500f);
		desc.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		text.AddChild(desc);

		h.AddChild(MakeUnlockControl(unlocked, item.UnlockCost, credits, () =>
		{
			if (pm != null && pm.TryUnlockItem(item))
				_messageLabel.Text = $"¡Desbloqueaste {item.Name}! Ya puede aparecer en cofres y tiendas.";
		}));
		return row;
	}

	private Control MakeWeaponRow(WeaponData weapon)
	{
		var pm = ProfileManager.Instance;
		bool unlocked = WeaponLibrary.IsUnlocked(weapon);
		int credits = pm != null ? pm.Credits : 0;
		Color typeColor = weapon.WeaponType switch
		{
			"melee" => new Color(1f, 0.6f, 0.4f),
			"magic" => new Color(0.7f, 0.5f, 1f),
			_ => new Color(0.5f, 0.85f, 1f),
		};

		var row = UiFactory.MakePanel(new Color(0.1f, 0.09f, 0.14f, 0.95f), unlocked ? typeColor : new Color(0.35f, 0.35f, 0.4f), 2, 8f);
		var h = new HBoxContainer();
		h.AddThemeConstantOverride("separation", 12);
		row.AddChild(h);

		var iconBox = new Panel { CustomMinimumSize = new Vector2(40f, 40f) };
		iconBox.AddThemeStyleboxOverride("panel", UiFactory.MakeBox(new Color(0f, 0f, 0f, 0.4f), typeColor, 2, 4, 0f));
		Texture2D weaponIcon = WeaponLibrary.GetIcon(weapon);
		if (weaponIcon != null)
		{
			var tex = new TextureRect
			{
				Texture = weaponIcon,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				TextureFilter = TextureFilterEnum.Nearest,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			tex.SetAnchorsPreset(LayoutPreset.FullRect);
			iconBox.AddChild(tex);
		}
		if (!unlocked) iconBox.Modulate = new Color(0.5f, 0.5f, 0.5f);
		h.AddChild(iconBox);

		var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		h.AddChild(text);
		text.AddChild(UiFactory.MakeLabel(weapon.Name, 16, unlocked ? typeColor : UiFactory.TextMuted));
		string tags = $"Arma {WeaponLibrary.TagsText(weapon)}";
		text.AddChild(UiFactory.MakeLabel(tags, 11, UiFactory.TextMuted));
		if (!string.IsNullOrEmpty(weapon.Description))
			text.AddChild(UiFactory.MakeWrappedLabel(weapon.Description, 12, UiFactory.TextMuted, 300f));
		text.AddChild(UiFactory.MakeLabel($"Daño {weapon.Damage}   Cadencia {weapon.FireRate}s", 13, Colors.White));

		h.AddChild(MakeUnlockControl(unlocked, weapon.UnlockCost, credits, () =>
		{
			if (pm != null && pm.TryUnlockWeapon(weapon))
				_messageLabel.Text = $"¡Desbloqueaste {weapon.Name}! Ya puede aparecer al subir de nivel.";
		}));
		return row;
	}

	private static Control MakeUnlockControl(bool unlocked, int cost, int credits, System.Action onUnlock)
	{
		if (unlocked)
		{
			var label = UiFactory.MakeLabel("Desbloqueado", 14, new Color(0.5f, 1f, 0.5f));
			label.CustomMinimumSize = new Vector2(190f, 0f);
			label.HorizontalAlignment = HorizontalAlignment.Center;
			label.VerticalAlignment = VerticalAlignment.Center;
			return label;
		}
		var button = UiFactory.MakeButton($"Desbloquear ({cost})", 14, 190f, 40f);
		button.Disabled = credits < cost;
		button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		if (button.Disabled) button.TooltipText = $"Necesitas {cost - credits} {Balance.MetaCurrencyName} más.";
		button.Pressed += () => onUnlock();
		return button;
	}

	private void OnBack()
	{
		GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
	}
}
