using Terraria.ModLoader;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.API {
	/// <summary>
	/// Represents a line of text for a part's modifier
	/// </summary>
	public sealed class ModifierText {
		public readonly string? langText;

		public readonly Material material;
		public readonly int partID;

		public StatModifier Stat { get; internal set; }

		public ModifierText(string? langText, Material material, int partID, StatModifier stat) {
			this.langText = langText;
			this.material = material;
			this.partID = partID;
			Stat = stat;
		}

		public ModifierText(string langText, ItemPart part, StatModifier stat) {
			this.langText = langText;
			material = part.material;
			partID = part.partID;
			Stat = stat;
		}

		public ItemPart GetPart() => ItemPart.partData.Get(material, partID);

		public ModifierText Clone() => (ModifierText)MemberwiseClone();
	}
}
