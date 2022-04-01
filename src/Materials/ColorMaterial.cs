using System;
using Terraria.ModLoader.IO;

namespace TerrariansConstructLib.Materials {
	public enum ColorMaterialType {
		Red,
		Blue,
		White,
		Yellow,
		Green,
		Orange,

		Count
	}

	public class ColorMaterial : Material {
		public const int StaticBaseType = -100900;

		public override string GetModName() => "TerrariansConstruct";

		public override string GetItemName()
			=> Type switch {
				StaticBaseType => "ColorMaterialRed",
				StaticBaseType + 1 => "ColorMaterialBlue",
				StaticBaseType + 2 => "ColorMaterialWhite",
				StaticBaseType + 3 => "ColorMaterialYellow",
				StaticBaseType + 4 => "ColorMaterialGreen",
				StaticBaseType + 5 => "ColorMaterialOrange",
				_ => throw new Exception()
			};

		public override string GetName() => GetItemName();

		public ColorMaterialType ColorType => (ColorMaterialType)(Type - StaticBaseType);

		public ColorMaterial(ColorMaterialType type) {
			Type = StaticBaseType + (int)type;
		}

		public override Material Clone() => new ColorMaterial((ColorMaterialType)(Type - StaticBaseType));

		public override TagCompound SerializeData()
			=> new() {
				["type"] = Type - StaticBaseType
			};

		public static new Func<TagCompound, ColorMaterial> DESERIALIZER = tag => {
			ColorMaterialType type = (ColorMaterialType)tag.GetInt("type");
			return new ColorMaterial(type);
		};
	}
}
