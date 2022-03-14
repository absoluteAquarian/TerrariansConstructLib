using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariansConstructLib.API {
	public static partial class Extensions {
		public static void DefaultToMeleeWeapon(this Item item, int singleUseTime, int projOnSwing = ProjectileID.None, float projSpeed = 0f) {
			item.DamageType = DamageClass.Melee;
			item.useTime = item.useAnimation = singleUseTime;

			if (projOnSwing > ProjectileID.None) {
				item.shoot = projOnSwing;
				item.shootSpeed = projSpeed;
			}
		}
	}
}
