using Godot;
using System;
using System.Collections.Generic;

public partial class ExplosiveWeapon : Bullet
{
	private bool hasExploded = false;
	private Area2D explosionArea;
	private CollisionShape2D initialCollision;
	private CollisionShape2D explosionCollision;
	private AnimatedSprite2D sprite;
	private bool explosionActive = false;
	private HashSet<Node> enemiesHit = new();
	
	public override void _Ready(){
		base._Ready();
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		explosionArea = GetNode<Area2D>("ExplosionArea");
		initialCollision = GetNode<CollisionShape2D>("CollisionShape2D");
		explosionCollision = explosionArea.GetNode<CollisionShape2D>("CollisionShape2D");
		explosionArea.Monitoring = true;
		explosionCollision.Disabled = false;
	}
	protected override void OnBodyEntered(Node2D body){
		if(hasExploded) return;
		if(!body.IsInGroup("enemies")) return;
		if(body is IDamageable damageable){
			damageable.TakeDamage(Damage);
			TriggerHit(Damage);
		}
		Explode();
	}
private void Explode()
{
	hasExploded = true;
	explosionActive = true;
	Speed = 0f;
	SetDeferred("monitoring", false);
	sprite.Play("explosion");
	sprite.Scale *= 2f;
	ApplyExplosionDamage();
	GetTree().CreateTimer(0.1f).Timeout += () =>
	{
		explosionActive = false;
	};
	
	GetTree().CreateTimer(0.25f).Timeout += QueueFree;
}
	protected override void BaseProcess(double delta)
	{
		if (hasExploded)
			return;
		base.BaseProcess(delta);
	}
	private void ApplyExplosionDamage()
{
	foreach (Node2D body in explosionArea.GetOverlappingBodies())
	{
		if (!body.IsInGroup("enemies"))
			continue;
			
		if (enemiesHit.Contains(body))
			continue;
			
		enemiesHit.Add(body);
		
		if (body is IDamageable damageable)
		{
			damageable.TakeDamage(Damage);
		}
	}
}
}
