using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Definitions;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.Default;

namespace TerrariansConstructLib.API {
	public static class PartDefinitionLoader {
		public static int Count { get; private set; }

		internal static readonly List<PartDefinition> parts = new();

		public static int Add(PartDefinition definition) {
			parts.Add(definition);
			return Count++;
		}

		public static PartDefinition? Get(int index) {
			return index < 0 || index >= parts.Count ? null : parts[index];
		}

		public static string GetIdentifier(int partID) {
			if (Get(partID) is not PartDefinition def)
				return "<unknown>";

			return def.Mod.Name + ":" + def.Name;
		}

		public static void Unload() {
			parts.Clear();
			Count = 0;
		}

		//Helper methods for MaterialDefinition.ValidParts usage
		public static IEnumerable<PartDefinition> ShartPart()
			=> new PartDefinition[] {
				ModContent.GetInstance<ShardPartDefinition>()
			};
		
		public static IEnumerable<PartDefinition> AllHeadParts() => parts.Where(p => p.StatType == StatType.Head);

		public static IEnumerable<PartDefinition> AllHandleParts() => parts.Where(p => p.StatType == StatType.Handle);

		public static IEnumerable<PartDefinition> AllParts() => parts;
	}
}
