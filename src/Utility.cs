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
		public static T GetValueFromPartDictionary<T>(this Dictionary<int, Dictionary<int, T>> partDictionary, Material material, int partID) {
			int materialType = material.type;
			if (!partDictionary.TryGetValue(materialType, out var dictByPartID))
				throw new ArgumentException($"Unknown material type: \"{material.GetItemName()}\" (ID: {material})");

			if (partID < 0 || partID >= PartRegistry.Count)
				throw new Exception($"Part ID {partID} was invalid");

			if (!dictByPartID.TryGetValue(partID, out T value))
				throw new ArgumentException($"Material type \"{material.GetItemName()}\" (ID: {material}) did not have an entry for part ID {partID}");

			return value;
		}

		public static void SetValueInPartDictionary<T>(this Dictionary<int, Dictionary<int, T>> partDictionary, Material material, int partID, T value) {
			int materialType = material.type;
			if (!partDictionary.TryGetValue(materialType, out var dictByPartID))
				throw new ArgumentException($"Unknown material type: \"{material.GetItemName()}\" (ID: {material})");

			//Ensure that the part exists
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new Exception($"Part ID {partID} was invalid");

			dictByPartID[partID] = value;
		}

		public static bool TryGetValueFromPartDictionary<T>(this Dictionary<int, Dictionary<int, T>> partDictionary, Material material, int partID, out T value) {
			int materialType = material.type;
			if (!partDictionary.TryGetValue(materialType, out var dictByPartID)) {
				value = default;
				return false;
			}

			if (partID >= PartRegistry.Count) {
				value = default;
				return false;
			}

			return dictByPartID.TryGetValue(partID, out value);
		}

		public static void SafeSetValueInPartDictionary<T>(this Dictionary<int, Dictionary<int, T>> partDictionary, Material material, int partID, T value) {
			int materialType = material.type;
			if (!partDictionary.TryGetValue(materialType, out var dictByPartID))
				dictByPartID = partDictionary[materialType] = new();

			//Ensure that the part exists
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new Exception($"Part ID {partID} was invalid");

			dictByPartID[partID] = value;
		}

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
