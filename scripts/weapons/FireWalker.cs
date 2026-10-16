using Godot;
using System;

public partial class FireWalker : ZoneWeapon
{
	private AnimatedSprite2D sprite;
	public override void _Ready()
	{
		base._Ready();
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		sprite?.Play();
	}
}
