using Godot;
using System.Collections.Generic;

public partial class Lightning : OnEnemyWeapon
{
	private AnimatedSprite2D _sprite;
	[Export] public int MaxJumps = 4;
	[Export] public float JumpRange = 150f;
	private Line2D line;
	public override void _Ready()
	{
		base._Ready();
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		line = GetNode<Line2D>("Line2D");
		line.ClearPoints();
		if(Target == null)
		{
			QueueFree();
			return;
		}
		Scale = Vector2.One * BulletScale;
		GlobalPosition = Target.GlobalPosition;
		HashSet<Node2D> hitEnemies = new();
		ChainHit(Target,hitEnemies,MaxJumps,Damage);
		_sprite.Play();
	}
	private void ChainHit(
		Node2D enemy,
		HashSet<Node2D> hitEnemies,
		int jumpsLeft,
		float currentDamage)
	{
		if(enemy == null || jumpsLeft <= 0)
			return;
		hitEnemies.Add(enemy);
if(enemy is IDamageable damageable)
{
	damageable.TakeDamage(currentDamage);
}
		float nextDamage = currentDamage * 0.8f;
		Node2D nextEnemy = FindNearestEnemy(enemy, hitEnemies);
		if(nextEnemy != null)
		{
			CreateLightningSegment(enemy.GlobalPosition,nextEnemy.GlobalPosition);
			ChainHit(nextEnemy,hitEnemies,jumpsLeft - 1,nextDamage);
		}
	}

	private void CreateLightningSegment(
		Vector2 start,
		Vector2 end)
		{
		line.AddPoint(ToLocal(start));
		for(int i = 1; i < 5; i++)
		{
			float t = i / 5.0f;
			Vector2 point = start.Lerp(end, t);
			point += new Vector2((float)GD.RandRange(-25, 25),(float)GD.RandRange(-25, 25));
			line.AddPoint(ToLocal(point));
		}
		line.AddPoint(ToLocal(end));
	}
	private Node2D FindNearestEnemy(
		Node2D current,
		HashSet<Node2D> hitEnemies)
	{
		Node2D nearest = null;
		float minDist = float.MaxValue;
		foreach(Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if(node is not Node2D enemy)
				continue;
			if(hitEnemies.Contains(enemy))
				continue;
			float distance =
				current.GlobalPosition.DistanceTo(
					enemy.GlobalPosition
				);
			if(distance > JumpRange)
				continue;
			if(distance < minDist)
			{
				minDist = distance;
				nearest = enemy;
			}
		}
		return nearest;
	}
}
