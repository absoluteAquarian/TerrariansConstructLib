using System;
using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Definitions;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.Items {
	public class ItemPart : TagSerializable, INetHooks {
		internal static PartsDictionary<ItemPart> partData;

		public IPartStats? GetStat(StatType type)
			=> material.GetStat(type);

		public T? GetStat<T>(StatType type) where T : class, IPartStats
			=> material.GetStat<T>(type);

		/// <summary>
		/// The material used to create this item part
		/// </summary>
		public Material material;

		/// <summary>
		/// The part type associated with this item part
		/// </summary>
		public int partID;

		public virtual ItemPart Clone() => new(){
			material = material.Clone(),
			partID = partID
		};

		public TagCompound SerializeData() {
			TagCompound tag = new();
			
			tag["material"] = material;

			if (this is UnloadedItemPart u) {
				tag["part"] = new TagCompound() {
					["mod"] = u.mod,
					["name"] = u.internalName
				};

				return tag;
			}

			var data = PartDefinitionLoader.Get(partID)!;

			tag["part"] = new TagCompound() {
				["mod"] = data.Mod.Name,
				["name"] = data.Name
			};

			return tag;
		}

		public static Func<TagCompound, ItemPart> DESERIALIZER = tag => {
			Material material = tag.Get<Material>("material");
			
			if (material is null)
				material = tag.Get<UnloadedMaterial>("material");

			TagCompound part = tag.GetCompound("part");

			string modName = part.GetString("mod");
			string internalName = part.GetString("name");

			if (!ModLoader.TryGetMod(modName, out var mod) || !mod.TryFind<PartDefinition>(internalName, out var data)) {
				// Unloaded part.  Save the mod and name, but nothing else
				return new UnloadedItemPart() {
					mod = modName,
					internalName = internalName,
					partID = -1,
					material = material
				};
			}

			return partData!.Get(material, data.Type).Clone();
		};

		public void NetSend(BinaryWriter writer) {
			writer.Write(material.Type);
			material.NetSend(writer);
			writer.Write(partID);
		}

		public void NetReceive(BinaryReader reader) {
			material = Material.FromItem(reader.ReadInt32());
			material.NetReceive(reader);
			partID = reader.ReadInt32();
		}

		public override bool Equals(object? obj)
			=> obj is ItemPart part && material.Type == part.material.Type && partID == part.partID;

		public override int GetHashCode()
			=> HashCode.Combine(material.Type, partID);

		public static bool operator ==(ItemPart left, ItemPart right)
			=> left.material.Type == right.material.Type && left.partID == right.partID;

		public static bool operator !=(ItemPart left, ItemPart right)
			=> !(left == right);
	}
}
