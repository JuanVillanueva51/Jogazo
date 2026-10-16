using Godot;

/// <summary>
/// Cofre de objetos. Se puede colocar a mano en los mapas (scenes/interactables/chest.tscn)
/// o lo sueltan élites y jefes (siempre gratis).
///  - Tier: tipo de cofre (define rarezas posibles).
///  - BaseCost: -1 = costo por defecto del tipo, 0 = gratis. El costo escala con la dificultad.
/// Se abre con la acción "interact" (E).
/// </summary>
public partial class Chest : Interactable
{
	private const string AtlasPath = "res://sprites/sprites3erNivel/doors_lever_chest_animation.png";
	private static readonly Rect2 ClosedRegion = new Rect2(0f, 128f, 32f, 32f);
	private static readonly Rect2 OpenRegion   = new Rect2(128f, 128f, 32f, 32f);

	[Export] public ChestTier Tier { get; set; } = ChestTier.Pequeno;
	[Export] public int BaseCost { get; set; } = -1;
	/// <summary>Los cofres que sueltan los enemigos son gratis.</summary>
	[Export] public bool IsFree { get; set; } = false;

	private bool _opened;
	private float _time;
	private Sprite2D _sprite;
	private AtlasTexture _closedTex;
	private AtlasTexture _openTex;

	public int CurrentCost => IsFree ? 0 : LootTable.ChestCost(Tier, BaseCost);

	public static Color TierColor(ChestTier tier) => tier switch
	{
		ChestTier.Pequeno => RarityInfo.GetColor(ItemRarity.Comun),
		ChestTier.Grande  => RarityInfo.GetColor(ItemRarity.PocoComun),
		ChestTier.Jefe    => RarityInfo.GetColor(ItemRarity.Legendario),
		ChestTier.Equipo  => new Color(1f, 0.55f, 0.2f),
		ChestTier.Maldito => RarityInfo.GetColor(ItemRarity.Maldito),
		_ => Colors.White,
	};

	public static string TierName(ChestTier tier) => tier switch
	{
		ChestTier.Pequeno => "Cofre",
		ChestTier.Grande  => "Cofre grande",
		ChestTier.Jefe    => "Cofre de jefe",
		ChestTier.Equipo  => "Cofre de equipo",
		ChestTier.Maldito => "Cofre maldito",
		_ => "Cofre",
	};

	public override void _Ready()
	{
		base._Ready();
		ZIndex = 1;

		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (_sprite == null)
		{
			_sprite = new Sprite2D { Name = "Sprite2D" };
			AddChild(_sprite);
		}
		_sprite.TextureFilter = TextureFilterEnum.Nearest;

		if (ResourceLoader.Exists(AtlasPath))
		{
			var atlas = GD.Load<Texture2D>(AtlasPath);
			_closedTex = new AtlasTexture { Atlas = atlas, Region = ClosedRegion };
			_openTex   = new AtlasTexture { Atlas = atlas, Region = OpenRegion };
			_sprite.Texture = _closedTex;
		}
		// Tinte suave según el tipo
		_sprite.SelfModulate = Colors.White.Lerp(TierColor(Tier), 0.45f);
		if (Tier == ChestTier.Grande || Tier == ChestTier.Jefe)
			_sprite.Scale = new Vector2(1.3f, 1.3f);
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		if (!_opened) QueueRedraw();
		// El costo cambia con la dificultad: refrescar el texto si el jugador está cerca
		if (PlayerNearby != null && !_opened && !IsFree) RefreshPrompt();
	}

	public override void _Draw()
	{
		if (_opened) return;
		Color c = TierColor(Tier);
		float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 3f);
		c.A = 0.15f + 0.15f * pulse;
		DrawCircle(new Vector2(0f, 6f), 20f + 3f * pulse, c);
	}

	public override string GetPromptText()
	{
		int cost = CurrentCost;
		string name = TierName(Tier);
		return cost > 0 ? $"[E] {name} — {cost} monedas" : $"[E] Abrir {name.ToLower()}";
	}

	public override bool CanInteract(Player player) => !_opened;

	public override void Interact(Player player)
	{
		if (_opened || GameManager.Instance == null) return;
		int cost = CurrentCost;
		if (cost > 0 && !GameManager.Instance.TrySpendCoins(cost))
		{
			FloatingText.Spawn(GlobalPosition, "Monedas insuficientes", UiFactory.ErrorColor);
			return;
		}
		Open(player);
	}

	private void Open(Player player)
	{
		_opened = true;
		RefreshPrompt();
		QueueRedraw();
		if (_openTex != null) _sprite.Texture = _openTex;
		GameManager.Instance.Run.ChestsOpened++;

		float luck = player != null ? player.Stats.Luck : 0f;
		ItemDefinition item = LootTable.RollChest(Tier, luck);
		if (item != null)
		{
			Vector2 land = GlobalPosition + new Vector2((float)GD.RandRange(-24.0, 24.0), 28f);
			ItemPickup.Spawn(item, GlobalPosition, land);
			FxBurst.Spawn(GlobalPosition, 40f, item.Color, 0.4f);
		}
		else
		{
			// No hay nada desbloqueado para este cofre: se devuelven monedas
			int refund = Mathf.Max(5, CurrentCostForRefund());
			GameManager.Instance.AddCoinsRaw(refund);
			FloatingText.Spawn(GlobalPosition, $"Vacío: +{refund} monedas", UiFactory.CoinColor);
		}

		// Desaparece poco después de abrirse
		var tween = CreateTween();
		tween.TweenInterval(2.5);
		tween.TweenProperty(this, "modulate:a", 0f, 0.6f);
		tween.TweenCallback(Callable.From(QueueFree));
	}

	private int CurrentCostForRefund() => IsFree ? 10 : LootTable.ChestCost(Tier, BaseCost);
}
