using Godot;
using System.Collections.Generic;

public partial class EnemySpawner : Node
{
	[Export] public PackedScene[] RangedEnemies;
	[Export] public EnemySpawnData[] spawnList;
	[Export] public PackedScene bossScene;
	[Export] public float SpawnRadius       = 400f;
	[Export] public float BaseSpawnInterval = 2f; //Normalmente es 2f
	[Export] public float RangedSpawnChance = 0.1f;
	[Export] public float waveDuration = 30f;
	[Export] public float waveCooldown = 180f;
	[Export] public float waveSpawnInterval = 0.15f;
	/// <summary>Límite de enemigos vivos para cuidar el rendimiento.</summary>
	[Export] public int MaxAliveEnemies = 250;
	public bool isWaveActive = false;
	public float waveTimer = 0f;
	private float  _spawnTimer      = 0f;
	private float  _currentInterval;
	private Player _player;

	public override void _Ready()
	{
		_player          = GetTree().GetFirstNodeInGroup("player") as Player;
		_currentInterval = BaseSpawnInterval;
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

		float minutesInMap = gm.ElapsedTime / 60f;
		float normalInterval = Balance.SpawnInterval(BaseSpawnInterval, minutesInMap, gm.diffModifier);
		_currentInterval = isWaveActive ? Mathf.Min(waveSpawnInterval, normalInterval) : normalInterval;

		if (_spawnTimer >= _currentInterval)
		{
			_spawnTimer = 0f;
			if (GetTree().GetNodesInGroup("enemies").Count < MaxAliveEnemies)
				SpawnEnemy();
		}
	}

	private void SpawnEnemy()
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
		Vector2 spawnPosition = GetSpawnPosition();
		float level = gm.EnemyLevel;

		if (spawnRanged)
			SpawnRanged(chosenScene, spawnPosition, level, isElite);
		else
			SpawnMelee(chosenScene, spawnPosition, level, isElite);
	}

	private void SpawnMelee(PackedScene scene, Vector2 position, float level, bool isElite)
	{
		Enemy enemy  = scene.Instantiate<Enemy>();
		enemy.MaxHp   *= Balance.EnemyHpMultiplier(level);
		enemy.Damage  *= Balance.EnemyDamageMultiplier(level);
		enemy.Speed   *= Balance.EnemySpeedMultiplier(level);
		enemy.XpValue *= Balance.EnemyXpMultiplier(level);
		enemy.CoinValue = Balance.EnemyCoinValue(level);
		if (isElite) enemy.MakeElite();
		_player.GetParent().AddChild(enemy);
		enemy.GlobalPosition = position;
	}

	private void SpawnRanged(PackedScene scene, Vector2 position, float level, bool isElite)
	{
		RangedEnemy enemy = scene.Instantiate<RangedEnemy>();
		enemy.MaxHp   *= Balance.EnemyHpMultiplier(level);
		enemy.Damage  *= Balance.EnemyDamageMultiplier(level);
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
		boss.MaxHp   *= Balance.BossHpMultiplier(coefficient);
		boss.Damage  *= Balance.EnemyDamageMultiplier(level);
		boss.XpValue *= Balance.BossXpMultiplier;
		boss.CoinValue = Balance.EnemyCoinValue(level) * Balance.BossCoinMultiplier;
		_player.GetParent().AddChild(boss);
		boss.GlobalPosition = GetSpawnPosition();
	}

	private Vector2 GetSpawnPosition()
	{
		float angle = GD.Randf() * Mathf.Tau;
		return _player.GlobalPosition + new Vector2(
			Mathf.Cos(angle) * SpawnRadius,
			Mathf.Sin(angle) * SpawnRadius
		);
	}
}
