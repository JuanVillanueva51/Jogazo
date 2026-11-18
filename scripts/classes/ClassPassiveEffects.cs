using System.Collections.Generic;
using Godot;

// ════════════════════════════════════════════════════════════════════════════
//  Pasivas de clase con lógica propia.
//  Usan el mismo sistema de ganchos que los objetos (ItemEffect), pero se agregan
//  al inventario como "efectos innatos": no ocupan espacio ni se pueden perder.
//  Siempre se llaman con stacks = 1 y Definition = null.
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Piromante: probabilidad de quemar al enemigo (3 pulsos de daño).</summary>
public sealed class BurnOnHitPassive : ItemEffect
{
	private static readonly Color FireColor = new Color(1f, 0.5f, 0.15f);
	private const float Chance = 0.15f;
	private const int Ticks = 3;
	private readonly HashSet<ulong> _burning = new();

	public override void OnEnemyHit(ItemContext ctx, int stacks, HitInfo hit)
	{
		if (hit.FromProc || hit.Target == null || hit.Target.IsDead) return;
		var tree = ctx.Tree;
		if (tree == null || !ctx.Roll(Chance)) return;
		Node2D body = hit.Target.Body;
		if (body == null || !GodotObject.IsInstanceValid(body)) return;
		ulong id = body.GetInstanceId();
		if (!_burning.Add(id)) return; // ya se está quemando

		IEnemy target = hit.Target;
		float damage = ctx.BaseDamage * 0.4f;
		for (int i = 1; i <= Ticks; i++)
		{
			bool last = i == Ticks;
			tree.CreateTimer(0.8 * i, false).Timeout += () =>
			{
				if (last) _burning.Remove(id);
				if (!GodotObject.IsInstanceValid(body) || target.IsDead) { _burning.Remove(id); return; }
				FxBurst.Spawn(body.GlobalPosition, 14f, FireColor, 0.25f);
				CombatEvents.DealProcDamage(target, damage);
			};
		}
	}
}

/// <summary>Astrónomo: cada pocos segundos cae una estrella sobre un enemigo cercano.</summary>
public sealed class StarfallPassive : ItemEffect
{
	private static readonly Color StarColor = new Color(1f, 0.9f, 0.5f);
	private const float Interval = 7f;
	private const float Radius = 55f;
	private float _timer;

	public override void OnProcess(ItemContext ctx, int stacks, float delta)
	{
		_timer += delta;
		if (_timer < Interval) return;
		var tree = ctx.Tree;
		if (tree == null) return;
		var candidates = CombatEvents.EnemiesInRadius(tree, ctx.PlayerPosition, 320f);
		if (candidates.Count == 0)
		{
			_timer = Interval; // esperar sin acumular
			return;
		}
		_timer = 0f;
		IEnemy target = candidates[GD.RandRange(0, candidates.Count - 1)];
		Vector2 pos = target.Body.GlobalPosition;
		FxBolt.Spawn(pos + new Vector2(-60f, -200f), pos, StarColor, 3f, 0.25f);
		FxBurst.Spawn(pos, Radius, StarColor, 0.4f);
		float damage = ctx.BaseDamage * 1.5f;
		foreach (var enemy in CombatEvents.EnemiesInRadius(tree, pos, Radius))
			CombatEvents.DealProcDamage(enemy, damage);
	}
}

/// <summary>Nigromante: cada muerte cura un poco.</summary>
public sealed class SoulHarvestPassive : ItemEffect
{
	public override void OnEnemyKilled(ItemContext ctx, int stacks, KillInfo kill)
	{
		if (ctx.Player == null) return;
		ctx.Player.Heal(1f + ctx.Player.MaxHp * 0.005f);
	}
}

/// <summary>Segador: más daño a enemigos heridos.</summary>
public sealed class ReaperPassive : ItemEffect
{
	public override float ModifyDamageToEnemy(ItemContext ctx, int stacks, IEnemy target, float damage)
	{
		if (target == null || target.MaxHealth <= 0f) return damage;
		return target.CurrentHealth / target.MaxHealth < 0.35f ? damage * 1.3f : damage;
	}
}

/// <summary>Aeromante: los golpes a veces empujan al enemigo.</summary>
public sealed class GalePassive : ItemEffect
{
	public override void OnEnemyHit(ItemContext ctx, int stacks, HitInfo hit)
	{
		if (hit.FromProc || hit.Target == null || hit.Target.IsDead || !ctx.Roll(0.10f)) return;
		Node2D body = hit.Target.Body;
		if (body == null || !GodotObject.IsInstanceValid(body)) return;
		Vector2 away = body.GlobalPosition - ctx.PlayerPosition;
		if (away == Vector2.Zero) away = Vector2.Right;
		hit.Target.Knockback(away, 18f);
	}
}

/// <summary>Criomante: todos los golpes ralentizan un poco.</summary>
public sealed class FrostbitePassive : ItemEffect
{
	public override void OnEnemyHit(ItemContext ctx, int stacks, HitInfo hit)
	{
		if (hit.FromProc || hit.Target == null || hit.Target.IsDead) return;
		hit.Target.ApplySlow(0.2f, 1.5f);
	}
}
