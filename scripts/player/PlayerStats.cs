using Godot;

/// <summary>
/// Estadísticas del jugador. Se usa en dos capas:
///  - Player.BaseStats: mejoras permanentes de la partida (pasiva de clase, torres de mejora).
///  - Player.Stats:     BaseStats + objetos + efectos temporales (se recalcula con Player.RecalculateStats()).
/// Ya no es un Node: es un objeto de datos simple que se puede clonar.
/// </summary>
public class PlayerStats
{
	// Vida
	public float MaxHpFlat              = 0f;    // vida máxima extra plana
	public float MaxHpMultiplier        = 1f;    // multiplicador de vida máxima
	public float HpRegen                = 0f;    // HP/segundo
	public float LifeSteal              = 0f;    // 0.0 - 1.0 (% del daño infligido)
	public float Thorns                 = 0f;    // daño devuelto al recibir golpes (en múltiplos del daño base)
	public float Armor                  = 0f;    // reducción de daño: 100 / (100 + armadura)
	public float DamageTakenMultiplier  = 1f;    // >1 = recibes más daño

	// Daño
	public float DamageMultiplier       = 1f;    // multiplicador general (antes 6, ahora Balance.PlayerDamageScale)
	public float MeleeDamageBonus       = 0f;    // bonus solo a melee
	public float RangedDamageBonus      = 0f;    // bonus solo a distancia
	public float MagicDamageBonus       = 0f;    // bonus solo a mágicas
	public float CritChance             = Balance.BaseCritChance;
	public float CritDamage             = Balance.BaseCritDamage;

	// Ataque
	public float AttackSpeedMultiplier  = 1f;    // >1 = más rápido
	public int   BonusProjectiles       = 0;
	public int   BonusBounce            = 0;
	public int   BonusPierce            = 0;
	public float SizeMultiplier         = 1f;
	public float ProjectileSpeedMult    = 1f;

	// Objetos activos
	public float ActiveCooldownMultiplier = 1f;  // <1 = enfriamiento más corto

	// Movimiento
	public float MovementSpeedMult      = 1f;

	// Recolección
	public float PickupRangeMult        = 1f;
	public float XpMultiplier           = 1f;
	public float GoldMultiplier         = 1f;

	// Otros
	public float Luck                   = 0f;    // cada punto = una tirada extra de rareza

	public PlayerStats Clone() => (PlayerStats)MemberwiseClone();
}
