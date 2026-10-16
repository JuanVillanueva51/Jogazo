using Godot;
using System.Collections.Generic;

public partial class ClassSelect : Control
{
	// Retratos de cada clase (se conserva por compatibilidad con la escena;
	// ahora los retratos salen de ClassData.PortraitPath o del ícono del arma inicial)
	[Export] public Texture2D[] ClassPortraits;

	private static readonly Color SelectedBorder = new Color(1f, 0.85f, 0f);
	private static readonly Color IdleBorder     = new Color(0.25f, 0.22f, 0.3f);
	private static readonly Color ArchetypeColor = new Color(0.65f, 0.85f, 1f);

	private int                 _selectedIndex = 0;
	private readonly List<PanelContainer> _cards = new();
	private List<ClassData>     _classes;

	private TextureRect _classPreview;
	private Label       _classNameLabel;
	private Label       _classArchetypeLabel;
	private Label       _classDescLabel;
	private Label       _classBuffsLabel;
	private Label       _classWeaponLabel;
	private Button      _playButton;
	private Button      _backButton;
	private GridContainer _classGrid;

	public override void _Ready()
	{
		_classPreview   = GetNode<TextureRect>("RightPanel/VBox/ClassPreview");
		_classNameLabel = GetNode<Label>("RightPanel/VBox/ClassNameLabel");
		_classDescLabel = GetNode<Label>("RightPanel/VBox/ClassDescLabel");
		_classBuffsLabel = GetNode<Label>("RightPanel/VBox/ClassBuffsLabel");
		_playButton     = GetNode<Button>("RightPanel/VBox/PlayButton");
		_backButton     = GetNode<Button>("BackButton");
		_classGrid      = GetNode<GridContainer>("LeftPanel/VBox/ScrollContainer/ClassGrid");

		// La escena no trae etiqueta para el arma inicial (era la causa del error
		// "Node not found: ClassWeaponLabel"): se crea si falta.
		var vbox = GetNode<VBoxContainer>("RightPanel/VBox");
		_classWeaponLabel = GetNodeOrNull<Label>("RightPanel/VBox/ClassWeaponLabel");
		if (_classWeaponLabel == null)
		{
			_classWeaponLabel = new Label { Name = "ClassWeaponLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart };
			vbox.AddChild(_classWeaponLabel);
			vbox.MoveChild(_classWeaponLabel, _playButton.GetIndex());
		}
		// Dificultad elegida en Configuración (se aplica a esta partida)
		if (SettingsManager.Instance != null)
		{
			var preset = Balance.GetDifficulty(SettingsManager.Instance.DifficultyIndex);
			var difficultyLabel = new Label { Name = "DifficultyLabel", Text = $"Dificultad: {preset.Name} (cámbiala en Configuración)", AutowrapMode = TextServer.AutowrapMode.WordSmart };
			difficultyLabel.AddThemeFontSizeOverride("font_size", 11);
			difficultyLabel.AddThemeColorOverride("font_color", new Color(1f, 0.6f, 0.55f));
			vbox.AddChild(difficultyLabel);
			vbox.MoveChild(difficultyLabel, _playButton.GetIndex());
		}
		_classArchetypeLabel = new Label { Name = "ClassArchetypeLabel", AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_classArchetypeLabel.AddThemeColorOverride("font_color", ArchetypeColor);
		_classArchetypeLabel.AddThemeFontSizeOverride("font_size", 13);
		vbox.AddChild(_classArchetypeLabel);
		vbox.MoveChild(_classArchetypeLabel, _classNameLabel.GetIndex() + 1);

		// Más información en el panel derecho: texto más compacto
		_classBuffsLabel.AddThemeFontSizeOverride("font_size", 12);
		_classDescLabel.AddThemeFontSizeOverride("font_size", 13);
		_classWeaponLabel.AddThemeFontSizeOverride("font_size", 13);
		_classWeaponLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.5f));
		_classPreview.CustomMinimumSize = new Vector2(140f, 140f);
		_classPreview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_classPreview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		var separator = GetNodeOrNull<Control>("RightPanel/VBox/HSeparator");
		if (separator != null) separator.CustomMinimumSize = new Vector2(0f, 6f);
		// La escena trae una barra de desplazamiento suelta dentro de la etiqueta: se oculta
		var stray = GetNodeOrNull<Control>("RightPanel/VBox/ClassBuffsLabel/VScrollBar");
		if (stray != null) stray.Visible = false;

		_backButton.Pressed += OnBack;
		_playButton.Pressed += OnPlay;

		_classes = ClassLibrary.All;
		BuildGrid();

		CallDeferred(nameof(SelectClass), 0);
	}

