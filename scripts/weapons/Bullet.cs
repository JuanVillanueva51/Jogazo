using Godot;
using System;

public partial class Bullet : Area2D
{
	[Export] public float Speed      = 350f;
	[Export] public float Damage     = 30f;
	[Export] public float Lifetime   = 3f;
	[Export] public float BulletScale = 1f;
	[Export] public int   PierceCount = 1;
	[Export] public int   BounceCount = 0;
	//Evento que notifica al jugador cuánto daño hizo
	public event Action<float> OnHit;
	private Vector2      _direction;
	private float        _lifeTimer  = 0f;
	private int          _pierceLeft;
	private int          _bounceLeft;
	public override void _Ready()
	{
		_pierceLeft = PierceCount;
		_bounceLeft = BounceCount;
		Scale       = Vector2.One * BulletScale;
		BodyEntered += OnBodyEntered;
	}

	public void Initialize(Vector2 direction)
	{
		_direction = direction.Normalized();
		Rotation   = _direction.Angle();
	}
	
public override void _Process(double delta)
{
	BaseProcess(delta);
}

protected virtual void BaseProcess(double delta)
{
	GlobalPosition += _direction * Speed * (float)delta;

	_lifeTimer += (float)delta;

	if (_lifeTimer >= Lifetime)
		QueueFree();
}

	protected virtual void OnBodyEntered(Node2D body)
	{
		if (!body.IsInGroup("enemies")) return;

		if (body is IDamageable damageable)
		{
			damageable.TakeDamage(Damage);
			OnHit?.Invoke(Damage);
		}

		_pierceLeft--;

		if (_pierceLeft > 0) return;

		// Sin pierce — intentar rebotar
		if (_bounceLeft > 0)
		{
			_bounceLeft--;
			_pierceLeft = PierceCount; // resetear pierce para el rebote

			Node2D next = FindNextTarget(body);
			if (next != null)
			{
				_direction = (next.GlobalPosition - GlobalPosition).Normalized();
				Rotation   = _direction.Angle();
				_lifeTimer = 0f; // resetear lifetime en rebote
				return;
			}
		}

		QueueFree();
	}

	private Node2D FindNextTarget(Node2D exclude)
	{
		var    enemies  = GetTree().GetNodesInGroup("enemies");
		Node2D nearest  = null;
		float  minDist  = float.MaxValue;

		foreach (Node node in enemies)
		{
			if (node == exclude) continue;
			if (node is Node2D e)
			{
				float d = GlobalPosition.DistanceTo(e.GlobalPosition);
				if (d < minDist) { minDist = d; nearest = e; }
			}
		}
		return nearest;
	}
	protected void TriggerHit(float damage)
	{
	OnHit?.Invoke(damage);
	}
}
