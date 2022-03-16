using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.API.Commands {
	internal class ListItems : RegistryCommand<ItemRegistry.Data> {
		public override string ChatString => "Registered Items:";

		public override string UsageString => "/li";

		public override string Command => "li";
		public override string Description => "Lists all registered item types loaded by Terrarians' Construct";

		public override Dictionary<int, ItemRegistry.Data> GetRegistry() => ItemRegistry.registeredIDs;

		public override string GetReplyString(int id, ItemRegistry.Data data)
			=> $"Item #{id}, Name: {data.name}, Item: {(data.mod.TryFind<ModItem>(data.itemInternalName, out var item) ? item.GetType().FullName + " (" + item.Type + ")" : "<invalid>")}\n" +
				$"   Parts: {string.Join(", ", data.validPartIDs.Select(PartRegistry.IDToIdentifier))}";
	}
}
