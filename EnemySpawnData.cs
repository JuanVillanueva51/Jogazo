using Godot;
[GlobalClass]
public partial class EnemySpawnData : Resource
{
	[Export] public PackedScene EnemyScene { get; set; }
	[Export] public int mapLevel { get; set; } = 1;
	[Export] public float MinSpawnTimeSeconds { get; set; } = 0f;
}
