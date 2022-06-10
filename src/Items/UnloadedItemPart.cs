using System;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.Items {
	internal class UnloadedItemPart : ItemPart {
		public string mod, internalName;

		public override ItemPart Clone() => new UnloadedItemPart(){
			mod = mod,
			internalName = internalName,
			material = material.Clone(),
			partID = partID
		};

		public static new Func<TagCompound, UnloadedItemPart> DESERIALIZER = tag => {
			Material material = tag.Get<Material>("material");
			
			if (material is null)
				material = tag.Get<UnloadedMaterial>("material");

			TagCompound part = tag.GetCompound("part");

			string modName = part.GetString("mod");
			string internalName = part.GetString("name");

			return new UnloadedItemPart() {
				mod = modName,
				internalName = internalName,
				material = material,
				partID = -1
			};
		};
	}
}
