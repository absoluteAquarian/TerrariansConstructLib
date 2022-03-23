using Terraria.Localization;
using Terraria.ModLoader;

namespace TerrariansConstructLib.Stats {
	/// <summary>
	/// An object for displaying stat modifiers in item tooltips
	/// </summary>
	public struct ItemStat {
		public readonly string name;
		public readonly string langKey;

		public ItemStat(string name, string langKey) {
			this.name = name;
			this.langKey = langKey;
		}

		public string Format(float stat)
			=> Language.GetTextValue(langKey, stat);
	}
}
