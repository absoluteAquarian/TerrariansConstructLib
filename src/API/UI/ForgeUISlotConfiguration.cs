namespace TerrariansConstructLib.API.UI {
	public struct ForgeUISlotConfiguration {
		public readonly int slot;
		public readonly int position;
		public readonly int partID;

		public ForgeUISlotConfiguration(int slot, int position, int partID) {
			this.slot = slot;
			this.position = position;
			this.partID = partID;
		}

		public static implicit operator (int slot, int position, int partID)(ForgeUISlotConfiguration configuration)
			=> (configuration.slot, configuration.position, configuration.partID);

		public static implicit operator ForgeUISlotConfiguration((int slot, int position, int partID) tuple)
			=> new(tuple.slot, tuple.position, tuple.partID);
	}
}
