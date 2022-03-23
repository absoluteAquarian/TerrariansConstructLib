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
	}
}
