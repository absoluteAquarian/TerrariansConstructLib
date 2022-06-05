using Terraria;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.Global {
	internal class ItemSpaceGlobalItem : GlobalItem {
		public override bool OnPickup(Item item, Player player)
			=> player.HeldItem.ModItem is not BaseTCItem tc || tc.modifiers.OnPickup(item, player);
	}
}
