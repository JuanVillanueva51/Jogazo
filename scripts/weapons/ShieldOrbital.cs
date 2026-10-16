using Godot;

public partial class ShieldOrbital : OrbitalWeapon
{
	[Export] public float RechargeTime = 4f;
	public int ShieldIndex;
	public bool IsActive = true;
	private float rechargeTimer = 0f;
	private Sprite2D sprite;
	private CollisionShape2D collision;
	// Golpe de escudo: los escudos también empujan y dañan al chocar (antes no hacían daño,
	// y una clase que empezara solo con escudos no podía matar enemigos)
	private const float BashCooldown = 0.3f;
	private float bashTimer = 0f;
public override void _Ready()
{
	sprite = GetNode<Sprite2D>("Sprite2D");
	collision = GetNode<CollisionShape2D>("CollisionShape2D");
	AddToGroup("shields");
	CollisionMask |= 2; // capa de los enemigos
	BodyEntered += OnBashBodyEntered;
}
	private void OnBashBodyEntered(Node2D body)
	{
		if (!IsActive || bashTimer > 0f || Data == null || Player == null) return;
		if (!body.IsInGroup("enemies") || body is not IDamageable damageable) return;
		damageable.TakeDamage(DamageCalculator.ForWeapon(Player, Data, Data.Damage).Amount);
		if (body is IEnemy enemy && !enemy.IsDead)
			enemy.Knockback(body.GlobalPosition - GlobalPosition, 14f);
		bashTimer = BashCooldown;
	}
	public override void _Process(double delta)
	{
		base._Process(delta);
		if (bashTimer > 0f) bashTimer -= (float)delta;
		if (!IsActive)
		{
			rechargeTimer -= (float)delta;
			if (rechargeTimer <= 0f)
			{
				RestoreShield();
			}
		}
	}
	public bool ConsumeShield()
	{
		if (!IsActive)
			return false;
		IsActive = false;
		sprite.Visible = false;
		collision.Disabled = true;
		rechargeTimer = RechargeTime;
		return true;
	}
	private void RestoreShield()
	{
		IsActive = true;
		sprite.Visible = true;
		collision.Disabled = false;
	}
	public override void SetOrbitData(
		float angleOffset,
		float orbitRadius,
		WeaponData data)
	{
		base.SetOrbitData(
			angleOffset,
			45f,
			data
		);
	}
	protected override void UpdateOrbit()
	{
		base.UpdateOrbit();
		Rotation = 0f;
	}
}
