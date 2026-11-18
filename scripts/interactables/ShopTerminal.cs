using System.Collections.Generic;
using Godot;

/// <summary>Una oferta de la tienda.</summary>
public sealed class ShopOffer
{
	public ItemDefinition Item;
	public int Price;
	public bool Sold;
}

/// <summary>
/// Tienda colocada en el mapa (scenes/interactables/shop_terminal.tscn).
/// Vende objetos a cambio de las monedas recogidas en la partida.
/// Las ofertas se generan la primera vez que se abre y se mantienen (se pueden re-tirar pagando).
/// </summary>
public partial class ShopTerminal : Interactable
{
	private const string SpritePath = "res://sprites/Shop.png";

	[Export] public int OfferCount { get; set; } = 3;

	public List<ShopOffer> Offers { get; } = new();
	public int RerollCount { get; private set; }

	private Sprite2D _sprite;

	public override void _Ready()
	{
		base._Ready();
		ZIndex = 1;
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (_sprite == null)
		{
			_sprite = new Sprite2D { Name = "Sprite2D" };
			AddChild(_sprite);
			if (ResourceLoader.Exists(SpritePath))
				_sprite.Texture = GD.Load<Texture2D>(SpritePath);
			_sprite.Scale = new Vector2(0.75f, 0.75f);
			_sprite.Position = new Vector2(0f, -12f);
		}
		_sprite.TextureFilter = TextureFilterEnum.Nearest;
	}

	public override string GetPromptText() => "[E] Tienda";

	public override bool CanInteract(Player player) => GameManager.Instance == null || !GameManager.Instance.IsModalOpen;

	public override void Interact(Player player)
	{
		if (Offers.Count == 0) GenerateOffers(player);
		ShopScreen.Open(this, player);
	}

	public int RerollCost
	{
		get
		{
			float scale = GameManager.Instance != null ? GameManager.Instance.CostScale : 1f;
			return Mathf.Max(1, Mathf.RoundToInt(Balance.ShopRerollBaseCost * scale * Mathf.Pow(Balance.ShopRerollGrowth, RerollCount)));
		}
	}

	public void GenerateOffers(Player player)
	{
		Offers.Clear();
		float luck = player != null ? player.Stats.Luck : 0f;
		var used = new HashSet<string>();
		for (int i = 0; i < OfferCount; i++)
		{
			ItemDefinition item = LootTable.RollShopItem(luck, used);
			if (item == null) continue;
			used.Add(item.Id);
			Offers.Add(new ShopOffer { Item = item, Price = LootTable.ShopPrice(item) });
		}
	}

	public bool TryReroll(Player player)
	{
		int cost = RerollCost;
		if (GameManager.Instance == null || !GameManager.Instance.TrySpendCoins(cost)) return false;
		RerollCount++;
		GenerateOffers(player);
		return true;
	}

	public bool TryBuy(ShopOffer offer, Player player)
	{
		if (offer == null || offer.Sold || player == null || GameManager.Instance == null) return false;
		if (!GameManager.Instance.TrySpendCoins(offer.Price)) return false;
		offer.Sold = true;
		GameManager.Instance.Run.ItemsBought++;
		player.Inventory.PickUp(offer.Item, player.GlobalPosition);
		return true;
	}
}
