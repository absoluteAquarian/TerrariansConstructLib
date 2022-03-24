using System;
using System.Collections.Generic;
using System.Linq;

namespace TerrariansConstructLib.API.UI {
	public struct ForgeUISlotConfiguration {
		//   0  1  2  3  4
		//   5  6  7  8  9
		//  10 11 12 13 14
		//  15 16 17 18 19
		//  20 21 22 23 24

		internal static Dictionary<int, ForgeUISlotConfiguration[]> registeredConfigurations;

		internal static void Initialize() {
			registeredConfigurations = new();
		}

		internal static void Unload() {
			registeredConfigurations = null!;
		}

		public static void Register(int registeredItemID, params ForgeUISlotConfiguration[] configurations) {
			if (registeredConfigurations.ContainsKey(registeredItemID))
				throw new Exception($"Registered item ID {registeredItemID} already has a registered Forge UI slot configuration");

			registeredConfigurations[registeredItemID] = (ForgeUISlotConfiguration[])configurations.Clone();
		}

		public static void Register(int registeredItemID, params (int slot, int position, int partID)[] configurations)
			=> Register(registeredItemID, configurations.Select(t => (ForgeUISlotConfiguration)t).ToArray());

		public static ForgeUISlotConfiguration[] Get(int registeredItemID)
			=> registeredConfigurations.TryGetValue(registeredItemID, out var array)
				? array
				: throw new Exception($"Requested ID ({registeredItemID}) was either invalid or did not have a registered Forge UI slot configuration");

		public readonly int slot;
		public readonly int position;
		public readonly int partID;

		public ForgeUISlotConfiguration(int slot, int position, int partID) {
			this.slot = slot;
			this.position = position;
			this.partID = partID;
		}

		public static implicit operator (int slot, int position, int partID)(ForgeUISlotConfiguration configuration)
			=> (configuration.slot, configuration.position, configuration.partID);

		public static implicit operator ForgeUISlotConfiguration((int slot, int position, int partID) tuple)
			=> new(tuple.slot, tuple.position, tuple.partID);
	}
}
