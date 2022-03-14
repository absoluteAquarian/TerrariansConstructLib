using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace TerrariansConstructLib.Registry {
	public static class PartRegistry {
		internal static int Register(Mod mod, string internalName, string name, string assetFolderPath) {
			if (mod is null)
				throw new ArgumentNullException(nameof(mod));

			if (internalName is null)
				throw new ArgumentNullException(nameof(internalName));

			if (name is null)
				throw new ArgumentNullException(nameof(name));

			if (assetFolderPath == null)
				throw new ArgumentNullException(nameof(assetFolderPath));

			if (TryFindData(mod, internalName, out _))
				throw new Exception($"The part entry \"{mod.Name}:{internalName}\" already exists");

			int next = nextID;

			registeredIDs[next] = new() {
				mod = mod,
				name = name,
				internalName = internalName,
				assetFolder = assetFolderPath
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

		public static string IDToIdentifier(int id) {
			if (!registeredIDs.TryGetValue(id, out var data))
				throw new Exception("Invalid part ID: " + id);

			return data.mod.Name + ":" + data.internalName;
		}

		internal class Data {
			public Mod mod;
			public string name;
			public string assetFolder;
			public string internalName;
		}
	}
}
