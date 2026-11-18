using Godot;
using System;

public partial class SignText : Area2D
{
	private Sprite2D dialogo;
	
	public override void _Ready()
	{
		dialogo = GetNode<Sprite2D>("dialogo");
		dialogo.Visible = false;
		
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	private void OnBodyEntered(Node body)
	{
		GD.Print(body.Name);
		string name = body.Name;
		
		if(body.Name ==  name)
		{
			GD.Print("Se cumplio el if");
			dialogo.Visible = true;
		}
	}

	private void OnBodyExited(Node body)
	{
		string name = body.Name;
		if(body.Name == name)
		{
			dialogo.Visible = false;
		}
	}
}
