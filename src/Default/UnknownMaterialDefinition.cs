using System.Collections.Generic;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Definitions;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Modifiers;

namespace TerrariansConstructLib.Default {
	internal class UnknownMaterialDefinition : MaterialDefinition {
		public override Material? Material => new UnknownMaterial();

		public override BaseTrait? Trait => GetTrait<UnknownTrait>();

		public override IEnumerable<IPartStats> GetMaterialStats()
			=> new IPartStats[] {
				new HeadPartStats(),
				new HandlePartStats(),
				new ExtraPartStats()
			};

		public override IEnumerable<PartDefinition> ValidParts => PartDefinitionLoader.AllParts();
	}
}
