using System;
using System.Collections.Generic;
using Godot;

// ════════════════════════════════════════════════════════════════════════════
//  Efectos genéricos
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Objeto que solo modifica estadísticas.</summary>
public sealed class StatEffect : ItemEffect
{
	private readonly Action<PlayerStats, int> _apply;
	public StatEffect(Action<PlayerStats, int> apply) { _apply = apply; }
	public override void ModifyStats(PlayerStats stats, int stacks) => _apply?.Invoke(stats, stacks);
}

/// <summary>Sin efecto (p. ej. objetos consumidos).</summary>
public sealed class NoEffect : ItemEffect { }

// ════════════════════════════════════════════════════════════════════════════
//  Pasivos con lógica
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Regla Metálica: más daño a enemigos con casi toda su vida (Crowbar de RoR2).</summary>
public sealed class HighHealthBonusEffect : ItemEffect
{
	public override float ModifyDamageToEnemy(ItemContext ctx, int stacks, IEnemy target, float damage)
	{
		if (target == null || target.MaxHealth <= 0f) return damage;
		if (target.CurrentHealth >= target.MaxHealth * 0.9f)
			return damage * (1f + 0.6f * stacks);
		return damage;
	}
}

/// <summary>Cable Pelado: probabilidad de rayo encadenado (Ukulele de RoR2).</summary>
public sealed class ChainLightningEffect : ItemEffect
{
	private static readonly Color BoltColor = new Color(0.55f, 0.85f, 1f);

	public override void OnEnemyHit(ItemContext ctx, int stacks, HitInfo hit)
	{
		if (hit.FromProc || !ctx.Roll(0.20f)) return;
		var tree = ctx.Tree;
		if (tree == null) return;

		int targets = 1 + stacks;
		var already = new HashSet<IEnemy> { hit.Target };
		Vector2 from = hit.Position;
		float damage = hit.Damage * 0.6f;
		for (int i = 0; i < targets; i++)
		{
			IEnemy next = CombatEvents.NearestEnemy(tree, from, 140f, already);
			if (next == null) break;
			already.Add(next);
			Vector2 to = next.Body.GlobalPosition;
			FxBolt.Spawn(from, to, BoltColor);
			CombatEvents.DealProcDamage(next, damage);
			from = to;
		}
	}
}

/// <summary>Casco de Obra: probabilidad hiperbólica de bloquear un golpe (Tougher Times).</summary>
public sealed class BlockChanceEffect : ItemEffect
{
	public override float ModifyIncomingDamage(ItemContext ctx, int stacks, float damage)
	{
		float chance = 1f - 1f / (1f + 0.12f * stacks);
		if (GD.Randf() < chance)
		{
			FloatingText.Spawn(ctx.PlayerPosition, "¡Bloqueado!", new Color(0.8f, 0.9f, 1f));
			return 0f;
		}
		return damage;
	}
}

/// <summary>Encendedor: al matar, explosión alrededor del enemigo (Gasoline).</summary>
public sealed class KillExplosionEffect : ItemEffect
{
	private static readonly Color FireColor = new Color(1f, 0.55f, 0.2f);

	public override void OnEnemyKilled(ItemContext ctx, int stacks, KillInfo kill)
	{
		if (kill.FromProc) return;
		float radius = 60f + 15f * (stacks - 1);
		float damage = ctx.BaseDamage * 1.2f;
		FxBurst.Spawn(kill.Position, radius, FireColor);
		foreach (var enemy in CombatEvents.EnemiesInRadius(ctx.Tree, kill.Position, radius, kill.Target))
			CombatEvents.DealProcDamage(enemy, damage);
	}
}

/// <summary>Colmillo de Murciélago: cura al matar.</summary>
public sealed class HealOnKillEffect : ItemEffect
{
	public override void OnEnemyKilled(ItemContext ctx, int stacks, KillInfo kill)
	{
		ctx.Player?.Heal(3f * stacks);
	}
}

/// <summary>Cronómetro: tras 2 s sin recibir daño, más velocidad (Red Whip).</summary>
public sealed class OutOfCombatSpeedEffect : ItemEffect
{
	private float _sinceHit = 99f;
	private bool _active;

	public override void ModifyStats(PlayerStats stats, int stacks)
	{
		if (_active) stats.MovementSpeedMult += 0.25f + 0.15f * (stacks - 1);
	}

	public override void OnProcess(ItemContext ctx, int stacks, float delta)
	{
		_sinceHit += delta;
		if (!_active && _sinceHit >= 2f)
		{
			_active = true;
			ctx.RequestStatRefresh();
		}
	}

	public override void OnPlayerDamaged(ItemContext ctx, int stacks, float damage, Node2D source)
	{
		_sinceHit = 0f;
		if (_active)
		{
			_active = false;
			ctx.RequestStatRefresh();
		}
	}
}

/// <summary>Termo de Hielo: probabilidad de ralentizar al golpear.</summary>
public sealed class SlowOnHitEffect : ItemEffect
{
	public override void OnEnemyHit(ItemContext ctx, int stacks, HitInfo hit)
	{
		if (hit.FromProc || hit.Target == null || hit.Target.IsDead) return;
		float chance = 0.10f + 0.05f * (stacks - 1);
		if (ctx.Roll(chance)) hit.Target.ApplySlow(0.6f, 2f);
	}
}

