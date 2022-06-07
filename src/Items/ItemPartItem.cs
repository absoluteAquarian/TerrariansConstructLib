using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Numbers;
using TerrariansConstructLib.API.Stats;
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

				string asset = $"{partData.mod.Name}/{partData.assetFolder}/{part.material.GetName()}";

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

			return $"{data.mod.Name}/{data.assetFolder}/Unknown";
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

		public static ItemPartItem Create(Material material, int partID) {
			return new ItemPartItem(new ItemPart() {
				material = material,
				partID = partID
			});
		}

		public override bool CanStack(Item item2)
			=> item2.ModItem is ItemPartItem pItem
				&& part.material.Type == pItem.part.material.Type && part.partID == pItem.part.partID;

		public override bool CanStackInWorld(Item item2)
			=> CanStack(item2);

		protected override bool CloneNewInstances => true;

		public override ModItem Clone(Item item) {
			ModItem clone = base.Clone(item);
			(clone as ItemPartItem)!.part = part.Clone();
			return clone;
		}

		public override void SetStaticDefaults() {
			part = registeredPartsByItemID[Type];

			// TODO: localization handling
			string name = part.material.GetItemName();
			if (name.EndsWith(" Bar"))
				name = name.AsSpan()[..^4].ToString();
			else if (name.EndsWith(" Block"))
				name = name.AsSpan()[..^6].ToString();

			DisplayName.SetDefault(name + " " + PartRegistry.registeredIDs[part.partID].name);

			Tooltip.SetDefault("<TOOLTIP>\n"
				+ "<STATS>");
		}

		public override void SetDefaults() {
			part = registeredPartsByItemID[Type];

			Item? materialItem = part.material.AsItem();

			Item.DamageType = DamageClass.Default;
			Item.rare = materialItem?.rare ?? ItemRarityID.White;  //Unknown and Unloaded materials return null for the item
		}

		public override void AddRecipes() {
			var part = registeredPartsByItemID[Type];

			if (part.material is UnloadedMaterial or UnknownMaterial)
				return;  //No recipe

			var partData = PartRegistry.registeredIDs[part.partID];

			// TODO: forge tile?

			if (PartMold.TryGetMold(part.partID, true, false, out var simpleMold))
				AddRecipeFromMold(part, partData, simpleMold);
			if (PartMold.TryGetMold(part.partID, false, false, out var complexMold))
				AddRecipeFromMold(part, partData, complexMold);
			if (PartMold.TryGetMold(part.partID, false, true, out var complexPlatinumMold))
				AddRecipeFromMold(part, partData, complexPlatinumMold);
		}

		private void AddRecipeFromMold(ItemPart part, PartRegistry.Data partData, PartMold? mold) {
			NetworkText text = NetworkText.FromLiteral("Crafted in the Forge UI");

			int materialWorth = Material.worthByMaterialID[part.material.Type];
			int totalCostTimesTwo = materialWorth * partData.materialCost;
			
			if (part.partID == CoreLibMod.RegisteredParts.Shard) {
				//Only add a recipe if the material has a Shard part registered
				if (itemPartToItemID.TryGet(part.material, part.partID, out _)) {
					(materialWorth % 2 != 0 ? CreateRecipe(2) : CreateRecipe(1))
						.AddIngredient(part.material.Type, materialWorth % 2 != 0 ? materialWorth : materialWorth / 2)
						.AddIngredient(mold)
						.AddCondition(text, r => false)
						.Register();
				}

				return;
			}

			var recipe = CreateRecipe()
				.AddIngredient(part.material.Type, totalCostTimesTwo / 2);
			if (totalCostTimesTwo % 2 != 0)
				recipe.AddIngredient(CoreLibMod.GetItemPartItemType(part.material, CoreLibMod.RegisteredParts.Shard), 1);

			recipe.AddIngredient(mold)
				.AddCondition(text, r => false)
				.Register();

			if (totalCostTimesTwo % 2 != 0) {
				//Add another recipe for an additional material item
				CreateRecipe()
					.AddIngredient(part.material.Type, totalCostTimesTwo / 2 + 1)
					.AddIngredient(mold)
					.AddCondition(text, r => false)
					.Register();
			}
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			StatType type = PartRegistry.registeredIDs[part.partID].type;

			string? tooltip = CoreLibMod.GetMaterialTooltip(part.material);

			if (tooltip is not null) {
				if (tooltip.Contains("{R}"))
					tooltip = tooltip.Replace("{R}", Roman.Convert(1));

				Utility.FindAndModify(tooltips, "<TOOLTIP>", tooltip);
			} else
				Utility.FindAndRemoveLine(tooltips, "<TOOLTIP>");

			string? lines = part.material.GetStat(type)?.GetTooltipLines(part.partID);

			if (lines is not null)
				Utility.FindAndInsertLines(Mod, tooltips, "<STATS>", i => "PartStat_" + i, lines);
			else
				Utility.FindAndRemoveLine(tooltips, "<STATS>");
		}

		public override void SaveData(TagCompound tag) {
			tag["part"] = part;
		}

		public override void LoadData(TagCompound tag) {
			part = tag.Get<ItemPart>("part");
		}

		public override void NetSend(BinaryWriter writer) {
			part.NetSend(writer);
		}

		public override void NetReceive(BinaryReader reader) {
			part.NetReceive(reader);
		}
	}
}
