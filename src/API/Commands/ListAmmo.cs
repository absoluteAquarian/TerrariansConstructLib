using System.Collections.Generic;
using Terraria.ModLoader;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.API.Commands {
	internal class ListAmmo : RegistryCommand<ConstructedAmmoRegistry.Data> {
		public override string ChatString => "Constructed Ammo:";

		public override string UsageString => "/la";

		public override string Command => "la";

		public override string Description => "Lists all constructed ammo types loaded by Terrarians' Construct";

		public override Dictionary<int, ConstructedAmmoRegistry.Data> GetRegistry() => ConstructedAmmoRegistry.registeredIDs;

		public override string GetReplyString(int id, ConstructedAmmoRegistry.Data data)
			=> $"Ammo #{id}, Name: {data.name}, Shot Projectile: {(data.mod.TryFind<ModProjectile>(data.projectileInternalName, out var proj) ? proj.GetType().FullName : "<invalid>")}";
	}
}
