using System.Collections.Generic;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Definitions;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Modifiers;

namespace TerrariansConstructLib.Default {
	internal class UnloadedMaterialDefinition : MaterialDefinition {
		public override Material? Material => new UnloadedMaterial();

		public override BaseTrait? Trait => GetTrait<UnloadedTrait>();

		public override IEnumerable<IPartStats> GetMaterialStats()
			=> new IPartStats[] {
				new HeadPartStats(),
				new HandlePartStats(),
				new ExtraPartStats()
			};

		public override IEnumerable<PartDefinition> ValidParts => PartDefinitionLoader.AllParts();
	}
}
