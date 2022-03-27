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

		public readonly bool useMultiplicativeOnly, useAdditiveOnly;

		public ModifierText(Material material, int partID, CreationContext context) {
			this.material = material;
			this.partID = partID;
			
			langKey = context.langKey;
			Stat = context.Stat;
			useMultiplicativeOnly = context.useMultiplicativeOnly;
			useAdditiveOnly = context.useAdditiveOnly;
		}

		public ModifierText(ItemPart part, CreationContext context) {
			material = part.material;
			partID = part.partID;
			
			langKey = context.langKey;
			Stat = context.Stat;
			useMultiplicativeOnly = context.useMultiplicativeOnly;
			useAdditiveOnly = context.useAdditiveOnly;
		}

		public ItemPart GetPart() => ItemPart.partData.Get(material, partID);

		public ModifierText Clone() => (ModifierText)MemberwiseClone();

		public struct CreationContext {
			public string? langKey;

			public StatModifier Stat;

			public bool useMultiplicativeOnly, useAdditiveOnly;

			public CreationContext(string? langKey, StatModifier? stat = null, bool useMultiplicativeOnly = false, bool useAdditiveOnly = false) {
				this.langKey = langKey;
				Stat = stat ?? StatModifier.One;
				this.useMultiplicativeOnly = useMultiplicativeOnly;
				this.useAdditiveOnly = useAdditiveOnly;
			}
		}
	}
}
