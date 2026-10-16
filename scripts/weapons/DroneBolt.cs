using System.Collections.Generic;
using Godot;

/// <summary>Proyectil del Dron de Vigilancia (se crea desde código).</summary>
public partial class DroneBolt : Area2D
{
	private static readonly Color BoltColor = new Color(0.45f, 0.95f, 1f);

	public float Damage = 10f;
	public float Speed = 420f;
	public int Pierce = 1;
	public float Lifetime = 1.6f;
	public Vector2 Direction = Vector2.Right;

	private float _time;
	private readonly HashSet<ulong> _hit = new();

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 2;
		Monitorable = false;
		ZIndex = 3;
		Direction = Direction == Vector2.Zero ? Vector2.Right : Direction.Normalized();
		Rotation = Direction.Angle();
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 4f } });
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		GlobalPosition += Direction * Speed * dt;
		_time += dt;
		if (_time >= Lifetime) QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (Pierce <= 0 || !body.IsInGroup("enemies") || body is not IDamageable damageable) return;
		if (!_hit.Add(body.GetInstanceId())) return;
		damageable.TakeDamage(Damage);
		Pierce--;
		if (Pierce <= 0) QueueFree();
	}

	public override void _Draw()
	{
		Color trail = BoltColor;
		trail.A = 0.35f;
		DrawLine(new Vector2(-9f, 0f), Vector2.Zero, trail, 2f);
		DrawCircle(Vector2.Zero, 2.5f, BoltColor);
	}
}
