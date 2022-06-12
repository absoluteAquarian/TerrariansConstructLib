using Terraria;
using TerrariansConstructLib.API.Sources;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Edits.Detours {
	internal static partial class Vanilla {
		internal static bool Hook_Player_HasAmmo(On.Terraria.Player.orig_HasAmmo_Item_bool orig, Player self, Item sItem, bool canUse) {
			if (sItem.useAmmo <= 0)
				return orig(self, sItem, canUse);
			
			canUse = false;
			for (int i = 0; i < 58; i++) {
				//If the ammo item is a TC ammo item, make sure it has enough ammo in its reserve
				if (self.inventory[i].ammo == sItem.useAmmo && self.inventory[i].stack > 0 && (sItem.ModItem is not BaseTCItem tc || tc.CurrentDurability > 0)) {
					canUse = true;

					break;
				}
			}

			return canUse;
		}
		
		internal static bool Hook_Player_IsAmmoFreeThisShot(On.Terraria.Player.orig_IsAmmoFreeThisShot orig, Player self, Item weapon, Item ammo, int projToShoot) {
			bool free = orig(self, weapon, ammo, projToShoot);

			if (!free && ammo.ModItem is BaseTCItem tc) {
				if (tc.CurrentDurability > 0) {
					//Consume an ammo from the reserve
					
					
					tc.TryReduceDurability(self, 1, new DurabilityModificationSource_AmmoConsumption(weapon, tc, self));
				}

				//Prevent the ammo item from being consumed
				return true;
			}

			return free;
		}
	}
}
