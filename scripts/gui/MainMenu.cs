using Godot;
using System;

public partial class MainMenu : Control
{
	private Button _playButton;
	private Button _unlocksButton;
	private Button _configButton;
	private Button _exitButton;
	private TextureRect _background;
	private Label _creditsLabel;

	public override void _Ready()
	{
		_playButton  = GetNode<Button>("CenterContainer/VBox/PlayButton");
		_exitButton  = GetNode<Button>("CenterContainer/VBox/ExitButton");
		_background  = GetNode<TextureRect>("Background");
		_unlocksButton = GetNodeOrNull<Button>("CenterContainer/VBox/UnlocksButton");
		_configButton  = GetNodeOrNull<Button>("CenterContainer/VBox/ConfigButton");

		_playButton.Pressed += OnPlay;
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
		GetTree().ChangeSceneToFile("res://scenes/ui/character_select.tscn");
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
