using Godot;
using System;

public partial class Meteor : OnEnemyWeapon
{
		private Vector2 targetPosition;
		private bool exploded = false;
		private AnimatedSprite2D sprite;
		private AnimatedSprite2D warningSprite;
		public float FallSpeed = 600f;
public override async void _Ready()
{
	base._Ready();
	
	sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
	warningSprite = GetNode<AnimatedSprite2D>("WarningSprite");
	
	if(Target == null)
	{
		QueueFree();
		return;
	}
	Scale =
	Vector2.One *
	BulletScale;
	targetPosition = Target.GlobalPosition;
	
	warningSprite.Reparent(GetParent());
	warningSprite.GlobalPosition = targetPosition;
	warningSprite.Visible = true;
	warningSprite.Play();
	sprite.Visible = false;
	sprite.Visible = true;
	
	GlobalPosition = targetPosition + Vector2.Up * 250f;
}
	public override void _Process(double delta)
	{
	GlobalPosition = GlobalPosition.MoveToward(
		targetPosition,
		FallSpeed * (float)delta
	);
	if(GlobalPosition.DistanceTo(targetPosition) < 1f)
	{
		Impact();
	}
	}
private void Impact()
{
	warningSprite.QueueFree();
	if(exploded) return;
	
	exploded = true;
	
	ApplyExplosionDamage();
	
	sprite.Play("explosion");
	
	SetProcess(false);
	
	GetTree().CreateTimer(0.3f).Timeout += QueueFree;
}
private void ApplyExplosionDamage()
{
	var enemies = GetTree().GetNodesInGroup("enemies");
	
	foreach(Node node in enemies)
	{
		if(node is not Node2D enemy)
			continue;
			
		if(enemy.GlobalPosition.DistanceTo(targetPosition) > 80f)
			continue;
			
		if(enemy is IDamageable damageable){
			float distance =
	enemy.GlobalPosition.DistanceTo(targetPosition);
	
	float radius = 80f;
	
	float multiplier =
	1f - (distance / radius);
	
	multiplier = Mathf.Clamp(multiplier, 0.2f, 1f);
	
	damageable.TakeDamage(Damage * multiplier);
			}
	}
}

}
