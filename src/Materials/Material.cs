using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TerrariansConstructLib.Materials {
	public class Material : TagSerializable {
		/// <summary>
		/// The ID of the item used as the material
		/// </summary>
		public int type = -1;

		public virtual Material Clone() => new() { type = type };

		public virtual string GetModName() {
			if (ItemID.Search.TryGetName(type, out _))
				return "Terraria";
			else if (ModContent.GetModItem(type) is ModItem mItem)
				return mItem.Mod.Name;
			throw new Exception("Invalid material type ID");
		}

		public virtual string GetName() {
			if (ItemID.Search.TryGetName(type, out string materialName))
				return materialName;
			else if (ModContent.GetModItem(type) is ModItem mItem)
				return mItem.Name;
			throw new Exception("Invalid material type ID");
		}

		/// <summary>
		/// Gets the name for this material
		/// </summary>
		public virtual string GetItemName()
			=> Lang.GetItemNameValue(type);

		/// <summary>
		/// Gets an instance of the item this material references
		/// </summary>
		/// <returns>A new <see cref="Item"/> instance, or <see langword="null"/> if this material is an <seealso cref="UnloadedMaterial"/> or <seealso cref="UnknownMaterial"/></returns>
		public Item AsItem() => this is UnloadedMaterial or UnknownMaterial ? null : new(type);

		public static Material FromItem(int type)
			=> new(){ type = type };

		public virtual TagCompound SerializeData() {
			TagCompound tag = new();

			if (type < ItemID.Count) {
				tag["mod"] = "Terraria";
				tag["id"] = type;
			} else {
				if (ModContent.GetModItem(type) is not ModItem item)
					throw new Exception("Material item type was invalid");

				tag["mod"] = item.Mod.Name;
				tag["name"] = item.Name;
			}

			return tag;
		}

		public static readonly Func<TagCompound, Material> DESERIALIZER = tag => {
			string mod = tag.GetString("mod");

			int type;
			if (mod == "Terraria")
				type = tag.GetInt("id");
			else {
				string name = tag.GetString("name");

				if (!ModLoader.TryGetMod(mod, out Mod instance) || !instance.TryFind(name, out ModItem item)) {
					//Inform the thing using the material that it was an unloaded material
					return null;
				}

				type = item.Type;
			}

			return new Material(){
				type = type
			};
		};

		public override bool Equals(object obj)
			=> obj is Material material && type == material.type;

		public override int GetHashCode()
			=> type.GetHashCode();

		public static bool operator ==(Material left, Material right)
			=> left.type == right.type;

		public static bool operator !=(Material left, Material right)
			=> !(left == right);
	}
}
