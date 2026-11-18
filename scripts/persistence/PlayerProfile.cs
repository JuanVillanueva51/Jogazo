using System;
using System.Collections.Generic;

/// <summary>
/// Datos persistentes del jugador (fuera de la partida).
/// Es un objeto plano para que se pueda guardar igual en un archivo JSON
/// o en una base de datos (cada propiedad = una columna / campo).
/// </summary>
public sealed class PlayerProfile
{
	public const int CurrentSchemaVersion = 1;

	public int SchemaVersion { get; set; } = CurrentSchemaVersion;
	public string PlayerId { get; set; } = Guid.NewGuid().ToString();
	public string PlayerName { get; set; } = "Jugador";

	/// <summary>Moneda global: solo se gasta en el menú principal.</summary>
	public int Credits { get; set; }
	public int LifetimeCredits { get; set; }

	public List<string> UnlockedItems { get; set; } = new();
	public List<string> UnlockedWeapons { get; set; } = new();

	// Estadísticas generales
	public int RunsPlayed { get; set; }
	public int RunsWon { get; set; }
	public int TotalKills { get; set; }
	public int TotalBossKills { get; set; }
	public float BestRunSeconds { get; set; }

	public string CreatedAtUtc { get; set; } = DateTime.UtcNow.ToString("o");
	public string UpdatedAtUtc { get; set; } = DateTime.UtcNow.ToString("o");
}

/// <summary>Resumen de una partida terminada (para estadísticas y futuras tablas de BD).</summary>
public sealed class RunSummary
{
	public bool Won;
	public float Seconds;
	public int Kills;
	public int EliteKills;
	public int BossKills;
	public int StagesCleared;
	public int CreditsEarned;
	public int LevelReached;
	public int ChestsOpened;
	public int ItemsBought;
}
