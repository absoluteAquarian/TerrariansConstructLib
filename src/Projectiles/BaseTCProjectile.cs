using System;
using Terraria;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.Projectiles {
	[Autoload(false)]
	public class BaseTCProjectile : ModProjectile {
		internal ItemPart[] parts = Array.Empty<ItemPart>();

		protected ReadOnlySpan<ItemPart> GetParts() => parts;

		protected ItemPart GetPart(int index) => parts[index];

		public sealed override void SetStaticDefaults() {
			SafeSetStaticDefaults();

			DisplayName.SetDefault("Constructed Projectile");
		}

		/// <inheritdoc cref="SetStaticDefaults"/>
		public virtual void SafeSetStaticDefaults() { }

		public sealed override void SetDefaults() {
			SafeSetDefaults();

			for (int i = 0; i < parts.Length; i++)
				parts[i].SetProjectileDefaults(parts[i].partID, Projectile);
		}

		/// <inheritdoc cref="SetDefaults"/>
		public virtual void SafeSetDefaults() { }

		public sealed override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].OnHitNPC(parts[i].partID, Projectile, target, damage, knockback, crit);
		}

		public sealed override void OnHitPlayer(Player target, int damage, bool crit) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].OnHitPlayer(parts[i].partID, Projectile, target, damage, crit);
		}

		public sealed override void AI() {
			for (int i = 0; i < parts.Length; i++)
				parts[i].ProjectileAI(parts[i].partID, Projectile);

			SafeAI();
		}

		public virtual void SafeAI() { }
	}
}
