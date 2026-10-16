using Godot;

/// <summary>
/// Ayudas para agregar nodos al mapa actual de forma segura.
/// Se usa CallDeferred porque muchas muertes/efectos ocurren dentro de callbacks de física,
/// donde Godot no permite agregar áreas o cuerpos nuevos.
/// </summary>
public static class World
{
	public static SceneTree Tree => Engine.GetMainLoop() as SceneTree;

	public static Node CurrentParent
	{
		get
		{
			var tree = Tree;
			if (tree == null) return null;
			return tree.CurrentScene ?? tree.Root;
		}
	}

	public static void SpawnDeferred(Node2D node, Vector2 globalPosition)
	{
		if (node == null) return;
		Node parent = CurrentParent;
		if (parent == null)
		{
			node.QueueFree();
			return;
		}
		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(node)) return;
			if (!GodotObject.IsInstanceValid(parent))
			{
				node.QueueFree();
				return;
			}
			parent.AddChild(node);
			node.GlobalPosition = globalPosition;
		}).CallDeferred();
	}

	public static Player FindPlayer()
	{
		return Tree?.GetFirstNodeInGroup("player") as Player;
	}

	/// <summary>Capas de los muros invisibles y edificios (1 y 3).</summary>
	public const uint WallMask = 1u | 4u;

	/// <summary>
	/// Punto al que el jugador puede llegar caminando en línea recta desde "from" hacia "to":
	/// si hay un muro en medio, se queda justo antes del muro. También lo mete dentro de los
	/// límites del mapa. Sirve para que cofres y objetos no caigan detrás de un muro
	/// (los enemigos sí atraviesan muros y pueden morir ahí).
	/// Usar fuera de los callbacks de física (p. ej. dentro de un CallDeferred o en _Ready).
	/// </summary>
	public static Vector2 ReachablePoint(Vector2 from, Vector2 to, float padding = 18f)
	{
		if (MapBounds.Current != null)
			to = MapBounds.Current.Clamp(to, 24f);

		var tree = Tree;
		var space = tree?.Root?.World2D?.DirectSpaceState;
		if (space == null) return to;

		Vector2 delta = to - from;
		float length = delta.Length();
		if (length < 0.5f) return to;

		var query = PhysicsRayQueryParameters2D.Create(from, to, WallMask);
		query.CollideWithAreas = false;
		query.CollideWithBodies = true;
		var player = FindPlayer();
		if (player != null)
			query.Exclude = new Godot.Collections.Array<Rid> { player.GetRid() };

		var hit = space.IntersectRay(query);
		if (hit.Count == 0) return to;

		Vector2 point = (Vector2)hit["position"];
		Vector2 dir = delta / length;
		float back = Mathf.Min(padding, from.DistanceTo(point));
		return point - dir * back;
	}

	/// <summary>Como SpawnDeferred, pero mueve el nodo a un punto alcanzable desde el jugador.</summary>
	public static void SpawnDeferredReachable(Node2D node, Vector2 globalPosition)
	{
		if (node == null) return;
		Node parent = CurrentParent;
		if (parent == null)
		{
			node.QueueFree();
			return;
		}
		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(node)) return;
			if (!GodotObject.IsInstanceValid(parent))
			{
				node.QueueFree();
				return;
			}
			Vector2 position = globalPosition;
			var player = FindPlayer();
			if (player != null)
				position = ReachablePoint(player.GlobalPosition, globalPosition, 24f);
			parent.AddChild(node);
			node.GlobalPosition = position;
		}).CallDeferred();
	}
}
