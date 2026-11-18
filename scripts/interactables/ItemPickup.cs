using Godot;

/// <summary>
/// Objeto tirado en el mundo. Se recoge al pasar encima (como en Risk of Rain 2 / Megabonk).
/// Se crea desde código con ItemPickup.Spawn().
/// </summary>
public partial class ItemPickup : Area2D
{
	public ItemDefinition Item { get; private set; }

	private float _pickupDelay = 0.6f;
	private float _time;
	private bool _collected;
	private Label _nameLabel;
	private Vector2 _launchFrom;
	private Vector2 _launchTo;
	private float _launchDuration = 0.35f;
	// Si es true, el jugador debe salir del área antes de poder recogerlo
	// (evita re-recoger en bucle un activo que se acaba de soltar al intercambiar).
	private bool _armed = true;

	public static void Spawn(ItemDefinition item, Vector2 fromPosition, Vector2 landPosition,
		float pickupDelay = 0.6f, bool requireExitFirst = false)
	{
		if (item == null) return;
		var pickup = new ItemPickup
		{
			Item = item,
			_pickupDelay = pickupDelay,
			_launchFrom = fromPosition,
			_launchTo = landPosition,
			_armed = !requireExitFirst,
			ZIndex = 3,
		};
		World.SpawnDeferred(pickup, fromPosition);
	}

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 1;
		Monitorable = false;
		// Que nunca caiga dentro o detrás de un muro
		_launchTo = World.ReachablePoint(_launchFrom, _launchTo, 14f);

		var shape = new CollisionShape2D { Shape = new CircleShape2D { Radius = 12f } };
		AddChild(shape);

		_nameLabel = UiFactory.MakeLabel(Item != null ? Item.Name : "?", 8, Item != null ? Item.Color : Colors.White);
		_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_nameLabel.Size = new Vector2(140f, 12f);
		_nameLabel.Position = new Vector2(-70f, -30f);
		_nameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_nameLabel);

		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;

		// Pequeño "salto" al salir del cofre
		if (_time < _launchDuration)
		{
			float k = _time / _launchDuration;
			Vector2 flat = _launchFrom.Lerp(_launchTo, k);
			float arc = Mathf.Sin(k * Mathf.Pi) * 18f;
			GlobalPosition = flat + new Vector2(0f, -arc);
		}
		else if (_time - dt < _launchDuration)
		{
			GlobalPosition = _launchTo;
		}

		QueueRedraw();

		// Si el jugador ya estaba encima cuando terminó el retraso, recógelo
		if (!_collected && _time >= _pickupDelay)
		{
			Player overlapping = null;
			foreach (Node2D body in GetOverlappingBodies())
			{
				if (body is Player player)
				{
					overlapping = player;
					break;
				}
			}
			if (!_armed)
			{
				if (overlapping == null) _armed = true;
			}
			else if (overlapping != null)
			{
				Collect(overlapping);
			}
		}
	}

	public override void _Draw()
	{
		if (Item == null) return;
		Color c = Item.Color;
		float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 4f);
		var glow = c;
		glow.A = 0.18f + 0.12f * pulse;
		DrawCircle(Vector2.Zero, 13f + 2f * pulse, glow);
		var core = new Color(c.R * 0.35f, c.G * 0.35f, c.B * 0.35f, 1f);
		DrawCircle(Vector2.Zero, 7f, core);
		DrawArc(Vector2.Zero, 7f, 0f, Mathf.Tau, 20, c, 2f);
		if (Item.IsActive)
		{
			// Los activos se marcan con un rombo
			var diamond = new Vector2[] { new(0f, -4f), new(4f, 0f), new(0f, 4f), new(-4f, 0f) };
			DrawColoredPolygon(diamond, c);
		}
		else
		{
			DrawCircle(Vector2.Zero, 3f, c);
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_collected || !_armed || _time < _pickupDelay) return;
		if (body is Player player) Collect(player);
	}

	private void Collect(Player player)
	{
		if (_collected || player.Inventory == null || Item == null) return;
		_collected = true;
		player.Inventory.PickUp(Item, GlobalPosition);
		QueueFree();
	}
}
