using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.Global {
	internal class MaterialWorthGlobalItem : GlobalItem {
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			if (Material.worthByMaterialID.TryGetValue(item.type, out int worth)) {
				int index = tooltips.FindIndex(line => line.Name == "Material");

				if (index >= 0)
					tooltips.Insert(index + 1, new TooltipLine(Mod, "TCMaterialWorth", $"  Material worth: {1f / worth}"));
			}
		}
	}
}
