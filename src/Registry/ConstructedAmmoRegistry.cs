using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace TerrariansConstructLib.Registry {
	public static class ConstructedAmmoRegistry {
		internal static int Register(Mod mod, string name, int ammoID, string projectileInternalName) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			if (ammoID <= 0)
				throw new ArgumentOutOfRangeException(nameof(ammoID), "Ammo ID must be a positive, non-zero integer");

			if (TryFindData(mod, name, out _))
				throw new Exception($"The ammo entry \"{mod.Name}:{name}\" already exists");

			int next = nextID;

			registeredIDs[nextID] = new() {
				mod = mod,
				name = name,
				ammoID = ammoID,
				projectileInternalName = projectileInternalName
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

		public static int Count => nextID;

		internal static Dictionary<int, Data> registeredIDs;
		internal static int nextID;

		internal static bool TryFindData(Mod mod, string name, out int id) {
			foreach (var (i, d) in registeredIDs) {
				if (d.mod == mod && d.name == name) {
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
			public int ammoID;
			public string projectileInternalName;
		}
	}
}
