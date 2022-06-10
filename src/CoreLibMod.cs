using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Commands;
using TerrariansConstructLib.API.Definitions;
using TerrariansConstructLib.API.Edits;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.Default;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Modifiers;
using TerrariansConstructLib.Stats;

namespace TerrariansConstructLib {
	public class CoreLibMod : Mod {
		public static ModKeybind ActivateAbility;

		public static CoreLibMod Instance => ModContent.GetInstance<CoreLibMod>();

		internal static event Action UnloadReflection;

		public static CachedItemTexturesDictionary ItemTextures { get; internal set; }

		internal static CoreLibMod directDetourInstance;

		public CoreLibMod() {
			directDetourInstance = this;

			DirectDetourManager.ModCtorLoad();
		}

		private static FieldInfo Interface_loadMods;
		private static MethodInfo UIProgress_set_SubProgressText;

		public static string ProgressText_FinishResourceLoading => Language.GetTextValue("tModLoader.MSFinishingResourceLoading");

		public static void SetLoadingSubProgressText(string text)
			=> UIProgress_set_SubProgressText.Invoke(Interface_loadMods.GetValue(null), new object[] { text });

		internal static StreamWriter writer;

		public override void Load() {
			if (!ModLoader.HasMod("TerrariansConstruct"))
				throw new Exception(Language.GetTextValue("tModLoader.LoadErrorDependencyMissing", "TerrariansConstruct", Name));

			Interface_loadMods = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.Interface")!.GetField("loadMods", BindingFlags.NonPublic | BindingFlags.Static)!;
			UIProgress_set_SubProgressText = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.UIProgress")!.GetProperty("SubProgressText", BindingFlags.Public | BindingFlags.Instance)!.GetSetMethod()!;

			Utility.LocalizationLoader_AutoloadTranslations = typeof(LocalizationLoader).GetMethod("AutoloadTranslations", BindingFlags.NonPublic | BindingFlags.Static)!;
			Utility.LocalizationLoader_SetLocalizedText = typeof(LocalizationLoader).GetMethod("SetLocalizedText", BindingFlags.NonPublic | BindingFlags.Static)!;
			Utility.LanguageManager__localizedTexts = typeof(LanguageManager).GetField("_localizedTexts", BindingFlags.NonPublic | BindingFlags.Instance)!;

			Utility.ForceLoadModHJsonLocalization(this);

			ItemPart.partData = new();
			ItemPartItem.registeredPartsByItemID = new();
			ItemPartItem.itemPartToItemID = new();
			PartMold.moldsByPartID = new();
			PartMold.registeredMolds = new();

			ItemStatCollection.Load();

			ItemTextures = new();

			if (!Main.dedServ) {
				ActivateAbility = KeybindLoader.RegisterKeybind(this, "Activate Tool Ability", Keys.G);
			}

			string path = Program.SavePath;
			path = Path.Combine(path, "aA Mods", "TerrariansConstructLib");
			Directory.CreateDirectory(path);

			string logFile = Path.Combine(path, "logs.txt");
			writer = new(File.Open(logFile, FileMode.Create));

			Logger.Info("Logging information to:  " + logFile);

			writer.WriteLine($"Date: {DateTime.Now:d}");
			writer.WriteLine($"============================");

			writer.Flush();

			SetLoadingSubProgressText(Language.GetTextValue("Mods.TerrariansConstructLib.Loading.FindingDependents"));

			dependents = new(FindDependents());

			//Unused, but they're needed for displaying the parts in the Forge UI
			for (ColorMaterialType c = ColorMaterialType.Red; c < ColorMaterialType.Count; c++)
				AddContent(new ColorMaterialDefinition(c));

			EditsLoader.Load();

			DirectDetourManager.Load();

			SetLoadingSubProgressText(ProgressText_FinishResourceLoading);
		}

