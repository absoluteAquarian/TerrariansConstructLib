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
		public sealed override bool IsLoadingEnabled(Mod mod) => SafeIsLoadingEnabled(mod) ?? false;

		/// <summary>
		/// Allows you to safely request whether this projectile should be autoloaded
		/// </summary>
		/// <param name="mod">The mod adding this projectile</param>
		/// <returns><see langword="null"/> for the default behaviour (don't autoload projectile), <see langword="true"/> to let the projectile autoload or <see langword="false"/> to prevent the projectile from autoloading</returns>
		public virtual bool? SafeIsLoadingEnabled(Mod mod) => null;

		/// <summary>
		/// The name for the projectile, used in <see cref="SetStaticDefaults"/><br/>
		/// Defaults to: <c>"Projectile"</c>
		/// </summary>
		public virtual string ProjectileTypeName => "Projectile";

		// TODO: projectile drawing
		public sealed override string Texture => "TerrariansConstructLib/Assets/DummyProjectile";

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
