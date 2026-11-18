using Godot;
using System.Collections.Generic;

public partial class EnemySpawner : Node
{
	[Export] public PackedScene[] RangedEnemies;
	[Export] public EnemySpawnData[] spawnList;
	[Export] public PackedScene bossScene;
	/// <summary>Distancia de aparición si no hay cámara (normalmente aparecen justo fuera de la pantalla).</summary>
	[Export] public float SpawnRadius       = 400f;
	[Export] public float BaseSpawnInterval = 2f; //Normalmente es 2f
	[Export] public float RangedSpawnChance = Balance.RangedSpawnChance;
	[Export] public float waveDuration = Balance.WaveDuration;
	[Export] public float waveCooldown = Balance.WaveCooldown;
	[Export] public float waveSpawnInterval = Balance.WaveSpawnInterval;
	/// <summary>Límite de enemigos vivos para cuidar el rendimiento.</summary>
	[Export] public int MaxAliveEnemies = 250;
	/// <summary>True mientras hay una oleada (el HUD lo muestra).</summary>
	public static bool WaveActive { get; private set; }
	public bool isWaveActive = false;
	public float waveTimer = 0f;
	private float  _spawnTimer      = 0f;
	private float  _currentInterval;
	private Player _player;

	public override void _Ready()
	{
		_player          = GetTree().GetFirstNodeInGroup("player") as Player;
		_currentInterval = BaseSpawnInterval;
		WaveActive = false;

		// Modo demo: más enemigos, oleadas más seguidas
		if (GameManager.Instance != null && GameManager.Instance.IsDemo)
		{
			waveCooldown = Balance.DemoWaveCooldown;
			waveDuration = Balance.DemoWaveDuration;
			MaxAliveEnemies = Mathf.Max(MaxAliveEnemies, Balance.DemoMaxAliveEnemies);
		}
	}

	public override void _ExitTree()
	{
		WaveActive = false;
	}

	public override void _Process(double delta)
	{
		if (_player == null)
		{
			_player = GetTree().GetFirstNodeInGroup("player") as Player;
			return;
		}
		var gm = GameManager.Instance;
		if (gm.ElapsedTime >= gm.WinTime && !gm.isBossSpawned){
			SpawnBoss();
		}
		_spawnTimer += (float)delta;
		waveTimer += (float)delta;

		if(!isWaveActive)
		{
			if(waveTimer >= waveCooldown)
			{
				isWaveActive = true;
				waveTimer = 0f;
				GD.Print("Iniciamo la oleada");
			}
		}
		else
		{
			if(waveTimer >= waveDuration)
			{
				isWaveActive = false;
				waveTimer = 0f;
				GD.Print("Acaba la oleada");
			}
		}

		WaveActive = isWaveActive;

		float minutesInMap = gm.ElapsedTime / 60f;
		float spawnRate = gm.Difficulty.SpawnRate * (gm.IsDemo ? Balance.DemoSpawnRate : 1f);
		// En el demo el tiempo "cuenta" más rápido para que los grupos crezcan antes
		float minutesForGroups = minutesInMap * (gm.IsDemo ? Balance.DemoTimeScaling : 1f);
		float normalInterval = Balance.SpawnInterval(BaseSpawnInterval, minutesForGroups, gm.diffModifier, spawnRate);
		_currentInterval = isWaveActive ? Mathf.Min(waveSpawnInterval, normalInterval) : normalInterval;

		if (_spawnTimer >= _currentInterval)
		{
			_spawnTimer = 0f;
			int alive = GetTree().GetNodesInGroup("enemies").Count;
			if (alive < MaxAliveEnemies)
			{
				// Grupos: con el tiempo cada aparición puede traer varios enemigos juntos
				int group = 1;
				float extra = Balance.ExtraSpawnChance(minutesForGroups) + (gm.IsDemo ? Balance.DemoExtraSpawnBonus : 0f);
				while (group < Balance.MaxGroupSize && GD.Randf() < extra) group++;
				group = Mathf.Min(group, MaxAliveEnemies - alive);

				Vector2 center = GetSpawnPosition();
				for (int i = 0; i < group; i++)
				{
					Vector2 offset = i == 0 ? Vector2.Zero
						: Vector2.FromAngle(GD.Randf() * Mathf.Tau) * (float)GD.RandRange(14.0, 34.0);
					SpawnEnemy(center + offset);
				}
			}
		}
	}

	private void SpawnEnemy(Vector2 spawnPosition)
	{
		var gm = GameManager.Instance;
		bool isElite = GD.Randf() < gm.CurrentEliteChance;
		bool spawnRanged = RangedEnemies != null
						   && RangedEnemies.Length > 0
						   && GD.Randf() < RangedSpawnChance;

		PackedScene chosenScene = null;
		if(spawnRanged){
			chosenScene = RangedEnemies[GD.RandRange(0, RangedEnemies.Length - 1)];
		}
		else
		{
			if (spawnList == null || spawnList.Length == 0) return;
			List<PackedScene> validEnemies = new List<PackedScene>();
			foreach (var data in spawnList)
			{
				if (data == null) continue;
				if (data.mapLevel == gm.currentMapLevel &&
					gm.ElapsedTime >= data.MinSpawnTimeSeconds)
				{
					validEnemies.Add(data.EnemyScene);
				}
			}
			if (validEnemies.Count == 0) return;
			chosenScene = validEnemies[GD.RandRange(0, validEnemies.Count - 1)];
		}
		if (chosenScene == null) return;
		float level = gm.EnemyLevel;

		if (spawnRanged)
			SpawnRanged(chosenScene, spawnPosition, level, isElite);
		else
			SpawnMelee(chosenScene, spawnPosition, level, isElite);
	}

