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
using TerrariansConstructLib.API.Edits;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.API.UI;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;
using TerrariansConstructLib.Stats;

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

			directDetourInstance = null!;
		}

		private static FieldInfo Interface_loadMods;
		private static MethodInfo UIProgress_set_SubProgressText;

		internal static void SetLoadingSubProgressText(string text)
			=> UIProgress_set_SubProgressText.Invoke(Interface_loadMods.GetValue(null), new object[]{ text });

		private static StreamWriter writer;

		public override void Load() {
			if(!ModLoader.HasMod("TerrariansConstruct"))
				throw new Exception(Language.GetTextValue("tModLoader.LoadErrorDependencyMissing", "TerrariansConstruct", Name));

			Interface_loadMods = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.Interface")!.GetField("loadMods", BindingFlags.NonPublic | BindingFlags.Static)!;
			UIProgress_set_SubProgressText = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.UIProgress")!.GetProperty("SubProgressText", BindingFlags.Public | BindingFlags.Instance)!.GetSetMethod()!;

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
			Material.statsByMaterialID = new();
			Material.worthByMaterialID = new();
			ForgeUISlotConfiguration.Initialize();

			ItemStatCollection.Load();

			itemTextures = new();

			if (!Main.dedServ) {
				ActivateAbility = KeybindLoader.RegisterKeybind(this, "Activate Tool Ability", Keys.G);
			}

			string path = Program.SavePath;
			path = Path.Combine(path, "aA Mods", "TerrariansConstructLib");
			Directory.CreateDirectory(path);

			string logFile = Path.Combine(path, "logs.txt");
			writer = new(File.Open(logFile, FileMode.Create));

			Logger.Info("Logging information to:  " + logFile);

			writer.WriteLine($"Date: {DateTime.Now :d}");
			writer.WriteLine($"============================");

			writer.Flush();

			//In order for all parts/ammos/etc. to be visible by all mods that use the library, we have to do some magic
			RegisteredParts.Shard = RegisterPart(ModLoader.GetMod("TerrariansConstruct"), "ItemCraftLeftover", "Shard", 1, hasSimpleMold: true, "Assets/Parts/ItemCraftLeftover", StatType.Extra);

			SetLoadingSubProgressText("Finding Dependents");

			dependents = new(FindDependents());

			LoadAllOfTheThings("RegisterTCItemParts");

			foreach (var (id, data) in PartRegistry.registeredIDs) {
				writer.WriteLine($"Item Part \"{data.name}\" (ID: {id}) added by {data.mod.Name}");
				writer.Flush();
			}

			LoadAllOfTheThings("RegisterTCAmmunition");

			foreach (var (id, data) in ConstructedAmmoRegistry.registeredIDs) {
				writer.WriteLine($"Constructed Ammo \"{data.name}\" (ID: {id}) added by {data.mod.Name}");
				writer.Flush();
			}

			LoadAllOfTheThings("RegisterTCItems");

			foreach (var (id, data) in ItemRegistry.registeredIDs) {
				writer.WriteLine($"Item Definition \"{data.name}\" (ID: {id}) added by {data.mod.Name}\n" +
					$"  -- parts: {string.Join(", ", data.validPartIDs.Select(PartRegistry.IDToIdentifier))}");
				writer.Flush();
			}

			AddMoldItems();

			//head part: damage, knockback, crit, useSpeed, toolPower
			RegisterMaterialStats(RegisteredMaterials.Unloaded, 1,
				new HeadPartStats(0, 0, 0, 20, 0, 1),
				new HandlePartStats(),
				new ExtraPartStats());
			RegisterMaterialStats(RegisteredMaterials.Unknown, 1,
				new HeadPartStats(0, 0, 0, 20, 0, 1),
				new HandlePartStats(),
				new ExtraPartStats());

			LoadAllOfTheThings("RegisterTCMaterials");

			foreach (var (type, stats) in Material.statsByMaterialID) {
				Material copy = type == UnloadedMaterial.StaticType ? RegisteredMaterials.Unloaded : type == UnknownMaterial.StaticType ? RegisteredMaterials.Unknown : Material.FromItem(type);

				writer.WriteLine($"Stats for material \"{copy.GetModName()}:{copy.GetName()}\" was registered with the following part types: " + string.Join(", ", stats.Select(s => s.ToString())));
				writer.Flush();
			}

			for (int i = 0; i < PartRegistry.Count; i++)
				AddPart(this, RegisteredMaterials.Unloaded, i, PartActions.NoActions, null, null);
			for (int i = 0; i < PartRegistry.Count; i++)
				AddPart(this, RegisteredMaterials.Unknown, i, PartActions.NoActions, null, null);

			EditsLoader.Load();

			DirectDetourManager.Load();

			SetLoadingSubProgressText("Finishing Resource Loading");

			isLoadingParts = false;
		}

		private static void AddMoldItems() {
			for (int partID = 0; partID < PartRegistry.Count; partID++) {
				var partData = PartRegistry.registeredIDs[partID];

				PartMold? simpleMold = !partData.hasSimpleMold ? null : PartMold.Create(partID, true, false);
				PartMold complexMold = PartMold.Create(partID, false, false);
				PartMold complexPlatinumMold = PartMold.Create(partID, false, true);

				complexPlatinumMold.isPlatinumMold = true;

				ReflectionHelper<Mod>.InvokeSetterFunction("loading", partData.mod, true);

				if (simpleMold is not null)
					partData.mod.AddContent(simpleMold);
				
				partData.mod.AddContent(complexMold);
				partData.mod.AddContent(complexPlatinumMold);

				ReflectionHelper<Mod>.InvokeSetterFunction("loading", partData.mod, false);

				if (simpleMold is not null)
					PartMold.registeredMolds[simpleMold.Type] = simpleMold;
				
				PartMold.registeredMolds[complexMold.Type] = complexMold;
				PartMold.registeredMolds[complexPlatinumMold.Type] = complexPlatinumMold;

				PartMold.moldsByPartID[partID] = new() { simple = simpleMold, complex = complexMold, complexPlatinum = complexPlatinumMold };

				writer.WriteLine($"{(partData.hasSimpleMold ? "Simple and complex item part molds" : "Complex item part molds")} for part ID \"{partData.mod.Name}:{partData.internalName}\" added by mod \"{partData.mod.Name}\"");
				writer.Flush();
			}
		}

		public override void PostSetupContent() {
			writer.Dispose();
			writer = null!;
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
					object properties = BuildProperties_ReadModFile.Invoke(null, new object[]{ modFile })!;
					return (BuildProperties_RefNames.Invoke(properties, new object[]{ true }) as IEnumerable<string>)!;
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
			SetLoadingSubProgressText("Invoking: " + methodToInvoke);

			Type MemoryTracking = typeof(Mod).Assembly.GetType("Terraria.ModLoader.Core.MemoryTracking")!;

			foreach (var (mod, method) in Dependents.Select(m => (m, m.GetType().GetMethod(methodToInvoke, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)))) {
				if (method is null) {
					Instance.Logger.Warn($"Mod \"{mod.Name}\" does not have a \"public static {methodToInvoke}(Mod mod)\" method declared in its Mod class");
					continue;
				}

				string methodDescriptor = $"{method.DeclaringType!.FullName}.{method.Name}()";

				if (!method.IsPublic)
					throw new Exception($"Method {methodDescriptor} was not public");

				if (!method.IsStatic)
					throw new Exception($"Method {methodDescriptor} was not static");

				if (method.ReturnType != typeof(void))
					throw new Exception($"Method {methodDescriptor} did not have a void return type");

				var parameters = method.GetParameters();

				if (parameters.Length == 0)
					throw new Exception($"Method {methodDescriptor} should have a \"Mod mod\" parameter.  No parameters were detected");

				if (parameters.Length != 1)
					throw new Exception($"Method {methodDescriptor} should have only one \"Mod mod\" parameter.  Multiple parameters were detected");

				if (parameters[0].ParameterType != typeof(Mod))
					throw new Exception($"Method {methodDescriptor} did not have its parameter be of type \"{typeof(Mod).FullName}\"");

				MemoryTracking.GetCachedMethod("Checkpoint")?.Invoke(null, null);

				try {
					method.Invoke(null, new object[]{ mod });
				} catch (Exception ex) {
					ex.Data["mod"] = mod.Name;
					throw;
				} finally {
					MemoryTracking.GetCachedMethod("Update")?.Invoke(null, new object[]{ mod.Name });
				}
			}
		}

		public override void AddRecipeGroups() {
			//Make a recipe group for each part type
			for (int i = 0; i < PartRegistry.Count; i++) {
				// OrderBy ensures that the Unkonwn material ends up first in the list due to its type being the smallest
				int[] ids = GetKnownMaterials()
					.Where(m => ItemPartItem.itemPartToItemID.Has(m, i))
					.Select(m => GetItemPartItem(m, i))
					.OrderBy(i => i.part.material.Type)
					.Select(i => i.Type)
					.ToArray();

				if (ids.Length == 0)
					continue;  //Failsafe for if a part ID has no parts registered to it

				RegisterRecipeGroup(GetRecipeGroupName(i), PartRegistry.registeredIDs[i].name, ids);
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

		public override void Unload() {
			Interface_loadMods = null!;
			UIProgress_set_SubProgressText = null!;

			DirectDetourManager.Unload();
			
			ConstructedAmmoRegistry.Unload();
			PartRegistry.Unload();
			ItemRegistry.Unload();

			PartActions.builders = null!;
			ItemPart.partData = null!;
			ItemPartItem.registeredPartsByItemID = null!;
			ItemPartItem.itemPartToItemID = null!;
			PartMold.moldsByPartID = null!;
			PartMold.registeredMolds = null!;
			Material.statsByMaterialID = null!;
			Material.worthByMaterialID = null!;
			ForgeUISlotConfiguration.Unload();

			ItemStatCollection.Unload();

			itemTextures?.Clear();
			itemTextures = null!;

			Interlocked.Exchange(ref UnloadReflection!, null)?.Invoke();

			Interface_loadMods = null!;
			UIProgress_set_SubProgressText = null!;

			writer.Dispose();
			writer = null!;
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
		/// <param name="hasSimpleMold">Whether the part can be made with the simple mold</param>
		/// <param name="assetFolderPath">The path to the folder containing the part's textures</param>
		/// <param name="type">Which type of stats the part should use</param>
		/// <returns>The ID of the registered part</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentNullException"/>
		public static int RegisterPart(Mod mod, string internalName, string name, int materialCost, bool hasSimpleMold, string assetFolderPath, StatType type) {
			if (!isLoadingParts)
				throw new Exception(GetLateLoadReason("RegisterTCItemParts"));

			return PartRegistry.Register(mod, internalName, name, materialCost, hasSimpleMold, assetFolderPath, type);
		}

		/// <summary>
		/// Registers a name and <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> for the next constructed ammo type to be assigned an ID
		/// </summary>
		/// <param name="mod">The mod that the ammo belongs to</param>
		/// <param name="name">The name of the constructed ammo type</param>
		/// <param name="ammoID">The <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> for the constructed ammo ID</param>
		/// <param name="projectileInternalName">The projectile that this constructed ammo will shoot.  Use the string you'd use to access the projectile via <seealso cref="Mod.Find{T}(string)"/></param>
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
		/// Registers item part stats for a certain material
		/// </summary>
		/// <param name="material">The material for the item part stats</param>
		/// <param name="worth">How much material is needed to create one Shard part</param>
		/// <param name="stats">The stats for the material</param>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentException"/>
		public static Material RegisterMaterialStats(int material, int worth, params IPartStats[] stats) {
			Material instance = Material.FromItem(material);
			
			RegisterMaterialStats(instance, worth, stats);

			return instance;
		}

		//Used to load the stats for the Unloaded and Unknown materials
		private static void RegisterMaterialStats(Material material, int worth, params IPartStats[] stats) {
			if (stats is null || stats.Length == 0)
				throw new ArgumentException("Stats array was null or had a zero length");
			
			if (Material.statsByMaterialID.ContainsKey(material.Type))
				throw new Exception($"Stats for the material \"{material.GetModName()}:{material.GetName()}\" have already been registered");

			Material.statsByMaterialID[material.Type] = (IPartStats[])stats.Clone();
			Material.worthByMaterialID[material.Type] = worth;
		}

		/// <summary>
		/// Gets an enumeration of <seealso cref="Material"/> instances that were used to create item parts
		/// </summary>
		public static IEnumerable<Material> GetKnownMaterials()
			=> new ListUsedMaterials().GetRegistry().Values;

		/// <summary>
		/// Flags or clears a part ID as being an axe part
		/// </summary>
		/// <param name="partID">The part ID</param>
		/// <param name="isAxe">Whether the part is an axe part</param>
		/// <exception cref="ArgumentException"/>
		public static void SetPartAsAxeToolPart(int partID, bool isAxe) {
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			if (PartRegistry.isAxePart.Length < partID)
				PartRegistry.isAxePart.Length = partID + 1;

			PartRegistry.isAxePart[partID] = isAxe;
		}

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
		public static string? GetPartTooltip(Material material, int partID)
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
		public static ModifierText? GetPartModifierText(Material material, int partID)
			=> ItemPart.partData.Get(material, partID).modifierText;

		/// <summary>
		/// Gets the global modifier text's stat of an <seealso cref="ItemPart"/>, or a default value if there is none
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		/// <param name="defaultValue">The value to use if the part does not have a global modifier text instance</param>
		/// <returns>The global modifier stat, or the default value if it's not defined</returns>
		public static StatModifier GetPartModifierStatOrDefault(Material material, int partID, StatModifier defaultValue)
			=> ItemPart.partData.Get(material, partID).modifierText?.Stat ?? defaultValue;

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
		/// Converts a registered Terrarians' Contruct item ID into its tModLoader item ID
		/// </summary>
		/// <param name="registeredItemID">The registered item ID</param>
		/// <returns>The tModLoader item ID</returns>
		/// <exception cref="Exception"/>
		public static int GetItemType(int registeredItemID) {
			if (registeredItemID < 0 || registeredItemID >= ItemRegistry.Count)
				throw new Exception($"A registered item with ID {registeredItemID} does not exist");

			var data = ItemRegistry.registeredIDs[registeredItemID];

			return data.mod.Find<ModItem>(data.itemInternalName).Type;
		}

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
			=> (ModContent.GetModItem(GetItemPartItemType(material, partID)) as ItemPartItem)!;

		/// <summary>
		/// Registers a part item for the material, <paramref name="material"/>
		/// </summary>
		/// <param name="mod">The mod instance to add the part to</param>
		/// <param name="material">The material instance</param>
		/// <param name="partID">The part ID</param>
		/// <param name="actions">The actions</param>
		/// <param name="tooltip">The tooltip for this part.  Can be modified via <seealso cref="ItemPart.SetGlobalTooltip(Material, int, string)"/></param>
		/// <param name="modifierTextLangKey">The lang key for the <see cref="ModifierText"/>.  If <see langword="null"/>, the item part will have no modifier text.</param>
		/// <param name="modifierStat">The <seealso cref="StatModifier"/> for the modifier text.  If <paramref name="modifierTextLangKey"/> is <see langword="null"/>, this parameter is ignored.  Defaults to <seealso cref="StatModifier.One"/></param>
		public static void AddPart(Mod mod, Material material, int partID, ItemPartActionsBuilder actions, string? tooltip, string? modifierTextLangKey = null, StatModifier? modifierStat = null) {
			if (partID < 0 || partID >= PartRegistry.Count)
				throw new ArgumentException("Part ID was invalid");

			if (!Material.statsByMaterialID.ContainsKey(material.Type))
				throw new ArgumentException($"Material was not registered: \"{material.GetItemName()}\" (ID: {material.Type})");

			ItemPartItem item = ItemPartItem.Create(material, partID, actions, tooltip, modifierTextLangKey is null ? null : new ModifierText(modifierTextLangKey, material, partID, modifierStat ?? StatModifier.One));

			ReflectionHelper<Mod>.InvokeSetterFunction("loading", mod, true);

			mod.AddContent(item);

			ReflectionHelper<Mod>.InvokeSetterFunction("loading", mod, false);

			//ModItem.Type is only set after Mod.AddContent is called and the item is actually registered
			if (item.Type > 0) {
				ItemPartItem.registeredPartsByItemID[item.Type] = item.part;
				ItemPartItem.itemPartToItemID.Set(material, partID, item.Type);

				writer.WriteLine($"Added item part \"{item.Name}\" (ID: {item.Type})");
				writer.Flush();
			}
		}

		public static class RegisteredParts {
			public static int Shard { get; internal set; }
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
			public const string HeadToolPower = "head.toolPower";
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