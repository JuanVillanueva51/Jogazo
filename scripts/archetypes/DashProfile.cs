using Godot;

/// <summary>Tipo de dash: define el efecto especial que acompaña al movimiento.</summary>
public enum DashStyle
{
	Rodada,         // dash normal
	Embestida,      // daña y empuja a los enemigos que atraviesa
	Parpadeo,       // casi instantáneo; explosión arcana al salir
	OndaExpansiva,  // al terminar empuja y ralentiza alrededor
	PasoSombrio,    // el siguiente golpe es crítico seguro
	Bastion,        // al terminar reduce mucho el daño recibido
	Sprint,         // muchas cargas cortas + velocidad extra
	Propulsor,      // deja una mina que explota
	GolpeDeSuerte,  // a veces no gasta carga
	PasoVital,      // cura al terminar
	EstelaToxica,   // deja charcos tóxicos por donde pasa
}

/// <summary>
/// Configuración del dash de un personaje. Cada personaje tiene uno acorde a su arquetipo
/// (lo usa Player y lo muestra el HUD).
/// </summary>
public sealed class DashProfile
{
	public string    Name        = "Rodada";
	public string    Description = "Esquiva rápida.";
	public DashStyle Style       = DashStyle.Rodada;
	/// <summary>Cargas máximas.</summary>
	public int       Charges     = 2;
	public float     Distance    = 120f;
	/// <summary>Duración del movimiento (invulnerable mientras dura).</summary>
	public float     Duration    = 0.25f;
	/// <summary>Segundos para recuperar una carga.</summary>
	public float     Recharge    = 1.5f;
	/// <summary>Tiempo mínimo entre dos dashes seguidos.</summary>
	public float     Cooldown    = 0.2f;
	/// <summary>Multiplicador del efecto especial (daño, curación...).</summary>
	public float     Power       = 1f;
	public Color     Color       = new Color(0.6f, 0.85f, 1f);

	public static DashProfile Default() => new DashProfile();

	public string StatsText()
	{
		string charges = Charges == 1 ? "1 carga" : $"{Charges} cargas";
		return $"Dash «{Name}»: {Description} ({charges}, recarga {Recharge:0.#} s)";
	}
}
