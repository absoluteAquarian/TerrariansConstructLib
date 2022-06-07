using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using TerrariansConstructLib.API.UI;

#pragma warning disable CA2208

namespace TerrariansConstructLib.Registry {
	public static class ItemRegistry {
		internal static int Register(Mod mod, string internalName, string name, string itemInternalName, RegistrationContext context) {
			if (mod is null)
				throw new ArgumentNullException(nameof(mod));

			if (internalName is null)
				throw new ArgumentNullException(nameof(internalName));

			if (name is null)
				throw new ArgumentNullException(nameof(name));

			if (context.configuration is null)
				throw new ArgumentNullException(nameof(context) + "." + nameof(context.configuration));

			if (context.configuration.Length <= 1)
				throw new ArgumentException("Slot configuration array must have at least 2 elements", nameof(context) + "." + nameof(context.configuration));

			if (TryFindData(mod, internalName, out _))
				throw new Exception($"The weapon entry \"{mod.Name}:{internalName}\" already exists");

			int[] ids = context.configuration.Select(p => p.partID).ToArray();

			foreach (var (id, data) in registeredIDs)
				if (data.context.validPartIDs.SequenceEqual(ids))
					throw new Exception($"Unable to add the weapon entry \"{mod.Name}:{internalName}\"\n" +
						$"The weapon entry \"{data.mod.Name}:{data.internalName}\" already contains the wanted part sequence:\n" +
						$"   {string.Join(", ", data.context.validPartIDs.Select(PartRegistry.IDToIdentifier))}");

			context.validPartIDs = ids;

			int next = nextID;

			registeredIDs[next] = new() {
				mod = mod,
				name = name,
				internalName = internalName,
				itemInternalName = itemInternalName,
				context = context
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
				configuration = data.context.configuration;
				return true;
			}

			configuration = Array.Empty<ForgeUISlotConfiguration>();
			return false;
		}

		internal class Data {
			public Mod mod;
			public string name;
			public string internalName;
			public string itemInternalName;
			public RegistrationContext context;
		}

		public sealed class RegistrationContext {
			internal int[] validPartIDs;
			internal ForgeUISlotConfiguration[] configuration;
			public readonly string partVisualsFolder;
			public readonly float useSpeedMultiplier;
			public readonly int ammoConsumedPerShot;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="partVisualsFolder">The folder where the item's part visuals is located, relative to the mod they're from</param>
			/// <param name="useSpeedMultiplier">A modifier applied to the base use speed generated from the item's parts</param>
			/// <param name="ammoConsumedPerShot"></param>
			public RegistrationContext(string partVisualsFolder, float useSpeedMultiplier, int ammoConsumedPerShot = 1) {
				this.partVisualsFolder = partVisualsFolder;
				this.useSpeedMultiplier = useSpeedMultiplier;
				this.ammoConsumedPerShot = ammoConsumedPerShot;
			}
			
			/// <summary>
			/// 
			/// </summary>
			/// <param name="configuration">The array of parts that comprise the weapon and their slot contexts in the Forge UI</param>
			/// <returns></returns>
			public RegistrationContext WithConfiguration(params ForgeUISlotConfiguration[] configuration) {
				this.configuration = configuration;
				return this;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="configuration">The array of parts that comprise the weapon and their slot contexts in the Forge UI</param>
			/// <returns></returns>
			public RegistrationContext WithConfiguration(params (int slot, int position, int partID)[] configuration) {
				this.configuration = configuration.Select(t => (ForgeUISlotConfiguration)t).ToArray();
				return this;
			}
		}
	}
}
