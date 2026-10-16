using Godot;

/// <summary>
/// Lógica de un objeto. Cada método es un "gancho" que el inventario llama en el momento adecuado.
/// Sobrescribe solo los que necesites. "stacks" es cuántas copias tiene el jugador.
/// </summary>
public class ItemEffect
{
	public ItemDefinition Definition { get; internal set; }

	/// <summary>Modifica estadísticas. Debe ser una función pura del estado del efecto.</summary>
	public virtual void ModifyStats(PlayerStats stats, int stacks) { }

	/// <summary>Se llama cada vez que se obtiene una copia.</summary>
	public virtual void OnAcquired(ItemContext ctx, int stacks) { }

	/// <summary>Se llama cuando el jugador pierde todas las copias.</summary>
	public virtual void OnRemoved(ItemContext ctx) { }

	public virtual void OnProcess(ItemContext ctx, int stacks, float delta) { }

	/// <summary>Modifica el daño de cualquier golpe del jugador contra un enemigo (antes de aplicarlo).</summary>
	public virtual float ModifyDamageToEnemy(ItemContext ctx, int stacks, IEnemy target, float damage) => damage;

	public virtual void OnEnemyHit(ItemContext ctx, int stacks, HitInfo hit) { }

	public virtual void OnEnemyKilled(ItemContext ctx, int stacks, KillInfo kill) { }

	/// <summary>Modifica el daño que recibe el jugador. Devuelve 0 para bloquearlo.</summary>
	public virtual float ModifyIncomingDamage(ItemContext ctx, int stacks, float damage) => damage;

	public virtual void OnPlayerDamaged(ItemContext ctx, int stacks, float damage, Node2D source) { }

	/// <summary>Devuelve true si el objeto evita la muerte.</summary>
	public virtual bool TryPreventDeath(ItemContext ctx, int stacks) => false;

	/// <summary>Solo objetos activos. Devuelve true si se usó (y entra en enfriamiento).</summary>
	public virtual bool Activate(ItemContext ctx) => false;
}

/// <summary>Accesos útiles para los efectos de objetos.</summary>
public sealed class ItemContext
{
	public Player Player { get; }
	public PlayerInventory Inventory { get; }

	public ItemContext(Player player, PlayerInventory inventory)
	{
		Player = player;
		Inventory = inventory;
	}

	public SceneTree Tree => Player != null && GodotObject.IsInstanceValid(Player) ? Player.GetTree() : null;
	public Vector2 PlayerPosition => Player != null && GodotObject.IsInstanceValid(Player) ? Player.GlobalPosition : Vector2.Zero;
	public float BaseDamage => DamageCalculator.ItemBaseDamage(Player);
	public float Luck => Player != null ? Player.Stats.Luck : 0f;

	/// <summary>Tirada de probabilidad; la suerte da tiradas extra (como el trébol de RoR2).</summary>
	public bool Roll(float chance)
	{
		if (chance <= 0f) return false;
		if (chance >= 1f) return true;
		int rolls = 1 + Mathf.Max(0, Mathf.FloorToInt(Luck));
		for (int i = 0; i < rolls; i++)
			if (GD.Randf() < chance) return true;
		return false;
	}

	public void RequestStatRefresh() => Player?.RecalculateStats();
}
