﻿using System;
using System.Collections.Generic;
using System.IO;
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
				if (part.partID < 0 || part.partID >= PartRegistry.Count)
					throw new ArgumentException("Part ID was invalid");

				var partData = PartRegistry.registeredIDs[part.partID];

				string asset = $"{partData.mod.Name}/{partData.assetFolder}/{partData.internalName}/{part.material.GetName()}";

				if (!ModContent.HasAsset(asset)) {
					// Default to the "unknown" asset
					string path = GetUnkownTexturePath(part.partID);

					Mod.Logger.Warn($"Part texture (Material: \"{part.material.GetItemName()}\", Name: \"{PartRegistry.registeredIDs[part.partID].name}\") could not be found." +
						"  Defaulting to Unknown texture path:\n   " +
						path);

					return path;
				}

				return asset;
			}
		}

		public static string GetUnkownTexturePath(int partID) {
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			var data = PartRegistry.registeredIDs[partID];

			return $"{data.mod.Name}/{data.assetFolder}/{data.internalName}/Unknown";
		}

		/// <summary>
		/// The information for the item part
		/// </summary>
		public ItemPart part;

		internal static Dictionary<int, ItemPart> registeredPartsByItemID;
		internal static PartsDictionary<int> itemPartToItemID;

		//Needed to allow multiple ItemParItem instances to be loaded
		public override string Name {
			get {
				part ??= registeredPartsByItemID[Type];

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
			ModItem clone = base.Clone(item);
			(clone as ItemPartItem).part = part.Clone();
			return clone;
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

		public override void AddRecipes() {
			var part = registeredPartsByItemID[Type];
			PartMold.TryGetMold(part.partID, true, out var simpleMold);
			var partData = PartRegistry.registeredIDs[part.partID];

			// TODO: forge tile?

			AddRecipeFromMold(part, partData, simpleMold);

			if (PartMold.TryGetMold(part.partID, false, out var complexMold))
				AddRecipeFromMold(part, partData, complexMold);
		}

		private void AddRecipeFromMold(ItemPart part, PartRegistry.Data partData, PartMold mold) {
			var recipe = CreateRecipe()
				.AddIngredient(part.material.type, partData.materialCost / 2);
			if (partData.materialCost % 2 != 0)
				recipe.AddIngredient(CoreLibMod.GetItemPartItemType(part.material, CoreLibMod.RegisteredParts.Shard), 1);

			recipe.AddIngredient(mold)
				.Register();

			if (partData.materialCost % 2 != 0) {
				//Add another recipe for an additional material item
				CreateRecipe()
					.AddIngredient(part.material.type, partData.materialCost / 2 + 1)
					.AddIngredient(mold)
					.Register();
			}
		}

		public override void SaveData(TagCompound tag) {
			tag["part"] = part;
		}

		public override void LoadData(TagCompound tag) {
			part = tag.Get<ItemPart>("part");
		}
	}
}
