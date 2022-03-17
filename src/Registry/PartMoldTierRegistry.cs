using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Rarities;

namespace TerrariansConstructLib.Registry {
	public static class PartMoldTierRegistry {
		internal static int Register(Mod mod, string internalName, string name, Color tooltipColor, List<int> validMaterials) {
			if (mod is null)
				throw new ArgumentNullException(nameof(mod));

			if (internalName is null)
				throw new ArgumentNullException(nameof(internalName));

			if (name is null)
				throw new ArgumentNullException(nameof(name));

			int next = nextID;

			registeredIDs[next] = new(){
				mod = mod,
				name = name,
				internalName = internalName,
				color = tooltipColor,
				validMaterials = new(validMaterials)
			};

			nextID++;

			registeredIDs[next].rarity = new(next);

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

		public static bool IsValidMaterial(Material material, int moldTier)
			=> material is UnloadedMaterial or UnknownMaterial && registeredIDs[moldTier].validMaterials.Contains(material.type);

		public static void SetAsValidMaterial(Material material, int moldTier) {
			if (material is UnloadedMaterial or UnknownMaterial)
				return;

			registeredIDs[moldTier].validMaterials.Add(material.type);
		}

		internal class Data {
			public Mod mod;
			public string name;
			public string internalName;
			public Color color;
			public HashSet<int> validMaterials;
			internal BasePartMoldRarity rarity;
		}
	}
}
