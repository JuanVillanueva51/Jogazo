using Godot;
using System;

public partial class EventManager : Node
{
	public static EventManager Instance { get; private set; }
	
	[Signal] public delegate void PlayerDiedEventHandler();
	[Signal] public delegate void LevelUpEventHandler(int level);
	[Signal] public delegate void CoinChangedEventHandler(int coin);
	[Signal] public delegate void XpChangedEventHandler(float xp, float xpToNextLevel);
	[Signal] public delegate void GameWonEventHandler();
	[Signal] public delegate void GamePausedEventHandler();
	[Signal] public delegate void WeaponEquippedEventHandler(string weaponName);
	
	public override void _EnterTree()
	{
		Instance = this;
	}
	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}
	public void EmitGameWon(){
		GD.Print("Se llama evento de win");
		EmitSignal(SignalName.GameWon);
	}
	public void EmitGamePaused(){
		GD.Print("Se llama evento de pausado");
		EmitSignal(SignalName.GamePaused);
	}
	public void EmitPlayerDied(){
		GD.Print("Se llama evento de muerte");
		EmitSignal(SignalName.PlayerDied);
	}
	public void EmitLevelUp(int level){
		EmitSignal(SignalName.LevelUp,level);
		GD.Print("Se llama evento de nivel");
	}
	public void EmitCoinChanged(int newAmount){
		EmitSignal(SignalName.CoinChanged,newAmount);
		GD.Print("Se llama evento de moneda");
	}
	public void EmitXpChanged(float xp, float xpToNextLevel){
		EmitSignal(SignalName.XpChanged,xp,xpToNextLevel);
		GD.Print("Se llama evento de experiencia");
	}
	public void EmitWeaponEquipped(WeaponData weapon){
		GD.Print("Se llama evento de equipar arma: " + weapon.Name);
		EmitSignal(SignalName.WeaponEquipped, weapon.Name);
	}
}
