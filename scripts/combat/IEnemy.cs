using Godot;

/// <summary>
/// Contrato común para Enemy, RangedEnemy y BossEnemy. Permite que los objetos
/// (cadenas de rayos, ejecuciones, ralentizaciones...) traten a todos los enemigos igual.
/// </summary>
public interface IEnemy : IDamageable
{
	Node2D Body { get; }
	float CurrentHealth { get; }
	float MaxHealth { get; }
	bool IsElite { get; }
	bool IsBoss { get; }
	bool IsDead { get; }

	/// <summary>strength: 0 = sin efecto, 1 = detenido por completo.</summary>
	void ApplySlow(float strength, float duration);
	void Knockback(Vector2 direction, float distance);
}
