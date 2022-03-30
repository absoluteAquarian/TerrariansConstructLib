namespace TerrariansConstructLib.API.Sources {
	public sealed class DurabilityModificationSource_Regen : IDurabilityModificationSource {
		public readonly object source;

		public DurabilityModificationSource_Regen(object source) {
			this.source = source;
		}
	}
}
