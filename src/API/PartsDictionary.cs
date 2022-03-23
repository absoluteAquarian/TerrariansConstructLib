using System;
using System.Collections.Generic;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.API {
	public class PartsDictionary<T> : Dictionary<int, Dictionary<int, T>> {
		public T Get(Material material, int partID) {
			int materialType = material.Type;
			if (!Material.statsByMaterialID.ContainsKey(materialType))
				throw new ArgumentException($"Material was not registered: \"{material.GetItemName()}\" (ID: {material.Type})");

			if (!TryGetValue(materialType, out var dictByPartID))
				throw new ArgumentException($"Unknown material type: \"{material.GetItemName()}\" (ID: {material.Type})");

			if (partID < 0 || partID >= PartRegistry.Count)
				throw new Exception($"Part ID {partID} was invalid");

			if (!dictByPartID.TryGetValue(partID, out T? value))
				throw new ArgumentException($"Material type \"{material.GetItemName()}\" (ID: {material.Type}) did not have an entry for part ID {partID}");

			return value;
		}

		public bool TryGet(Material material, int partID, out T? value) {
			int materialType = material.Type;
			if (!TryGetValue(materialType, out var dictByPartID)) {
				value = default;
				return false;
			}

			if (partID >= PartRegistry.Count) {
				value = default;
				return false;
			}

			return dictByPartID.TryGetValue(partID, out value);
		}

		public void Set(Material material, int partID, T value) {
			//Ensure that the part exists
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new Exception($"Part ID {partID} was invalid");

			int materialType = material.Type;
			if (!TryGetValue(materialType, out var dictByPartID))
				dictByPartID = this[materialType] = new();

			dictByPartID[partID] = value;
		}

		public bool Has(Material material, int partID) {
			//Ensure that the part exists
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new Exception($"Part ID {partID} was invalid");

			return TryGetValue(material.Type, out var dictByPartID) && dictByPartID.ContainsKey(partID);
		}
	}
}
