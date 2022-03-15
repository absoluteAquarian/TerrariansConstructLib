using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib {
	public static class Utility {
		/// <summary>
		/// Spawns a shallow copy of an item in the world
		/// </summary>
		/// <param name="source">The entity source</param>
		/// <param name="item">The item instance</param>
		/// <param name="rectangle">The area where the item will spawn</param>
		public static void DropItem(IEntitySource source, Item item, Rectangle rectangle) {
			ReflectionHelperVoid<Item, IEntitySource, Item, Rectangle>.InvokeMethod("DropItem", null, source, item, rectangle);
		}

		public static void FindAndModify(List<TooltipLine> tooltips, string searchPhrase, string replacePhrase){
			int searchIndex = tooltips.FindIndex(t => t.text.Contains(searchPhrase));
			if(searchIndex >= 0)
				tooltips[searchIndex].text = tooltips[searchIndex].text.Replace(searchPhrase, replacePhrase);
		}

		public static void FindAndRemoveLine(List<TooltipLine> tooltips, string fullLine){
			int searchIndex = tooltips.FindIndex(t => t.text == fullLine);
			if(searchIndex >= 0)
				tooltips.RemoveAt(searchIndex);
		}

		public static void FindAndInsertLines(Mod mod, List<TooltipLine> tooltips, string searchLine, Func<int, string> lineNames, string replaceLines){
			int searchIndex = tooltips.FindIndex(t => t.text == searchLine);
			if(searchIndex >= 0){
				tooltips.RemoveAt(searchIndex);

				string lines = replaceLines;

				int inserted = 0;
				foreach(var line in lines.Split('\n')){
					tooltips.Insert(searchIndex++, new TooltipLine(mod, lineNames(inserted), line));
					inserted++;
				}
			}
		}
	}
}
