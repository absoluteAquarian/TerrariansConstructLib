using System.Collections.Generic;
using TerrariansConstructLib.API.Definitions;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.API {
	public static class MaterialDefinitionLoader {
		public static int Count { get; private set; }

		internal static readonly List<MaterialDefinition> materials = new();

		public static int Add(MaterialDefinition definition) {
			materials.Add(definition);
			return Count++;
		}

		public static MaterialDefinition? Get(int index) {
			return index < 0 || index >= materials.Count ? null : materials[index];
		}

		public static void Unload() {
			materials.Clear();
			Count = 0;
		}

		public static MaterialDefinition? Find(Material material) {
			if (material is null)
				return null;
			
			foreach (var def in materials) {
				if (def.Material == material)
					return def;
			}

			return null;
		}
	}
}
