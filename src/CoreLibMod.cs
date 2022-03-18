using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.Exceptions;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Commands;
using TerrariansConstructLib.API.Edits;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Projectiles;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib {
	public class CoreLibMod : Mod {
		public static ModKeybind ActivateAbility;

		private static bool isLoadingParts = false;

		public static CoreLibMod Instance => ModContent.GetInstance<CoreLibMod>();

		internal static event Action UnloadReflection;

		internal static CachedItemTexturesDictionary itemTextures;

		internal static CoreLibMod directDetourInstance;

		public CoreLibMod() {
			directDetourInstance = this;

			DirectDetourManager.ModCtorLoad();
		}

		public override void Load() {
			if(!ModLoader.HasMod("TerrariansConstruct"))
				throw new Exception(Language.GetTextValue("tModLoader.LoadErrorDependencyMissing", "TerrariansConstruct", Name));

			isLoadingParts = true;

			ConstructedAmmoRegistry.Load();
			PartRegistry.Load();
			ItemRegistry.Load();

			PartActions.builders = new();
			ItemPart.partData = new();
			ItemPartItem.registeredPartsByItemID = new();
			ItemPartItem.itemPartToItemID = new();
			PartMold.moldsByPartID = new();
			PartMold.registeredMolds = new();

			itemTextures = new();

			if (!Main.dedServ) {
				ActivateAbility = KeybindLoader.RegisterKeybind(this, "Activate Tool Ability", Keys.G);
			}

			//In order for all parts/ammos/etc. to be visible by all mods that use the library, we have to do some magic
			RegisteredParts.Shard = RegisterPart(ModLoader.GetMod("TerrariansConstruct"), "ItemCraftLeftover", "Shard", 1, hasComplexMold: true, "Assets/Parts/ItemCraftLeftover");

			LoadAllOfTheThings("RegisterTCItemParts");

			foreach (var (id, data) in PartRegistry.registeredIDs)
				Logger.Debug($"Item Part \"{data.name}\" (ID: {id}) added by {data.mod.Name}");

			LoadAllOfTheThings("RegisterTCAmmunition");

			foreach (var (id, data) in ConstructedAmmoRegistry.registeredIDs)
				Logger.Debug($"Constructed Ammo \"{data.name}\" (ID: {id}) added by {data.mod.Name}");

			LoadAllOfTheThings("RegisterTCItems");

			foreach (var (id, data) in ItemRegistry.registeredIDs)
				Logger.Debug($"Item Definition \"{data.name}\" (ID: {id}) added by {data.mod.Name}\n" +
					$"  -- parts: {string.Join(", ", data.validPartIDs.Select(PartRegistry.IDToIdentifier))}");

			AddMoldItems();

			//This needs to go here
			AddAllPartsOfMaterial(this, new UnloadedMaterial(), PartActions.NoActions, "[c/fc51ff:Unloaded Part]", null);
			AddAllPartsOfMaterial(this, new UnknownMaterial(), PartActions.NoActions, "[c/616161:Unknown Part]", null);

			EditsLoader.Load();

			DirectDetourManager.Load();

			isLoadingParts = false;
		}

		private static void AddMoldItems() {
			for (int partID = 0; partID < PartRegistry.Count; partID++) {
				var partData = PartRegistry.registeredIDs[partID];

				PartMold simpleMold = PartMold.Create(partID, true);
				PartMold complexMold = !partData.hasComplexMold ? null : PartMold.Create(partID, false);
				PartMold complexPlatinumMold = complexMold is null ? null : PartMold.Create(partID, false);

				if (complexPlatinumMold is not null)
					complexPlatinumMold.isPlatinumMold = true;

				ReflectionHelper<Mod>.InvokeSetterFunction("loading", partData.mod, true);

				partData.mod.AddContent(simpleMold);
				if (complexMold is not null)
					partData.mod.AddContent(complexMold);
				if (complexPlatinumMold is not null)
					partData.mod.AddContent(complexPlatinumMold);

				ReflectionHelper<Mod>.InvokeSetterFunction("loading", partData.mod, false);

				PartMold.registeredMolds[simpleMold.Type] = simpleMold;
				if (complexMold is not null)
					PartMold.registeredMolds[complexMold.Type] = complexMold;
				if (complexPlatinumMold is not null)
					PartMold.registeredMolds[complexPlatinumMold.Type] = complexPlatinumMold;

				PartMold.moldsByPartID[partID] = new() { simple = simpleMold, complex = complexMold, complexPlatinum = complexPlatinumMold };

				Instance.Logger.Info($"{(partData.hasComplexMold ? "Simple and complex item part molds" : "Simple item part mold")} for part ID \"{partData.mod.Name}:{partData.internalName}\" added by mod \"{partData.mod.Name}\"");
			}
		}

		private static readonly Type BuildProperties = typeof(Mod).Assembly.GetType("Terraria.ModLoader.Core.BuildProperties");
		private static readonly MethodInfo BuildProperties_ReadModFile = BuildProperties.GetMethod("ReadModFile", BindingFlags.NonPublic | BindingFlags.Static);
		private static readonly MethodInfo BuildProperties_RefNames = BuildProperties.GetMethod("RefNames", BindingFlags.Public | BindingFlags.Instance);

		private static IEnumerable<Mod> FindDependents() {
			static IEnumerable<string> GetReferences(Mod mod) {
				TmodFile modFile = ReflectionHelperReturn<Mod, TmodFile>.InvokeMethod("get_File", mod);

				using (modFile.Open()) {
					object properties = BuildProperties_ReadModFile.Invoke(null, new object[]{ modFile });
					return BuildProperties_RefNames.Invoke(properties, new object[]{ true }) as IEnumerable<string>;
				}
			}

			//Skip the ModLoaderMod entry
			foreach (Mod mod in ModLoader.Mods[1..]) {
				string[] dependencies = GetReferences(mod).ToArray();

				if (Array.IndexOf(dependencies, nameof(TerrariansConstructLib)) > -1)
					yield return mod;
			}
		}

		private static void LoadAllOfTheThings(string methodToInvoke) {
			Type MemoryTracking = typeof(Mod).Assembly.GetType("Terraria.ModLoader.Core.MemoryTracking");

			foreach (var (mod, method) in FindDependents().Select(m => (m, m.GetType().GetMethod(methodToInvoke, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)))) {
				if (method is null) {
					Instance.Logger.Warn($"Mod \"{mod.Name}\" does not have a \"public static {methodToInvoke}(Mod mod)\" method declared in its Mod class");
					continue;
				}

				string methodDescriptor = $"{method.DeclaringType.FullName}.{method.Name}()";

				if (!method.IsPublic)
					throw new Exception($"Method {methodDescriptor} was not public");

				if (!method.IsStatic)
					throw new Exception($"Method {methodDescriptor} was not static");

				if (method.ReturnType != typeof(void))
					throw new Exception($"Method {methodDescriptor} did not have a void return type");

				var parameters = method.GetParameters();

				if (parameters.Length != 1)
					throw new Exception($"Method {methodDescriptor} should have only one parameter");

				if (parameters[0].ParameterType != typeof(Mod))
					throw new Exception($"Method {methodDescriptor} did not have its parameter be of type \"{typeof(Mod).FullName}\"");

				MemoryTracking.GetCachedMethod("Checkpoint").Invoke(null, null);

				try {
					method.Invoke(null, new object[]{ mod });
				} catch (Exception ex) {
					ex.Data["mod"] = mod.Name;
					throw;
				} finally {
					MemoryTracking.GetCachedMethod("Update").Invoke(null, new object[]{ mod.Name });
				}
			}
		}

		public override void AddRecipeGroups() {
			//Make a recipe group for each part type
			for (int i = 0; i < PartRegistry.Count; i++) {
				// OrderBy ensures that the Unkonwn material ends up first in the list due to its type being the smallest
				RegisterRecipeGroup(GetRecipeGroupName(i), PartRegistry.registeredIDs[i].name, GetKnownMaterials()
					.Select(m => GetItemPartItem(m, i))
					.OrderBy(i => i.part.material.type)
					.Select(i => i.Type)
					.ToArray());
			}
		}

		public static void RegisterRecipeGroup(string groupName, string anyName, params int[] validTypes)
			=> RecipeGroup.RegisterGroup(groupName, new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {anyName}", validTypes));

		public static void RegisterRecipeGroup(string groupName, int itemForAnyName, params int[] validTypes)
			=> RecipeGroup.RegisterGroup(groupName, new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(itemForAnyName)}", validTypes));

		public static string GetRecipeGroupName(int partID) {
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			return "TerrariansConstructLib:PartGroup_" + PartRegistry.registeredIDs[partID].internalName;
		}

		public override void AddRecipes() {
			//Make a recipe for each item definition

			foreach (var (id, data) in ItemRegistry.registeredIDs) {
				if (!data.mod.TryFind<ModItem>(data.itemInternalName, out var item))
					throw new RecipeException($"Registered item #{id} (source mod: {data.mod.Name}) was assigned an invalid internal BaseTCItem.Name value: {data.itemInternalName}");

				if (item is not BaseTCItem)
					throw new Exception($"Registered item #{id} (source mod: {data.mod.Name}) was assigned a ModItem that doesn't inherit from BaseTCItem");

				Recipe recipe = item.Mod.CreateRecipe(item.Type);

				foreach (int part in data.validPartIDs)
					recipe.AddRecipeGroup(GetRecipeGroupName(part));

				// TODO: forge tile?

				recipe.AddCondition(NetworkText.FromLiteral("Must be crafted from the Forge UI"), r => false);

				recipe.Register();

				Logger.Debug($"Created recipe for BaseTCItem \"{item.GetType().GetSimplifiedGenericTypeName()}\"");
			}
		}

		public override void Unload() {
			DirectDetourManager.Unload();
			
			ConstructedAmmoRegistry.Unload();
			PartRegistry.Unload();
			ItemRegistry.Unload();

			PartActions.builders = null;
			ItemPart.partData = null;
			ItemPartItem.registeredPartsByItemID = null;
			ItemPartItem.itemPartToItemID = null;
			PartMold.moldsByPartID = null;
			PartMold.registeredMolds = null;

			itemTextures?.Clear();
			itemTextures = null;

			Interlocked.Exchange(ref UnloadReflection, null)?.Invoke();
		}

		//No Mod.Call() implementation.  If people want to add content/add support for content to this mod, they better use a strong/weak reference
		private static string GetLateLoadReason(string method)
			=> "Method was called too late in the loading process.  This method should be called in a public static void" + method + "() method in your Mod class";

		/// <summary>
		/// Registers a part definition
		/// </summary>
		/// <param name="mod">The mod that the part belongs to</param>
		/// <param name="internalName">The internal name of the part</param>
		/// <param name="name">The name of the part</param>
		/// <param name="materialCost">How much material is required to craft this part, multiplied by 2</param>
		/// <param name="hasComplexMold">Whether the part can be made with the complex mold</param>
		/// <param name="assetFolderPath">The path to the folder containing the part's textures</param>
		/// <returns>The ID of the registered part</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentNullException"/>
		public static int RegisterPart(Mod mod, string internalName, string name, int materialCost, bool hasComplexMold, string assetFolderPath) {
			if (!isLoadingParts)
				throw new Exception(GetLateLoadReason("RegisterTCItemParts"));

			return PartRegistry.Register(mod, internalName, name, materialCost, hasComplexMold, assetFolderPath);
		}

		/// <summary>
		/// Registers a name and <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> for the next constructed ammo type to be assigned an ID
		/// </summary>
		/// <param name="mod">The mod that the ammo belongs to</param>
		/// <param name="name">The name of the constructed ammo type</param>
		/// <param name="ammoID">The <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> for the constructed ammo ID</param>
		/// <param name="projectileInternalName">The projectile that this constructed ammo will shoot.  Use the string you'd use to access the projectile via <seealso cref="Mod.Find{T}(string)"/></param>
		/// <typeparam name="T">The <see langword="class"/> of the <seealso cref="BaseTCProjectile"/> to spawn when using the ammo</typeparam>
		/// <returns>The ID of the registered constructed ammo type</returns>
		/// <remarks>Note: The returned ID does not correlate with <seealso cref="AmmoID"/> nor <seealso cref="ItemID"/></remarks>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		/// <exception cref="ArgumentNullException"/>
		public static int RegisterAmmo(Mod mod, string name, int ammoID, string projectileInternalName) {
			if (!isLoadingParts)
				throw new Exception(GetLateLoadReason("RegisterTCAmmunition"));

			return ConstructedAmmoRegistry.Register(mod, name, ammoID, projectileInternalName);
		}

		/// <summary>
		/// Registers a name and valid <seealso cref="ItemPart"/> IDs for an item
		/// </summary>
		/// <typeparam name="T">The <see langword="class"/> of the <seealso cref="BaseTCItem"/> associated with this registered item type</typeparam>
		/// <param name="mod">The mod that the weapon belongs to</param>
		/// <param name="internalName">The internal name of the weapon</param>
		/// <param name="name">The default item type name used by <seealso cref="BaseTCItem.RegisteredItemTypeName"/></param>
		/// <param name="itemInternalName">The item type that this registered item ID will be applied to.  Use the string you'd use to access the item via <seealso cref="Mod.Find{T}(string)"/></param>
		/// <param name="partVisualsFolder">The folder where the item's part visuals is located, relative to the mod they're from</param>
		/// <param name="validPartIDs">The array of parts that comprise the weapon</param>
		/// <returns>The ID of the registered item</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentNullException"/>
		public static int RegisterItem(Mod mod, string internalName, string name, string itemInternalName, string partVisualsFolder, params int[] validPartIDs) {
			if (!isLoadingParts)
				throw new Exception(GetLateLoadReason("RegisterTCItems"));

			return ItemRegistry.Register(mod, internalName, name, itemInternalName, partVisualsFolder, validPartIDs);
		}

		/// <summary>
		/// Gets an enumeration of <seealso cref="Material"/> instances that were used to create item parts
		/// </summary>
		public static IEnumerable<Material> GetKnownMaterials()
			=> new ListUsedMaterials().GetRegistry().Values.Select(p => p.material);

		/// <summary>
		/// Gets the name of a registered part
		/// </summary>
		/// <param name="id">The ID of the part to get</param>
		/// <returns>The name of the registered part, or throws an exception if a part of type <paramref name="id"/> does not exist</returns>
		/// <exception cref="Exception"/>
		public static string GetPartName(int id)
			=> id >= 0 && id < PartRegistry.Count ? PartRegistry.registeredIDs[id].name : throw new Exception($"A part with ID {id} does not exist");

		/// <summary>
		/// Gets the internal name of a registered part
		/// </summary>
		/// <param name="id">The ID of the part to get</param>
		/// <returns>The internal name of the registered part, or throws an exception if a part of type <paramref name="id"/> does not exist</returns>
		/// <exception cref="Exception"/>
		public static string GetPartInternalName(int id)
			=> id >= 0 && id < PartRegistry.Count ? PartRegistry.registeredIDs[id].internalName : throw new Exception($"A part with ID {id} does not exist");

		/// <summary>
		/// Gets the global tooltip of an <seealso cref="ItemPart"/>
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		/// <returns>The global tooltip</returns>
		public static string GetPartTooltip(Material material, int partID)
			=> ItemPart.partData.Get(material, partID).tooltip;

		/// <summary>
		/// Sets the global tooltip of an <seealso cref="ItemPart"/>
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		/// <param name="tooltip">The new tooltip</param>
		public static void SetGlobalPartTooltip(Material material, int partID, string tooltip)
			=> ItemPart.SetGlobalTooltip(material, partID, tooltip);

		/// <summary>
		/// Gets the global modifier text of an <seealso cref="ItemPart"/>
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		/// <returns>The global modifier text</returns>
		public static string GetPartModifierText(Material material, int partID)
			=> ItemPart.partData.Get(material, partID).modifierText;

		/// <summary>
		/// Sets the global modifier text of an <seealso cref="ItemPart"/>
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		/// <param name="tooltip">The new modifier text</param>
		public static void SetGlobalPartModifierText(Material material, int partID, string tooltip)
			=> ItemPart.SetGlobalTooltip(material, partID, tooltip);

		/// <summary>
		/// Gets the material cost of an <seealso cref="PartMold"/>
		/// </summary>
		/// <param name="partID">The part ID</param>
		public static int GetMoldCost(int partID)
			=> PartMold.GetMaterialCost(partID);

		/// <summary>
		/// Sets the material cost for item part molds using the part ID, <paramref name="partID"/>, to <paramref name="materialCost"/>
		/// </summary>
		/// <param name="partID">The part ID</param>
		/// <param name="materialCost">The new global material cost.  The displayed material cost is this value divided by 2</param>
		public static void SetMoldCost(int partID, int materialCost)
			=> PartMold.SetMaterialCost(partID, materialCost);

		/// <summary>
		/// Gets the name of a registered constructed ammo type
		/// </summary>
		/// <param name="constructedAmmoID">The ID of the constructed ammo type to get</param>
		/// <returns>The name of the registered constructed ammo type, or throws an exception if a constructed ammo type of type <paramref name="constructedAmmoID"/> does not exist</returns>
		/// <exception cref="Exception"/>
		/// <remarks>Note: The input ID does not correlate with <seealso cref="AmmoID"/> nor <seealso cref="ItemID"/></remarks>
		public static string GetAmmoName(int constructedAmmoID)
			=> constructedAmmoID >= 0 && constructedAmmoID < ConstructedAmmoRegistry.Count
				? ConstructedAmmoRegistry.registeredIDs[constructedAmmoID].name
				: throw new Exception($"A constructed ammo type with ID {constructedAmmoID} does not exist");

		/// <summary>
		/// Gets the <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> of a registered constructed ammo type
		/// </summary>
		/// <param name="constructedAmmoID">The ID of the constructed ammo type to get</param>
		/// <returns>The <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> of the registered constructed ammo type, or throws an exception if a constructed ammo type of type <paramref name="constructedAmmoID"/> does not exist</returns>
		/// <exception cref="Exception"/>
		/// <remarks>Note: The input ID does not correlate with <seealso cref="AmmoID"/> nor <seealso cref="ItemID"/></remarks>
		public static int GetAmmoID(int constructedAmmoID)
			=> constructedAmmoID >= 0 && constructedAmmoID < ConstructedAmmoRegistry.Count
				? ConstructedAmmoRegistry.registeredIDs[constructedAmmoID].ammoID
				: throw new Exception($"A constructed ammo type with ID {constructedAmmoID} does not exist");

		/// <summary>
		/// Gets the projectile ID of a registered constructed ammo type
		/// </summary>
		/// <param name="constructedAmmoID">The ID of the constructed ammo type to get</param>
		/// <returns>The <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> of the registered constructed ammo type, or throws an exception if a constructed ammo type of type <paramref name="constructedAmmoID"/> does not exist</returns>
		/// <exception cref="Exception"/>
		public static int GetAmmoProjectileType(int constructedAmmoID)
			=> constructedAmmoID >= 0 && constructedAmmoID < ConstructedAmmoRegistry.Count
				? ConstructedAmmoRegistry.registeredIDs[constructedAmmoID].mod.Find<ModProjectile>(ConstructedAmmoRegistry.registeredIDs[constructedAmmoID].projectileInternalName).Type
				: throw new Exception($"A constructed ammo type with ID {constructedAmmoID} does not exist");

		/// <summary>
		/// Gets the name for a registered item
		/// </summary>
		/// <param name="registeredItemID">The ID for the registered item.  Not to be confused with its <seealso cref="ItemID"/> or <seealso cref="ModItem.Type"/></param>
		/// <returns>The name for the registered item</returns>
		/// <exception cref="Exception"/>
		public static string GetItemName(int registeredItemID)
			=> registeredItemID >= 0 && registeredItemID < ItemRegistry.Count
				? ItemRegistry.registeredIDs[registeredItemID].name
				: throw new Exception($"A registered item with ID {registeredItemID} does not exist");

		/// <summary>
		/// Gets the internal name for a registered item
		/// </summary>
		/// <param name="registeredItemID">The ID for the registered item.  Not to be confused with its <seealso cref="ItemID"/> or <seealso cref="ModItem.Type"/></param>
		/// <returns>The internal name for the registered item</returns>
		/// <exception cref="Exception"/>
		public static string GetItemInternalName(int registeredItemID)
			=> registeredItemID >= 0 && registeredItemID < ItemRegistry.Count
				? ItemRegistry.registeredIDs[registeredItemID].internalName
				: throw new Exception($"A registered item with ID {registeredItemID} does not exist");

		/// <summary>
		/// Gets a clone of the valid part IDs for a registered item
		/// </summary>
		/// <param name="registeredItemID">The ID for the registered item.  Not to be confused with its <seealso cref="ItemID"/> or <seealso cref="ModItem.Type"/></param>
		/// <returns>A clone of the valid part IDs for the registered item</returns>
		/// <exception cref="Exception"/>
		public static int[] GetItemValidPartIDs(int registeredItemID)
			=> registeredItemID >= 0 && registeredItemID < ItemRegistry.Count
				? (int[])ItemRegistry.registeredIDs[registeredItemID].validPartIDs.Clone()
				: throw new Exception($"A registered item with ID {registeredItemID} does not exist");

		/// <summary>
		/// Attempts to find a registered item whose valid part IDs are set to the same values as <paramref name="partIDs"/>
		/// </summary>
		/// <param name="partIDs">The part IDs to check</param>
		/// <returns>A value &gt;= <c>0</c> and &lt; <seealso cref="ItemRegistry.Count"/> if successful, <c>-1</c> otherwise</returns>
		public static int FindItem(int[] partIDs) {
			foreach (var (id, data) in ItemRegistry.registeredIDs)
				if (data.validPartIDs.SequenceEqual(partIDs))
					return id;

			return -1;
		}

		/// <summary>
		/// Attempts to find a registered item whose valid part IDs are set to the same values as <paramref name="partIDs"/>
		/// </summary>
		/// <param name="partIDs">The part IDs to check</param>
		/// <param name="registeredItemID">A value &gt;= <c>0</c> and &lt; <seealso cref="ItemRegistry.Count"/> if successful, <c>-1</c> otherwise</param>
		/// <returns><see langword="true"/> if the search was successful, <see langword="false"/> otherwise</returns>
		public static bool TryFindItem(int[] partIDs, out int registeredItemID) {
			registeredItemID = FindItem(partIDs);
			return registeredItemID > -1;
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
			=> ModContent.GetModItem(GetItemPartItemType(material, partID)) as ItemPartItem;

		/// <summary>
		/// Registers a part item for the material, <paramref name="material"/>
		/// </summary>
		/// <param name="mod">The mod instance to add the part to</param>
		/// <param name="material">The material instance</param>
		/// <param name="partID">The part ID</param>
		/// <param name="actions">The actions</param>
		/// <param name="tooltip">The tooltip for this part.  Can be modified via <seealso cref="ItemPart.SetGlobalTooltip(Material, int, string)"/></param>
		/// <param name="modifierText">The modifier text that will be assigned to all parts.  Can be modified via <seealso cref="ItemPart.SetGlobalModifierText(Material, int, string)"/></param>
		public static void AddPart(Mod mod, Material material, int partID, ItemPartActionsBuilder actions, string tooltip, string modifierText) {
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			ItemPartItem item = ItemPartItem.Create(material, partID, actions, tooltip, modifierText);

			ReflectionHelper<Mod>.InvokeSetterFunction("loading", mod, true);

			mod.AddContent(item);

			ReflectionHelper<Mod>.InvokeSetterFunction("loading", mod, false);

			//ModItem.Type is only set after Mod.AddContent is called and the item is actually registered
			if (item.Type > 0) {
				ItemPartItem.registeredPartsByItemID[item.Type] = item.part;
				ItemPartItem.itemPartToItemID.Set(material, partID, item.Type);

				Instance.Logger.Info($"Added item part \"{item.Name}\" (ID: {item.Type})");
			}
		}

		/// <summary>
		/// Registers a part item for the material, <paramref name="materialType"/>, with the given rarity, <paramref name="rarity"/>
		/// </summary>
		/// <param name="mod">The mod instance to add the part to</param>
		/// <param name="materialType">The item ID</param>
		/// <param name="rarity">The item rarity</param>
		/// <param name="partID">The part ID</param>
		/// <param name="actions">The actions</param>
		/// <param name="tooltip">The tooltip for this part.  Can be modified via <seealso cref="ItemPart.SetGlobalTooltip(Material, int, string)"/></param>
		/// <param name="modifierText">The modifier text that will be assigned to all parts.  Can be modified via <seealso cref="ItemPart.SetGlobalModifierText(Material, int, string)"/></param>
		public static void AddPart(Mod mod, int materialType, int rarity, int partID, ItemPartActionsBuilder actions, string tooltip, string modifierText)
			=> AddPart(mod, new Material(){ type = materialType, rarity = rarity }, partID, actions, tooltip, modifierText);

		/// <summary>
		/// Registers the part items for the material, <paramref name="materialType"/>
		/// </summary>
		/// <param name="mod">The mod instance to add the part to</param>
		/// <param name="materialType">The item ID</param>
		/// <param name="actions">The actions</param>
		/// <param name="tooltipForAllParts">The tooltip that will be assigned to all parts.  Can be modified via <seealso cref="ItemPart.SetGlobalTooltip(Material, int, string)"/></param>
		/// <param name="modifierTextForAllParts">The modifier text that will be assigned to all parts.  Can be modified via <seealso cref="ItemPart.SetGlobalModifierText(Material, int, string)"/></param>
		/// <param name="partIDsToIgnore">The IDs to ignore when iterating to create the part items</param>
		public static void AddAllPartsOfType(Mod mod, int materialType, ItemPartActionsBuilder actions, string tooltipForAllParts, string modifierTextForAllParts, params int[] partIDsToIgnore)
			=> AddAllPartsOfMaterial(mod, Material.FromItem(materialType), actions, tooltipForAllParts, modifierTextForAllParts, partIDsToIgnore);

		/// <summary>
		/// Registers the part items for the material, <paramref name="material"/>
		/// </summary>
		/// <param name="mod">The mod instance to add the part to</param>
		/// <param name="material">The material instance</param>
		/// <param name="actions">The actions</param>
		/// <param name="tooltipForAllParts">The tooltip that will be assigned to all parts.  Can be modified via <seealso cref="ItemPart.SetGlobalTooltip(Material, int, string)"/></param>
		/// <param name="modifierTextForAllParts">The modifier text that will be assigned to all parts.  Can be modified via <seealso cref="ItemPart.SetGlobalModifierText(Material, int, string)"/></param>
		/// <param name="partIDsToIgnore">The IDs to ignore when iterating to create the part items</param>
		public static void AddAllPartsOfMaterial(Mod mod, Material material, ItemPartActionsBuilder actions, string tooltipForAllParts, string modifierTextForAllParts, params int[] partIDsToIgnore) {
			for (int partID = 0; partID < PartRegistry.Count; partID++) {
				if (Array.IndexOf(partIDsToIgnore, partID) > -1)
					continue;

				AddPart(mod, material, partID, actions, tooltipForAllParts, modifierTextForAllParts);
			}
		}

		public static class RegisteredParts {
			public static int Shard { get; internal set; }
		}
	}
}