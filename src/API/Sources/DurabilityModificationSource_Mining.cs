using TerrariansConstructLib.DataStructures;

namespace TerrariansConstructLib.API.Sources {
	public sealed class DurabilityModificationSource_Mining : IDurabilityModificationSource {
		public readonly TileDestructionContext context;
		public readonly int x, y;

		public DurabilityModificationSource_Mining(TileDestructionContext context, int x, int y) {
			this.context = context;
			this.x = x;
			this.y = y;
		}
	}
}
