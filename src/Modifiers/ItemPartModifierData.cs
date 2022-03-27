using System;
using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Modifiers {
	internal class ItemPartModifierData : TagSerializable, INetHooks {
		public int currentUpgradeProgress;
		public int currentTier;

		public readonly int modifierID;

		public ItemPartModifierData(int modifierID) {
			if (modifierID < 0 || modifierID >= ModifierRegistry.Count)
				throw new ArgumentException("Modifier ID was invalid");

			currentUpgradeProgress = 0;
			currentTier = 1;
		}

		public virtual void NetReceive(BinaryReader reader) {
			currentTier = reader.ReadByte();
			currentUpgradeProgress = reader.ReadInt16();
		}

		public virtual void NetSend(BinaryWriter writer) {
			writer.Write((byte)currentTier);
			writer.Write((short)currentUpgradeProgress);
		}

		public virtual TagCompound SerializeData() {
			var data = ModifierRegistry.registeredIDs[modifierID];

			return new() {
				["progress"] = currentUpgradeProgress,
				["tier"] = currentTier,
				["mod"] = data.mod.Name,
				["name"] = data.name
			};
		}

		public static Func<TagCompound, ItemPartModifierData> DESERIALIZER = tag => {
			int progress = tag.GetInt("progress");
			int tier = tag.GetInt("tier");

			string mod = tag.GetString("mod");
			string name = tag.GetString("name");

			if (!ModLoader.TryGetMod(mod, out var instance) || !ModifierRegistry.TryFindData(instance, name, out int id))
				return UnloadedItemPartModifierData.DESERIALIZER(tag);

			return new ItemPartModifierData(id) {
				currentUpgradeProgress = progress,
				currentTier = tier
			};
		};
	}
}
