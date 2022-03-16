using System.Collections.Generic;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Commands {
	internal class ListUsedMaterials : RegistryCommand<ItemPart> {
		public override string ChatString => "Known Materials:";

		public override string UsageString => "/lm";

		public override string Command => "lm";

		public override string Description => "Lists all known materials used by parts loaded by Terrarians' Construct";

		public override Dictionary<int, ItemPart> GetRegistry() {
			HashSet<int> usedIDs = new();
			Dictionary<int, ItemPart> dict = new();

			foreach (var (id, part) in ItemPartItem.registeredPartsByItemID) {
				if (usedIDs.Add(part.material.type))
					dict.Add(id, part);
			}

			return dict;
		}

		public override string GetReplyString(int id, ItemPart data)
			=> $"Item ID: \"{data.material.GetModName()}:{data.material.GetItemName()}\" ({data.material.type})";
	}
}
