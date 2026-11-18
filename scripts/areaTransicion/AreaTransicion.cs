using Godot;
using System.Threading.Tasks;

public partial class AreaTransicion : Area2D
{
	[Export] public string RutaEscena = "res://scenes/loadscreens/load_screen_1.tscn";
	[Export] public int targetMapLevel = 1;
	private ColorRect fade;
	private bool _transitioning = false;

	public override void _Ready()
	{
		fade = GetNode<ColorRect>("../CanvasLayer/ColorRect");
		fade.Modulate = new Color(0, 0, 0, 0);
		BodyEntered += OnBodyEntered;
	}

	private async void OnBodyEntered(Node body)
	{
		if (_transitioning) return;
		if (!body.IsInGroup("player")) return;
		
		_transitioning = true;
		if (body is Player player)
			GameManager.Instance.SavePlayerState(player);
		
		GameManager.Instance.Transition(targetMapLevel);
		Tween tween = CreateTween();
		tween.TweenProperty(fade, "modulate:a", 1.0f, 1.0f);
		await ToSignal(tween, Tween.SignalName.Finished);

		GetTree().ChangeSceneToFile(RutaEscena);
	}
}
