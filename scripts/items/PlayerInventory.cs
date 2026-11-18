using System;
using System.Collections.Generic;
using Godot;

/// <summary>Copia del inventario para pasarlo entre mapas.</summary>
public sealed class InventorySnapshot
{
	public List<string> Order = new();
	public Dictionary<string, int> Stacks = new();
	public string ActiveId;
	public float ActiveCooldown;
}

public readonly struct ItemStack
{
	public readonly ItemDefinition Item;
	public readonly int Count;
	public ItemStack(ItemDefinition item, int count) { Item = item; Count = count; }
}

/// <summary>
/// Inventario de objetos del jugador: pasivos apilables + un objeto activo (tecla Q).
/// Lo crea Player en _Ready. Llama a los ganchos de cada ItemEffect y recalcula
/// las estadísticas del jugador cuando algo cambia.
/// </summary>
public partial class PlayerInventory : Node
{
	private sealed class TimedBuff
	{
		public string Id;
		public float Remaining;
		public Action<PlayerStats> Apply;
	}

	public event Action<ItemDefinition, int> ItemAdded;       // objeto, copias totales
	public event Action InventoryChanged;
	public event Action<ItemDefinition> ActiveUsed;

	public Player OwnerPlayer { get; private set; }
	public ItemContext Context { get; private set; }

	public ItemDefinition ActiveItem { get; private set; }
	public float ActiveCooldownRemaining { get; private set; }
	public float ActiveCooldownTotal => ActiveItem == null ? 0f
		: ActiveItem.Cooldown * Mathf.Clamp(OwnerPlayer != null ? OwnerPlayer.Stats.ActiveCooldownMultiplier : 1f, 0.1f, 2f);
	public bool ActiveReady => ActiveItem != null && ActiveCooldownRemaining <= 0f;

	private readonly List<string> _order = new();
	private readonly Dictionary<string, int> _stacks = new();
	private readonly Dictionary<string, ItemEffect> _effects = new();
	private ItemEffect _activeEffect;
	// Efectos que no son objetos (p. ej. pasivas de clase): siempre activos con 1 copia
	private readonly List<ItemEffect> _innate = new();
	private readonly List<TimedBuff> _buffs = new();
	private Func<IEnemy, float, float> _outgoingModifier;

	public void Setup(Player owner)
	{
		OwnerPlayer = owner;
		Context = new ItemContext(owner, this);
	}

	public override void _Ready()
	{
		_outgoingModifier = ModifyOutgoingDamage;
		CombatEvents.OutgoingDamageModifier = _outgoingModifier;
		CombatEvents.EnemyHit += OnEnemyHit;
		CombatEvents.EnemyKilled += OnEnemyKilled;
	}

	public override void _ExitTree()
	{
		CombatEvents.EnemyHit -= OnEnemyHit;
		CombatEvents.EnemyKilled -= OnEnemyKilled;
		if (CombatEvents.OutgoingDamageModifier == _outgoingModifier)
			CombatEvents.OutgoingDamageModifier = null;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (ActiveCooldownRemaining > 0f)
			ActiveCooldownRemaining = Mathf.Max(0f, ActiveCooldownRemaining - dt);

		if (_buffs.Count > 0)
		{
			bool expired = false;
			for (int i = _buffs.Count - 1; i >= 0; i--)
			{
				_buffs[i].Remaining -= dt;
				if (_buffs[i].Remaining <= 0f)
				{
					_buffs.RemoveAt(i);
					expired = true;
				}
			}
			if (expired) RefreshStats();
		}

		foreach (var (effect, stacks) in SnapshotEffects())
			effect.OnProcess(Context, stacks, dt);
	}

	// ───────────────────────── Consultas ─────────────────────────

	public int GetStacks(string id) => id != null && _stacks.TryGetValue(id, out int n) ? n : 0;

	public int TotalPassiveCount
	{
		get
		{
			int total = 0;
			foreach (var kv in _stacks) total += kv.Value;
			return total;
		}
	}

