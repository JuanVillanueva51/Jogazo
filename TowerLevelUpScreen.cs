using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class StatsUpgradeOptions
{
	public string Name;
	public string Description;
	public System.Action<PlayerStats> Apply;
}

public partial class TowerLevelUpScreen : CanvasLayer
{
	private Player player;
	
	public override void _Ready(){
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;
	}
	public void OpenStatsScreen(){
		if(player == null)
			player = GetTree().GetFirstNodeInGroup("player") as Player;
		if(player == null) return;
		
		Visible = true;
		GetTree().Paused = true;
		ShowStatUpgrades();
	}
	private void ShowStatUpgrades(){
		// Las torres dan mejoras permanentes de la partida: se aplican a BaseStats
		PlayerStats currentStats = player.BaseStats;
		List<StatsUpgradeOptions> options = CreateUpgradeOptions(currentStats);
		options = options.OrderBy(_ => GD.Randf()).Take(3).ToList();
		Button[] cards = GetCards();
		for (int i = 0; i < cards.Length; i++)
		{
			DisconnectCard(cards[i]);
			if (i < options.Count)
			{
				var upgrade = options[i];
				cards[i].Visible = true;
				cards[i].Text = $"{upgrade.Name}\n{upgrade.Description}";
				cards[i].Pressed += () => 
				{ 
					upgrade.Apply(currentStats); 
					player.RecalculateStats();
					Close(); 
				};
			}
			else
			{
				cards[i].Visible = false;
			}
		}
	}
	private List<StatsUpgradeOptions> CreateUpgradeOptions(PlayerStats stats)
	{
		var options = new List<StatsUpgradeOptions>();
		float GetRandomPercent() => (float)GD.RandRange(0.10,0.30);
		float hpPercent = GetRandomPercent();
		options.Add(new StatsUpgradeOptions
		{
			Name = "Vida Extra",
			Description = $"+{Mathf.RoundToInt(hpPercent * 100)}% Vida extra",
			Apply = s => s.MaxHpMultiplier *= (1f + hpPercent)
		});
		options.Add(new StatsUpgradeOptions
		{
			Name = "Dificultad",
			Description = $"30% de dificultad añadida",
			Apply = s => GameManager.Instance.diffModifier *= 1.30f
		});
		options.Add(new StatsUpgradeOptions
		{
			Name = "Probabilidad de spawn de elites",
			Description = $"10% de Elite Spawn",
			Apply = s => GameManager.Instance.eliteProbability *= 1.10f
		});
		float regenValue = stats.HpRegen <= 0f ? (float)GD.RandRange(1.0, 3.0) : stats.HpRegen * GetRandomPercent();
		options.Add(new StatsUpgradeOptions
		{
			Name = "Regeneracion de HP",
			Description = $"+{regenValue:F1} Rgeneracion de vida",
			Apply = s => s.HpRegen += regenValue
		});
		float lsValue = (float)GD.RandRange(0.01, 0.05);
		options.Add(new StatsUpgradeOptions
		{
			Name = "Robo de vida",
			Description = $"+{Mathf.RoundToInt(lsValue * 100)}% Robo de vida",
			Apply = s => s.LifeSteal += lsValue
		});
		float msPercent = GetRandomPercent();
		options.Add(new StatsUpgradeOptions
		{
			Name = "Velocidad de movimiento",
			Description = $"+{Mathf.RoundToInt(msPercent * 100)}% Velocidad de movimiento",
			Apply = s => s.MovementSpeedMult *= (1f + msPercent)
		});
		float asPercent = GetRandomPercent();
		options.Add(new StatsUpgradeOptions
		{
			Name = "Velocidad de ataque",
			Description = $"+{Mathf.RoundToInt(asPercent * 100)}% Velocidad de ataque",
			Apply = s => s.AttackSpeedMultiplier *= (1f + asPercent)
		});
		float sizePercent = GetRandomPercent();
		options.Add(new StatsUpgradeOptions
		{
			Name = "Tamaño de projectiles",
			Description = $"+{Mathf.RoundToInt(sizePercent * 100)}% Tamaño",
			Apply = s => s.SizeMultiplier *= (1f + sizePercent)
		});
		float psPercent = GetRandomPercent();
		options.Add(new StatsUpgradeOptions
		{
			Name = "Velocidad de projectil",
			Description = $"+{Mathf.RoundToInt(psPercent * 100)}% Velocidad de projectil",
			Apply = s => s.ProjectileSpeedMult *= (1f + psPercent)
		});
		int projValue = GD.RandRange(1, 2);
		options.Add(new StatsUpgradeOptions
		{
			Name = "Cantidad de pojectiles",
			Description = $"+{projValue} Projectiles extra",
			Apply = s => s.BonusProjectiles += projValue
		});
		options.Add(new StatsUpgradeOptions
		{
			Name = "Rebote extra",
			Description = "+1 Rebote",
			Apply = s => s.BonusBounce += 1
		});
		options.Add(new StatsUpgradeOptions
		{
			Name = "Penetracion",
			Description = "+1 Perforacion",
			Apply = s => s.BonusPierce += 1
		});
		return options;
	}
	private Button[] GetCards() => new Button[]
	{
		GetNode<Button>("Panel/VBox/UpgradeCard1"),
		GetNode<Button>("Panel/VBox/UpgradeCard2"),
		GetNode<Button>("Panel/VBox/UpgradeCard3"),
	};
	private void DisconnectCard(Button card)
	{
		foreach (var conn in card.GetSignalConnectionList("pressed"))
			card.Disconnect("pressed", (Callable)conn["callable"]);
	}
	private void Close() 
	{ 
		Visible = false; 
		GetTree().Paused = false; 
	}
}
