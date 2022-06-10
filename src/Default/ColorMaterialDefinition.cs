using System.Collections.Generic;
using Terraria.ModLoader;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Definitions;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Modifiers;

namespace TerrariansConstructLib.Default {
	[Autoload(false)]
	internal class ColorMaterialDefinition : MaterialDefinition {
		public readonly ColorMaterialType colorType;

		public override string Name => base.Name + "_" + colorType;

		public ColorMaterialDefinition(ColorMaterialType colorType) {
			this.colorType = colorType;
		}

		public override Material? Material => new ColorMaterial(colorType);

		public override BaseTrait? Trait => new UnknownTrait();

		public override IEnumerable<PartDefinition> ValidParts => PartDefinitionLoader.AllParts();
	}
}
