using System.Collections.Generic;
using TerrariansConstructLib.API.Definitions;

namespace TerrariansConstructLib.API {
	public static class ItemDefinitionLoader {
		public static int Count { get; private set; }

		internal static readonly List<TCItemDefinition> items = new();

		public static int Add(TCItemDefinition definition) {
			items.Add(definition);
			return Count++;
		}

		public static TCItemDefinition? Get(int index) {
			return index < 0 || index >= items.Count ? null : items[index];
		}

		public static void Unload() {
			items.Clear();
			Count = 0;
		}		
	}
}
