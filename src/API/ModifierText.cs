using Terraria.ModLoader;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.API {
	/// <summary>
	/// Represents a line of text for a part's modifier
	/// </summary>
	public sealed class ModifierText {
		public readonly string? langKey;

		public readonly Material material;
		public readonly int partID;

		public StatModifier Stat { get; internal set; }

		/// <summary>
		/// Whether the lang key should be treated as a formatted language key (<see langword="false"/>) or a string literal (<see langword="true"/>)<br/>
		/// Defaults to <see langword="false"/>
		/// </summary>
		public bool LangKeyIsLiteral { get; set; }

		public ModifierText(string? langKey, Material material, int partID, StatModifier stat) {
			this.langKey = langKey;
			this.material = material;
			this.partID = partID;
			Stat = stat;
		}

		public ModifierText(string langKey, ItemPart part, StatModifier stat) {
			this.langKey = langKey;
			material = part.material;
			partID = part.partID;
			Stat = stat;
		}

		public ItemPart GetPart() => ItemPart.partData.Get(material, partID);

		public ModifierText Clone() => (ModifierText)MemberwiseClone();
	}
}
