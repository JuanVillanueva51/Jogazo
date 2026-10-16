using Godot;

public partial class LevelUpTower : Area2D
{
	[Export] public float WaitTime = 15.0f;
	private float timer = 0f;
	private bool IsInside = false;
	private bool Used = false;
	private TowerLevelUpScreen upgradeScreen;
	private ProgressBar progressBar;
	public override void _Ready()
	{
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