	/// <summary>Pasivos en orden de obtención.</summary>
	public List<ItemStack> GetPassiveItems()
	{
		var list = new List<ItemStack>();
		foreach (string id in _order)
		{
			int n = GetStacks(id);
			var def = ItemLibrary.Get(id);
			if (n > 0 && def != null) list.Add(new ItemStack(def, n));
		}
		return list;
	}

	public List<string> OwnedIds() => new List<string>(_order);

	// ───────────────────────── Obtener / quitar ─────────────────────────

	/// <summary>Recoger un objeto del mundo. Los activos se intercambian: el anterior cae al suelo.</summary>
	public void PickUp(ItemDefinition def, Vector2 worldPosition)
	{
		if (def == null) return;
		if (def.IsActive)
		{
			ItemDefinition previous = SetActiveItem(def);
			if (previous != null && previous.Id != def.Id)
				ItemPickup.Spawn(previous, worldPosition, worldPosition + new Vector2(0f, 26f), 1f, true);
		}
		else
		{
			AddItem(def, 1);
		}
	}

	public void AddItem(ItemDefinition def, int count = 1, bool silent = false)
	{
		if (def == null || count <= 0) return;
		if (def.IsActive)
		{
			SetActiveItem(def, silent);
			return;
		}

		if (!_effects.ContainsKey(def.Id))
		{
			ItemEffect effect = def.CreateEffect != null ? def.CreateEffect() : new NoEffect();
			effect.Definition = def;
			_effects[def.Id] = effect;
		}
		if (!_stacks.ContainsKey(def.Id))
		{
			_stacks[def.Id] = 0;
			_order.Add(def.Id);
		}
		_stacks[def.Id] += count;
		int total = _stacks[def.Id];

		var fx = _effects[def.Id];
		for (int i = 0; i < count; i++) fx.OnAcquired(Context, total);

		RefreshStats();
		if (!silent) ItemAdded?.Invoke(def, total);
		InventoryChanged?.Invoke();
	}

	public void RemoveItem(string id, int count = 1)
	{
		if (!_stacks.TryGetValue(id, out int current) || current <= 0) return;
		int remaining = current - Mathf.Max(1, count);
		if (remaining > 0)
		{
			_stacks[id] = remaining;
		}
		else
		{
			_stacks.Remove(id);
			_order.Remove(id);
			if (_effects.TryGetValue(id, out var effect))
			{
				effect.OnRemoved(Context);
				_effects.Remove(id);
			}
		}
		RefreshStats();
		InventoryChanged?.Invoke();
	}

	/// <summary>Equipa un activo. Devuelve el que había antes (o null).</summary>
	public ItemDefinition SetActiveItem(ItemDefinition def, bool silent = false)
	{
		ItemDefinition previous = ActiveItem;
		ActiveItem = def;
		_activeEffect = null;
		if (def != null)
		{
			_activeEffect = def.CreateEffect != null ? def.CreateEffect() : new NoEffect();
			_activeEffect.Definition = def;
		}
		ActiveCooldownRemaining = 0f;
		if (!silent && def != null) ItemAdded?.Invoke(def, 1);
		InventoryChanged?.Invoke();
		return previous;
	}

	public bool TryActivate()
	{
		if (ActiveItem == null || _activeEffect == null) return false;
		if (ActiveCooldownRemaining > 0f)
		{
			FloatingText.Spawn(Context.PlayerPosition, $"{ActiveItem.Name}: {Mathf.CeilToInt(ActiveCooldownRemaining)} s", UiFactory.TextMuted, 0.6f);
			return false;
		}
		bool used;
		try
		{
			used = _activeEffect.Activate(Context);
		}
		catch (Exception e)
		{
			GD.PushError($"Error al usar {ActiveItem.Id}: {e.Message}");
			used = false;
		}
		if (used)
		{
			ActiveCooldownRemaining = ActiveCooldownTotal;
			ActiveUsed?.Invoke(ActiveItem);
		}
		return used;
	}

