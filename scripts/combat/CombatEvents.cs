using System;
using System.Collections.Generic;
using Godot;

public readonly struct HitInfo
{
	public readonly IEnemy Target;
	public readonly float Damage;
	public readonly bool FromProc;
	public readonly Vector2 Position;

	public HitInfo(IEnemy target, float damage, bool fromProc, Vector2 position)
	{
		Target = target;
		Damage = damage;
		FromProc = fromProc;
		Position = position;
	}
}

public readonly struct KillInfo
{
	public readonly IEnemy Target;
	public readonly bool FromProc;
	public readonly bool IsElite;
	public readonly bool IsBoss;
	public readonly Vector2 Position;

	public KillInfo(IEnemy target, bool fromProc, bool isElite, bool isBoss, Vector2 position)
	{
		Target = target;
		FromProc = fromProc;
		IsElite = isElite;
		IsBoss = isBoss;
		Position = position;
	}
}

/// <summary>
/// Bus de eventos de combate. Todos los enemigos reportan aquí cada golpe y cada muerte,
/// sin importar qué arma u objeto hizo el daño. El inventario del jugador se suscribe
/// para disparar efectos "al golpear" / "al matar".
///
/// El daño hecho por efectos de objetos se marca como "proc" (DealProcDamage) para que
/// no vuelva a disparar efectos y evitar cadenas infinitas.
/// </summary>
public static class CombatEvents
{
	public static event Action<HitInfo> EnemyHit;
	public static event Action<KillInfo> EnemyKilled;

	/// <summary>Modificador de daño saliente (lo registra el inventario del jugador).</summary>
	public static Func<IEnemy, float, float> OutgoingDamageModifier;

	private static int _procDepth;
	public static bool InProc => _procDepth > 0;

	public static float ModifyDamageToEnemy(IEnemy target, float amount)
	{
		var modifier = OutgoingDamageModifier;
		return modifier == null ? amount : modifier(target, amount);
	}

	public static void RaiseEnemyHit(IEnemy target, float damage)
	{
		if (target == null) return;
		Vector2 pos = GodotObject.IsInstanceValid(target.Body) ? target.Body.GlobalPosition : Vector2.Zero;
		SafeInvoke(EnemyHit, new HitInfo(target, damage, InProc, pos));
	}

	public static void RaiseEnemyKilled(IEnemy target)
	{
		if (target == null) return;
		Vector2 pos = GodotObject.IsInstanceValid(target.Body) ? target.Body.GlobalPosition : Vector2.Zero;
		SafeInvoke(EnemyKilled, new KillInfo(target, InProc, target.IsElite, target.IsBoss, pos));
	}

	/// <summary>Daño de objetos: no vuelve a disparar efectos de objetos.</summary>
	public static void DealProcDamage(IEnemy target, float amount)
	{
		if (target == null || target.IsDead || amount <= 0f) return;
		if (!GodotObject.IsInstanceValid(target.Body)) return;
		_procDepth++;
		try
		{
			target.TakeDamage(amount);
		}
		finally
		{
			_procDepth--;
		}
	}

	/// <summary>Enemigos vivos dentro de un radio.</summary>
	public static List<IEnemy> EnemiesInRadius(SceneTree tree, Vector2 center, float radius, IEnemy exclude = null)
	{
		var result = new List<IEnemy>();
		if (tree == null) return result;
		float r2 = radius * radius;
		foreach (Node node in tree.GetNodesInGroup("enemies"))
		{
			if (node is not IEnemy enemy || enemy == exclude || enemy.IsDead) continue;
			if (!GodotObject.IsInstanceValid(enemy.Body)) continue;
			if (enemy.Body.GlobalPosition.DistanceSquaredTo(center) <= r2)
				result.Add(enemy);
		}
		return result;
	}

	/// <summary>Enemigo vivo más cercano (o null).</summary>
	public static IEnemy NearestEnemy(SceneTree tree, Vector2 from, float maxDistance, ICollection<IEnemy> exclude = null)
	{
		if (tree == null) return null;
		IEnemy best = null;
		float bestD2 = maxDistance * maxDistance;
		foreach (Node node in tree.GetNodesInGroup("enemies"))
		{
			if (node is not IEnemy enemy || enemy.IsDead) continue;
			if (exclude != null && exclude.Contains(enemy)) continue;
			if (!GodotObject.IsInstanceValid(enemy.Body)) continue;
			float d2 = enemy.Body.GlobalPosition.DistanceSquaredTo(from);
			if (d2 < bestD2)
			{
				bestD2 = d2;
				best = enemy;
			}
		}
		return best;
	}

	private static void SafeInvoke<T>(Action<T> handler, T args)
	{
		if (handler == null) return;
		foreach (Delegate d in handler.GetInvocationList())
		{
			try
			{
				((Action<T>)d)(args);
			}
			catch (Exception e)
			{
				GD.PushError($"CombatEvents: error en un efecto: {e.Message}\n{e.StackTrace}");
			}
		}
	}
}
