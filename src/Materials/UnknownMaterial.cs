using System;
using Terraria.ModLoader.IO;

namespace TerrariansConstructLib.Materials {
	public sealed class UnknownMaterial : Material {
		public override string GetModName() => nameof(TerrariansConstructLib);

		public override string GetName() => "Unknown";

		public override string GetItemName() => GetName();

		public override TagCompound SerializeData() {
			TagCompound tag = new();

			tag["mod"] = GetModName();
			tag["name"] = GetName();
			tag["rarity"] = rarity;

			return tag;
		}

		public static new Func<TagCompound, UnknownMaterial> DESERIALIZER = tag => {
			int rarity = tag.GetInt("rarity");

			return new UnknownMaterial(){
				rarity = rarity,
				type = -100612
			};
		};
	}
}
