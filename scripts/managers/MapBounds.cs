using Godot;

/// <summary>
/// Límites visibles del mapa (nodo "LimitesMapa" en cada mapa).
///  - Fija los límites de la cámara del jugador al área pintada del mapa: la cámara deja de
///    seguir al jugador al llegar al borde y nunca muestra el espacio oscuro de afuera.
///  - EnemySpawner lo usa para que los enemigos aparezcan dentro del mapa.
/// Los muros invisibles que detienen al jugador son StaticBody2D aparte (pueden estar dentro de
/// este nodo o sueltos en el mapa). En el editor el rectángulo se dibuja en azul.
/// </summary>
[Tool]
public partial class MapBounds : Node2D
{
	private Rect2 _cameraLimits = new Rect2(-1000f, -1000f, 2000f, 2000f);
	private bool _showInEditor = true;

	/// <summary>Área que puede mostrar la cámara, relativa a este nodo.</summary>
	[Export]
	public Rect2 CameraLimits
	{
		get => _cameraLimits;
		set { _cameraLimits = value; QueueRedraw(); }
	}

	/// <summary>Dibuja el rectángulo en el editor (no se ve en el juego).</summary>
	[Export]
	public bool ShowInEditor
	{
		get => _showInEditor;
		set { _showInEditor = value; QueueRedraw(); }
	}

	/// <summary>Límites del mapa actual (null en mapas sin este nodo).</summary>
	public static MapBounds Current { get; private set; }

	/// <summary>CameraLimits en coordenadas globales.</summary>
	public Rect2 GlobalLimits => new Rect2(GlobalPosition + _cameraLimits.Position, _cameraLimits.Size);

	private ulong _appliedCameraId;

	public override void _EnterTree()
	{
		if (!Engine.IsEditorHint()) Current = this;
	}

	public override void _ExitTree()
	{
		if (Current == this) Current = null;
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint()) return;
		// El jugador aparece un frame después del mapa (WorldSpawner): aplicar cuando haya cámara
		Camera2D camera = GetViewport()?.GetCamera2D();
		if (camera == null || !IsInstanceValid(camera)) return;
		ulong id = camera.GetInstanceId();
		if (id == _appliedCameraId) return;
		Apply(camera);
		_appliedCameraId = id;
	}

	/// <summary>Aplica los límites a una cámara.</summary>
	public void Apply(Camera2D camera)
	{
		Rect2 r = GlobalLimits;
		camera.LimitLeft   = Mathf.CeilToInt(r.Position.X);
		camera.LimitTop    = Mathf.CeilToInt(r.Position.Y);
		camera.LimitRight  = Mathf.FloorToInt(r.End.X);
		camera.LimitBottom = Mathf.FloorToInt(r.End.Y);
		camera.ResetSmoothing();
	}

	/// <summary>Lleva un punto al interior de los límites (con un margen).</summary>
	public Vector2 Clamp(Vector2 point, float margin = 0f)
	{
		Rect2 r = GlobalLimits.Grow(-margin);
		return new Vector2(
			Mathf.Clamp(point.X, r.Position.X, r.End.X),
			Mathf.Clamp(point.Y, r.Position.Y, r.End.Y));
	}

	public override void _Draw()
	{
		if (!Engine.IsEditorHint() || !_showInEditor) return;
		DrawRect(_cameraLimits, new Color(0.25f, 0.75f, 1f, 0.9f), false, 6f);
	}
}
