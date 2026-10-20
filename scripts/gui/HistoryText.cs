/// <summary>
/// Texto de la introducción (pantalla Historia), sincronizado con el audio
/// res://sprites/music/historia_introduccion.mp3.
/// Cada línea tiene el segundo del audio en que empieza; se muestra hasta que empieza la siguiente.
/// Para corregir una palabra, cámbiala aquí. Si cambias el audio, ajusta también los tiempos.
/// </summary>
public static class HistoryText
{
	public const string AudioPath = "res://sprites/music/historia_introduccion.mp3";

	public readonly struct Line
	{
		public readonly float Start;
		public readonly string Text;
		public Line(float start, string text) { Start = start; Text = text; }
	}

	public static readonly Line[] Lines =
	{
		new(1.0f,   "Hace dos años, un grupo de magos consumidos por la obsesión del poder prohibido inició investigaciones sobre la magia oscura"),
		new(9.4f,   "y las fuerzas que habitaban más allá de la realidad conocida."),
		new(14.5f,  "En su intento por dominar aquella energía,"),
		new(17.0f,  "cometieron un error que cambiaría el destino del reino para siempre."),
		new(21.1f,  "Abrieron una fisura entre dos mundos."),
		new(24.7f,  "La colisión entre el mundo de los hombres y el reino del Ente desató el caos."),
		new(29.1f,  "No pasó mucho tiempo antes de que aquella presencia comenzara a expandirse como una plaga imparable,"),
		new(34.1f,  "devorando ciudades enteras y corrompiendo todo a su paso."),
		new(38.5f,  "Animales, guerreros y aldeanos fueron transformados en criaturas deformes y sin alma."),
		new(44.8f,  "Aquellos seres lo suficientemente fuertes para soportar la corrupción fueron convertidos en los temidos paladines del Ente,"),
		new(53.2f,  "guardianes monstruosos de un poder imposible de comprender."),
		new(58.3f,  "Ante la destrucción inminente,"),
		new(60.5f,  "los tres grandes magos del reino unieron sus fuerzas y levantaron una gigantesca barrera mágica para contener el avance del Ente."),
		new(68.2f,  "El sacrificio logró frenar la invasión, pero no detenerla."),
		new(74.9f,  "Ahora, dos años después, la barrera comienza a debilitarse."),
		new(79.2f,  "Las tierras consumidas permanecen envueltas en oscuridad,"),
		new(83.6f,  "liberando una extraña materia corrupta que atrae tanto miedo como curiosidad."),
		new(89.0f,  "Expediciones son enviadas constantemente a las zonas prohibidas para estudiar la corrupción y buscar una forma de detener el desastre antes de que sea demasiado tarde."),
		new(98.3f,  "Y ahora es tu turno."),
		new(101.5f, "Deberás adentrarte en ciudades abandonadas, enfrentar criaturas nacidas de la oscuridad"),
		new(107.2f, "y descubrir la verdad detrás del Ente antes de que el reino caiga por completo."),
		new(113.1f, "El tiempo se agota."),
		new(115.1f, "La barrera está muriendo y tú eres la última esperanza del reino."),
	};

	/// <summary>Índice de la línea que corresponde a un segundo del audio (-1 antes de la primera).</summary>
	public static int LineAt(float seconds)
	{
		int index = -1;
		for (int i = 0; i < Lines.Length; i++)
		{
			if (seconds + 0.05f >= Lines[i].Start) index = i;
			else break;
		}
		return index;
	}
}
