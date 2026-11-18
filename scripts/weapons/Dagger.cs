using Godot;

public partial class Dagger : OnEnemyWeapon
{
	private AnimatedSprite2D _sprite;
	public override void _Ready()
	{
		base._Ready();
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		if(Target == null)
		{
			QueueFree();
			return;
		}
		GlobalPosition = Target.GlobalPosition;
		if(Target is IDamageable damageable)
			damageable.TakeDamage(Damage);
		_sprite.Play();
		_sprite.AnimationFinished += OnAnimationFinished;
	}

	private void OnAnimationFinished()
	{
		QueueFree();
	}
}
