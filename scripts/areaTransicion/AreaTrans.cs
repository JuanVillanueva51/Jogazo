using Godot;
using System.Threading.Tasks;

public partial class AreaTrans : Area2D
{
	[Export] public string RutaEscena = "res://scenes/loadscreens/load_screen_3.tscn";
	[Export] public int targetMapLevel = 3;
	private ColorRect fade;
	private bool      _transitioning = false;
	
	public override void _Ready()
	{
		GD.Print("AreaTransicion2 cargada");
		fade = GetNode<ColorRect>("../CanvasLayer/ColorRect");
		fade.Modulate = new Color(0, 0, 0, 0);
		BodyEntered += OnBodyEntered;
	}

	private async void OnBodyEntered(Node body)
	{
		if (_transitioning) return;
		GD.Print("Entró: " + body.Name);
		if (!body.IsInGroup("player")) return;
		if (GameManager.Instance.currentMapLevel > 0 && !GameManager.Instance.isBossDefeated)
 		{
 		 GD.Print("Cambio bloqueado");
		return;  
		}
		_transitioning = true;
		if (body is Player player)
			GameManager.Instance.SavePlayerState(player);
		
		GameManager.Instance.Transition(targetMapLevel);
		GD.Print("Cambio de escena iniciado");

		Tween tween = CreateTween();
		tween.TweenProperty(fade, "modulate:a", 1.0f, 1.0f);
		await ToSignal(tween, Tween.SignalName.Finished);

		GetTree().ChangeSceneToFile(RutaEscena);
	}
}
