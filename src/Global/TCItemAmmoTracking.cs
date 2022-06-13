using Terraria;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.Global {
	internal class TCItemAmmoTracking : GlobalItem {
		public override void PickAmmo(Item weapon, Item ammo, Player player, ref int type, ref float speed, ref StatModifier damage, ref float knockback) {
			if (weapon.ModItem is BaseTCItem) {
				//Preserve the ammo item's data unless it's a BaseTCItem, in which case that's unnecessary
				API.Edits.MSIL.Vanilla.PickAmmo_Item = ammo.ModItem is BaseTCItem ? ammo : ammo.Clone();
			}
		}
	}
}
