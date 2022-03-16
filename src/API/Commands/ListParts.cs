using System.Collections.Generic;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.API.Commands {
	internal class ListParts : RegistryCommand<PartRegistry.Data> {
		public override string ChatString => "Parts:";

		public override string UsageString => "/lp";

		public override string Command => "lp";

		public override string Description => "Lists all item parts loaded by Terrarians' Construct";

		public override Dictionary<int, PartRegistry.Data> GetRegistry() => PartRegistry.registeredIDs;

		public override string GetReplyString(int id, PartRegistry.Data data)
			=> $"Part #{id}, Name: {data.name}, ID: \"{data.mod.Name}:{data.internalName}\"";
	}
}