		internal static void CheckItemDefinitions() {
			SetLoadingSubProgressText(nameof(TerrariansConstructLib) + "::" + nameof(CheckItemDefinitions));

			for (int itemID = 0; itemID < ItemDefinitionLoader.Count; itemID++) {
				var definition = ItemDefinitionLoader.Get(itemID)!;

				var parts = definition.GetValidPartIDs().ToList();

				for (int check = itemID + 1; check < ItemDefinitionLoader.Count; check++) {
					var checkDefinition = ItemDefinitionLoader.Get(check)!;

					var checkParts = checkDefinition.GetValidPartIDs().ToList();

					if (parts.SequenceEqual(checkParts)) {
						throw new Exception($"Unable to add the weapon entry \"{definition.Mod.Name}:{definition.Name}\"\n" +
							$"The weapon entry \"{checkDefinition.Mod.Name}:{checkDefinition.Name}\" already contains the wanted part sequence:\n" +
							$"   {string.Join(", ", checkParts.Select(PartDefinitionLoader.GetIdentifier))}");
					}
				}
			}

			for (int modifierID = 0; modifierID < ModifierLoader.Count; modifierID++) {
				var modifier = ModifierLoader.Get(modifierID)!;

				writer.WriteLine($"Modifier \"{modifier.Name}\" added by {modifier.Mod.Name}");
				writer.Flush();
			}

			for (int materialID = 0; materialID < MaterialDefinitionLoader.Count; materialID++) {
				var material = MaterialDefinitionLoader.Get(materialID)!;
				Material? copy = material.Material;

				if (copy is null)
					continue;

				writer.WriteLine($"Material definition for material \"{copy.GetIdentifier()}\" added by {material.Mod.Name}");
			}

			SetLoadingSubProgressText("");
		}

		internal static void AddPartItems() {
			SetLoadingSubProgressText(nameof(TerrariansConstructLib) + "::" + nameof(AddPartItems));

			for (int partID = 0; partID < PartDefinitionLoader.Count; partID++) {
				var partDefinition = PartDefinitionLoader.Get(partID)!;

				writer.WriteLine($"Item Part \"{partDefinition.Name}\" (ID: {partDefinition.Type}) added by {partDefinition.Mod.Name}");

				if (partDefinition.ToolType > ToolType.None)
					writer.WriteLine($"  Tool flags: {partDefinition.ToolType}");

				for (int materialID = 0; materialID < MaterialDefinitionLoader.Count; materialID++) {
					var materialDefinition = MaterialDefinitionLoader.Get(materialID)!;

					if (!materialDefinition.ValidParts.Any(p => p.Type == partDefinition.Type))
						continue;

					var material = materialDefinition.MaterialOrUnloaded;

					//Color material parts do not have items for them... only visuals
					if (material is ColorMaterial)
						continue;

					ItemPartItem item = ItemPartItem.Create(material, partID);

					ReflectionHelper<Mod>.InvokeSetterFunction("loading", partDefinition.Mod, true);

					partDefinition.Mod.AddContent(item);

					ReflectionHelper<Mod>.InvokeSetterFunction("loading", partDefinition.Mod, false);

					//ModItem.Type is only set after Mod.AddContent is called and the item is actually registered
					if (item.Type > 0) {
						ItemPartItem.registeredPartsByItemID[item.Type] = item.part;
						ItemPartItem.itemPartToItemID.Set(material, partID, item.Type);
						ItemPart.partData.Set(material, partID, item.part);

						writer.WriteLine($"Added item part \"{item.Name}\" (ID: {item.Type})");
						writer.Flush();
					}
				}

				writer.Flush();
			}

			for (int materialID = 0; materialID < MaterialDefinitionLoader.Count; materialID++) {
				var data = MaterialDefinitionLoader.Get(materialID)!;
				Material? copy = data.Material;

				if (copy is null)
					continue;

				writer.WriteLine($"Stats for material \"{copy.GetIdentifier()}\" was registered with the following part types:\n"
					+ "  " + string.Join(", ", data.GetMaterialStats().Select(s => s.ToString())));
				writer.Flush();
			}

			SetLoadingSubProgressText("");
		}

