using Godot;

public partial class LevelUpTower : Area2D
{
	[Export] public float WaitTime = 15.0f;
	private float timer = 0f;
	private bool IsInside = false;
	private bool Used = false;
	private TowerLevelUpScreen upgradeScreen;
	private ProgressBar progressBar;
	/// <summary>True cuando ya se usó (lo usa la guía del tutorial).</summary>
	public bool IsUsed => Used;
	public override void _Ready()
	{
		// Modo demo: las torres se cargan más rápido
		if (GameManager.Instance != null && GameManager.Instance.IsDemo)
			WaitTime *= Balance.DemoTowerTimeMultiplier;

		upgradeScreen = GetNode<TowerLevelUpScreen>("TowerLevelUpScreen");
		progressBar = GetNode<ProgressBar>("ProgressBar");
		
		progressBar.MaxValue = WaitTime;
		progressBar.Value = 0;
		progressBar.Visible = false;
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}
	private void OnBodyEntered(Node2D body)
	{
		if (body is Player) 
		{ 
			IsInside = true; 
			progressBar.Visible = true; 
		}
	}
	private void OnBodyExited(Node2D body)
	{
		if (body is Player) 
		{ 
			IsInside = false; 
			timer = 0f; 
			progressBar.Value = 0; 
			progressBar.Visible = false; 
		}
	}
	public override void _Process(double delta)
	{
		if (IsInside && !Used)
		{
			timer += (float)delta;
			progressBar.Value = timer;
			if (timer >= WaitTime)
			{
				TriggerUpgrade();
			}
		}
	}
	private void TriggerUpgrade()
	{
		Used = true;
		progressBar.Visible = false;
		upgradeScreen.OpenStatsScreen();
		Modulate = new Color(0.5f, 0.5f, 0.5f); 
	}
}
