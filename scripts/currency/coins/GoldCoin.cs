using Godot;
using System;

public partial class GoldCoin : Area2D
{
	[Export] public int   CoinValue    = 1;
	[Export] public float MagnetRange  = 60f;
	[Export] public float MagnetSpeed  = 120f;

	private Player _player;
	private bool   _attracted = false;

	public override void _Ready()
	{
		_player = GetTree().GetFirstNodeInGroup("player") as Player;
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		if (_player == null) return;

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);

		if (distance < MagnetRange * Mathf.Max(0.1f, _player.Stats.PickupRangeMult))
			_attracted = true;

		if (_attracted)
		{
			Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
			GlobalPosition  += direction * MagnetSpeed * (float)delta;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player)
		{
			GameManager.Instance.AddCoins(CoinValue);
			QueueFree();
		}
	}
}
