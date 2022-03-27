using System;
using System.IO;
using Terraria.ModLoader.IO;

namespace TerrariansConstructLib.Modifiers {
	internal sealed class UnloadedItemPartModifierData : ItemPartModifierData {
		public string mod, name;

		public UnloadedItemPartModifierData(int modifierID, string mod, string name) : base(modifierID) {
			this.mod = mod;
			this.name = name;
		}

		public override void NetReceive(BinaryReader reader) {
			base.NetReceive(reader);

			mod = reader.ReadString();
			name = reader.ReadString();
		}

		public override void NetSend(BinaryWriter writer) {
			base.NetSend(writer);

			writer.Write(mod);
			writer.Write(name);
		}

		public override TagCompound SerializeData()
			=> new() {
				["progress"] = currentUpgradeProgress,
				["tier"] = currentTier,
				["mod"] = mod,
				["name"] = name
			};

		public static new Func<TagCompound, UnloadedItemPartModifierData> DESERIALIZER = tag => {
			int progress = tag.GetInt("progress");
			int tier = tag.GetInt("tier");

			string mod = tag.GetString("mod");
			string name = tag.GetString("name");

			return new UnloadedItemPartModifierData(-1, mod, name) {
				currentUpgradeProgress = progress,
				currentTier = tier
			};
		};
	}
}