		internal static void AddMoldItems() {
			SetLoadingSubProgressText(nameof(TerrariansConstructLib) + "::" + nameof(AddMoldItems));

			for (int partID = 0; partID < PartDefinitionLoader.Count; partID++) {
				var definition = PartDefinitionLoader.Get(partID);

				if (definition is null)
					continue;

				PartMold? simpleMold = !definition.HasWoodMold ? null : PartMold.Create(partID, true, false);
				PartMold complexMold = PartMold.Create(partID, false, false);
				PartMold complexPlatinumMold = PartMold.Create(partID, false, true);

				complexPlatinumMold.isPlatinumMold = true;

				ReflectionHelper<Mod>.InvokeSetterFunction("loading", definition.Mod, true);

				if (simpleMold is not null)
					definition.Mod.AddContent(simpleMold);

				definition.Mod.AddContent(complexMold);
				definition.Mod.AddContent(complexPlatinumMold);

				ReflectionHelper<Mod>.InvokeSetterFunction("loading", definition.Mod, false);

				if (simpleMold is not null)
					PartMold.registeredMolds[simpleMold.Type] = simpleMold;

				PartMold.registeredMolds[complexMold.Type] = complexMold;
				PartMold.registeredMolds[complexPlatinumMold.Type] = complexPlatinumMold;

				PartMold.moldsByPartID[partID] = new() { simple = simpleMold, complex = complexMold, complexPlatinum = complexPlatinumMold };

				writer.WriteLine($"{(definition.HasWoodMold ? "Simple and complex item part molds" : "Complex item part molds")} for part ID \"{definition.Mod.Name}:{definition.Name}\" added by mod \"{definition.Mod.Name}\"");
				writer.Flush();
			}

			SetLoadingSubProgressText("");
		}

		private static readonly Type BuildProperties = typeof(Mod).Assembly.GetType("Terraria.ModLoader.Core.BuildProperties")!;
		private static readonly MethodInfo BuildProperties_ReadModFile = BuildProperties.GetMethod("ReadModFile", BindingFlags.NonPublic | BindingFlags.Static)!;
		private static readonly MethodInfo BuildProperties_RefNames = BuildProperties.GetMethod("RefNames", BindingFlags.Public | BindingFlags.Instance)!;

		private static List<Mod> dependents;
		internal static IEnumerable<Mod> Dependents => dependents;

		private static IEnumerable<Mod> FindDependents() {
			static IEnumerable<string> GetReferences(Mod mod) {
				TmodFile modFile = ReflectionHelperReturn<Mod, TmodFile>.InvokeMethod("get_File", mod);

				using (modFile.Open()) {
					object properties = BuildProperties_ReadModFile.Invoke(null, new object[] { modFile })!;
					return (BuildProperties_RefNames.Invoke(properties, new object[] { true }) as IEnumerable<string>)!;
				}
			}

			//Skip the ModLoaderMod entry
			foreach (Mod mod in ModLoader.Mods[1..]) {
				string[] dependencies = GetReferences(mod).ToArray();

				if (Array.IndexOf(dependencies, nameof(TerrariansConstructLib)) > -1)
					yield return mod;
			}
		}

		public override void AddRecipeGroups() {
			//Make a recipe group for each part type
			for (int i = 0; i < PartDefinitionLoader.Count; i++) {
				// OrderBy ensures that the Unkonwn material ends up first in the list due to its type being the smallest
				int[] ids = GetKnownMaterials()
					.Where(m => ItemPartItem.itemPartToItemID.Has(m, i))
					.Select(m => GetItemPartItem(m, i))
					.OrderBy(i => i.part.material.Type)
					.Select(i => i.Type)
					.ToArray();

				if (ids.Length == 0)
					continue;  //Failsafe for if a part ID has no parts registered to it

				RegisterRecipeGroup(GetRecipeGroupName(i), PartDefinitionLoader.Get(i)!.Name, ids);
			}
		}

