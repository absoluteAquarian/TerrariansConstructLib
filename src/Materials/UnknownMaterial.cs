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

		public const int StaticType = -100612;

		public UnknownMaterial() {
			Type = StaticType;
		}

		public override Material Clone() => new UnknownMaterial() {
			Type = Type
		};

		public override TagCompound SerializeData()
			=> new();

		public static new Func<TagCompound, UnknownMaterial> DESERIALIZER = tag => new UnknownMaterial() { Type = StaticType };
	}
}
