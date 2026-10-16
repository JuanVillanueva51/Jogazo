using Godot;
using System.Collections.Generic;

/// <summary>Contadores de la partida actual (para recompensas y estadísticas).</summary>
public sealed class RunStats
{
	public bool Active;
	public bool Finalized;
	public int Kills;
	public int EliteKills;
	public int BossKills;
	public int StagesCleared;
	public int CreditsEarned;
	public int ChestsOpened;
	public int ItemsBought;
}

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public int   CurrentLevel          { get; set; } = 1;
	public float CurrentXp             { get; set; } = 0f;
	public float XpToNextLevel         { get; set; } = Balance.FirstLevelXp;
	/// <summary>Tiempo en el mapa actual (decide cuándo aparece el jefe).</summary>
	public float ElapsedTime           { get; set; } = 0f;
	/// <summary>Tiempo total de la partida (decide la dificultad).</summary>
	public float TotalRunTime          { get; private set; } = 0f;
	public float WinTime               { get; }       = 420f;
	public int   Coins                 { get; set; } = 0;

	public string      SelectedCharacterScene { get; set; } = "res://scenes/player/cimarron_char.tscn";
	public int         SelectedCharacterIndex { get; set; } = 0;
	public int         SelectedClassIndex     { get; set; } = 0;
	public PassiveType SelectedPassive        { get; set; } = PassiveType.None;
	/// <summary>Modificador de dificultad elegido por el jugador (torres). Acelera el escalado por tiempo.</summary>
	public float diffModifier = 1f;
	public float eliteProbability = 0.03f;
	public int currentMapLevel {get; set; } = 0;
	public bool isBossSpawned {get; set; } = false;
	public bool isBossDefeated {get; set;} = false;
	//estado entre mapas
	public List<WeaponData> SavedWeapons           { get; set; } = null;
	public float            SavedCurrentHp         { get; set; } = -1f;
	public PlayerStats      SavedStats             { get; set; } = null;
	public float            SavedPassiveDamageBonus { get; set; } = 1f;
	public InventorySnapshot SavedInventory        { get; set; } = null;
	public bool             HasSavedState          => SavedWeapons != null;

	public RunStats Run { get; private set; } = new();
	/// <summary>True mientras una ventana modal (tienda) está abierta.</summary>
	public bool IsModalOpen { get; set; }

	private float _coinRemainder;
	private float _creditMinuteTimer;

	// ───────────────────────── Dificultad ─────────────────────────
	public float DifficultyCoefficient => Balance.Coefficient(TotalRunTime, Mathf.Max(0, currentMapLevel - 1), diffModifier);
	public float EnemyLevel => Balance.EnemyLevel(DifficultyCoefficient);
	public float CostScale => Balance.CostScale(DifficultyCoefficient);
	public float CurrentEliteChance => Balance.EliteChance(eliteProbability, TotalRunTime);

	public override void _EnterTree() => Instance = this;

	public override void _Ready()
	{
		CombatEvents.EnemyKilled += OnEnemyKilled;
	}

	public override void _ExitTree()
	{
		CombatEvents.EnemyKilled -= OnEnemyKilled;
		if (Instance == this) Instance = null;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		if (currentMapLevel > 0 && !isBossSpawned)
		{
			ElapsedTime += dt;
		}
		if (currentMapLevel > 0 && Run.Active)
		{
			TotalRunTime += dt;
			_creditMinuteTimer += dt;
			if (_creditMinuteTimer >= 60f)
			{
				_creditMinuteTimer -= 60f;
				AwardCredits(Balance.CreditsPerMinute);
			}
		}
	}

	public void Transition(int nextMapLevel){
		if (currentMapLevel > 0)
		{
			Run.StagesCleared++;
			AwardCredits(Balance.CreditsPerStageCleared);
		}
		currentMapLevel = nextMapLevel;
		ElapsedTime = 0f;
		isBossSpawned = false;
		isBossDefeated = false;
		// La subida de dificultad entre mapas ahora la maneja Balance.StageFactor
	}

	public void SavePlayerState(Player player){ //guardar estado del jugador
		var weaponManager = player.GetNodeOrNull<WeaponManager>("WeaponManager");
		if (weaponManager != null){
			SavedWeapons = weaponManager.GetActiveWeapons();
		}

		SavedCurrentHp = player.CurrentHp;
		SavedStats = player.BaseStats.Clone();
		SavedPassiveDamageBonus = player.PassiveDamageBonus;
		SavedInventory = player.Inventory?.CreateSnapshot();
	}

	// ───────────────────────── Experiencia y monedas ─────────────────────────

	public void AddXp(float amount)
	{
		var player = GetTree().GetFirstNodeInGroup("player") as Player;
		float mult = player != null ? Mathf.Max(0f, player.Stats.XpMultiplier) : 1f;
		CurrentXp += amount * mult;
		if (CurrentXp >= XpToNextLevel)
		{
			CurrentXp     -= XpToNextLevel;
			CurrentLevel++;
			XpToNextLevel = Balance.NextXpRequirement(XpToNextLevel);

			EventManager.Instance.EmitLevelUp(CurrentLevel);
		}
		EventManager.Instance.EmitXpChanged(CurrentXp,XpToNextLevel);
	}

	/// <summary>Monedas recogidas del suelo (aplica el multiplicador de oro del jugador).</summary>
	public void AddCoins(int amount)
	{
		var player = GetTree().GetFirstNodeInGroup("player") as Player;
		float mult = player != null ? Mathf.Max(0f, player.Stats.GoldMultiplier) : 1f;
		float total = amount * mult + _coinRemainder;
		int whole = Mathf.FloorToInt(total);
		_coinRemainder = total - whole;
		if (whole <= 0) return;
		Coins += whole;
		EventManager.Instance.EmitCoinChanged(whole);
	}

	/// <summary>Monedas exactas (sin multiplicadores).</summary>
	public void AddCoinsRaw(int amount)
	{
		if (amount <= 0) return;
		Coins += amount;
		EventManager.Instance.EmitCoinChanged(amount);
	}

	public bool TrySpendCoins(int amount)
	{
		if (amount <= 0) return true;
		if (Coins < amount) return false;
		Coins -= amount;
		EventManager.Instance.EmitCoinChanged(-amount);
		return true;
	}

	// ───────────────────────── Créditos (moneda global) ─────────────────────────

	public void AwardCredits(int amount)
	{
		if (amount <= 0) return;
		Run.CreditsEarned += amount;
		ProfileManager.Instance?.AddCredits(amount);
	}

	private void OnEnemyKilled(KillInfo kill)
	{
		if (!Run.Active) return;
		Run.Kills++;
		if (kill.IsElite)
		{
			Run.EliteKills++;
			AwardCredits(Balance.CreditsPerEliteKill);
		}
		if (kill.IsBoss)
		{
			Run.BossKills++;
			AwardCredits(Balance.CreditsPerBossKill);
		}
		LootDropper.HandleKill(kill);
	}

	// ───────────────────────── Ciclo de la partida ─────────────────────────

	/// <summary>Empieza una partida nueva (llamar al confirmar clase).</summary>
	public void StartNewRun()
	{
		ResetRun();
		Run = new RunStats { Active = true };
	}

	/// <summary>
	/// Termina la partida: da el bono de victoria, guarda estadísticas y el perfil.
	/// Es seguro llamarlo varias veces (solo cuenta la primera).
	/// </summary>
	public RunSummary EndRun(bool won)
	{
		if (!Run.Active || Run.Finalized) return null;
		if (won) AwardCredits(Balance.CreditsVictoryBonus);
		Run.Finalized = true;
		Run.Active = false;

		var summary = new RunSummary
		{
			Won = won,
			Seconds = TotalRunTime,
			Kills = Run.Kills,
			EliteKills = Run.EliteKills,
			BossKills = Run.BossKills,
			StagesCleared = Run.StagesCleared,
			CreditsEarned = Run.CreditsEarned,
			LevelReached = CurrentLevel,
			ChestsOpened = Run.ChestsOpened,
			ItemsBought = Run.ItemsBought,
		};
		ProfileManager.Instance?.RecordRun(summary);
		return summary;
	}

	public void ResetRun()
	{
		CurrentLevel = 1;
		CurrentXp = 0f;
		Coins = 0;
		XpToNextLevel = Balance.FirstLevelXp;
		ElapsedTime   = 0f;
		TotalRunTime  = 0f;
		isBossSpawned = false;
		isBossDefeated = false;
		currentMapLevel = 0;
		diffModifier = 1f;
		eliteProbability = 0.03f;
		IsModalOpen = false;
		_coinRemainder = 0f;
		_creditMinuteTimer = 0f;
		//limpiar estado al reiniciar
		SavedWeapons = null;
		SavedCurrentHp = -1f;
		SavedStats = null;
		SavedPassiveDamageBonus = 1f;
		SavedInventory = null;
		Run = new RunStats();
	}
}
