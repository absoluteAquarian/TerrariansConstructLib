using Terraria;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Edits.Detours {
	partial class Vanilla {
		internal static bool Hook_Player_HasAmmo(On.Terraria.Player.orig_HasAmmo orig, Player self, Item sItem, bool canUse) {
			if (sItem.useAmmo <= 0)
				return orig(self, sItem, canUse);

			canUse = false;
			for (int i = 0; i < 58; i++) {
				//If the ammo item is a TC ammo item, make sure it has enough ammo in its reserve
				if (self.inventory[i].ammo == sItem.useAmmo && self.inventory[i].stack > 0 && (sItem.ModItem is not BaseTCItem tc || tc.ammoReserve > 0)) {
					canUse = true;

					break;
				}
			}

			return canUse;
		}
	}
}
