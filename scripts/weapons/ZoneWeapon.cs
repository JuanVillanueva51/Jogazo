using Godot;
using System;
using System.Collections.Generic;

public partial class ZoneWeapon : Area2D
{
	public WeaponData Data;
	protected HashSet<Node2D> enemiesInside = new();
	private float tickTimer;
	private float lifeTimer;
	public float FinalDamage;
	public float FinalScale = 1f;
	
	public virtual void Initialize(WeaponData data)
	{
		Data = data;
	}
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		Scale = Vector2.One * FinalScale;
	}
	public override void _Process(double delta)
	{
		lifeTimer += (float)delta;
		if(lifeTimer >= Data.Lifetime)
		{
			QueueFree();
			return;
		}
		tickTimer += (float)delta;
		if(tickTimer >= Data.FireRate)
		{
		tickTimer = 0f;
		foreach(var enemy in enemiesInside)
		{
			if(enemy is IDamageable damageable)
			{
			damageable.TakeDamage(FinalDamage);
			}
		}
		}
	}
	private void OnBodyEntered(Node2D body)
	{
	if(body.IsInGroup("enemies"))
	{
		enemiesInside.Add(body);
	}
	}
	private void OnBodyExited(Node2D body)
	{
		enemiesInside.Remove(body);
	}
}
