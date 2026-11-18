using Godot;
using System;

public partial class BossEnemy : Enemy
{
	public override bool IsBoss => true;

	public override void _Ready()
	{
		// Color y tamaño antes de base._Ready() para que se conserven al aplicar estados
		Modulate = new Color(0.5f, 0f, 1f);
		Scale = new Vector2(3f, 3f);
		base._Ready();
	}

	protected override void OnDeath()
	{
		GameManager.Instance.isBossDefeated = true;
		GD.Print("Boss derrotado");
		// Modo demo: un solo mapa, vencer al primer jefe es la victoria
		if (GameManager.Instance.currentMapLevel == 3 || GameManager.Instance.IsDemo)
		{
			EventManager.Instance.EmitGameWon();
		}
	}
}
