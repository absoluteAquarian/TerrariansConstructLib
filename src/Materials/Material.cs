using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Stats;

namespace TerrariansConstructLib.Materials {
	public class Material : TagSerializable, INetHooks {
		/// <summary>
		/// The ID of the item used as the material
		/// </summary>
		public int Type { get; protected set; } = -1;

		internal static Dictionary<int, IPartStats[]> statsByMaterialID;
		internal static Dictionary<int, int> worthByMaterialID;

		public virtual Material Clone() => new() { Type = Type };

		public virtual string GetModName() {
			if (ItemID.Search.TryGetName(Type, out _))
				return "Terraria";
			else if (ModContent.GetModItem(Type) is ModItem mItem)
				return mItem.Mod.Name;
			throw new Exception("Invalid material type ID");
		}

		public virtual string GetName() {
			if (ItemID.Search.TryGetName(Type, out string materialName))
				return materialName;
			else if (ModContent.GetModItem(Type) is ModItem mItem)
				return mItem.Name;
			throw new Exception("Invalid material type ID");
		}

		/// <summary>
		/// Gets the name for this material
		/// </summary>
		public virtual string GetItemName()
			=> Lang.GetItemNameValue(Type);

		public string GetIdentifier() => GetModName() + ":" + GetName();

		public IPartStats? GetStat(StatType type)
			=> statsByMaterialID[Type].FirstOrDefault(s => s.Type == type);

		public S? GetStat<S>(StatType type) where S : class, IPartStats
			=> statsByMaterialID[Type].FirstOrDefault(s => s.Type == type && s is S) is S s ? s : null;

		/// <summary>
		/// Gets an instance of the item this material references
		/// </summary>
		/// <returns>A new <see cref="Item"/> instance, or <see langword="null"/> if this material is an <seealso cref="UnloadedMaterial"/> or <seealso cref="UnknownMaterial"/></returns>
		public Item? AsItem() => this is UnloadedMaterial or UnknownMaterial ? null : new(Type);

		public static Material FromItem(int type)
			=> type == UnloadedMaterial.StaticType
				? new UnloadedMaterial()
				: type == UnknownMaterial.StaticType
					? new UnknownMaterial()
					: type >= ColorMaterial.StaticBaseType && type < ColorMaterial.StaticBaseType + (int)ColorMaterialType.Count
						? new ColorMaterial((ColorMaterialType)(type - ColorMaterial.StaticBaseType))
						: new Material(){ Type = type };

		public virtual TagCompound SerializeData() {
			TagCompound tag = new();

			if (Type < ItemID.Count) {
				tag["mod"] = "Terraria";
				tag["id"] = Type;
			} else {
				if (ModContent.GetModItem(Type) is not ModItem item)
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
					return null!;
				}

				type = item.Type;
			}

			return new Material(){
				Type = type
			};
		};

		public virtual void NetSend(BinaryWriter writer) {
			writer.Write(Type);
		}

		public virtual void NetReceive(BinaryReader reader) {
			Type = reader.ReadInt32();
		}

		public override bool Equals(object? obj)
			=> obj is Material material && Type == material.Type;

		public override int GetHashCode()
			=> Type.GetHashCode();

		public static bool operator ==(Material left, Material right)
			=> left.Type == right.Type;

		public static bool operator !=(Material left, Material right)
			=> !(left == right);
	}
}
