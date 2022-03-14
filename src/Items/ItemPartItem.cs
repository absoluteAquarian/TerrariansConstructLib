using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Items {
	/// <summary>
	/// Represents an instance of an item part item
	/// </summary>
	[Autoload(false)]
	public sealed class ItemPartItem : ModItem {
		public override string Texture {
			get {
				StringBuilder asset = new();

				if (part.partID >= 0 && part.partID < PartRegistry.Count) {
					//Modded part ID
					asset.Append(PartRegistry.registeredIDs[part.partID].assetFolder);
				} else
					throw new Exception("Part ID was invalid");

				asset.Append('/');

				asset.Append(part.material.GetName());

				if (!ModContent.HasAsset(asset.ToString())) {
					// Default to the "unknown" asset
					string path = GetUnkownTexturePath(part.partID);

					Mod.Logger.Warn($"Part texture (Material: \"{part.material.GetItemName()}\", Name: \"{PartRegistry.registeredIDs[part.partID].name}\") could not be found." +
						"  Defaulting to Unknown texture path:\n" +
						path);

					return path;
				}

				return asset.ToString();
			}
		}

		public static string GetUnkownTexturePath(int partID)
			=> $"{PartRegistry.registeredIDs[partID].assetFolder}/{PartRegistry.registeredIDs[partID].internalName}/Unknown";

		/// <summary>
		/// The information for the item part
		/// </summary>
		public ItemPart part;

		internal static Dictionary<int, ItemPart> registeredPartsByItemID;
		internal static PartsDictionary<int> itemPartToItemID;

		//Needed to allow multiple ItemParItem instances to be loaded
		public override string Name {
			get {
				if (part is null)
					part = registeredPartsByItemID[Type];

				return $"ItemPart_{part.material.GetModName()}_{part.material.GetName()}_{PartRegistry.registeredIDs[part.partID].internalName}";
			}
		}

		public ItemPartItem(ItemPart part) {
			this.part = part;
		}

		public static ItemPartItem Create(Material material, int partID, ItemPartActionsBuilder builder, string tooltip, string modifierText) {
			int materialType = material.type;

			if (!PartActions.builders.TryGetValue(materialType, out var buildersByPartID))
				buildersByPartID = PartActions.builders[materialType] = new();

			if (buildersByPartID.ContainsKey(partID))
				throw new ArgumentException($"The part type \"{PartRegistry.registeredIDs[partID].name}\" has already been assigned to the material type \"{material.GetItemName()}\" (ID: {materialType})");

			buildersByPartID[partID] = builder;

			return new ItemPartItem(new ItemPart() {
				material = material,
				partID = partID,
				tooltip = tooltip,
				modifierText = modifierText
			});
		}

		public override bool CanStack(Item item2)
			=> item2.ModItem is ItemPartItem pItem
				&& part.material.type == pItem.part.material.type && part.partID == pItem.part.partID;

		public override bool CanStackInWorld(Item item2)
			=> CanStack(item2);

		public override ModItem Clone(Item item) {
			ItemPartItem source = item.ModItem as ItemPartItem;
			
			return new ItemPartItem(source.part);
		}

		public override void SetStaticDefaults() {
			part = registeredPartsByItemID[Type];

			// TODO: localization handling
			string name = part.material.GetItemName();
			if (name.EndsWith(" Bar"))
				name = name.AsSpan()[..^4].ToString();

			DisplayName.SetDefault(name + " " + PartRegistry.registeredIDs[part.partID].name);

			if (part.tooltip is not null)
				Tooltip.SetDefault(part.tooltip);
		}

		public override void SetDefaults() {
			part = registeredPartsByItemID[Type];

			Item.DamageType = DamageClass.NoScaling;
			Item.rare = part.material.rarity;
		}

		public override void SaveData(TagCompound tag) {
			tag["part"] = part;
		}

		public override void LoadData(TagCompound tag) {
			part = tag.Get<ItemPart>("part");
		}
	}
}
