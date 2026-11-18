using System.Collections.Generic;
using Godot;

/// <summary>
/// Base para cosas con las que el jugador interactúa con la acción "interact" (E):
/// cofres, tiendas, etc. El jugador interactúa con la más cercana que esté en rango.
/// Las clases hijas que sobrescriban _Ready deben llamar a base._Ready().
/// </summary>
public partial class Interactable : Area2D
{
	private static readonly List<Interactable> InRange = new();
	private static ulong _lockedUntilMsec;

	/// <summary>
	/// Bloquea la interacción un momento (p. ej. al cerrar la tienda con un clic,
	/// para que ese mismo clic no la vuelva a abrir).
	/// </summary>
	public static void LockInteraction(float seconds)
	{
		_lockedUntilMsec = Time.GetTicksMsec() + (ulong)Mathf.Max(0, Mathf.RoundToInt(seconds * 1000f));
	}

	[Export] public float PromptOffsetY = -40f;
	[Export] public float InteractRadius = 30f;

	protected Player PlayerNearby { get; private set; }
	private Label _prompt;

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 1;
		Monitorable = false;

		// Si la escena no trae forma de colisión, se crea una
		bool hasShape = false;
		foreach (Node child in GetChildren())
			if (child is CollisionShape2D || child is CollisionPolygon2D) { hasShape = true; break; }
		if (!hasShape)
			AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = InteractRadius } });

		_prompt = UiFactory.MakeLabel("", 8, Colors.White);
		_prompt.HorizontalAlignment = HorizontalAlignment.Center;
		_prompt.Size = new Vector2(180f, 12f);
		_prompt.Position = new Vector2(-90f, PromptOffsetY);
		_prompt.MouseFilter = Control.MouseFilterEnum.Ignore;
		_prompt.ZIndex = 10;
		_prompt.Visible = false;
		AddChild(_prompt);

		BodyEntered += OnInteractBodyEntered;
		BodyExited += OnInteractBodyExited;
	}

	public override void _ExitTree()
	{
		InRange.Remove(this);
	}

	public virtual string GetPromptText() => "[E] Interactuar";
	public virtual bool CanInteract(Player player) => true;
	public virtual void Interact(Player player) { }

	/// <summary>Actualiza el texto flotante (llamar cuando cambie el estado).</summary>
	protected void RefreshPrompt()
	{
		if (_prompt == null) return;
		bool show = PlayerNearby != null && CanInteract(PlayerNearby);
		_prompt.Visible = show;
		if (show) _prompt.Text = GetPromptText();
	}

	private void OnInteractBodyEntered(Node2D body)
	{
		if (body is not Player player) return;
		PlayerNearby = player;
		if (!InRange.Contains(this)) InRange.Add(this);
		RefreshPrompt();
	}

	private void OnInteractBodyExited(Node2D body)
	{
		if (body is not Player) return;
		PlayerNearby = null;
		InRange.Remove(this);
		RefreshPrompt();
	}

	/// <summary>Interactuable más cercano al jugador (o null).</summary>
	public static Interactable FindBest(Player player)
	{
		if (player == null || Time.GetTicksMsec() < _lockedUntilMsec) return null;
		Interactable best = null;
		float bestD2 = float.MaxValue;
		for (int i = InRange.Count - 1; i >= 0; i--)
		{
			var it = InRange[i];
			if (!IsInstanceValid(it) || !it.IsInsideTree())
			{
				InRange.RemoveAt(i);
				continue;
			}
			if (!it.CanInteract(player)) continue;
			float d2 = it.GlobalPosition.DistanceSquaredTo(player.GlobalPosition);
			if (d2 < bestD2)
			{
				bestD2 = d2;
				best = it;
			}
		}
		return best;
	}
}
