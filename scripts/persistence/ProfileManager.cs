using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Autoload que mantiene el perfil persistente del jugador: moneda global (Créditos),
/// objetos y armas desbloqueados y estadísticas. Guarda automáticamente.
/// El almacenamiento real se delega a un IPlayerDataRepository (hoy: archivos JSON).
/// </summary>
public partial class ProfileManager : Node
{
	public static ProfileManager Instance { get; private set; }

	/// <summary>(total, cambio)</summary>
	public event Action<int, int> CreditsChanged;
	public event Action UnlocksChanged;

	public PlayerProfile Profile { get; private set; }
	public IPlayerDataRepository Repository { get; private set; }

	public int Credits => Profile?.Credits ?? 0;
	public string SaveLocation => Repository?.Description ?? "";

	private bool _dirty;
	private float _autosaveTimer;
	private const float AutosaveInterval = 10f;

	private HashSet<string> _unlockedItems = new();
	private HashSet<string> _unlockedWeapons = new();

	public override void _EnterTree()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		Repository = CreateRepository();
		LoadProfile();
	}

	public override void _ExitTree()
	{
		SaveNow();
		if (Instance == this) Instance = null;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
			SaveNow();
	}

	public override void _Process(double delta)
	{
		if (!_dirty) return;
		_autosaveTimer += (float)delta;
		if (_autosaveTimer >= AutosaveInterval) SaveNow();
	}

	/// <summary>
	/// Punto único para cambiar de almacenamiento. Para usar una base de datos,
	/// devuelve aquí tu propia implementación de IPlayerDataRepository.
	/// </summary>
	private static IPlayerDataRepository CreateRepository()
	{
		return new JsonFilePlayerDataRepository("user://save");
	}

	private void LoadProfile()
	{
		Profile = Repository.Load();
		bool isNew = Profile == null;
		Profile ??= new PlayerProfile();
		Profile.UnlockedItems ??= new List<string>();
		Profile.UnlockedWeapons ??= new List<string>();

		_unlockedItems = new HashSet<string>(Profile.UnlockedItems);
		_unlockedWeapons = new HashSet<string>(Profile.UnlockedWeapons);

		// Siempre asegurar los desbloqueos iniciales (también si se agregan objetos nuevos al juego)
		bool changed = false;
		foreach (var def in ItemLibrary.All)
			if (def.UnlockedByDefault && _unlockedItems.Add(def.Id)) changed = true;
		foreach (var weapon in WeaponLibrary.BuildAll())
			if (weapon.UnlockCost <= 0 && _unlockedWeapons.Add(weapon.Name)) changed = true;

		if (Profile.SchemaVersion < PlayerProfile.CurrentSchemaVersion)
		{
			Profile.SchemaVersion = PlayerProfile.CurrentSchemaVersion;
			changed = true;
		}

		SyncLists();
		if (isNew || changed) SaveNow();
		GD.Print($"Perfil cargado desde {SaveLocation} — {Profile.Credits} {Balance.MetaCurrencyName}");
	}

	private void SyncLists()
	{
		Profile.UnlockedItems = new List<string>(_unlockedItems);
		Profile.UnlockedItems.Sort(StringComparer.Ordinal);
		Profile.UnlockedWeapons = new List<string>(_unlockedWeapons);
		Profile.UnlockedWeapons.Sort(StringComparer.Ordinal);
	}

	public void MarkDirty()
	{
		_dirty = true;
	}

	public void SaveNow()
	{
		if (Profile == null || Repository == null) return;
		SyncLists();
		if (Repository.Save(Profile))
		{
			_dirty = false;
			_autosaveTimer = 0f;
		}
	}

	// ───────────────────────── Moneda global ─────────────────────────

	public void AddCredits(int amount)
	{
		if (amount <= 0 || Profile == null) return;
		Profile.Credits += amount;
		Profile.LifetimeCredits += amount;
		MarkDirty();
		CreditsChanged?.Invoke(Profile.Credits, amount);
	}

	public bool TrySpendCredits(int amount)
	{
		if (Profile == null || amount < 0 || Profile.Credits < amount) return false;
		Profile.Credits -= amount;
		CreditsChanged?.Invoke(Profile.Credits, -amount);
		SaveNow();
		return true;
	}

	// ───────────────────────── Desbloqueos ─────────────────────────

	public bool IsItemUnlocked(string itemId) => itemId != null && _unlockedItems.Contains(itemId);

	public bool IsWeaponUnlocked(string weaponName) => weaponName != null && _unlockedWeapons.Contains(weaponName);

	public bool TryUnlockItem(ItemDefinition item)
	{
		if (item == null || IsItemUnlocked(item.Id)) return false;
		if (!TrySpendCredits(item.UnlockCost)) return false;
		_unlockedItems.Add(item.Id);
		SaveNow();
		UnlocksChanged?.Invoke();
		return true;
	}

	public bool TryUnlockWeapon(WeaponData weapon)
	{
		if (weapon == null || IsWeaponUnlocked(weapon.Name)) return false;
		if (!TrySpendCredits(weapon.UnlockCost)) return false;
		_unlockedWeapons.Add(weapon.Name);
		SaveNow();
		UnlocksChanged?.Invoke();
		return true;
	}

	public int UnlockedItemCount => _unlockedItems.Count;
	public int UnlockedWeaponCount => _unlockedWeapons.Count;

	// ───────────────────────── Partidas ─────────────────────────

	public void RecordRun(RunSummary run)
	{
		if (run == null || Profile == null) return;
		Profile.RunsPlayed++;
		if (run.Won) Profile.RunsWon++;
		Profile.TotalKills += run.Kills;
		Profile.TotalBossKills += run.BossKills;
		if (run.Seconds > Profile.BestRunSeconds) Profile.BestRunSeconds = run.Seconds;
		Repository.AppendRunHistory(Profile.PlayerId, run);
		SaveNow();
	}
}
