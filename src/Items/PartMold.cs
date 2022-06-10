using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;

namespace TerrariansConstructLib.Items {
	/// <summary>
	/// Represents an instance of an item part mold
	/// </summary>
	[Autoload(false)]
	public sealed class PartMold : ModItem {
		public int partID;

		internal bool isSimpleMold;
		internal bool isPlatinumMold;

		internal static Dictionary<int, PartMold> registeredMolds;
		internal static Dictionary<int, Data> moldsByPartID;

		public static PartMold Create(int partID, bool isSimpleMold, bool isPlatinumMold) {
			if (partID < 0 || partID >= PartDefinitionLoader.Count)
				throw new ArgumentException("Part ID was invalid");

			if (!moldsByPartID.TryGetValue(partID, out var data))
				data = moldsByPartID[partID] = new();

			var mold = new PartMold(){
				partID = partID,
				isSimpleMold = isSimpleMold,
				isPlatinumMold = isPlatinumMold
			};

			if (isSimpleMold)
				data.simple = mold;
			else if (!isPlatinumMold)
				data.complex = mold;
			else
				data.complexPlatinum = mold;

			return mold;
		}

		public static bool TryGetMold(int partID, bool getSimpleMold, bool getPlatinumVariantForComplexMold, out PartMold? mold) {
			mold = null;

			if (partID < 0 || partID >= PartDefinitionLoader.Count)
				throw new ArgumentException("Part ID was invalid");
			
			var data = moldsByPartID[partID];

			if (getSimpleMold) {
				mold = data.simple;
				return true;
			} else if(data.complex is not null && !getPlatinumVariantForComplexMold) {
				mold = data.complex;
				return true;
			} else if(data.complexPlatinum is not null && getPlatinumVariantForComplexMold) {
				mold = data.complexPlatinum;
				return true;
			}

			return false;
		}

		public override string Texture {
			get {
				if (partID < 0 || partID >= PartDefinitionLoader.Count)
					throw new ArgumentException("Part ID was invalid");

				var mold = registeredMolds[Type];
				var partData = PartDefinitionLoader.Get(mold.partID)!;

				return $"{mold.Mod.Name}/{partData.RelativeAssetFolder}/Molds/{(mold.isSimpleMold ? "Simple" : !mold.isPlatinumMold ? "Complex" : "ComplexPlatinum")}";
			}
		}

		public override string Name  {
			get {
				if (partID < 0 || partID >= PartDefinitionLoader.Count)
					throw new ArgumentException("Part ID was invalid");

				var partData = PartDefinitionLoader.Get(partID)!;

				return $"PartMold_{partData.Mod.Name}_{partData.Name}_{(isSimpleMold ? "Simple" : !isPlatinumMold ? "Complex" : "ComplexPlatinum")}";
			}
		}

		public override void SetStaticDefaults() {
			PartMold mold = registeredMolds[Type];

			DisplayName.SetDefault(PartDefinitionLoader.Get(mold.partID)!.DisplayName + " Mold");
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

			var part = PartDefinitionLoader.Get(mold.partID)!;

			Utility.FindAndModify(tooltips, "<MATERIAL_COST>", $"{part.MaterialCost / 2f}");
		}

		public override void AddRecipes() {
			PartMold mold = registeredMolds[Type];

			var recipe = CreateRecipe();

			if (mold.isSimpleMold)
				recipe.AddRecipeGroup(RecipeGroupID.Wood, 20);
			else if (!mold.isPlatinumMold)
				recipe.AddIngredient(ItemID.GoldBar, 8);
			else
				recipe.AddIngredient(ItemID.PlatinumBar, 8);

			recipe.AddTile(isSimpleMold ? TileID.WorkBenches : TileID.Anvils);

			recipe.Register();
		}

		public override void OnCraft(Recipe recipe) {
			if (recipe.HasRecipeGroup(RecipeGroupID.Wood)) {
				isSimpleMold = true;
				isPlatinumMold = false;
			} else if(recipe.HasIngredient(ItemID.GoldBar)) {
				isSimpleMold = false;
				isPlatinumMold = false;
			} else {
				isSimpleMold = false;
				isPlatinumMold = true;
			}
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(partID);

			BitsByte bb = new(isSimpleMold, isPlatinumMold);

			writer.Write(bb);
		}

		public override void NetReceive(BinaryReader reader) {
			partID = reader.ReadInt32();

			BitsByte bb = reader.ReadByte();

			bb.Retrieve(ref isSimpleMold, ref isPlatinumMold);
		}

		public override void SaveData(TagCompound tag) {
			tag["id"] = partID;
			tag["flags"] = (byte)new BitsByte(isSimpleMold, isPlatinumMold);
		}

		public override void LoadData(TagCompound tag) {
			if (tag.ContainsKey("id"))
				partID = tag.GetInt("id");

			if (tag.ContainsKey("flags")) {
				BitsByte bb = tag.GetByte("flags");

				bb.Retrieve(ref isSimpleMold, ref isPlatinumMold);
			}
		}

		internal class Data {
			public PartMold? simple;
			public PartMold complex;
			public PartMold complexPlatinum;
		}
	}
}
