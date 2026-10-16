using Godot;
using System;

public partial class EnemyProjectile : Area2D
{
	[Export] public float Speed    = 30f;
	[Export] public float Damage   = 10f;
	[Export] public float Lifetime = 4f;

	private Vector2 _direction;
	private float   _lifeTimer = 0f;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public void Initialize(Vector2 direction)
	{
		_direction = direction.Normalized();
		Rotation   = _direction.Angle();
	}

	public override void _Process(double delta)
	{
		GlobalPosition += _direction * Speed * (float)delta;

		_lifeTimer += (float)delta;
		if (_lifeTimer >= Lifetime)
			QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player player)
		{
			player.TakeDamage(Damage);
			QueueFree();
		}
	}
}
