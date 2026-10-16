using Godot;
using System;

public partial class OnEnemyWeapon : Area2D
{
	protected Node2D Target;
	
	[Export] public float Damage = 10f;
	[Export] public float duration = 1f;
	[Export] public float BulletScale = 1f;
	
	public virtual void Initialize(Node2D target)
	{
		Target = target;
	}
	public override void _Ready(){
		Scale = Vector2.One * BulletScale;
		if(duration > 0f){
			GetTree().CreateTimer(duration).Timeout += QueueFree;
		}
	}
	
}