		public static void RegisterRecipeGroup(string groupName, string anyName, params int[] validTypes)
			=> RecipeGroup.RegisterGroup(groupName, new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {anyName}", validTypes));

		public static void RegisterRecipeGroup(string groupName, int itemForAnyName, params int[] validTypes)
			=> RecipeGroup.RegisterGroup(groupName, new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(itemForAnyName)}", validTypes));

		public static string GetRecipeGroupName(int partID) {
			if (partID < 0 || partID >= PartDefinitionLoader.Count)
				throw new ArgumentException("Part ID was invalid");

			return "TerrariansConstructLib:PartGroup_" + PartDefinitionLoader.Get(partID)!.Name;
		}

		public override void Unload() {
			Interface_loadMods = null!;
			UIProgress_set_SubProgressText = null!;

			Utility.LocalizationLoader_AutoloadTranslations = null!;
			Utility.LocalizationLoader_SetLocalizedText = null!;
			Utility.LanguageManager__localizedTexts = null!;

			DirectDetourManager.Unload();

			MaterialDefinitionLoader.Unload();
			ItemDefinitionLoader.Unload();
			PartDefinitionLoader.Unload();
			ModifierLoader.Unload();

			ItemPart.partData = null!;
			ItemPartItem.registeredPartsByItemID = null!;
			ItemPartItem.itemPartToItemID = null!;
			PartMold.moldsByPartID = null!;
			PartMold.registeredMolds = null!;

			ItemStatCollection.Unload();

			ItemTextures?.Clear();
			ItemTextures = null!;

			Interlocked.Exchange(ref UnloadReflection!, null)?.Invoke();

			Interface_loadMods = null!;
			UIProgress_set_SubProgressText = null!;

			writer?.Dispose();
			writer = null!;

			directDetourInstance = null!;
		}

		/// <summary>
		/// Gets an enumeration of <seealso cref="Material"/> instances that were used to create item parts
		/// </summary>
		public static IEnumerable<Material> GetKnownMaterials()
			=> new ListUsedMaterials().GetRegistry().Values;

		/// <summary>
		/// Gets the global tooltip of an <seealso cref="ItemPart"/>
		/// </summary>
		/// <param name="material">The material</param>
		/// <returns>The global tooltip</returns>
		public static string? GetMaterialTooltip(Material material) {
			var modifier = MaterialDefinitionLoader.Find(material)?.TraitOrUnloaded ?? new UnloadedTrait();

			return $"[c/{modifier.TooltipColor.Hex3()}:{Language.GetTextValue(modifier.LangKey)}]";
		}

		public static int PartType<T>() where T : PartDefinition => ModContent.GetInstance<T>()?.Type ?? -1;

		public static PartDefinition? GetPartDefinition<T>() where T : PartDefinition => PartDefinitionLoader.Get(PartType<T>());

		public static PartDefinition? GetPartDefinition(int index) => PartDefinitionLoader.Get(index);

		public static int ItemType<T>() where T : TCItemDefinition => ModContent.GetInstance<T>()?.Type ?? -1;

		public static TCItemDefinition? GetItemDefinition<T>() where T : TCItemDefinition => ItemDefinitionLoader.Get(ItemType<T>());

		public static TCItemDefinition? GetItemDefinition(int index) => ItemDefinitionLoader.Get(index);

		public static int MaterialType<T>() where T : MaterialDefinition => ModContent.GetInstance<T>()?.Type ?? -1;

		public static MaterialDefinition? GetMaterialDefinition<T>() where T : MaterialDefinition => MaterialDefinitionLoader.Get(MaterialType<T>());

		public static MaterialDefinition? GetMaterialDefinition(Material material) => MaterialDefinitionLoader.Get(MaterialType(material));

		/// <summary>
		/// Attempts to find the corresponding <see cref="MaterialDefinition"/> ID for the input <see cref="Material"/>, <paramref name="material"/>
		/// </summary>
		/// <param name="material">The material instance</param>
		/// <returns>The <see cref="MaterialDefinition.Type"/> if successful, <c>-1</c> otherwise</returns>
		public static int MaterialType(Material material) => MaterialDefinitionLoader.Find(material)?.Type ?? -1;

		/// <summary>
		/// Converts a registered Terrarians' Contruct item ID into its tModLoader item ID
		/// </summary>
		/// <param name="registeredItemID">The registered item ID</param>
		/// <returns>The tModLoader item ID</returns>
		/// <exception cref="Exception"/>
		public static int GetItemType(int registeredItemID) {
			if (registeredItemID < 0 || registeredItemID >= ItemDefinitionLoader.Count)
				throw new Exception($"A registered item with ID {registeredItemID} does not exist");

			var data = ItemDefinitionLoader.Get(registeredItemID)!;

			return data.ItemType;
		}

		/// <summary>
		/// Gets an <seealso cref="ItemPart"/> instance clone from a material and part ID
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		/// <returns>The <seealso cref="ItemPart"/> instance</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentException"/>
		public static ItemPart GetItemPart(Material material, int partID)
			=> ItemPart.partData.Get(material, partID).Clone();

		/// <summary>
		/// Gets an <seealso cref="ItemPartItem"/> item type from a material and part ID
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		/// <returns>The <seealso cref="ItemPartItem"/> item type</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentException"/>
		public static int GetItemPartItemType(Material material, int partID)
			=> ItemPartItem.itemPartToItemID.Get(material, partID);

		/// <summary>
		/// Gets an <seealso cref="ItemPartItem"/> item instance (via <seealso cref="ModContent.GetModItem(int)"/>) from a material and part ID
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		/// <returns>The <seealso cref="ItemPartItem"/> item instance</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentException"/>
		public static ItemPartItem GetItemPartItem(Material material, int partID)
			=> (ModContent.GetModItem(GetItemPartItemType(material, partID)) as ItemPartItem)!;

		/// <summary>
		/// Attempts to find a registered item whose valid part IDs are set to the same values as <paramref name="partIDs"/>
		/// </summary>
		/// <param name="partIDs">The part IDs to check</param>
		/// <param name="registeredItemID">A value &gt;= <c>0</c> and &lt; <seealso cref="ItemDefinitionLoader.Count"/> if successful, <c>-1</c> otherwise</param>
		/// <returns><see langword="true"/> if the search was successful, <see langword="false"/> otherwise</returns>
		public static bool TryFindItem(int[] partIDs, out int registeredItemID) {
			registeredItemID = FindItem(partIDs);
			return registeredItemID > -1;
		}

		/// <summary>
		/// Attempts to find a registered item whose valid part IDs are set to the same values as <paramref name="partIDs"/>
		/// </summary>
		/// <param name="partIDs">The part IDs to check</param>
		/// <returns>A value &gt;= <c>0</c> and &lt; <seealso cref="ItemDefinitionLoader.Count"/> if successful, <c>-1</c> otherwise</returns>
		public static int FindItem(int[] partIDs) {
			foreach (var data in ItemDefinitionLoader.items) {
				if (data.GetValidPartIDs().SequenceEqual(partIDs))
					return data.Type;
			}

			return -1;
		}
		
		public static class RegisteredMaterials {
			public static Material Unloaded { get; } = new UnloadedMaterial();

			public static Material Unknown { get; } = new UnknownMaterial();
		}

		public static class KnownStatModifiers {
			public const string HeadDamage = "head.damage";
			public const string HeadKnockback = "head.knockback";
			public const string HeadCrit = "head.crit";
			public const string HeadUseSpeed = "head.useSpeed";
			public const string HeadPickPower = "head.pickPower";
			public const string HeadAxePower = "head.axePower";
			public const string HeadHammerPower = "head.hammerPower";
			public const string HeadDurability = "head.durability";

			public const string HandleMiningSpeed = "handle.miningSpeed";
			public const string HandleAttackSpeed = "handle.attackSpeed";
			public const string HandleAttackDamage = "handle.attackDamage";
			public const string HandleAttackKnockback = "handle.attackKnockback";
			public const string HandleDurability = "handle.durability";

			public const string ExtraDurability = "extra.durability";
			public const string BowDrawSpeed = "extra.bow_draw_speed";
			public const string BowArrowSpeed = "extra.bow_arrow_speed";
		}
	}
}