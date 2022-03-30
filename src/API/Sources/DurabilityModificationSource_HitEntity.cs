using Terraria;

namespace TerrariansConstructLib.API.Sources {
	public sealed class DurabilityModificationSource_HitEntity : IDurabilityModificationSource {
		public readonly Entity entity;
		public readonly bool doubledLossFromUsingMiningTool;

		public DurabilityModificationSource_HitEntity(Entity entity, bool doubledLossFromUsingMiningTool) {
			this.entity = entity;
			this.doubledLossFromUsingMiningTool = doubledLossFromUsingMiningTool;
		}
	}
}
