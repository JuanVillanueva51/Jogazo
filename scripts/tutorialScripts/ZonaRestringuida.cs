using Godot;

public partial class ZonaRestringuida : Area2D
{
	private TextureRect mensaje;

	public override void _Ready()
	{
		mensaje = GetNode<TextureRect>("../CanvasLayer/TextureRect");
		mensaje.Visible = false;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node body)
	{
		GD.Print("SI HE ENTRADO");
		string name = body.Name;
		if (body.IsInGroup("player"))
		{
			mensaje.Visible = true;
		}
	}

	private void OnBodyExited(Node body)
	{
		string name = body.Name;
		if (body.IsInGroup("player"))
		{
			mensaje.Visible = false;
		}
	}
}
