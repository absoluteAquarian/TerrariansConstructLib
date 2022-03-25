using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using TerrariansConstructLib.API.UI;

namespace TerrariansConstructLib.Registry {
	public static class ItemRegistry {
		internal static int Register(Mod mod, string internalName, string name, string itemInternalName, string partVisualsFolder, float useSpeedMultiplier, params ForgeUISlotConfiguration[] configuration) {
			if (mod is null)
				throw new ArgumentNullException(nameof(mod));

			if (internalName is null)
				throw new ArgumentNullException(nameof(internalName));

			if (name is null)
				throw new ArgumentNullException(nameof(name));

			if (configuration is null)
				throw new ArgumentNullException(nameof(configuration));

			if (configuration.Length <= 1)
				throw new ArgumentException("Slot configuration array must have at least 2 elements", nameof(configuration));

			if (TryFindData(mod, internalName, out _))
				throw new Exception($"The weapon entry \"{mod.Name}:{internalName}\" already exists");

			int[] ids = configuration.Select(p => p.partID).ToArray();

			foreach (var (id, data) in registeredIDs)
				if (data.validPartIDs.SequenceEqual(ids))
					throw new Exception($"Unable to add the weapon entry \"{mod.Name}:{internalName}\"\n" +
						$"The weapon entry \"{data.mod.Name}:{data.internalName}\" already contains the wanted part sequence:\n" +
						$"   {string.Join(", ", data.validPartIDs.Select(PartRegistry.IDToIdentifier))}");

			int next = nextID;

			registeredIDs[next] = new() {
				mod = mod,
				name = name,
				internalName = internalName,
				validPartIDs = ids,
				configuration = configuration,
				itemInternalName = itemInternalName,
				partVisualsFolder = partVisualsFolder,
				useSpeedMultiplier = useSpeedMultiplier
			};

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

		internal static int nextID;

		public static int Count => nextID;

		internal static Dictionary<int, Data> registeredIDs;

		public static bool TryFindData(Mod mod, string internalName, out int id) {
			foreach (var (i, d) in registeredIDs) {
				if (d.mod == mod && d.internalName == internalName) {
					id = i;
					return true;
				}
			}

			id = -1;
			return false;
		}

		public static bool TryGetConfiguration(int registeredItemID, out ReadOnlySpan<ForgeUISlotConfiguration> configuration) {
			if (registeredItemID < 0 || registeredItemID >= Count) {
				configuration = Array.Empty<ForgeUISlotConfiguration>();
				return false;
			}

			if (registeredIDs.TryGetValue(registeredItemID, out var data)) {
				configuration = data.configuration;
				return true;
			}

			configuration = Array.Empty<ForgeUISlotConfiguration>();
			return false;
		}

		internal class Data {
			public Mod mod;
			public string name;
			public string internalName;
			public int[] validPartIDs;
			public ForgeUISlotConfiguration[] configuration;
			public string itemInternalName;
			public string partVisualsFolder;
			public float useSpeedMultiplier;
		}
	}
}
