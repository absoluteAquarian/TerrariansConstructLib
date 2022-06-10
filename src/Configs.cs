using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace TerrariansConstructLib {
	[Label("Serverside Config")]
	public sealed class TCConfig : ModConfig {
		public override bool Autoload(ref string name) {
			name = "Serverside";

			return base.Autoload(ref name);
		}

		public override ConfigScope Mode => ConfigScope.ServerSide;

		public static TCConfig Instance => ModContent.GetInstance<TCConfig>();

		[Label("Enable Durability")]
		[Tooltip("Whether constructed items using Terrarians' Construct items should use durability.")]
		[DefaultValue(true)]
		public bool UseDurability;

		[Label("Limit Starting Inventory")]
		[Tooltip("Whether new players start without the Copper tools and with a Fist item instead.")]
		[DefaultValue(true)]
		public bool NewPlayersStartWithOnlyHand;
	}

	[Label("Clientside Config")]
	public sealed class TCClientConfig : ModConfig {
		public override bool Autoload(ref string name) {
			name = "Clientside";

			return base.Autoload(ref name);
		}

		public override ConfigScope Mode => ConfigScope.ClientSide;

		public static TCClientConfig Instance => ModContent.GetInstance<TCClientConfig>();

		[Label("Show Materials")]
		[Tooltip("Whether constructed items should display their part components in their tooltips.")]
		[DefaultValue(false)]
		public bool DisplayItemParts;

		[Label("Show Counters")]
		[Tooltip("Whether constructed items should display their ability counters in their tooltips.")]
		[DefaultValue(false)]
		public bool DisplayAbilityCounters;
	}
}
