using System;
using System.Collections.Generic;

public enum PassiveType
{
	None, Caballero, Arquero, Asesino, Berserker,
	Catalizador, Ogro, MagoMaestro, CaballeroCorcel,
	// Clases nuevas (agregar siempre al final para no cambiar los valores guardados)
	Piromante, Electromante, Astronomo, TecnicoLaser, Guardian, CaminanteIgneo,
	Estudiante, Pistolero, Nigromante, Segador, Cazador, Aeromante, Criomante, Ingeniero,
}

public partial class ClassData
{
	public string      Name;
	public string      Description;
	/// <summary>Estilo de juego de la clase (solo texto; las afinidades son las que dan el bonus).</summary>
	public string      Archetype;
	public string      PassiveName;
	public string      PassiveDescription;
	public string      StartingWeaponName; // debe existir en WeaponLibrary
	public PassiveType Passive;
	/// <summary>Retrato de la clase. Si no hay, se usa el ícono del arma inicial.</summary>
	public string      PortraitPath;
	/// <summary>Afinidades sutiles con tipos de arma (bonus pequeños, no obligatorios).</summary>
	public List<WeaponAffinity> Affinities = new();
	/// <summary>Pasiva con lógica propia (opcional). Se agrega al inventario como efecto innato.</summary>
	public Func<ItemEffect> CreatePassiveEffect;
}
