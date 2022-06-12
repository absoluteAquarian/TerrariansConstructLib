using Terraria.ID;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Projectiles;

namespace TerrariansConstructLib.API.Definitions {
	public abstract class TCProjectileDefinition : ModType {
		public int Type { get; private set; }

		/// <summary>
		/// The ID of the <see cref="BaseTCProjectile"/> projectile that this projectile definition is tied to
		/// </summary>
		public abstract int ProjectileType { get; }

		/// <summary>
		/// The ID of the <see cref="BaseTCItem"/> ammo item that this projectile definition is tied to.
		/// Defaults to 0, meaning this projectile is not spawned from ammo.
		/// </summary>
		public virtual int AmmoItemType => ItemID.None;

		protected sealed override void Register() {
			ModTypeLookup<TCProjectileDefinition>.Register(this);
			Type = ProjectileDefinitionLoader.Add(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();
	}
}
