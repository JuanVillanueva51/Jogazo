using System.Collections.Generic;
using Godot;

/// <summary>Estado de un dash en curso (enemigos ya golpeados, rastro...).</summary>
public sealed class DashRunState
{
	public readonly HashSet<ulong> Hit = new();
	public Vector2 From;
	public Vector2 Direction;
	public Vector2 LastTrailPoint;
}

/// <summary>
/// Efectos especiales de cada tipo de dash (ver DashStyle). Player llama a
/// OnStart / OnTick / OnEnd; aquí vive toda la lógica para no inflar Player.
/// </summary>
public static class DashEffects
{
	private static readonly Color ArcaneColor = new Color(0.75f, 0.45f, 1f);
	private static readonly Color ShockColor  = new Color(0.6f, 0.85f, 1f);
	private static readonly Color ShadowColor = new Color(0.45f, 0.3f, 0.6f);
	private static readonly Color HealColor   = new Color(0.45f, 1f, 0.55f);
	private static readonly Color LuckColor   = new Color(1f, 0.85f, 0.3f);
	private static readonly Color StoneColor  = new Color(0.8f, 0.8f, 0.85f);

	/// <summary>Al empezar el dash. Devuelve false si NO se debe gastar la carga.</summary>
	public static bool OnStart(Player p, DashProfile d, DashRunState state)
	{
		Vector2 from = state.From;
		float baseDamage = DamageCalculator.ItemBaseDamage(p) * d.Power;
		switch (d.Style)
		{
			case DashStyle.Parpadeo:
				// Explosión arcana donde estabas
				FxBurst.Spawn(from, 55f, ArcaneColor, 0.35f);
				HitArea(p, from, 55f, baseDamage * 1.0f, 0f, 0f, 0f);
				p.AddInvulnerability(0.25f);
				break;

			case DashStyle.PasoSombrio:
				FxBurst.Spawn(from, 22f, ShadowColor, 0.3f);
				p.GrantGuaranteedCrit(2f);
				p.AddInvulnerability(0.3f);
				break;

			case DashStyle.Sprint:
				p.Inventory?.AddTimedBuff("dash_sprint", 1.5f, s => s.MovementSpeedMult += 0.30f);
				break;

			case DashStyle.Propulsor:
				World.SpawnDeferred(new DashMine { Damage = baseDamage * 1.4f }, from);
				break;

			case DashStyle.EstelaToxica:
				SpawnPuddle(p, d, from);
				state.LastTrailPoint = from;
				break;

			case DashStyle.GolpeDeSuerte:
				float chance = 0.35f + 0.05f * Mathf.Max(0f, p.Stats.Luck);
				if (GD.Randf() < chance)
				{
					FloatingText.Spawn(p.GlobalPosition, "¡Suerte!", LuckColor, 0.6f);
					return false;
				}
				break;
		}
		return true;
	}

	/// <summary>Cada frame de física mientras dura el dash.</summary>
	public static void OnTick(Player p, DashProfile d, DashRunState state)
	{
		switch (d.Style)
		{
			case DashStyle.Embestida:
			{
				// Golpea y empuja a todo lo que atraviesa (una vez por enemigo)
				float damage = DamageCalculator.ItemBaseDamage(p) * 1.5f * d.Power;
				foreach (var enemy in CombatEvents.EnemiesInRadius(p.GetTree(), p.GlobalPosition, 24f))
				{
					ulong id = enemy.Body.GetInstanceId();
					if (!state.Hit.Add(id)) continue;
					enemy.TakeDamage(damage);
					if (!enemy.IsDead)
					{
						enemy.Knockback(state.Direction, 40f);
						enemy.ApplySlow(1f, 0.35f); // aturdido un instante
					}
					FxBurst.Spawn(enemy.Body.GlobalPosition, 14f, StoneColor, 0.2f);
				}
				break;
			}
			case DashStyle.EstelaToxica:
				if (p.GlobalPosition.DistanceTo(state.LastTrailPoint) >= 26f)
				{
					SpawnPuddle(p, d, p.GlobalPosition);
					state.LastTrailPoint = p.GlobalPosition;
				}
				break;
		}
	}

	/// <summary>Al terminar el dash.</summary>
	public static void OnEnd(Player p, DashProfile d, DashRunState state)
	{
		Vector2 to = p.GlobalPosition;
		float baseDamage = DamageCalculator.ItemBaseDamage(p) * d.Power;
		switch (d.Style)
		{
			case DashStyle.OndaExpansiva:
				FxBurst.Spawn(to, 85f, ShockColor, 0.35f);
				HitArea(p, to, 85f, baseDamage * 0.6f, 35f, 0.4f, 1.5f);
				break;

			case DashStyle.Bastion:
				p.Inventory?.AddTimedBuff("dash_bastion", 1.5f, s => s.DamageTakenMultiplier *= 0.4f);
				FxBurst.Spawn(to, 30f, StoneColor, 0.4f);
				FloatingText.Spawn(to, "Bastión", StoneColor, 0.6f);
				break;

			case DashStyle.PasoVital:
				p.Heal(p.MaxHp * 0.04f * d.Power);
				FxBurst.Spawn(to, 26f, HealColor, 0.35f);
				break;
		}
	}

	private static void SpawnPuddle(Player p, DashProfile d, Vector2 at)
	{
		float damage = DamageCalculator.ItemBaseDamage(p) * 0.25f * d.Power;
		World.SpawnDeferred(new ToxicPuddle { Damage = damage }, at);
	}

	/// <summary>Daña (y opcionalmente empuja/ralentiza) a los enemigos en un radio.</summary>
	private static void HitArea(Player p, Vector2 center, float radius, float damage,
		float knockback, float slowStrength, float slowDuration)
	{
		foreach (var enemy in CombatEvents.EnemiesInRadius(p.GetTree(), center, radius))
		{
			Vector2 away = enemy.Body.GlobalPosition - center;
			if (damage > 0f) enemy.TakeDamage(damage);
			if (enemy.IsDead) continue;
			if (knockback > 0f && away != Vector2.Zero) enemy.Knockback(away, knockback);
			if (slowStrength > 0f) enemy.ApplySlow(slowStrength, slowDuration);
		}
	}
}
