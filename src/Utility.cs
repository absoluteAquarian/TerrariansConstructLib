using System;
using System.Collections.Generic;
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
	}
}
