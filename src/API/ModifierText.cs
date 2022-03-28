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

		public readonly bool useMultiplicativeOnly, useAdditiveOnly, positiveValueIsGoodModifier, treatAdditiveAsMultiplier;

		public ModifierText(Material material, int partID, CreationContext context) {
			this.material = material;
			this.partID = partID;
			
			langKey = context.langKey;
			Stat = context.Stat;
			useMultiplicativeOnly = context.useMultiplicativeOnly;
			useAdditiveOnly = context.useAdditiveOnly;
			positiveValueIsGoodModifier = context.positiveValueIsGoodModifier;
			treatAdditiveAsMultiplier = context.treatAdditiveAsMultiplier;
		}

		public ModifierText(ItemPart part, CreationContext context) {
			material = part.material;
			partID = part.partID;
			
			langKey = context.langKey;
			Stat = context.Stat;
			useMultiplicativeOnly = context.useMultiplicativeOnly;
			useAdditiveOnly = context.useAdditiveOnly;
			positiveValueIsGoodModifier = context.positiveValueIsGoodModifier;
			treatAdditiveAsMultiplier = context.treatAdditiveAsMultiplier;
		}

		public ItemPart GetPart() => ItemPart.partData.Get(material, partID);

		public ModifierText Clone() => (ModifierText)MemberwiseClone();

		public struct CreationContext {
			public string? langKey;

			public StatModifier Stat;

			public bool useMultiplicativeOnly, useAdditiveOnly, positiveValueIsGoodModifier, treatAdditiveAsMultiplier;

			/// <summary>
			/// Creates a context for initializing <see cref="ModifierText"/> instances
			/// </summary>
			/// <param name="langKey">The language key.  Use <see langword="null"/> for no text</param>
			/// <param name="stat">The stat used when displaying the text</param>
			/// <param name="useMultiplicativeOnly">Whether only the <see cref="StatModifier.Multiplicative"/> property should be used when displaying the text</param>
			/// <param name="useAdditiveOnly">
			/// Whether only the <see cref="StatModifier.Additive"/> property should be used when displaying the text<br/>
			/// This parameter has lower precedence than <paramref name="useMultiplicativeOnly"/>
			/// </param>
			/// <param name="positiveValueIsGoodModifier">Whether resulting stats &gt; 1 should be considered positive stats.  Defaults to <see langword="true"/></param>
			/// <param name="treatAdditiveAsMultiplier">
			/// Whether the <see cref="StatModifier.Additive"/> property of <paramref name="stat"/> should be considered a flat value or multiplier<br/>
			/// This paramter has lower precedence than <paramref name="useMultiplicativeOnly"/>
			/// </param>
			public CreationContext(string? langKey, StatModifier? stat = null, bool useMultiplicativeOnly = false, bool useAdditiveOnly = false, bool positiveValueIsGoodModifier = true, bool treatAdditiveAsMultiplier = false) {
				this.langKey = langKey;
				Stat = stat ?? StatModifier.One;
				this.useMultiplicativeOnly = useMultiplicativeOnly;
				this.useAdditiveOnly = useAdditiveOnly;
				this.positiveValueIsGoodModifier = positiveValueIsGoodModifier;
				this.treatAdditiveAsMultiplier = treatAdditiveAsMultiplier;
			}
		}
	}
}
