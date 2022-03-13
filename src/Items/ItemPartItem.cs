using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.ID;
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

				if (part.partID >= 0 && part.partID < MaterialPartID.TotalCount) {
					//Modded part ID
					asset.Append(MaterialPartID.registeredIDsToAssetFolders[part.partID]);
				} else
					throw new Exception("Part ID was invalid");

				asset.Append('/');

				asset.Append(part.material.GetName());

				if (!ModContent.HasAsset(asset.ToString())) {
					// Default to the "unknown" asset
					string path = $"{MaterialPartID.registeredIDsToAssetFolders[part.partID]}/{MaterialPartID.registeredIDsToInternalNames[part.partID]}/Unknown";

					Mod.Logger.Warn($"Part texture (Material: \"{part.material.GetItemName()}\", Name: \"{MaterialPartID.registeredIDsToNames[part.partID]}\") could not be found." +
						"  Defaulting to Unknown texture path:\n" +
						path);

					return path;
				}

				return asset.ToString();
			}
		}

		/// <summary>
		/// The information for the item part
		/// </summary>
		public ItemPart part;

		internal static Dictionary<int, ItemPart> registeredPartsByItemID;

		//Needed to allow multiple ItemParItem instances to be loaded
		public override string Name {
			get {
				if (part is null)
					part = registeredPartsByItemID[Type];

				return $"ItemPart_{part.material.GetModName()}_{part.material.GetName()}_{MaterialPartID.registeredIDsToInternalNames[part.partID]}";
			}
		}

		public ItemPartItem(ItemPart part) {
			this.part = part;
		}

		public static ItemPartItem Create(Material material, int partID, ItemPartActionsBuilder builder, string tooltip) {
			int materialType = material.type;

			if (!PartActions.builders.TryGetValue(materialType, out var buildersByPartID))
				buildersByPartID = PartActions.builders[materialType] = new();

			if (buildersByPartID.ContainsKey(partID))
				throw new ArgumentException($"The part type \"{MaterialPartID.registeredIDsToNames[partID]}\" has already been assigned to the material type \"{material.GetItemName()}\" (ID: {materialType})");

			buildersByPartID[partID] = builder;

			return new ItemPartItem(new ItemPart() {
				material = material,
				partID = partID,
				tooltip = tooltip
			});
		}

		public override bool CanStack(Item item2)
			=> item2.ModItem is ItemPartItem pItem
				&& part.material.type == pItem.part.material.type && part.partID == pItem.part.partID;

		public override void SetStaticDefaults() {
			part = registeredPartsByItemID[Type];

			// TODO: localization handling
			string name = part.material.GetItemName();
			if (name.EndsWith(" Bar"))
				name = name.AsSpan()[..^4].ToString();

			DisplayName.SetDefault(name + " " + MaterialPartID.registeredIDsToNames[part.partID]);

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
