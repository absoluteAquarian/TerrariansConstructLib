using TerrariansConstructLib.API.Definitions;
using TerrariansConstructLib.API.Stats;

namespace TerrariansConstructLib.Default {
	public sealed class ShardPartDefinition : PartDefinition {
		public override StatType StatType => StatType.Extra;

		public override string Name => "ItemCraftLeftover";

		public override string DisplayName => "Shard";

		public override int MaterialCost => 1 * 2;
	}
}