/// <summary>Sentencia Final: ejecuta enemigos (no jefes) con poca vida (Old Guillotine).</summary>
public sealed class ExecuteEffect : ItemEffect
{
	public override void OnEnemyHit(ItemContext ctx, int stacks, HitInfo hit)
	{
		var target = hit.Target;
		if (target == null || target.IsDead || target.IsBoss) return;
		if (target.CurrentHealth <= 0f || target.MaxHealth <= 0f) return;
		float threshold = 1f - 1f / (1f + 0.13f * stacks);
		if (target.CurrentHealth / target.MaxHealth <= threshold)
		{
			FloatingText.Spawn(hit.Position, "¡Ejecutado!", new Color(1f, 0.4f, 0.4f), 0.6f);
			CombatEvents.DealProcDamage(target, target.CurrentHealth + 1f);
		}
	}
}

/// <summary>Credencial de Repuesto: evita la muerte una vez y se consume (Dio's Best Friend).</summary>
public sealed class ReviveEffect : ItemEffect
{
	public override bool TryPreventDeath(ItemContext ctx, int stacks)
	{
		if (stacks <= 0 || ctx.Player == null) return false;
		ctx.Inventory.RemoveItem(Definition.Id, 1);
		var spent = ItemLibrary.Get(ItemLibrary.SpentCredentialId);
		if (spent != null) ctx.Inventory.AddItem(spent, 1, true);
		ctx.Player.Revive(0.5f, 2f);
		FxBurst.Spawn(ctx.PlayerPosition, 120f, new Color(1f, 0.9f, 0.4f), 0.6f);
		FloatingText.Spawn(ctx.PlayerPosition, "¡Segunda oportunidad!", new Color(1f, 0.9f, 0.4f), 1.4f);
		return true;
	}
}

/// <summary>Tormenta de Exámenes: rayo periódico sobre el enemigo más cercano.</summary>
public sealed class PeriodicLightningEffect : ItemEffect
{
	private static readonly Color BoltColor = new Color(0.9f, 0.95f, 0.4f);
	private float _timer;

	public override void OnProcess(ItemContext ctx, int stacks, float delta)
	{
		float interval = 3f * Mathf.Pow(0.85f, stacks - 1);
		_timer += delta;
		if (_timer < interval) return;

		IEnemy target = CombatEvents.NearestEnemy(ctx.Tree, ctx.PlayerPosition, 420f);
		if (target == null)
		{
			_timer = interval; // espera al siguiente enemigo sin acumular
			return;
		}
		_timer = 0f;
		Vector2 pos = target.Body.GlobalPosition;
		FxBolt.Spawn(pos + new Vector2(0f, -220f), pos, BoltColor, 3f, 0.25f);
		FxBurst.Spawn(pos, 30f, BoltColor, 0.25f);
		CombatEvents.DealProcDamage(target, ctx.BaseDamage * (3f + 1.5f * (stacks - 1)));
	}
}

/// <summary>Matraz Explosivo: cada golpe de arma genera una pequeña explosión.</summary>
public sealed class OnHitExplosionEffect : ItemEffect
{
	private static readonly Color BlastColor = new Color(1f, 0.4f, 0.8f);
	private const float MaxTokens = 20f;
	private float _tokens = MaxTokens;

	public override void OnProcess(ItemContext ctx, int stacks, float delta)
	{
		// Límite de explosiones por segundo para no saturar el juego
		_tokens = Mathf.Min(MaxTokens, _tokens + delta * 20f);
	}

	public override void OnEnemyHit(ItemContext ctx, int stacks, HitInfo hit)
	{
		if (hit.FromProc || _tokens < 1f) return;
		_tokens -= 1f;
		float damage = hit.Damage * 0.4f * stacks;
		FxBurst.Spawn(hit.Position, 45f, BlastColor, 0.25f);
		foreach (var enemy in CombatEvents.EnemiesInRadius(ctx.Tree, hit.Position, 45f, hit.Target))
			CombatEvents.DealProcDamage(enemy, damage);
	}
}

// ════════════════════════════════════════════════════════════════════════════
//  Activos (tecla Q por defecto)
// ════════════════════════════════════════════════════════════════════════════

/// <summary>Botiquín de Emergencia: cura un % de la vida máxima.</summary>
public sealed class HealActive : ItemEffect
{
	private readonly float _fraction;
	public HealActive(float fraction) { _fraction = fraction; }

	public override bool Activate(ItemContext ctx)
	{
		var p = ctx.Player;
		if (p == null) return false;
		if (p.CurrentHp >= p.MaxHp)
		{
			FloatingText.Spawn(ctx.PlayerPosition, "Vida completa", UiFactory.TextMuted);
			return false;
		}
		p.Heal(p.MaxHp * _fraction);
		FxBurst.Spawn(ctx.PlayerPosition, 50f, new Color(0.4f, 1f, 0.5f));
		return true;
	}
}

