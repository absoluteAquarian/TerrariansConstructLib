using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Definitions;

namespace TerrariansConstructLib.API.Commands {
	internal class ListItems : RegistryCommand<TCItemDefinition> {
		public override string ChatString => "Registered Items:";

		public override string UsageString => "/li";

		public override string Command => "li";
		public override string Description => "Lists all registered item types loaded by Terrarians' Construct";

		public override Dictionary<int, TCItemDefinition> GetRegistry() => ItemDefinitionLoader.items.ToDictionary(d => d.Type, d => d);

		public override string GetReplyString(int id, TCItemDefinition data)
			=> $"Item #{id}, Name: {data.Name}, Item: {(ModContent.GetModItem(data.ItemType) is ModItem item ? item.GetType().FullName + " (" + item.Type + ")" : "<invalid>")}\n" +
				$"   Parts: {string.Join(", ", data.GetForgeSlotConfiguration().Select(f => PartDefinitionLoader.GetIdentifier(f.partID)))}";
	}
}
