using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Items {
	/// <summary>
	/// Represents an instance of an item part mold
	/// </summary>
	[Autoload(false)]
	public sealed class PartMold : ModItem {
		public int partID;

		internal bool isSimpleMold;

		internal static Dictionary<int, PartMold> registeredMolds;
		internal static Dictionary<int, Data> moldsByPartID;

		public void SetMaterialCost(int materialCost)
			=> SetMaterialCost(partID, materialCost);

		public static void SetMaterialCost(int partID, int materialCost) {
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			PartRegistry.registeredIDs[partID].materialCost = materialCost;
		}

		public static int GetMaterialCost(int partID) {
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			return PartRegistry.registeredIDs[partID].materialCost;
		}

		public static PartMold Create(int partID, bool isSimpleMold) {
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			if (!moldsByPartID.TryGetValue(partID, out var data))
				data = moldsByPartID[partID] = new();

			var mold = new PartMold(){
				partID = partID,
				isSimpleMold = isSimpleMold
			};

			if (isSimpleMold)
				data.simple = mold;
			else
				data.complex = mold;

			return mold;
		}

		public static bool TryGetMold(int partID, bool getSimpleMold, out PartMold mold) {
			mold = null;

			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");
			
			var data = moldsByPartID[partID];

			if (getSimpleMold) {
				mold = data.simple;
				return true;
			} else if(data.complex is not null) {
				mold = data.complex;
				return true;
			}

			return false;
		}

		public override string Texture {
			get {
				if (partID < 0 || partID >= PartRegistry.Count)
					throw new ArgumentException("Part ID was invalid");

				var mold = registeredMolds[Type];
				var partData = PartRegistry.registeredIDs[mold.partID];

				return Path.Combine(mold.Mod.Name, partData.assetFolder, "Molds", mold.isSimpleMold ? "Simple" : "Complex");
			}
		}

		public override void SetStaticDefaults() {
			PartMold mold = registeredMolds[Type];

			DisplayName.SetDefault(PartRegistry.registeredIDs[mold.partID].name + " Mold");
			Tooltip.SetDefault("Material cost: <MATERIAL_COST>");
		}

		public override void SetDefaults() {
			var mold = registeredMolds[Type];

			Item.width = 32;
			Item.height = 32;
			Item.maxStack = 1;
			Item.rare = mold.isSimpleMold ? ItemRarityID.Blue : ItemRarityID.Orange;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			var mold = registeredMolds[Type];

			var part = PartRegistry.registeredIDs[mold.partID];

			Utility.FindAndModify(tooltips, "<MATERIAL_COST>", $"{part.materialCost / 2f}");
		}

		public override void AddRecipes() {
			PartMold mold = registeredMolds[Type];

			var recipe = CreateRecipe();

			if (mold.isSimpleMold)
				recipe.AddRecipeGroup(RecipeGroupID.Wood, 20);
			else
				recipe.AddRecipeGroup(CoreLibMod.RecipeGroups.GoldOrPlatinumBars, 8);

			recipe.AddTile(isSimpleMold ? TileID.WorkBenches : TileID.Anvils);

			recipe.Register();
		}

		internal class Data {
			public PartMold simple;
			public PartMold complex;
		}
	}
}
