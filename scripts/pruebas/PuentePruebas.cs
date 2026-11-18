using Godot;
using Godot.Collections;

/// <summary>
/// Puente para las pruebas de GdUnit4 (test/casos_de_prueba_test.gd, UT-01 a UT-05).
/// La lógica del juego vive en clases estáticas de C# (Balance, DamageCalculator, LootTable)
/// que GDScript no puede llamar directamente; esta clase las expone con métodos normales.
/// El juego no la usa.
///
/// IMPORTANTE: este archivo NO debe ir dentro de la carpeta test/. GdUnit4 toma cualquier
/// archivo .cs que esté ahí como una suite de pruebas en C# e intentaría ejecutarlo.
/// </summary>
public partial class PuentePruebas : RefCounted
{
	// ───────────── UT-01: reducción de daño por armadura ─────────────

	/// <summary>Daño que recibe el jugador con cierta armadura (misma fórmula que Player.TakeDamageFrom).</summary>
	public float DanoRecibido(float dano, float armadura)
	{
		return dano * Balance.ArmorMultiplier(armadura);
	}

	// ───────────── UT-02: bonificaciones de daño ─────────────

	/// <summary>
	/// Daño de un arma cuerpo a cuerpo con una bonificación de tipo (+25% = 0.25), calculado con
	/// el DamageCalculator real del juego. Se divide entre Balance.PlayerDamageScale (la escala
	/// global de daño) para poder compararlo directo contra el daño base.
	/// </summary>
	public float DanoConBonificacion(float danoBase, float bonificacion)
	{
		var jugador = new Player();
		try
		{
			jugador.Stats.MeleeDamageBonus = bonificacion;
			var arma = new WeaponData { Name = "Arma de prueba", WeaponType = "melee" };
			return DamageCalculator.BaseForWeapon(jugador, arma, danoBase) / Balance.PlayerDamageScale;
		}
		finally
		{
			jugador.Free();
		}
	}

	// ───────────── UT-03 y UT-04: dificultad y nivel de enemigo ─────────────

	/// <summary>Cuántas dificultades hay (Normal, Difícil, Pesadilla).</summary>
	public int CantidadDificultades()
	{
		return Balance.Difficulties.Length;
	}

	/// <summary>Coeficiente de dificultad tras jugar "minutos" en el primer mapa (fórmula de Balance.Coefficient).</summary>
	public float Coeficiente(int dificultad, float minutos)
	{
		return Balance.Coefficient(minutos * 60f, 0, 1f, Balance.GetDifficulty(dificultad).ScalingValue);
	}

	/// <summary>Nivel de los enemigos tras jugar "minutos" en el primer mapa.</summary>
	public float NivelEnemigo(int dificultad, float minutos)
	{
		return Balance.EnemyLevel(Coeficiente(dificultad, minutos));
	}

	// ───────────── UT-05: tabla de botín con pesos ─────────────

	/// <summary>
	/// Tira "tiradas" veces con LootTable.RollRarity usando los pesos dados
	/// (claves: "Comun", "PocoComun", "Raro", "Legendario"; las que falten valen 0).
	/// Devuelve cuántas veces salió cada rareza, por ejemplo { "Comun": 7012, "Raro": 2988 }.
	/// </summary>
	public Dictionary ContarTiradas(Dictionary pesos, int tiradas)
	{
		float[] w =
		{
			PesoDe(pesos, "Comun"),
			PesoDe(pesos, "PocoComun"),
			PesoDe(pesos, "Raro"),
			PesoDe(pesos, "Legendario"),
		};
		var conteo = new Dictionary();
		for (int i = 0; i < tiradas; i++)
		{
			string nombre = LootTable.RollRarity(w, 0f).ToString();
			int previo = conteo.ContainsKey(nombre) ? (int)conteo[nombre] : 0;
			conteo[nombre] = previo + 1;
		}
		return conteo;
	}

	private static float PesoDe(Dictionary pesos, string clave)
	{
		return pesos.ContainsKey(clave) ? (float)pesos[clave] : 0f;
	}
}
