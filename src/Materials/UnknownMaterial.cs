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
			type = type
		};

		public override TagCompound SerializeData()
			=> new();

		public static new Func<TagCompound, UnknownMaterial> DESERIALIZER = tag => new UnknownMaterial(){ type = StaticType };
	}
}