/// <summary>Silbato del Prefecto: aturde y empuja a los enemigos cercanos.</summary>
public sealed class StunNovaActive : ItemEffect
{
	public override bool Activate(ItemContext ctx)
	{
		const float radius = 170f;
		Vector2 center = ctx.PlayerPosition;
		FxBurst.Spawn(center, radius, new Color(0.9f, 0.9f, 1f), 0.45f);
		foreach (var enemy in CombatEvents.EnemiesInRadius(ctx.Tree, center, radius))
		{
			enemy.ApplySlow(1f, 2.5f);
			Vector2 dir = (enemy.Body.GlobalPosition - center).Normalized();
			enemy.Knockback(dir, 70f);
		}
		return true;
	}
}

/// <summary>Bebida Energética: mejora temporal de estadísticas.</summary>
public sealed class TimedBuffActive : ItemEffect
{
	private readonly float _duration;
	private readonly Action<PlayerStats> _buff;
	private readonly Color _color;

	public TimedBuffActive(float duration, Color color, Action<PlayerStats> buff)
	{
		_duration = duration;
		_buff = buff;
		_color = color;
	}

	public override bool Activate(ItemContext ctx)
	{
		ctx.Inventory.AddTimedBuff(Definition.Id, _duration, _buff);
		FxBurst.Spawn(ctx.PlayerPosition, 60f, _color);
		return true;
	}
}

/// <summary>Bomba de Pintura: gran daño en área alrededor del jugador.</summary>
public sealed class DamageNovaActive : ItemEffect
{
	private readonly float _radius;
	private readonly float _damageMultiplier;
	public DamageNovaActive(float radius, float damageMultiplier) { _radius = radius; _damageMultiplier = damageMultiplier; }

	public override bool Activate(ItemContext ctx)
	{
		Vector2 center = ctx.PlayerPosition;
		float damage = ctx.BaseDamage * _damageMultiplier;
		FxBurst.Spawn(center, _radius, new Color(1f, 0.3f, 0.7f), 0.5f);
		foreach (var enemy in CombatEvents.EnemiesInRadius(ctx.Tree, center, _radius))
			CombatEvents.DealProcDamage(enemy, damage);
		return true;
	}
}

/// <summary>Reloj de Arena: ralentiza a todos los enemigos.</summary>
public sealed class GlobalSlowActive : ItemEffect
{
	public override bool Activate(ItemContext ctx)
	{
		FxBurst.Spawn(ctx.PlayerPosition, 300f, new Color(0.6f, 0.8f, 1f), 0.8f);
		foreach (var enemy in CombatEvents.EnemiesInRadius(ctx.Tree, ctx.PlayerPosition, 2000f))
			enemy.ApplySlow(0.7f, 5f);
		return true;
	}
}

/// <summary>Lluvia de Meteoros: impactos escalonados sobre enemigos aleatorios.</summary>
public sealed class MeteorShowerActive : ItemEffect
{
	private static readonly Color MeteorColor = new Color(1f, 0.5f, 0.15f);

	public override bool Activate(ItemContext ctx)
	{
		var tree = ctx.Tree;
		var enemies = CombatEvents.EnemiesInRadius(tree, ctx.PlayerPosition, 480f);
		if (enemies.Count == 0)
		{
			FloatingText.Spawn(ctx.PlayerPosition, "Sin objetivos", UiFactory.TextMuted);
			return false;
		}
		// Mezcla aleatoria
		for (int i = enemies.Count - 1; i > 0; i--)
		{
			int j = GD.RandRange(0, i);
			(enemies[i], enemies[j]) = (enemies[j], enemies[i]);
		}
		float damage = ctx.BaseDamage * 4f;
		int count = Mathf.Min(8, enemies.Count);
		for (int i = 0; i < count; i++)
		{
			Vector2 target = enemies[i].Body.GlobalPosition;
			tree.CreateTimer(0.12 * i, false).Timeout += () => Strike(tree, target, damage);
		}
		return true;
	}

	private static void Strike(SceneTree tree, Vector2 position, float damage)
	{
		FxBolt.Spawn(position + new Vector2(30f, -240f), position, MeteorColor, 4f, 0.2f);
		FxBurst.Spawn(position, 50f, MeteorColor, 0.35f);
		foreach (var enemy in CombatEvents.EnemiesInRadius(tree, position, 50f))
			CombatEvents.DealProcDamage(enemy, damage);
	}
}

/// <summary>Trato con el Diablo: monedas a cambio de vida.</summary>
public sealed class BloodForGoldActive : ItemEffect
{
	public override bool Activate(ItemContext ctx)
	{
		var p = ctx.Player;
		if (p == null || GameManager.Instance == null) return false;
		int coins = Mathf.RoundToInt(60f * GameManager.Instance.CostScale);
		GameManager.Instance.AddCoinsRaw(coins);
		p.LoseHealthFraction(0.25f);
		FloatingText.Spawn(ctx.PlayerPosition, $"+{coins} monedas", UiFactory.CoinColor, 1.2f);
		FxBurst.Spawn(ctx.PlayerPosition, 50f, new Color(0.8f, 0.1f, 0.2f));
		return true;
	}
}