	private void SpawnMelee(PackedScene scene, Vector2 position, float level, bool isElite)
	{
		var gm = GameManager.Instance;
		Enemy enemy  = scene.Instantiate<Enemy>();
		enemy.MaxHp   *= Balance.EnemyHpMultiplier(level) * gm.Difficulty.EnemyHp;
		enemy.Damage  *= Balance.EnemyDamageMultiplier(level) * gm.Difficulty.EnemyDamage;
		enemy.Speed   *= Balance.EnemySpeedMultiplier(level);
		enemy.XpValue *= Balance.EnemyXpMultiplier(level);
		enemy.CoinValue = Balance.EnemyCoinValue(level);
		if (isElite) enemy.MakeElite();
		_player.GetParent().AddChild(enemy);
		enemy.GlobalPosition = position;
	}

	private void SpawnRanged(PackedScene scene, Vector2 position, float level, bool isElite)
	{
		var gm = GameManager.Instance;
		RangedEnemy enemy = scene.Instantiate<RangedEnemy>();
		enemy.MaxHp   *= Balance.EnemyHpMultiplier(level) * gm.Difficulty.EnemyHp;
		enemy.Damage  *= Balance.EnemyDamageMultiplier(level) * gm.Difficulty.EnemyDamage;
		enemy.Speed   *= Balance.EnemySpeedMultiplier(level);
		enemy.XpValue *= Balance.EnemyXpMultiplier(level);
		enemy.CoinValue = Balance.EnemyCoinValue(level);
		if (isElite) enemy.MakeElite();
		_player.GetParent().AddChild(enemy);
		enemy.GlobalPosition = position;
	}

	private void SpawnBoss(){
		var gm = GameManager.Instance;
		gm.isBossSpawned = true;
		GD.Print("Boss Spawned");
		if (bossScene == null) return;
		BossEnemy boss = bossScene.Instantiate<BossEnemy>();
		float coefficient = gm.DifficultyCoefficient;
		float level = gm.EnemyLevel;
		boss.MaxHp   *= Balance.BossHpMultiplier(coefficient) * gm.Difficulty.EnemyHp;
		boss.Damage  *= Balance.EnemyDamageMultiplier(level) * gm.Difficulty.EnemyDamage;
		boss.XpValue *= Balance.BossXpMultiplier;
		boss.CoinValue = Balance.EnemyCoinValue(level) * Balance.BossCoinMultiplier;
		_player.GetParent().AddChild(boss);
		boss.GlobalPosition = GetSpawnPosition();
	}

	/// <summary>
	/// Punto de aparición justo fuera de lo que ve la cámara (así no aparecen de golpe en pantalla
	/// aunque la cámara esté pegada al borde del mapa). Prefiere puntos dentro de los límites del mapa;
	/// si no encuentra ninguno, aparecen fuera y entran atravesando los muros.
	/// </summary>
	private Vector2 GetSpawnPosition()
	{
		Vector2 from = _player.GlobalPosition;
		Camera2D camera = GetViewport()?.GetCamera2D();
		if (camera == null)
		{
			float angle = GD.Randf() * Mathf.Tau;
			return from + Vector2.FromAngle(angle) * SpawnRadius;
		}

		Vector2 viewSize = GetViewport().GetVisibleRect().Size / camera.Zoom;
		Rect2 view = new Rect2(camera.GetScreenCenterPosition() - viewSize * 0.5f, viewSize);
		Rect2? bounds = MapBounds.Current?.GlobalLimits;

		Vector2 candidate = from;
		for (int attempt = 0; attempt < 10; attempt++)
		{
			Vector2 dir = Vector2.FromAngle(GD.Randf() * Mathf.Tau);
			float distance = DistanceToRectEdge(view, from, dir) + Balance.OffscreenSpawnMargin;
			candidate = from + dir * distance;
			if (bounds == null || bounds.Value.Grow(24f).HasPoint(candidate))
				return candidate;
		}
		return candidate;
	}

	/// <summary>Distancia desde un punto (dentro del rectángulo) hasta su borde en una dirección.</summary>
	private static float DistanceToRectEdge(Rect2 rect, Vector2 from, Vector2 dir)
	{
		float tx = float.MaxValue, ty = float.MaxValue;
		if (dir.X > 0.0001f)       tx = (rect.End.X - from.X) / dir.X;
		else if (dir.X < -0.0001f) tx = (rect.Position.X - from.X) / dir.X;
		if (dir.Y > 0.0001f)       ty = (rect.End.Y - from.Y) / dir.Y;
		else if (dir.Y < -0.0001f) ty = (rect.Position.Y - from.Y) / dir.Y;
		return Mathf.Max(0f, Mathf.Min(tx, ty));
	}
}
