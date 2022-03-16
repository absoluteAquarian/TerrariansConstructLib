using System;
using Terraria.ModLoader.IO;

namespace TerrariansConstructLib.Materials {
	/// <summary>
	/// Represents an unknown material
	/// </summary>
	public sealed class UnknownMaterial : Material {
		public override string GetModName() => "TerrariansConstruct";

		public override string GetName() => "Unknown";

		public override string GetItemName() => GetName();

		public static readonly int StaticType = -100612;

		public UnknownMaterial() {
			type = StaticType;
		}

		public override Material Clone() => new UnknownMaterial() {
			type = type,
			rarity = rarity
		};

		public override TagCompound SerializeData() {
			TagCompound tag = new();

			tag["mod"] = GetModName();
			tag["name"] = GetName();
			tag["rarity"] = rarity;

			return tag;
		}

		public static new Func<TagCompound, UnknownMaterial> DESERIALIZER = tag => {
			string mod = tag.GetString("mod");
			string name = tag.GetString("name");
			int rarity = tag.GetInt("rarity");

			return new UnknownMaterial(){
				rarity = rarity,
				type = StaticType
			};
		};
	}
}
