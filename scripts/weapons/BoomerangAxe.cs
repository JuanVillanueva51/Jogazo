using System.Collections.Generic;
using Godot;

/// <summary>
/// Hacha bumerán: sale disparada, frena y regresa al jugador.
/// Atraviesa a todos los enemigos y puede golpear a cada uno una vez de ida y otra de vuelta.
/// </summary>
public partial class BoomerangAxe : Area2D, IWeaponSpawn
{
	private const string SheetPath = "res://sprites/Armas/AxeWeapon.png";
	private static readonly Rect2 AxeRegion = new Rect2(0f, 0f, 32f, 32f);

	private WeaponLaunch _launch;
	private Player _player;
	private Sprite2D _sprite;
	private Vector2 _velocity;
	private Vector2 _outDirection;
	private float _speed;
	private float _outTime;
	private float _time;
	private bool _returning;
	private readonly HashSet<ulong> _hitOut = new();
	private readonly HashSet<ulong> _hitBack = new();

	public void Launch(WeaponLaunch launch) => _launch = launch;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 2;
		Monitorable = false;
		ZIndex = 2;
		_player = _launch.Player;
		_speed = Mathf.Max(60f, _launch.Speed);
		_outDirection = _launch.Direction == Vector2.Zero ? Vector2.Right : _launch.Direction.Normalized();
		_velocity = _outDirection * _speed;
		// La mitad del tiempo de vida es la ida (alcance = velocidad * ida / 2; con 320 y 2 s ≈ 160 px)
		_outTime = Mathf.Max(0.2f, _launch.Lifetime * 0.5f);

		float scale = Mathf.Max(0.4f, _launch.Scale);
		AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 11f * scale } });

		if (ResourceLoader.Exists(SheetPath))
		{
			_sprite = new Sprite2D
			{
				Texture = new AtlasTexture { Atlas = GD.Load<Texture2D>(SheetPath), Region = AxeRegion },
				TextureFilter = TextureFilterEnum.Nearest,
				Scale = Vector2.One * scale * 0.8f,
			};
			AddChild(_sprite);
		}

		GlobalPosition = _launch.Origin;
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;
		if (_sprite != null) _sprite.Rotation += 18f * dt;

		if (!_returning)
		{
			// Frena de forma constante hasta detenerse y entonces regresa
			_velocity -= _outDirection * (_speed / _outTime) * dt;
			if (_velocity.Dot(_outDirection) <= 0f) _returning = true;
		}
		else
		{
			if (_player == null || !IsInstanceValid(_player))
			{
				QueueFree();
				return;
			}
			Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
			float returnSpeed = Mathf.Min(_speed * 1.3f, _velocity.Length() + _speed * 2.5f * dt);
			_velocity = toPlayer.Normalized() * returnSpeed;
			if (toPlayer.Length() < 14f)
			{
				QueueFree();
				return;
			}
		}

		GlobalPosition += _velocity * dt;

		// Seguridad: nunca vive más del triple de lo previsto
		if (_time > _launch.Lifetime * 3f + 1f) QueueFree();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("enemies") || body is not IDamageable damageable) return;
		var set = _returning ? _hitBack : _hitOut;
		if (!set.Add(body.GetInstanceId())) return;
		damageable.TakeDamage(_launch.Damage);
	}
}
