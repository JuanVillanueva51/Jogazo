using Godot;

public partial class LaserOrbital : OrbitalWeapon
{
	private Line2D laser;

	[Export] public float LaserLength = 150f;
	[Export] public float DamageInterval = 0.15f;
	[Export] public float LaserStartOffset = 25f;
	private float currentAngle;
	private float damageTimer;
	
	public override void _Ready()
	{
		base._Ready();

		laser = GetNode<Line2D>("Line2D");
	}

	public override void _Process(double delta)
	{
		if(Player == null)
		{
			Player = GetTree().GetFirstNodeInGroup("player") as Player;
			return;
		}
		laser.Scale = Vector2.One * Data.BulletScale * Player.Stats.SizeMultiplier * (1f + AffinityBonus.For(Player, Data).Size);
		currentAngle += OrbitSpeed * Player.Stats.AttackSpeedMultiplier * (float)delta;
		GlobalPosition = Player.GlobalPosition;
		Rotation = currentAngle;
		UpdateLaser();
		damageTimer -= (float)delta;
		if(damageTimer <= 0f)
		{
			damageTimer = DamageInterval / Player.Stats.AttackSpeedMultiplier;
			ApplyLaserDamage();
		}
	}
	private void UpdateLaser()
	{
		laser.ClearPoints();
		laser.AddPoint(Vector2.Right * LaserStartOffset);
		laser.AddPoint(Vector2.Right * (LaserStartOffset + LaserLength));
	}
	private void ApplyLaserDamage()
	{
		foreach(Node body in GetOverlappingBodies())
		{
			if(body is not Node2D enemy)
				continue;
			if(!enemy.IsInGroup("enemies"))
				continue;
			if(body is IDamageable damageable)
			{
			float damagePerTick = DamageCalculator.ForWeapon(Player, Data, Data.Damage).Amount * 0.15f;
				damageable.TakeDamage(damagePerTick);
			}
		}
	}
}
