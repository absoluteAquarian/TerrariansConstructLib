using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Rarities;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Items {
	/// <summary>
	/// Represents an instance of an item part mold
	/// </summary>
	[Autoload(false)]
	public sealed class PartMold : ModItem {
		public int partID;

		internal int partMaterialCost;

		public int moldTier = -1;

		internal Material material;
		internal int materialCost;
		internal ushort craftStation;

		internal string rootFolderForAssets;

		internal static Dictionary<int, PartMold> registeredMolds;
		internal static Dictionary<int, Dictionary<int, Dictionary<int, PartMold>>> moldsByPartIDMaterialAndTier;

		public void SetGlobalMaterialCost(int materialCost)
			=> SetGlobalMaterialCost(material, partID, materialCost);

		public static void SetGlobalMaterialCost(Material material, int partID, int materialCost) {
			if (material is UnloadedMaterial or UnknownMaterial)
				return;

			var moldsDict = moldsByPartIDMaterialAndTier[partID];

			if (!moldsDict.TryGetValue(material.type, out var molds))
				return;

			foreach (var (tier, mold) in molds) {
				if (mold.moldTier < 0 || mold.moldTier >= PartMoldTierRegistry.Count)
					throw new Exception($"Mold tier for part mold \"{PartRegistry.registeredIDs[mold.partID].name + " Mold"}\" was invalid");

				if (tier != mold.moldTier)
					throw new Exception($"Registered part mold \"{PartRegistry.registeredIDs[mold.partID].name + " Mold"}\" was not stored under its tier ID");

				mold.partMaterialCost = materialCost;
			}
		}

		public static PartMold Create(Material craftMaterial, int partID, int moldTier, int craftMaterialCost, ushort craftStation, int partMaterialCost, string rootFolderForAssets) {
			if (craftMaterial is UnloadedMaterial or UnknownMaterial)
				throw new Exception("Invalid material for crafting item part mold");

			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			if (moldTier < 0 || moldTier >= PartMoldTierRegistry.Count)
				throw new ArgumentException("Mold Tier ID was invalid");

			return new PartMold(){
				partID = partID,
				moldTier = moldTier,
				partMaterialCost = partMaterialCost,
				material = craftMaterial,
				materialCost = craftMaterialCost,
				craftStation = craftStation,
				rootFolderForAssets = rootFolderForAssets
			};
		}

		public static bool TryGetMold(Material partMaterial, int moldTier, int partID, out PartMold mold) {
			mold = null;

			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			if (moldTier < 0 || moldTier >= PartMoldTierRegistry.Count)
				throw new ArgumentException("Mold Tier ID was invalid");

			if (!PartMoldTierRegistry.IsValidMaterial(partMaterial, moldTier))
				return false;
			
			return moldsByPartIDMaterialAndTier.TryGetValue(partID, out var partDict) && partDict.TryGetValue(partMaterial.type, out var materialDict) && materialDict.TryGetValue(moldTier, out mold);
		}

		public override string Texture {
			get {
				if (partID < 0 || partID >= PartRegistry.Count)
					throw new ArgumentException("Part ID was invalid");

				if (moldTier < 0 || moldTier >= PartMoldTierRegistry.Count)
					throw new ArgumentException("Mold Tier ID was invalid");

				PartMold mold = registeredMolds[Type];

				return Path.Combine(Mod.Name, rootFolderForAssets, CoreLibMod.GetPartName(partID), "Molds", "Tier_" + PartMoldTierRegistry.registeredIDs[mold.moldTier].internalName + "_Mold");
			}
		}

		public override void SetStaticDefaults() {
			PartMold mold = registeredMolds[Type];

			DisplayName.SetDefault(PartRegistry.registeredIDs[mold.partID].name + " Mold");
			Tooltip.SetDefault("Tier: <MOLD_TIER>\n" +
				"Material cost: <MATERIAL_COST>");
		}

		public override void SetDefaults() {
			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 1;
			Item.rare = CoreLibMod.GetMoldTierRarityType(moldTier);
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			if (moldTier < 0 || moldTier >= PartMoldTierRegistry.Count)
				throw new ArgumentException("Mold Tier ID was invalid");

			var tier = PartMoldTierRegistry.registeredIDs[moldTier];

			Utility.FindAndModify(tooltips, "<MOLD_TIER>", $"[c/{tier.color.Hex3()}:{tier.name}]");

			Utility.FindAndModify(tooltips, "<MATERIAL_COST>", $"{(CoreLibMod.TryGetMoldCost(material, partID, moldTier, out int cost) ? $"{cost / 2f}" : "<invalid>")}");
		}

		public override void AddRecipes() {
			PartMold mold = registeredMolds[Type];

			CreateRecipe()
				.AddIngredient(mold.material.type, mold.materialCost)
				.AddTile(mold.craftStation)
				.Register();
		}
	}
}
