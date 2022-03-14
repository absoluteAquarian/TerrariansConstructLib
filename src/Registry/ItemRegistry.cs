using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.Registry {
	public static class ItemRegistry {
		internal static int Register<T>(Mod mod, string internalName, string name, T item, params int[] validPartIDs) where T : BaseTCItem {
			if (mod is null)
				throw new ArgumentNullException(nameof(mod));

			if (internalName is null)
				throw new ArgumentNullException(nameof(internalName));

			if (name is null)
				throw new ArgumentNullException(nameof(name));

			if (validPartIDs is null)
				throw new ArgumentNullException(nameof(validPartIDs));

			if (validPartIDs.Length <= 1)
				throw new ArgumentException("Valid IDs array must have at least 2 elements", nameof(validPartIDs));

			if (TryFindData(mod, internalName, out _))
				throw new Exception($"The weapon entry \"{mod.Name}:{internalName}\" already exists");

			foreach (var (id, data) in registeredIDs)
				if (data.validPartIDs.SequenceEqual(validPartIDs))
					throw new Exception($"Unable to add the weapon entry \"{mod.Name}:{internalName}\"\n" +
						$"The weapon entry \"{data.mod.Name}:{data.internalName}\" already contains the wanted part sequence:\n" +
						$"   {string.Join(", ", data.validPartIDs.Select(PartRegistry.IDToIdentifier))}");

			int next = nextID;

			registeredIDs[next] = new() {
				mod = mod,
				name = name,
				internalName = internalName,
				validPartIDs = validPartIDs,
				itemInternalName = item.Name
			};

			nextID++;
			
			return next;
		}

		internal static void Load() {
			registeredIDs = new();
			nextID = 0;
		}

		internal static void Unload() {
			registeredIDs = null;
			nextID = 0;
		}

		internal static int nextID;

		public static int Count => nextID;

		internal static Dictionary<int, Data> registeredIDs;

		internal static bool TryFindData(Mod mod, string internalName, out int id) {
			foreach (var (i, d) in registeredIDs) {
				if (d.mod == mod && d.internalName == internalName) {
					id = i;
					return true;
				}
			}

			id = -1;
			return false;
		}

		internal class Data {
			public Mod mod;
			public string name;
			public string internalName;
			public int[] validPartIDs;
			public string itemInternalName;
		}
	}
}
