using Godot;

/// <summary>
/// Rarezas de objetos (inspiradas en Risk of Rain 2 / Megabonk).
///  Común      → mejoras simples de estadísticas.
///  Poco común → efectos condicionales o por probabilidad.
///  Raro       → efectos fuertes.
///  Legendario → efectos que definen la build.
///  Maldito    → efectos muy fuertes con una desventaja (como los objetos lunares de RoR2).
/// </summary>
public enum ItemRarity
{
	Comun = 0,
	PocoComun = 1,
	Raro = 2,
	Legendario = 3,
	Maldito = 4,
}

public enum ItemKind
{
	Pasivo,
	Activo,
}

public static class RarityInfo
{
	public static Color GetColor(ItemRarity rarity) => rarity switch
	{
		ItemRarity.Comun      => new Color(0.86f, 0.86f, 0.86f),
		ItemRarity.PocoComun  => new Color(0.36f, 0.86f, 0.36f),
		ItemRarity.Raro       => new Color(0.35f, 0.62f, 1f),
		ItemRarity.Legendario => new Color(1f, 0.76f, 0.2f),
		ItemRarity.Maldito    => new Color(0.72f, 0.38f, 1f),
		_ => Colors.White,
	};

	public static string GetName(ItemRarity rarity) => rarity switch
	{
		ItemRarity.Comun      => "Común",
		ItemRarity.PocoComun  => "Poco común",
		ItemRarity.Raro       => "Raro",
		ItemRarity.Legendario => "Legendario",
		ItemRarity.Maldito    => "Maldito",
		_ => "?",
	};

	public static string GetKindName(ItemKind kind) => kind == ItemKind.Activo ? "Activo" : "Pasivo";

	/// <summary>Rareza inmediatamente inferior (para cuando no hay objetos disponibles en una rareza).</summary>
	public static bool TryLower(ItemRarity rarity, out ItemRarity lower)
	{
		switch (rarity)
		{
			case ItemRarity.Legendario: lower = ItemRarity.Raro; return true;
			case ItemRarity.Raro:       lower = ItemRarity.PocoComun; return true;
			case ItemRarity.PocoComun:  lower = ItemRarity.Comun; return true;
			default:                    lower = rarity; return false;
		}
	}
}
