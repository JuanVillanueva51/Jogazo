using Godot;

public partial class OrbitalWeapon : Area2D
{
	[Export] public float OrbitRadius = 80f;
	[Export] public float OrbitSpeed = 3f;
	[Export] public float DamageCooldown = 0.05f; //0.1 original

	public WeaponData Data;
	protected Player Player;
	protected float AngleOffset;
	public virtual void Initialize(float startAngle, WeaponData data)
	{
		AngleOffset = startAngle;
		Data = data;
	}
	public virtual void SetOrbitData(
		float angleOffset,
		float orbitRadius,
		WeaponData data)
	{
		AngleOffset = angleOffset;
		OrbitRadius = orbitRadius;
		Data = data;
	}
	// Tiempo propio de la órbita (antes usaba ElapsedTime, que se detiene durante el jefe)
	private static float _orbitClock;
	private static ulong _orbitClockFrame;

	public override void _Process(double delta)
	{
		if (Player == null)
		{
			Player = GetTree().GetFirstNodeInGroup("player") as Player;
			return;
		}
		if (Data == null) return;
		// Reloj compartido para que todos los orbitales queden sincronizados
		ulong frame = Engine.GetProcessFrames();
		if (frame != _orbitClockFrame)
		{
			_orbitClockFrame = frame;
			_orbitClock += (float)delta;
		}
		Scale = Vector2.One * Data.BulletScale * Player.Stats.SizeMultiplier * (1f + AffinityBonus.For(Player, Data).Size);
		UpdateOrbit();
	}
	
	protected virtual void UpdateOrbit()
	{
		float baseAngle = _orbitClock * (OrbitSpeed 
		* Player.Stats.AttackSpeedMultiplier);
		
		float finalAngle = baseAngle + AngleOffset;
		
		GlobalPosition = Player.GlobalPosition +new Vector2( Mathf.Cos(finalAngle) 
		* OrbitRadius,Mathf.Sin(finalAngle) 
		* OrbitRadius);
		
		Rotation = finalAngle + Mathf.Pi / 2f;
	}
}
