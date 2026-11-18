using Godot;
using System.Collections.Generic;

public partial class MeleeWeapon : Area2D
{
	[Export] public float Damage = 10f;
	[Export] public float Lifetime = 0.3f;
	[Export] public float BulletScale = 1f;
	protected HashSet<Node2D> enemiesHit = new();
	private float lifeTimer;
	public override void _Ready()
	{
		Scale = Vector2.One * BulletScale;
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		lifeTimer += (float)delta;
		if(lifeTimer >= Lifetime)
			QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if(!body.IsInGroup("enemies"))
			return;
		if(enemiesHit.Contains(body))
			return;
		enemiesHit.Add(body);
		if(body is IDamageable damageable)
			damageable.TakeDamage(Damage);
	}
}
