using Godot;

/// <summary>
/// Escena de configuración del menú principal (configuration_menu.tscn).
/// Muestra el fondo del menú y abre la pantalla de configuración; al cerrarla vuelve al menú.
/// </summary>
public partial class ConfigurationMenu : Control
{
	public override void _Ready()
	{
		SettingsScreen.Open(this, OnClosed, false);
	}

	private void OnClosed()
	{
		if (IsInsideTree())
			GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
	}
}
