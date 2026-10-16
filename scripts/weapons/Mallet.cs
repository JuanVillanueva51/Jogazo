using Godot;

public partial class Mallet : MeleeWeapon
{
	public override void _Ready()
	{
		base._Ready();
		CallDeferred(nameof(ApplyDamage));
	}
	private void ApplyDamage()
	{
		foreach(var body in GetOverlappingBodies())
		{
			if(body is IDamageable damageable)
				damageable.TakeDamage(Damage);
		}
	}
}