	/// <summary>Agrega un efecto permanente que no es un objeto (pasivas de clase). No se guarda entre mapas.</summary>
	public void AddInnateEffect(ItemEffect effect)
	{
		if (effect == null || _innate.Contains(effect)) return;
		_innate.Add(effect);
		effect.OnAcquired(Context, 1);
		RefreshStats();
	}

	public void AddTimedBuff(string id, float duration, Action<PlayerStats> apply)
	{
		_buffs.RemoveAll(b => b.Id == id);   // reiniciar si ya estaba activo
		_buffs.Add(new TimedBuff { Id = id, Remaining = duration, Apply = apply });
		RefreshStats();
	}

	// ───────────────────────── Ganchos ─────────────────────────

	public void ApplyStatModifiers(PlayerStats stats)
	{
		foreach (var (effect, n) in SnapshotEffects())
			effect.ModifyStats(stats, n);
		foreach (var buff in _buffs) buff.Apply?.Invoke(stats);
	}

	private float ModifyOutgoingDamage(IEnemy target, float damage)
	{
		foreach (var (effect, n) in SnapshotEffects())
			damage = effect.ModifyDamageToEnemy(Context, n, target, damage);
		return damage;
	}

	public float ModifyIncomingDamage(float damage)
	{
		foreach (var (effect, n) in SnapshotEffects())
		{
			damage = effect.ModifyIncomingDamage(Context, n, damage);
			if (damage <= 0f) return 0f;
		}
		return damage;
	}

	public void NotifyDamaged(float damage, Node2D source)
	{
		foreach (var (effect, n) in SnapshotEffects())
			effect.OnPlayerDamaged(Context, n, damage, source);
	}

	public bool TryPreventDeath()
	{
		foreach (var (effect, n) in SnapshotEffects())
			if (effect.TryPreventDeath(Context, n)) return true;
		return false;
	}

	private void OnEnemyHit(HitInfo hit)
	{
		if (OwnerPlayer == null || !IsInstanceValid(OwnerPlayer)) return;
		foreach (var (effect, n) in SnapshotEffects())
			effect.OnEnemyHit(Context, n, hit);
	}

	private void OnEnemyKilled(KillInfo kill)
	{
		if (OwnerPlayer == null || !IsInstanceValid(OwnerPlayer)) return;
		foreach (var (effect, n) in SnapshotEffects())
			effect.OnEnemyKilled(Context, n, kill);
	}

	/// <summary>
	/// Efectos activos con su número de copias (objetos con al menos 1 copia + efectos innatos).
	/// Es una copia para iterar sin problemas si un efecto modifica el inventario.
	/// </summary>
	private List<(ItemEffect, int)> SnapshotEffects()
	{
		var list = new List<(ItemEffect, int)>(_effects.Count + _innate.Count);
		foreach (var effect in _innate) list.Add((effect, 1));
		foreach (string id in _order)
		{
			int n = GetStacks(id);
			if (n > 0 && _effects.TryGetValue(id, out var effect)) list.Add((effect, n));
		}
		return list;
	}

	private void RefreshStats()
	{
		if (OwnerPlayer != null && IsInstanceValid(OwnerPlayer)) OwnerPlayer.RecalculateStats();
	}

	// ───────────────────────── Guardar / restaurar ─────────────────────────

	public InventorySnapshot CreateSnapshot()
	{
		var snap = new InventorySnapshot
		{
			ActiveId = ActiveItem?.Id,
			ActiveCooldown = ActiveCooldownRemaining,
		};
		foreach (string id in _order)
		{
			snap.Order.Add(id);
			snap.Stacks[id] = GetStacks(id);
		}
		return snap;
	}

	public void Restore(InventorySnapshot snap)
	{
		if (snap == null) return;
		foreach (string id in snap.Order)
		{
			var def = ItemLibrary.Get(id);
			if (def == null || !snap.Stacks.TryGetValue(id, out int n) || n <= 0) continue;
			AddItem(def, n, true);
		}
		if (!string.IsNullOrEmpty(snap.ActiveId))
		{
			SetActiveItem(ItemLibrary.Get(snap.ActiveId), true);
			ActiveCooldownRemaining = snap.ActiveCooldown;
		}
	}
}
