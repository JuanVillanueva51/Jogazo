using System.Collections.Generic;
using Godot;

/// <summary>
/// Guadaña: da una vuelta completa alrededor del jugador y golpea una vez
/// a cada enemigo que alcanza la hoja. Sigue al jugador mientras gira.
/// </summary>
public partial class ScytheSweep : Node2D, IWeaponSpawn
{
	private const string SheetPath = "res://sprites/Armas/Sprite-0001-Sheet-Sheet.png";
	private static readonly Rect2 BladeRegion = new Rect2(192f, 0f, 64f, 64f);
	private static readonly Color TrailColor = new Color(0.85f, 0.9f, 1f);
	private const float BaseRadius = 46f;

	private WeaponLaunch _launch;
	private Player _player;
	private Sprite2D _blade;
	private readonly HashSet<ulong> _hit = new();
	private float _time;
	private float _duration;
	private float _radius;
	private float _startAngle;
	private float _angle;

	public void Launch(WeaponLaunch launch) => _launch = launch;

	public override void _Ready()
	{
		ZIndex = 2;
		_player = _launch.Player;
		_duration = Mathf.Max(0.2f, _launch.Lifetime);
		_radius = BaseRadius * Mathf.Max(0.3f, _launch.Scale);
		// Empieza un poco antes de la dirección del objetivo para que el primer golpe sea hacia él
		Vector2 dir = _launch.Direction == Vector2.Zero ? Vector2.Right : _launch.Direction;
		_startAngle = dir.Angle() - 0.6f;
		_angle = _startAngle;

		if (ResourceLoader.Exists(SheetPath))
		{
			_blade = new Sprite2D
			{
				Texture = new AtlasTexture { Atlas = GD.Load<Texture2D>(SheetPath), Region = BladeRegion },
				TextureFilter = TextureFilterEnum.Nearest,
				Scale = Vector2.One * Mathf.Clamp(_launch.Scale, 0.5f, 2.5f) * 0.9f,
			};
			AddChild(_blade);
		}
		FollowPlayer();
		UpdateBlade();
	}

	public override void _Process(double delta)
	{
		if (_player == null || !IsInstanceValid(_player))
		{
			QueueFree();
			return;
		}
		FollowPlayer();

		float previous = _angle;
		_time += (float)delta;
		float t = Mathf.Clamp(_time / _duration, 0f, 1f);
		// Arranca rápido y frena al final (ease-out)
		_angle = _startAngle + Mathf.Tau * (1f - (1f - t) * (1f - t));
		HitBetween(previous, _angle);
		UpdateBlade();
		QueueRedraw();

		if (_time >= _duration) QueueFree();
	}

	private void FollowPlayer() => GlobalPosition = _player.GlobalPosition;

	private void UpdateBlade()
	{
		if (_blade == null) return;
		Vector2 dir = Vector2.FromAngle(_angle);
		_blade.Position = dir * _radius;
		_blade.Rotation = _angle + Mathf.Pi * 0.5f; // la parte curva mira hacia afuera
	}

	private void HitBetween(float from, float to)
	{
		float swept = to - from;
		if (swept <= 0f) return;
		Vector2 center = GlobalPosition;
		float reach = _radius + 14f;

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not Node2D enemy || !IsInstanceValid(enemy)) continue;
			if (enemy is IEnemy e && e.IsDead) continue;
			ulong id = enemy.GetInstanceId();
			if (_hit.Contains(id)) continue;

			Vector2 offset = enemy.GlobalPosition - center;
			float dist = offset.Length();
			if (dist > reach) continue;

			// ¿La hoja pasó por el ángulo del enemigo en este frame?
			bool inside = dist < 14f;
			if (!inside)
			{
				float rel = Mathf.PosMod(offset.Angle() - from, Mathf.Tau);
				inside = rel <= swept;
			}
			if (!inside) continue;

			_hit.Add(id);
			if (enemy is IDamageable damageable)
			{
				damageable.TakeDamage(_launch.Damage);
				if (enemy is IEnemy target && !target.IsDead && dist > 1f)
					target.Knockback(offset / dist, 6f);
			}
		}
	}

	public override void _Draw()
	{
		// Estela de la hoja
		float trail = 1.3f;
		float fade = 1f - Mathf.Clamp(_time / _duration, 0f, 1f) * 0.6f;
		for (int i = 0; i < 4; i++)
		{
			float a0 = _angle - trail * (i + 1) / 4f;
			float a1 = _angle - trail * i / 4f;
			Color c = TrailColor;
			c.A = (0.35f - i * 0.08f) * fade;
			DrawArc(Vector2.Zero, _radius, a0, a1, 8, c, 7f - i, true);
		}
	}
}
