using System.Collections.Generic;
using System.Linq;
using TerrariansConstructLib.API.Definitions;

namespace TerrariansConstructLib.API.Commands {
	internal class ListParts : RegistryCommand<PartDefinition> {
		public override string ChatString => "Parts:";

		public override string UsageString => "/lp";

		public override string Command => "lp";

		public override string Description => "Lists all item parts loaded by Terrarians' Construct";

		public override Dictionary<int, PartDefinition> GetRegistry() => PartDefinitionLoader.parts.ToDictionary(d => d.Type, d => d);

		public override string GetReplyString(int id, PartDefinition data)
			=> $"Part #{id}, Name: {data.Name}, ID: \"{data.GetIdentifier()}\"";
	}
}