	/// <summary>Un botón por clase: siempre coincide con ClassLibrary aunque se agreguen clases.</summary>
	private void BuildGrid()
	{
		foreach (Node child in _classGrid.GetChildren())
		{
			_classGrid.RemoveChild(child);
			child.QueueFree();
		}
		_classGrid.AddThemeConstantOverride("h_separation", 6);
		_classGrid.AddThemeConstantOverride("v_separation", 6);

		for (int i = 0; i < _classes.Count; i++)
		{
			ClassData cls = _classes[i];
			var card = new PanelContainer();
			card.AddThemeStyleboxOverride("panel", UiFactory.MakeBox(new Color(0f, 0f, 0f, 0.35f), IdleBorder, 3, 4, 2f));

			var button = new TextureButton
			{
				TextureNormal = GetPortrait(i),
				CustomMinimumSize = new Vector2(56f, 56f),
				IgnoreTextureSize = true,
				StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
				TextureFilter = TextureFilterEnum.Nearest,
				TooltipText = $"{cls.Name} — {cls.Archetype}",
			};
			int captured = i;
			button.Pressed += () => SelectClass(captured);
			card.AddChild(button);

			_classGrid.AddChild(card);
			_cards.Add(card);
		}
	}

	private Texture2D GetPortrait(int index)
	{
		ClassData cls = _classes[index];
		Texture2D portrait = ClassLibrary.GetPortrait(cls);
		if (portrait == null && ClassPortraits != null && index < ClassPortraits.Length)
			portrait = ClassPortraits[index];
		return portrait;
	}

	private void SelectClass(int index)
	{
		if (_classes == null || index < 0 || index >= _classes.Count) return;
		_selectedIndex = index;

		for (int i = 0; i < _cards.Count; i++)
		{
			Color border = i == index ? SelectedBorder : IdleBorder;
			_cards[i].AddThemeStyleboxOverride("panel", UiFactory.MakeBox(new Color(0f, 0f, 0f, 0.35f), border, 3, 4, 2f));
		}

		ClassData cls = _classes[index];
		_classPreview.Texture = GetPortrait(index);
		_classNameLabel.Text  = cls.Name;
		_classArchetypeLabel.Text = $"Arquetipo: {cls.Archetype}";
		_classDescLabel.Text  = cls.Description;
		string affinity = cls.Affinities.Count > 0 ? "\nAfinidad: " + WeaponAffinity.DescribeAll(cls.Affinities) : "";
		_classBuffsLabel.Text = $"{cls.PassiveName}: {cls.PassiveDescription}{affinity}";

		WeaponData weapon = WeaponLibrary.Get(cls.StartingWeaponName);
		string weaponType = weapon != null ? $" ({WeaponLibrary.TagsText(weapon)})" : "";
		_classWeaponLabel.Text = $"Arma inicial: {cls.StartingWeaponName}{weaponType}";
	}

	private void OnBack()
	{
		GetTree().ChangeSceneToFile("res://scenes/ui/character_select.tscn");
	}

	private void OnPlay()
	{
		if (_classes == null || _selectedIndex >= _classes.Count) return;
		ClassData cls = _classes[_selectedIndex];
		GameManager.Instance.SelectedClassIndex = _selectedIndex;
		GameManager.Instance.SelectedPassive    = cls.Passive;
		GameManager.Instance.StartNewRun();
		GetTree().ChangeSceneToFile("res://scenes/mapas/forest_map.tscn");
	}
}
