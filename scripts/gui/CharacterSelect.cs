using Godot;
using System.Collections.Generic;

public partial class CharacterSelect : Control
{
	//datos de cada personaje: ahora viven en CharacterLibrary (arquetipos, afinidades y dash)
	private static List<CharacterData> Characters => CharacterLibrary.All;

	private static readonly Color SelectedBorder = new Color(1f, 0.85f, 0f);
	private static readonly Color IdleBorder     = new Color(0.25f, 0.22f, 0.3f);

	private int _selectedIndex = 0;
	private readonly List<PanelContainer> _cards = new();

	private TextureRect   _charPreview;
	private Label         _charNameLabel;
	private Label         _charDescLabel;
	private Label         _charStatsLabel;
	private Button        _confirmButton;
	private GridContainer _charGrid;
	private Button        _backButton;

	public override void _Ready()
	{
		_charPreview    = GetNode<TextureRect>("RightPanel/VBoxContainer/CharPreview");
		_charNameLabel  = GetNode<Label>("RightPanel/VBoxContainer/CharNameLabel");
		_charDescLabel  = GetNode<Label>("RightPanel/VBoxContainer/CharDescLabel");
		_charStatsLabel = GetNode<Label>("RightPanel/VBoxContainer/CharStatsLabel");
		_confirmButton  = GetNode<Button>("RightPanel/VBoxContainer/ConfirmButton");
		_charGrid       = GetNode<GridContainer>("LeftPanel/VBoxContainer/ScrollContainer/CharGrid");
		_backButton     = GetNode<Button>("BackButton");

		// Más información en el panel derecho: texto más compacto
		_charStatsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_charStatsLabel.AddThemeFontSizeOverride("font_size", 12);
		_charDescLabel.AddThemeFontSizeOverride("font_size", 14);
		_charPreview.CustomMinimumSize = new Vector2(150f, 150f);
		_charPreview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_charPreview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		var separator = GetNodeOrNull<Control>("RightPanel/VBoxContainer/HSeparator");
		if (separator != null) separator.CustomMinimumSize = new Vector2(0f, 6f);

		_backButton.Pressed += OnBack;
		_confirmButton.Pressed += OnConfirm;

		BuildGrid();
		//Esperar un frame para que los botones tengan su posición calculada
		CallDeferred(nameof(SelectCharacter), 0);
	}

	/// <summary>Crea un botón por personaje (los de la escena se reemplazan para que siempre coincidan con la lista).</summary>
	private void BuildGrid()
	{
		foreach (Node child in _charGrid.GetChildren())
		{
			_charGrid.RemoveChild(child);
			child.QueueFree();
		}
		_charGrid.AddThemeConstantOverride("h_separation", 6);
		_charGrid.AddThemeConstantOverride("v_separation", 6);

		for (int i = 0; i < Characters.Count; i++)
		{
			CharacterData data = Characters[i];
			var card = new PanelContainer();
			card.AddThemeStyleboxOverride("panel", UiFactory.MakeBox(new Color(0f, 0f, 0f, 0.35f), IdleBorder, 3, 4, 2f));

			var button = new TextureButton
			{
				TextureNormal = CharacterLibrary.GetIcon(data),
				CustomMinimumSize = new Vector2(56f, 56f),
				IgnoreTextureSize = true,
				StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
				TextureFilter = TextureFilterEnum.Nearest,
				SelfModulate = data.Tint,
				TooltipText = $"{data.Name} — {data.Archetype}",
			};
			int captured = i;
			button.Pressed += () => SelectCharacter(captured);
			card.AddChild(button);

			if (data.IsPlaceholder)
			{
				// Marca pequeña para los personajes que aún no tienen sprite propio
				var tag = UiFactory.MakeLabel("?", 12, new Color(1f, 0.85f, 0.4f));
				tag.HorizontalAlignment = HorizontalAlignment.Right;
				tag.VerticalAlignment = VerticalAlignment.Top;
				tag.MouseFilter = MouseFilterEnum.Ignore;
				card.AddChild(tag);
			}

			_charGrid.AddChild(card);
			_cards.Add(card);
		}
	}

	private void SelectCharacter(int index)
	{
		if (index < 0 || index >= Characters.Count) return;
		_selectedIndex = index;

		for (int i = 0; i < _cards.Count; i++)
		{
			Color border = i == index ? SelectedBorder : IdleBorder;
			_cards[i].AddThemeStyleboxOverride("panel", UiFactory.MakeBox(new Color(0f, 0f, 0f, 0.35f), border, 3, 4, 2f));
		}

		CharacterData data = Characters[index];
		_charPreview.Texture = CharacterLibrary.GetIcon(data);
		_charPreview.SelfModulate = data.Tint;
		_charNameLabel.Text = data.IsPlaceholder ? $"{data.Name} (provisional)" : data.Name;
		_charDescLabel.Text = data.Description;
		_charStatsLabel.Text = data.StatsText();
	}

	private void OnConfirm()
	{
		GameManager.Instance.SelectedCharacterIndex = _selectedIndex;
		if (_selectedIndex < Characters.Count)
			GameManager.Instance.SelectedCharacterScene = Characters[_selectedIndex].ScenePath;

		GetTree().ChangeSceneToFile("res://scenes/ui/class_select.tscn");
	}

	private void OnBack()
	{
		GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
	}
}
