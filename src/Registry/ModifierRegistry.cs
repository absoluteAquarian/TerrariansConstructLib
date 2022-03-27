using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Modifiers;

namespace TerrariansConstructLib.Registry {
	public static class ModifierRegistry {
		internal static int Register(Mod mod, string name, BaseModifier modifier) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			if (modifier is null)
				throw new ArgumentNullException(nameof(modifier));

			if (TryFindData(mod, name, out _))
				throw new Exception($"The modifier entry \"{mod.Name}:{name}\" already exists");

			if (modifier.ID >= 0)
				throw new ArgumentException("Attempted to register the same BaseModifier instance twice");

			int next = nextID;

			registeredIDs[nextID] = new() {
				mod = mod,
				name = name,
				modifier = modifier
			};

			modifier.ID = next;

			nextID++;

			return next;
		}

		internal static void Load() {
			registeredIDs = new();

			nextID = 0;
		}

		internal static void Unload() {
			registeredIDs = null!;
			nextID = 0;
		}

		public static int Count => nextID;

		internal static Dictionary<int, Data> registeredIDs;
		internal static int nextID;

		public static bool TryFindData(Mod mod, string name, out int id) {
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
			public BaseModifier modifier;
		}
	}
}
