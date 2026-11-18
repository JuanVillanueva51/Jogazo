using Godot;
using System;

public partial class ExpOrb : Area2D
{
	[Export] public float XpValue      = 10f;  // el enemigo lo sobrescribe al soltarlo
	[Export] public float MagnetRange  = 80f;  // distancia a la que se atrae al jugador
	[Export] public float MagnetSpeed  = 200f; // velocidad de atracción

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

		// Moverse hacia el jugador si está dentro del rango
		if (distance < MagnetRange * Mathf.Max(0.1f, _player.Stats.PickupRangeMult))
		{
			_attracted = true;
		}

		if (_attracted)
		{
			Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
			GlobalPosition  += direction * MagnetSpeed * (float)delta;
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player)
		{
			GameManager.Instance.AddXp(XpValue); //el + 500 no va
			QueueFree();
		}
	}
}
