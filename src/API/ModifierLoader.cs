using System.Collections.Generic;
using TerrariansConstructLib.Modifiers;

namespace TerrariansConstructLib.API {
	public static class ModifierLoader {
		public static int Count { get; private set; }

		internal static readonly List<BaseTrait> modifiers = new();

		public static int Add(BaseTrait definition) {
			modifiers.Add(definition);
			return Count++;
		}

		public static BaseTrait? Get(int index) {
			return index < 0 || index >= modifiers.Count ? null : modifiers[index];
		}

		public static void Unload() {
			modifiers.Clear();
			Count = 0;
		}
	}
}
