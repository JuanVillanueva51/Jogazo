using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class LevelUpScreen : CanvasLayer
{
	private WeaponManager _weaponManager;
	private Player        _player;
	private List<WeaponData>    _weaponPool;
	private List<string>        _ownedWeapons = new();
	public int reroll = 5;
	public int skip = 5;
	private Button skipButton;
	private Button rerollButton;
	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;
		EventManager.Instance.LevelUp -= ShowLevelUpScreen;
		EventManager.Instance.LevelUp += ShowLevelUpScreen;
		//GameManager.Instance.LevelUp += ShowLevelUpScreen;
		skipButton = GetNode<Button>("Panel/VBox/HBoxContainer/SkipButton");
		skipButton.Pressed += Skip;
		rerollButton = GetNode<Button>("Panel/VBox/HBoxContainer/RerollButton");
		rerollButton.Pressed += Reroll;
		UpdateButtons();

		// Tarjetas más amplias para que quepan el ícono y la descripción del arma
		var panel = GetNodeOrNull<Control>("Panel");
		if (panel != null)
		{
			panel.OffsetLeft = -270f; panel.OffsetRight = 270f;
			panel.OffsetTop = -175f;  panel.OffsetBottom = 155f;
		}
		var vbox = GetNodeOrNull<Control>("Panel/VBox");
		if (vbox != null)
		{
			vbox.OffsetLeft = -255f; vbox.OffsetRight = 255f;
			vbox.OffsetTop = -160f;  vbox.OffsetBottom = 140f;
		}
		// El marco decorativo se ajusta al nuevo tamaño del panel
		var frame = GetNodeOrNull<Control>("TextureRect");
		if (frame != null)
		{
			Vector2 center = GetViewport().GetVisibleRect().Size * 0.5f;
			frame.Position = center + new Vector2(-284f, -194f);
			frame.Size = new Vector2(566f, 366f);
		}
		foreach (var card in GetCards())
		{
			card.AddThemeFontSizeOverride("font_size", 14);
			card.CustomMinimumSize = new Vector2(0f, 64f);
		}
	}
	
private void Reroll()
{
	if(reroll <= 0) return;
	reroll--;
	var available = GetAvailableWeapons();
	bool canAddWeapon =
		available.Count >= 1 &&
		_weaponManager.WeaponCount < WeaponManager.MaxWeapons;
	bool showWeapons =
		canAddWeapon &&
		GD.RandRange(0, 1) == 0;
	if (showWeapons)
		ShowWeaponCards(available);
	else
		ShowWeaponUpgradeCards();
	UpdateButtons();
}
	public override void _ExitTree()
	{
		EventManager.Instance.LevelUp -= ShowLevelUpScreen;
	}
private void UpdateButtons()
{
	skipButton.Text = $"Skip ({skip})";
	rerollButton.Text = $"Reroll ({reroll})";
}
	private void Skip(){
		if(skip <= 0) return;
		skip--;
		UpdateButtons();
		Close();
	}
	private void ShowLevelUpScreen(int level)
	{
		if (_player       == null) _player       = GetTree().GetFirstNodeInGroup("player") as Player;
		if (_weaponManager == null) _weaponManager = _player?.GetNode<WeaponManager>("WeaponManager");
		_weaponPool = WeaponLibrary.BuildAll();
		Visible          = true;
		GetTree().Paused = true;
		var available = GetAvailableWeapons();
		
		bool canAddWeapon = available.Count >= 1 && _weaponManager.WeaponCount < WeaponManager.MaxWeapons;
		bool showWeapons  = canAddWeapon && GD.RandRange(0, 1) == 0;
		
		if (showWeapons)
			ShowWeaponCards(available);
		else
			ShowWeaponUpgradeCards(); //mejoras por arma específica
	}

	// ── Pool de armas nuevas ───────────────────────────────────────────────
	private void ShowWeaponCards(List<WeaponData> available)
	{
		// Orden aleatorio ponderado: las armas afines al personaje/clase salen un poco más seguido
		CharacterData character = _player?.Character;
		ClassData cls = _player?.ClassInfo;
		available = available
			.OrderByDescending(w => GD.Randf() * AffinityBonus.OfferWeight(character, cls, w))
			.ToList();
		Button[] cards = GetCards();
		for (int i = 0; i < cards.Length; i++)
		{
			DisconnectCard(cards[i]);
			if (i < available.Count)
			{
				var w = available[i];
				cards[i].Visible = true;
				cards[i].Text    = $"NUEVA: {w.Name} ({WeaponTags.TypeDisplayName(w.WeaponType)})\n{w.Description}";
				SetCardIcon(cards[i], WeaponLibrary.GetIcon(w));
				cards[i].Pressed += () => SelectWeapon(w);
			}
			else cards[i].Visible = false;
		}
	}

	private void SelectWeapon(WeaponData weapon)
	{
		_weaponManager.TryAddWeapon(weapon);
		_ownedWeapons.Add(weapon.Name);
		Close();
	}

	// ── Mejoras por arma específica ───────────────────────────────────────
	private void ShowWeaponUpgradeCards()
	{
		var activeWeapons = _weaponManager.GetActiveWeapons(); // devuelve List<WeaponData>
		Button[] cards    = GetCards();
		
		for (int i = 0; i < cards.Length; i++)
		{
			DisconnectCard(cards[i]);
			
			if (i < activeWeapons.Count)
			{
				// Elegir un upgrade aleatorio para esta arma
				var upgrades = WeaponLibrary.GetUpgradesForWeapon(activeWeapons[i]);
				// (antes se usaba Sort con un comparador aleatorio, que puede lanzar excepción en .NET)
				var upgrade = upgrades[GD.RandRange(0, upgrades.Count - 1)];
				
				var weaponRef = activeWeapons[i]; // capturar referencia
				
				cards[i].Visible = true;
				cards[i].Text    = $"{upgrade.Name}\n{upgrade.Description}";
				SetCardIcon(cards[i], WeaponLibrary.GetIcon(weaponRef));
				cards[i].Pressed += () => { upgrade.Apply(weaponRef); Close(); };
			}
			else cards[i].Visible = false;
		}
	}

	private List<WeaponData> GetAvailableWeapons()
	{
		var owned     = _weaponManager.GetActiveWeaponNames();
		var available = new List<WeaponData>();
		foreach (var w in _weaponPool)
			if (!owned.Contains(w.Name) && !_ownedWeapons.Contains(w.Name) && WeaponLibrary.IsUnlocked(w))
				available.Add(w);
		return available;
	}
	private Button[] GetCards() => new Button[]
	{
		GetNode<Button>("Panel/VBox/UpgradeCard1"),
		GetNode<Button>("Panel/VBox/UpgradeCard2"),
		GetNode<Button>("Panel/VBox/UpgradeCard3"),
	};
	/// <summary>Muestra el ícono del arma en la tarjeta (los íconos de 16 px se escalan sin suavizado).</summary>
	private static void SetCardIcon(Button card, Texture2D icon)
	{
		card.Icon = icon;
		card.ExpandIcon = true;
		card.IconAlignment = HorizontalAlignment.Left;
		card.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
		card.AddThemeConstantOverride("icon_max_width", 32);
		card.AutowrapMode = TextServer.AutowrapMode.WordSmart;
	}

	private void DisconnectCard(Button card)
	{
		card.Icon = null;
		foreach (var conn in card.GetSignalConnectionList("pressed"))
			card.Disconnect("pressed", (Callable)conn["callable"]);
	}
	private void Close() { Visible = false; GetTree().Paused = false; }
}
