using Terraria;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Sources {
	public sealed class DurabilityModificationSource_AmmoConsumption : IDurabilityModificationSource {
		public readonly Item weapon;
		public readonly BaseTCItem ammo;
		public readonly Player player;

		public DurabilityModificationSource_AmmoConsumption(Item weapon, BaseTCItem ammo, Player player) {
			this.weapon = weapon;
			this.ammo = ammo;
			this.player = player;
		}
	}
}
