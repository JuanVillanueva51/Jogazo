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
}
