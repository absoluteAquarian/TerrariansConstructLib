using System;
using Terraria.ModLoader.IO;

namespace TerrariansConstructLib.Materials {
	public sealed class UnloadedMaterial : Material {
		public string modName, itemName;

		public override string GetModName() => nameof(TerrariansConstructLib);

		public override string GetName() => "Unloaded";

		public override string GetItemName() => GetName();

		public override TagCompound SerializeData() {
			TagCompound tag = new();

			tag["mod"] = modName;
			tag["name"] = itemName;
			tag["rarity"] = rarity;

			return tag;
		}

		public static new Func<TagCompound, UnloadedMaterial> DESERIALIZER = tag => {
			string mod = tag.GetString("mod");
			string name = tag.GetString("name");
			int rarity = tag.GetInt("rarity");

			return new UnloadedMaterial(){
				modName = mod,
				itemName = name,
				rarity = rarity,
				type = -100413
			};
		};
	}
}
