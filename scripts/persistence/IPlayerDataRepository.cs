/// <summary>
/// Punto de conexión con el almacenamiento de datos del jugador.
///
/// Hoy se usa JsonFilePlayerDataRepository (archivo local). Para conectar una base de datos
/// (SQLite, un servidor REST, Firebase, etc.) basta con crear otra clase que implemente esta
/// interfaz y devolverla en ProfileManager.CreateRepository(). El resto del juego no cambia.
/// </summary>
public interface IPlayerDataRepository
{
	/// <summary>Descripción legible del origen de datos (ruta del archivo, URL...).</summary>
	string Description { get; }

	/// <summary>Carga el perfil. Devuelve null si no existe todavía.</summary>
	PlayerProfile Load();

	/// <summary>Guarda el perfil completo. Devuelve false si falló.</summary>
	bool Save(PlayerProfile profile);

	/// <summary>Registra una partida terminada (en BD sería una tabla de historial).</summary>
	void AppendRunHistory(string playerId, RunSummary run);
}
