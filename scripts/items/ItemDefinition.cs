using System;
using Godot;

/// <summary>
/// Definición de un objeto (datos). La lógica vive en un ItemEffect que se crea
/// con la fábrica CreateEffect, una instancia por jugador.
/// Para agregar un objeto nuevo basta con añadir una entrada en ItemLibrary.
/// </summary>
public sealed class ItemDefinition
{
	public string Id;
	public string Name;
	public string Description;
	public ItemRarity Rarity = ItemRarity.Comun;
	public ItemKind Kind = ItemKind.Pasivo;

	/// <summary>Solo activos: segundos de enfriamiento.</summary>
	public float Cooldown = 30f;

	/// <summary>Disponible desde el inicio (sin gastar créditos).</summary>
	public bool UnlockedByDefault;

	/// <summary>-1 = costo según rareza (Balance.UnlockCost).</summary>
	public int UnlockCostOverride = -1;

	/// <summary>Objetos ocultos (p. ej. versiones "consumidas") no salen en cofres ni en desbloqueos.</summary>
	public bool Hidden;

	/// <summary>Ruta opcional a un ícono. Si no hay, se dibuja un ícono con la inicial y el color de rareza.</summary>
	public string IconPath;

	public Func<ItemEffect> CreateEffect;

	public int UnlockCost => UnlockCostOverride >= 0 ? UnlockCostOverride : Balance.UnlockCost(Rarity, Kind);
	public Color Color => RarityInfo.GetColor(Rarity);
	public bool IsActive => Kind == ItemKind.Activo;

	public string Initials
	{
		get
		{
			if (string.IsNullOrEmpty(Name)) return "?";
			string[] words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (words.Length >= 2 && words[1].Length > 2)
				return $"{char.ToUpper(words[0][0])}{char.ToUpper(words[1][0])}";
			return char.ToUpper(Name[0]).ToString();
		}
	}

	public string FullDescription()
	{
		string header = $"{RarityInfo.GetName(Rarity)} · {RarityInfo.GetKindName(Kind)}";
		if (IsActive) header += $" · Enfriamiento {Cooldown:0}s";
		return $"{header}\n{Description}";
	}
}
