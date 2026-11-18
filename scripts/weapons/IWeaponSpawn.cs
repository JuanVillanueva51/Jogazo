using Godot;

/// <summary>
/// Datos ya calculados (daño final, tamaño, velocidad...) que WeaponManager entrega
/// a las armas nuevas al dispararlas.
/// </summary>
public struct WeaponLaunch
{
	public Player     Player;
	public WeaponData Data;
	public Node2D     Target;
	public Vector2    Origin;
	public Vector2    Direction;
	public float      Damage;
	public bool       IsCrit;
	public float      Speed;
	public float      Scale;
	public int        Pierce;
	public int        Bounce;
	public float      Lifetime;
}

/// <summary>
/// Armas que se lanzan con un WeaponLaunch (guadaña, hacha bumerán, tornado...).
/// WeaponManager llama a Launch() antes de agregarlas a la escena; la posición
/// inicial (Origin) la aplica cada arma en su _Ready.
/// </summary>
public interface IWeaponSpawn
{
	void Launch(WeaponLaunch launch);
}
