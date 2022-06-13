using Terraria;
using Terraria.DataStructures;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Sources {
	public sealed class EntitySource_TCItemUse_WithAmmo : EntitySource_ItemUse_WithAmmo {
		public readonly Player owner;
		
		public readonly BaseTCItem weapon;

		public readonly Item ammo;
		
		public EntitySource_TCItemUse_WithAmmo(Player player, BaseTCItem weapon, Item ammo, string? context = null) : base(player, weapon.Item, ammo.type, context) {
			owner = player;
			this.weapon = weapon;
			this.ammo = ammo;
		}
	}
}
