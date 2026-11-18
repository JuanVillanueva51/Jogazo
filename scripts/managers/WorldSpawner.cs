using Godot;
using System;

public partial class WorldSpawner : Node
{
	// Posición donde aparece el jugador al iniciar
	[Export] public Vector2 SpawnPosition = Vector2.Zero;

	public override void _Ready()
	{
		CallDeferred(nameof(SpawnPlayer));;
	}

	private void SpawnPlayer()
	{
		string scenePath = GameManager.Instance.SelectedCharacterScene;

		var packed = GD.Load<PackedScene>(scenePath);
		if (packed == null)
		{
			GD.PrintErr($"WorldSpawner: No se encontró la escena {scenePath}");
			return;
		}

		var player = packed.Instantiate<CharacterBody2D>();
		GetParent().AddChild(player);
		player.GlobalPosition = SpawnPosition;
	}
}
