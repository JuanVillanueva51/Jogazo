using System;
using System.IO;
using System.Text.Json;
using Godot;

/// <summary>
/// Implementación con archivos JSON en la carpeta de datos del usuario (user://).
/// En Windows: %APPDATA%\Godot\app_userdata\uabc-roguelike\save\
///  - profile.json      → perfil (créditos, desbloqueos, estadísticas)
///  - profile.json.bak  → copia del guardado anterior
///  - runs.jsonl        → historial de partidas (una línea JSON por partida)
/// La escritura es atómica (archivo temporal + reemplazo) para no corromper el guardado
/// si el juego se cierra a mitad.
/// </summary>
public sealed class JsonFilePlayerDataRepository : IPlayerDataRepository
{
	private readonly string _directory;
	private readonly string _profilePath;
	private readonly string _historyPath;

	private static readonly JsonSerializerOptions Options = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
	};

	private static readonly JsonSerializerOptions LineOptions = new()
	{
		WriteIndented = false,
		IncludeFields = true,
	};

	public JsonFilePlayerDataRepository(string userDirectory = "user://save")
	{
		_directory = ProjectSettings.GlobalizePath(userDirectory);
		_profilePath = Path.Combine(_directory, "profile.json");
		_historyPath = Path.Combine(_directory, "runs.jsonl");
	}

	public string Description => _profilePath;

	public PlayerProfile Load()
	{
		try
		{
			if (!File.Exists(_profilePath))
				return TryLoadBackup();
			string json = File.ReadAllText(_profilePath);
			var profile = JsonSerializer.Deserialize<PlayerProfile>(json, Options);
			return profile ?? TryLoadBackup();
		}
		catch (Exception e)
		{
			GD.PushWarning($"Perfil dañado ({e.Message}). Se intenta usar la copia de respaldo.");
			return TryLoadBackup();
		}
	}

	private PlayerProfile TryLoadBackup()
	{
		string backup = _profilePath + ".bak";
		try
		{
			if (!File.Exists(backup)) return null;
			return JsonSerializer.Deserialize<PlayerProfile>(File.ReadAllText(backup), Options);
		}
		catch (Exception e)
		{
			GD.PushWarning($"No se pudo leer la copia de respaldo: {e.Message}");
			return null;
		}
	}

	public bool Save(PlayerProfile profile)
	{
		if (profile == null) return false;
		try
		{
			Directory.CreateDirectory(_directory);
			profile.UpdatedAtUtc = DateTime.UtcNow.ToString("o");
			string json = JsonSerializer.Serialize(profile, Options);
			string temp = _profilePath + ".tmp";
			File.WriteAllText(temp, json);
			if (File.Exists(_profilePath))
				File.Copy(_profilePath, _profilePath + ".bak", true);
			File.Move(temp, _profilePath, true);
			return true;
		}
		catch (Exception e)
		{
			GD.PushError($"No se pudo guardar el perfil en {_profilePath}: {e.Message}");
			return false;
		}
	}

	public void AppendRunHistory(string playerId, RunSummary run)
	{
		if (run == null) return;
		try
		{
			Directory.CreateDirectory(_directory);
			var entry = new
			{
				playerId,
				dateUtc = DateTime.UtcNow.ToString("o"),
				run,
			};
			File.AppendAllText(_historyPath, JsonSerializer.Serialize(entry, LineOptions) + "\n");
		}
		catch (Exception e)
		{
			GD.PushWarning($"No se pudo guardar el historial de partidas: {e.Message}");
		}
	}
}
