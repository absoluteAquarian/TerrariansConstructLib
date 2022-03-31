using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace TerrariansConstructLib {
	public sealed class TCConfig : ModConfig {
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
}
