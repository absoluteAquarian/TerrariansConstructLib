using System;
using Terraria;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.Projectiles {
	/// <summary>
	/// The base projectile class for any projectiles fired from Terrarians' Construct weapons
	/// </summary>
	public class BaseTCProjectile : ModProjectile {
		internal ItemPart[] parts = Array.Empty<ItemPart>();

		protected ReadOnlySpan<ItemPart> GetParts() => parts;

		protected ItemPart GetPart(int index) => parts[index];

		//Can't use [Autoload(false)] lest deriving types not get added
		public override bool IsLoadingEnabled(Mod mod) => false;

		/// <summary>
		/// The name for the projectile, used in <see cref="SetStaticDefaults"/><br/>
		/// Defaults to: <c>"Projectile"</c>
		/// </summary>
		public virtual string ProjectileTypeName => "Projectile";

		public sealed override void SetStaticDefaults() {
			SafeSetStaticDefaults();

			DisplayName.SetDefault("Constructed " + ProjectileTypeName);
		}

		/// <inheritdoc cref="SetStaticDefaults"/>
		public virtual void SafeSetStaticDefaults() { }

		public sealed override void SetDefaults() {
			SafeSetDefaults();

			for (int i = 0; i < parts.Length; i++)
				parts[i].SetProjectileDefaults?.Invoke(parts[i].partID, Projectile);
		}

		/// <inheritdoc cref="SetDefaults"/>
		public virtual void SafeSetDefaults() { }

		public sealed override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].OnHitNPC?.Invoke(parts[i].partID, Projectile, target, damage, knockback, crit);
		}

		/// <inheritdoc cref="OnHitNPC(NPC, int, float, bool)"/>
		public virtual void SafeOnHitNPC(NPC target, int damage, float knockback, bool crit) { }

		public sealed override void OnHitPlayer(Player target, int damage, bool crit) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].OnHitPlayer?.Invoke(parts[i].partID, Projectile, target, damage, crit);
		}

		/// <inheritdoc cref="OnHitPlayer(Player, int, bool)"/>
		public virtual void SafeOnHitPlayer(Player target, int damage, bool crit) { }

		public sealed override void AI() {
			for (int i = 0; i < parts.Length; i++)
				parts[i].ProjectileAI?.Invoke(parts[i].partID, Projectile);

			SafeAI();
		}

		/// <inheritdoc cref="AI"/>
		public virtual void SafeAI() { }
	}
}
