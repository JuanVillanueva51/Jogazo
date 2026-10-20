using Godot;
using System;

public partial class MainMenu : Control
{
	private Button _playButton;
	private Button _demoButton;
	private Button _historyButton;
	private Button _unlocksButton;
	private Button _configButton;
	private Button _exitButton;
	private TextureRect _background;
	private Label _creditsLabel;

	public override void _Ready()
	{
		// JUGAR y DEMO comparten fila (PlayRow); se acepta también la ruta vieja
		_playButton  = GetNodeOrNull<Button>("CenterContainer/VBox/PlayRow/PlayButton")
					?? GetNode<Button>("CenterContainer/VBox/PlayButton");
		_demoButton    = GetNodeOrNull<Button>("CenterContainer/VBox/PlayRow/DemoButton");
		_historyButton = GetNodeOrNull<Button>("CenterContainer/VBox/HistoryButton");
		_exitButton  = GetNode<Button>("CenterContainer/VBox/ExitButton");
		_background  = GetNode<TextureRect>("Background");
		_unlocksButton = GetNodeOrNull<Button>("CenterContainer/VBox/UnlocksButton");
		_configButton  = GetNodeOrNull<Button>("CenterContainer/VBox/ConfigButton");

		_playButton.Pressed += OnPlay;
		if (_demoButton != null) _demoButton.Pressed += OnDemo;
		if (_historyButton != null) _historyButton.Pressed += OnHistory;
		_exitButton.Pressed += OnExit;
		if (_unlocksButton != null) _unlocksButton.Pressed += OnUnlocks;
		if (_configButton != null) _configButton.Pressed += OnConfig;

		// Créditos (moneda global) en la esquina superior derecha
		_creditsLabel = UiFactory.MakeLabel("", 20, UiFactory.CreditColor);
		_creditsLabel.Position = new Vector2(GetViewportRect().Size.X - 330f, 12f);
		_creditsLabel.Size = new Vector2(318f, 28f);
		_creditsLabel.HorizontalAlignment = HorizontalAlignment.Right;
		AddChild(_creditsLabel);
		UpdateCredits();
	}

	private void UpdateCredits()
	{
		int credits = ProfileManager.Instance != null ? ProfileManager.Instance.Credits : 0;
		_creditsLabel.Text = $"{Balance.MetaCurrencyName}: {credits}";
	}

	private void OnPlay()
	{
		if (GameManager.Instance != null) GameManager.Instance.NextRunIsDemo = false;
		GetTree().ChangeSceneToFile("res://scenes/ui/character_select.tscn");
	}

	/// <summary>Modo demo: tutorial + un solo mapa, mucho más rápido y caótico.</summary>
	private void OnDemo()
	{
		if (GameManager.Instance != null) GameManager.Instance.NextRunIsDemo = true;
		GetTree().ChangeSceneToFile("res://scenes/ui/character_select.tscn");
	}

	private void OnHistory()
	{
		GetTree().ChangeSceneToFile(HistoryScreen.ScenePath);
	}

	private void OnUnlocks()
	{
		GetTree().ChangeSceneToFile("res://scenes/ui/unlocks_menu.tscn");
	}

	private void OnConfig()
	{
		GetTree().ChangeSceneToFile("res://scenes/ui/configuration_menu.tscn");
	}

	private void OnExit()
	{
		ProfileManager.Instance?.SaveNow();
		GetTree().Quit();
	}
}
