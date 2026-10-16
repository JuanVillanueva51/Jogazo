using Godot;

public partial class DialogoNPC : Area2D
{
	private Sprite2D dialogo;

	public override void _Ready()
	{
		GD.Print("Si se preparo el dialogo");
		dialogo = GetNode<Sprite2D>("../SpriteDialogo");
		dialogo.Visible = false;
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node body)
	{
		if(body.IsInGroup("player")){
		GD.Print("Se acerco al NPC");
			dialogo.Visible = true;
			}
	}

	private void OnBodyExited(Node body)
	{
		if(body.IsInGroup("player")){
			dialogo.Visible = false;
			}
	}
}
