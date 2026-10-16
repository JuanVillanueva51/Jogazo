using System.Collections.Generic;
using Godot;

/// <summary>
/// Tornado: avanza lento y serpenteando, atrapa a los enemigos que toca
/// (los jala hacia su centro) y les hace daño varias veces por segundo.
/// </summary>
public partial class TornadoProjectile : Area2D, IWeaponSpawn
{
	private const string SheetPath = "res://sprites/Armas/simplefx-alpha.png";
	private const float TickInterval = 0.3f;
	private const float PullSpeed = 45f;

	private WeaponLaunch _launch;
	private AnimatedSprite2D _sprite;
	private readonly List<Node2D> _inside = new();
	private Vector2 _direction;
	private Vector2 _side;
	private float _time;
	private float _tickTimer;
	private float _wobblePhase;

	public void Launch(WeaponLaunch launch) => _launch = launch;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 2;
		Monitorable = false;
		ZIndex = 2;

		_direction = _launch.Direction == Vector2.Zero ? Vector2.Right : _launch.Direction.Normalized();
		_side = new Vector2(-_direction.Y, _direction.X);
		_wobblePhase = (float)GD.RandRange(0.0, Mathf.Tau);

		float scale = Mathf.Max(0.4f, _launch.Scale);
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 16f * scale } });

		if (ResourceLoader.Exists(SheetPath))
		{
			var atlas = GD.Load<Texture2D>(SheetPath);
			var frames = new SpriteFrames();
			frames.SetAnimationSpeed("default", 14);
			for (int i = 1; i <= 4; i++)
				frames.AddFrame("default", new AtlasTexture { Atlas = atlas, Region = new Rect2(16f * i, 16f, 16f, 16f) });
			_sprite = new AnimatedSprite2D
			{
				SpriteFrames = frames,
				TextureFilter = TextureFilterEnum.Nearest,
				Scale = Vector2.One * 2.4f * scale,
			};
			AddChild(_sprite);
			_sprite.Play("default");
		}

		GlobalPosition = _launch.Origin;
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;

		// Avance con un vaivén lateral
		float wobble = Mathf.Cos(_time * 5f + _wobblePhase) * 40f;
		GlobalPosition += (_direction * _launch.Speed + _side * wobble) * dt;

		// Jalar a los enemigos atrapados hacia el centro
		_inside.RemoveAll(n => !IsInstanceValid(n));
		foreach (var body in _inside)
		{
			if (body is not IEnemy enemy || enemy.IsDead) continue;
			Vector2 toCenter = GlobalPosition - body.GlobalPosition;
			if (toCenter.Length() > 3f) enemy.Knockback(toCenter, PullSpeed * dt);
		}

		_tickTimer -= dt;
		if (_tickTimer <= 0f)
		{
			_tickTimer = TickInterval;
			foreach (var body in _inside.ToArray())
			{
				if (!IsInstanceValid(body)) continue;
				if (body is IEnemy enemy && enemy.IsDead) continue;
				if (body is IDamageable damageable) damageable.TakeDamage(_launch.Damage);
			}
		}

		// Se desvanece al final
		float remaining = _launch.Lifetime - _time;
		if (remaining < 0.3f)
		{
			var m = Modulate;
			m.A = Mathf.Clamp(remaining / 0.3f, 0f, 1f);
			Modulate = m;
		}
		if (remaining <= 0f) QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("enemies") && !_inside.Contains(body)) _inside.Add(body);
	}

	private void OnBodyExited(Node2D body) => _inside.Remove(body);
}
