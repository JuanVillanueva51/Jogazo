using Godot;
using System;
using System.Collections.Generic;

public partial class PauseScreen : CanvasLayer
{
	private BaseButton resumeButton;
	private BaseButton configButton;
	private BaseButton exitButton;
	private TextureRect[] weaponIconSlots;
	private bool isPaused = false;
	private Label statsLabel;
	private Label itemsLabel;

	public override void _Ready()
	{
		Visible = false;
		resumeButton = GetNode<BaseButton>("MainPanel/ReanudarButton");
		configButton = GetNode<BaseButton>("MainPanel/ConfigButton");
		exitButton = GetNode<BaseButton>("MainPanel/ExitButton");
		resumeButton.Pressed += Resume;
		configButton.Pressed += Config;
		exitButton.Pressed += Exit;
		// Cada botón con su texto (antes los tres usaban la imagen de "Reanudar")
		UiFactory.StyleMenuButton(resumeButton as Button, 22);
		UiFactory.StyleMenuButton(configButton as Button, 22);
		UiFactory.StyleMenuButton(exitButton as Button, 22);
		weaponIconSlots = new TextureRect[]
		{
			GetNode<TextureRect>("MainPanel/InventoryVBox/InventoryPanel/WeaponsHBox/FirstWeapon"),
			GetNode<TextureRect>("MainPanel/InventoryVBox/InventoryPanel/WeaponsHBox/SecondWeapon"),
			GetNode<TextureRect>("MainPanel/InventoryVBox/InventoryPanel/WeaponsHBox/ThirdWeapon"),
			GetNode<TextureRect>("MainPanel/InventoryVBox/InventoryPanel/WeaponsHBox/FourthWeapon")
		};
		statsLabel = GetNode<Label>("MainPanel/VBoxContainer/StatsPanel/StatsText");
		statsLabel.AddThemeFontSizeOverride("font_size", 14);
		foreach (var slot in weaponIconSlots)
		{
			// Los íconos de hoja de sprites son de 16x16: escalarlos sin suavizado
			slot.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
			slot.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			slot.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			slot.CustomMinimumSize = new Vector2(50f, 50f);
		}

		// Lista de objetos debajo de las armas (creada desde código)
		var inventoryPanel = GetNodeOrNull<Control>("MainPanel/InventoryVBox/InventoryPanel");
		if (inventoryPanel != null)
		{
			itemsLabel = UiFactory.MakeLabel("", 12, Colors.White);
			itemsLabel.Position = new Vector2(4f, 54f);
			itemsLabel.Size = new Vector2(328f, 140f);
			itemsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			itemsLabel.ClipText = true;
			inventoryPanel.AddChild(itemsLabel);
		}
	}

	public override void _Process(double delta)
	{
		// Mientras la configuración está abierta, Escape la cierra a ella
		if (SettingsScreen.IsOpen || SettingsScreen.LastClosedFrame == Engine.GetProcessFrames())
			return;
		if (Input.IsActionJustPressed("pause"))
		{
			togglePaused();
		}
	}

	private void togglePaused()
	{
		// Si la tienda está abierta, Escape la cierra a ella (no abre la pausa)
		if (!isPaused && GameManager.Instance != null && GameManager.Instance.IsModalOpen)
			return;
		if(isPaused)
			Resume();
		else
			PauseGame();
	}

	private void PauseGame()
	{
		isPaused = true;
		Visible = true;
		GetTree().Paused = true;
		UpdateInventoryUI();
		EventManager.Instance.EmitGamePaused();
		GD.Print("Juego pausado");
	}

	private void Resume()
	{
		isPaused = false;
		Visible = false;
		GetTree().Paused = false;
		GD.Print("Juego reanudado");
	}

	private void Config()
	{
		if (SettingsScreen.IsOpen) return;
		SettingsScreen.Open(this);
	}

	private void Exit()
	{
		GetTree().Paused = false;
		// Abandonar también guarda los créditos y estadísticas de la partida
		GameManager.Instance.EndRun(false);
		GameManager.Instance.ResetRun();
		GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
	}
	private void UpdateInventoryUI(){
		Player player = GetTree().GetFirstNodeInGroup("player") as Player;
		if(player == null) return;
		UpdateStats(player);
		UpdateWeaponsIcons(player);
		UpdateItems(player);
	}
	private void UpdateStats(Player player){
		if(statsLabel == null) return;
		PlayerStats stats = player.Stats;
		string text = "";
		text += $"Vida: {Mathf.RoundToInt(player.CurrentHp)}/{Mathf.RoundToInt(player.MaxHp)}\n";
		text += $"Regen: {stats.HpRegen:0.0}/s\n";
		text += $"Armadura: {stats.Armor:0}\n";
		text += $"Daño: x{stats.DamageMultiplier * player.PassiveDamageBonus:F2}\n";
		text += $"Crítico: {Mathf.RoundToInt(stats.CritChance * 100)}% (x{stats.CritDamage:0.0})\n";
		text += $"Daño Fisico: +{Mathf.RoundToInt(stats.MeleeDamageBonus * 100)}%\n";
		text += $"Daño Magico: +{Mathf.RoundToInt(stats.MagicDamageBonus * 100)}%\n";
		text += $"Vel. Ataque: x{stats.AttackSpeedMultiplier:F2}\n";
		text += $"Vel. Mov: x{stats.MovementSpeedMult:F2}\n";
		text += $"Proyectiles: +{stats.BonusProjectiles}\n";
		text += $"Rebotes: +{stats.BonusBounce}  Perforación: +{stats.BonusPierce}\n";
		text += $"Robo de vida: {Mathf.RoundToInt(stats.LifeSteal * 100)}%\n";
		text += $"Suerte: {stats.Luck}\n";
		if (player.Character != null) text += $"\nPersonaje: {player.Character.Name} ({player.Character.Archetype})";
		if (player.ClassInfo != null) text += $"\nClase: {player.ClassInfo.Name} ({player.ClassInfo.Archetype})";
		if (player.DashInfo != null) text += $"\nDash: {player.DashInfo.Name} ({player.DashMaxCharges} cargas)";
		statsLabel.Text = text;
	}

	private void UpdateItems(Player player){
		if (itemsLabel == null || player.Inventory == null) return;
		var parts = new List<string>();
		foreach (var stack in player.Inventory.GetPassiveItems())
			parts.Add(stack.Count > 1 ? $"{stack.Item.Name} x{stack.Count}" : stack.Item.Name);
		string text = parts.Count > 0 ? "Objetos: " + string.Join(", ", parts) : "Objetos: ninguno";
		if (player.Inventory.ActiveItem != null)
			text += $"\nActivo [Q]: {player.Inventory.ActiveItem.Name}";
		itemsLabel.Text = text;
	}
	private void UpdateWeaponsIcons(Player player){
		foreach (var slot in weaponIconSlots){
			slot.Texture = null;
		}
		WeaponManager wm = player.GetNode<WeaponManager>("WeaponManager");
		if(wm == null) return;
		List<WeaponData> activeWeapons = wm.GetActiveWeapons();
		for (int i = 0; i < activeWeapons.Count; i++)
		{
			if (i >= weaponIconSlots.Length) break; 
			weaponIconSlots[i].Texture = WeaponLibrary.GetIcon(activeWeapons[i]);
			weaponIconSlots[i].TooltipText = activeWeapons[i].Name;
		}
	}
}
