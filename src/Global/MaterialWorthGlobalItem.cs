using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TerrariansConstructLib.API;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.Global {
	internal class MaterialWorthGlobalItem : GlobalItem {
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			Material material = Material.FromItem(item.type);

			int loadedID = CoreLibMod.MaterialType(material);

			if (loadedID > 0) {
				int worth = MaterialDefinitionLoader.Get(loadedID)!.MaterialWorth;
				
				int index = tooltips.FindIndex(line => line.Name == "Material");

				if (index >= 0)
					tooltips.Insert(index + 1, new TooltipLine(Mod, "TCMaterialWorth", $"  Material worth: {1f / worth}"));
			}
		}
	}
}
