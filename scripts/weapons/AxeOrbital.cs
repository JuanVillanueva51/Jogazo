using Godot;

public partial class AxeOrbital : OrbitalWeapon
{
	[Export] public float DamageCooldown = 0.2f;
	private float damageTimer = 0f;
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}
	public override void _Process(double delta)
	{
		base._Process(delta);
		if (damageTimer > 0f)
			damageTimer -= (float)delta;
		if (Data != null && Player != null)
		{
			Scale =
				Vector2.One *
				Data.BulletScale *
				Player.Stats.SizeMultiplier *
				(1f + AffinityBonus.For(Player, Data).Size);
		}
	}
	private void OnBodyEntered(Node2D body)
	{
		if (damageTimer > 0f)
			return;
		if (!body.IsInGroup("enemies"))
			return;
		if (body is IDamageable damageable && Data != null && Player != null)
		{
			float damage = DamageCalculator.ForWeapon(Player, Data, Data.Damage).Amount;
			damageable.TakeDamage(damage);
			damageTimer = DamageCooldown;
		}
	}
}
