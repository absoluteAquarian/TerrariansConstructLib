using System.Collections.Generic;
using TerrariansConstructLib.API.Definitions;

namespace TerrariansConstructLib.API {
	public static class ProjectileDefinitionLoader {
		public static int Count { get; private set; }

		internal static readonly List<TCProjectileDefinition> projectiles = new();

		public static int Add(TCProjectileDefinition definition) {
			projectiles.Add(definition);
			return Count++;
		}

		public static TCProjectileDefinition? Get(int index) {
			return index < 0 || index >= projectiles.Count ? null : projectiles[index];
		}

		public static void Unload() {
			projectiles.Clear();
			Count = 0;
		}
	}
}
