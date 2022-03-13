using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariansConstructLib.Projectiles {
	[Autoload(false)]
	public class TCBulletProjectile : BaseTCProjectile {
		public override void SafeSetDefaults() {
			Projectile.CloneDefaults(ProjectileID.Bullet);
		}
	}
}
