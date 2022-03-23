using System.Collections.Generic;
using System.Linq;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.API.Commands {
	internal class ListUsedMaterials : RegistryCommand<Material> {
		public override string ChatString => "Registered Materials:";

		public override string UsageString => "/lm";

		public override string Command => "lm";

		public override string Description => "Lists all registered materials with stats";

		public override Dictionary<int, Material> GetRegistry() => Material.statsByMaterialID
			.Select(kvp => new KeyValuePair<int, Material>(kvp.Key, Material.FromItem(kvp.Key)))
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

		public override string GetReplyString(int id, Material data)
			=> $"Item ID: \"{data.GetModName()}:{data.GetItemName()}\" ({data.Type})";
	}
}
