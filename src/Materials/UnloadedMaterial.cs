using System;
using Terraria.ModLoader.IO;

namespace TerrariansConstructLib.Materials {
	/// <summary>
	/// Represents a material that used to be loaded, but no longer is
	/// </summary>
	public class UnloadedMaterial : Material {
		public string modName, itemName;

		public override string GetModName() => "TerrariansConstruct";

		public override string GetName() => "Unloaded";

		public override string GetItemName() => GetName();

		public static readonly int StaticType = -100413;

		public UnloadedMaterial() {
			type = StaticType;
		}

		public override Material Clone() => new UnloadedMaterial() {
			modName = modName,
			itemName = itemName,
			type = type
		};

		public override TagCompound SerializeData() {
			TagCompound tag = new();

			tag["mod"] = modName;
			tag["name"] = itemName;

			return tag;
		}

		public static new Func<TagCompound, UnloadedMaterial> DESERIALIZER = tag => {
			string mod = tag.GetString("mod");
			string name = tag.GetString("name");

			return new UnloadedMaterial(){
				modName = mod,
				itemName = name,
				type = StaticType
			};
		};
	}
}
