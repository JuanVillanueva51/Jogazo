using Godot;
using System;

public partial class GameOverScreen : CanvasLayer
{
	private Label _titleLabel;
	private Label _timeLabel;
	private Label _levelLabel;
	private Label _rewardLabel;
	private Button _restartButton;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;

		_titleLabel    = GetNode<Label>("Panel/VBoxContainer/TitleLabel");
		_timeLabel     = GetNode<Label>("Panel/VBoxContainer/TimeLabel");
		_levelLabel    = GetNode<Label>("Panel/VBoxContainer/LevelLabel");
		_restartButton = GetNode<Button>("Panel/VBoxContainer/RestartButton");

		// Resumen de recompensas (creado desde código, antes del botón)
		var box = GetNode<VBoxContainer>("Panel/VBoxContainer");
		_rewardLabel = UiFactory.MakeLabel("", 13, UiFactory.CreditColor);
		_rewardLabel.HorizontalAlignment = HorizontalAlignment.Center;
		box.AddChild(_rewardLabel);
		box.MoveChild(_rewardLabel, _restartButton.GetIndex());

		_restartButton.Text = "Menu Principal";
		_restartButton.Pressed += OnGoToMenu;

		EventManager.Instance.PlayerDied -= ShowGameOver;
		EventManager.Instance.GameWon    -= ShowWin;

		EventManager.Instance.PlayerDied += ShowGameOver;
		EventManager.Instance.GameWon    += ShowWin;
	}

	public override void _ExitTree()
	{
		if (EventManager.Instance == null) return;
		EventManager.Instance.PlayerDied -= ShowGameOver;
		EventManager.Instance.GameWon    -= ShowWin;
	}

	private void ShowGameOver() => Show("GAME OVER", false);
	private void ShowWin()      => Show("SOBREVIVISTE!", true);

	private void Show(string title, bool won)
	{
		Visible = true;
		GetTree().Paused = true;

		_titleLabel.Text = title;

		int seconds = (int)GameManager.Instance.TotalRunTime;
		_timeLabel.Text  = $"Tiempo: {seconds / 60:D2}:{seconds % 60:D2}";
		_levelLabel.Text = $"Nivel alcanzado: {GameManager.Instance.CurrentLevel}";

		// Cerrar la partida: guarda créditos y estadísticas en el perfil
		RunSummary summary = GameManager.Instance.EndRun(won);
		int earned = summary != null ? summary.CreditsEarned : GameManager.Instance.Run.CreditsEarned;
		int total = ProfileManager.Instance != null ? ProfileManager.Instance.Credits : 0;
		string kills = summary != null ? $"Enemigos: {summary.Kills}  Élites: {summary.EliteKills}  Jefes: {summary.BossKills}\n" : "";
		_rewardLabel.Text = $"{kills}+{earned} {Balance.MetaCurrencyName} (total: {total})";
	}

	private void OnGoToMenu()
	{
		GameManager.Instance.EndRun(false); // por si acaso; no hace nada si ya terminó
		GameManager.Instance.ResetRun();
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
	}
}
