using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Edits;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Projectiles;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib {
	public class CoreLibMod : Mod {
		public static ModKeybind ActivateAbility;

		private static bool isLoadingParts = false;

		public static bool LogAddedParts { get; set; }

		public static CoreLibMod Instance => ModContent.GetInstance<CoreLibMod>();

		public override void Load() {
			MethodInfo ModLoader_IsEnabled = typeof(ModLoader).GetMethod("IsEnabled", BindingFlags.NonPublic | BindingFlags.Static);

			if (!ModLoader.HasMod("TerrariansConstruct") || !(bool)ModLoader_IsEnabled.Invoke(null, new object[]{ "TerrariansConstruct" }))
				throw new Exception(Language.GetTextValue("tModLoader.LoadErrorDependencyMissing", "TerrariansConstruct", Name));

			isLoadingParts = false;

			ConstructedAmmoRegistry.Load();
			PartRegistry.Load();
			ItemRegistry.Load();

			PartActions.builders = new();
			ItemPart.partData = new();
			ItemPartItem.registeredPartsByItemID = new();

			if (!Main.dedServ) {
				ActivateAbility = KeybindLoader.RegisterKeybind(this, "Activate Tool Ability", Keys.G);
			}

			//In order for all parts/ammos/etc. to be visible by all mods that use the library, we have to do some magic
			LoadAllOfTheThings("RegisterTCItemParts");
			LoadAllOfTheThings("RegisterTCAmmunition");
			LoadAllOfTheThings("RegisterTCItems");

			EditsLoader.Load();

			DirectDetourManager.Load();

			isLoadingParts = false;
		}

		private static void LoadAllOfTheThings(string methodToInvoke) {
			foreach (var (mod, method) in ModLoader.Mods.Select(m => (m, m.GetType().GetMethod(methodToInvoke, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)))) {
				if (method is null) {
					Instance.Logger.Warn($"Mod \"{mod.Name}\" does not have a \"{methodToInvoke}\" method declared in its Mod class");
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

				method.Invoke(null, new object[]{ mod });
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
		/// <param name="assetFolderPath">The path to the folder containing the part's textures</param>
		/// <returns>The ID of the registered part</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentNullException"/>
		public static int RegisterPart(Mod mod, string internalName, string name, string assetFolderPath) {
			if (!isLoadingParts)
				throw new Exception(GetLateLoadReason("RegisterTCItemParts"));

			return PartRegistry.Register(mod, internalName, name, assetFolderPath);
		}

		/// <summary>
		/// Registers a name and <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> for the next constructed ammo type to be assigned an ID
		/// </summary>
		/// <param name="mod">The mod that the ammo belongs to</param>
		/// <param name="name">The name of the constructed ammo type</param>
		/// <param name="ammoID"></param>
		/// <typeparam name="T">The type of the projectile to spawn when using the ammo</typeparam>
		/// <returns>The ID of the registered constructed ammo type</returns>
		/// <remarks>Note: The returned ID does not correlate with <seealso cref="AmmoID"/> nor <seealso cref="ItemID"/></remarks>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		/// <exception cref="ArgumentNullException"/>
		public static int RegisterAmmo<T>(Mod mod, string name, int ammoID) where T : BaseTCProjectile{
			if (!isLoadingParts)
				throw new Exception(GetLateLoadReason("RegisterTCAmmunition"));

			return ConstructedAmmoRegistry.Register<T>(mod, name, ammoID);
		}

		/// <summary>
		/// Registers a name and valid <seealso cref="ItemPart"/> IDs for an item
		/// </summary>
		/// <param name="mod">The mod that the weapon belongs to</param>
		/// <param name="internalName">The internal name of the weapon</param>
		/// <param name="name">The default item type name used by <seealso cref="BaseTCItem.RegisteredItemTypeName"/></param>
		/// <param name="validPartIDs">The array of parts that comprise the weapon</param>
		/// <returns>The ID of the registered item</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentNullException"/>
		public static int RegisterItem(Mod mod, string internalName, string name, params int[] validPartIDs) {
			if (!isLoadingParts)
				throw new Exception(GetLateLoadReason("RegisterTCItems"));

			return ItemRegistry.Register(mod, internalName, name, validPartIDs);
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
				? ConstructedAmmoRegistry.registeredIDs[constructedAmmoID].projectileType
				: throw new Exception($"A constructed ammo type with ID {constructedAmmoID} does not exist");

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
		/// Gets an <seealso cref="ItemPart"/> instance from a material and part ID
		/// </summary>
		/// <param name="material">The material</param>
		/// <param name="partID">The part ID</param>
		/// <returns>The <seealso cref="ItemPart"/> instance</returns>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentException"/>
		public static ItemPart GetItemPart(Material material, int partID)
			=> ItemPart.partData.Get(material, partID);

		/// <summary>
		/// Registers the part items for the material, <paramref name="materialType"/>, with the given rarity, <paramref name="rarity"/>
		/// </summary>
		/// <param name="mod">The mod instance to add the part to</param>
		/// <param name="materialType">The item ID</param>
		/// <param name="rarity">The item rarity</param>
		/// <param name="actions">The actions</param>
		/// <param name="tooltipForAllParts">The tooltip that will be assigned to all parts.  Can be modified via <seealso cref="ItemPart.SetTooltip(Material, int, string)"/></param>
		/// <param name="partIDsToIgnore">The IDs to ignore when iterating to create the part items</param>
		public static void AddAllPartsOfType(Mod mod, int materialType, int rarity, ItemPartActionsBuilder actions, string tooltipForAllParts, params int[] partIDsToIgnore) {
			Material material = new(){
				type = materialType,
				rarity = rarity
			};

			AddAllPartsOfMaterial(mod, material, actions, tooltipForAllParts, partIDsToIgnore);
		}

		/// <summary>
		/// Registers the part items for the material, <paramref name="material"/>
		/// </summary>
		/// <param name="mod">The mod instance to add the part to</param>
		/// <param name="material">The material instance</param>
		/// <param name="actions">The actions</param>
		/// <param name="tooltipForAllParts">The tooltip that will be assigned to all parts.  Can be modified via <seealso cref="ItemPart.SetTooltip(Material, int, string)"/></param>
		/// <param name="partIDsToIgnore">The IDs to ignore when iterating to create the part items</param>
		public static void AddAllPartsOfMaterial(Mod mod, Material material, ItemPartActionsBuilder actions, string tooltipForAllParts, params int[] partIDsToIgnore) {
			for (int partID = 0; partID < PartRegistry.Count; partID++) {
				if (Array.IndexOf(partIDsToIgnore, partID) > -1)
					continue;

				AddPart(mod, material, partID, actions, tooltipForAllParts);
			}
		}

		/// <summary>
		/// Registers a part item for the material, <paramref name="material"/>
		/// </summary>
		/// <param name="mod">The mod instance to add the part to</param>
		/// <param name="material">The material instance</param>
		/// <param name="partID">The part ID</param>
		/// <param name="actions">The actions</param>
		/// <param name="tooltip">The tooltip for this part.  Can be modified via <seealso cref="ItemPart.SetTooltip(Material, int, string)"/></param>
		public static void AddPart(Mod mod, Material material, int partID, ItemPartActionsBuilder actions, string tooltip) {
			ItemPartItem item = ItemPartItem.Create(material, partID, actions, tooltip);

			mod.AddContent(item);

			//ModItem.Type is only set after Mod.AddContent is called
			ItemPartItem.registeredPartsByItemID[item.Type] = item.part;

			if (LogAddedParts)
				Instance.Logger.Info($"Added item part \"{item.Name}\" (ID: {item.Type})");
		}

		/// <summary>
		/// Registers a part item for the material, <paramref name="materialType"/>, with the given rarity, <paramref name="rarity"/>
		/// </summary>
		/// <param name="mod">The mod instance to add the part to</param>
		/// <param name="materialType">The item ID</param>
		/// <param name="rarity">The item rarity</param>
		/// <param name="partID">The part ID</param>
		/// <param name="actions">The actions</param>
		/// <param name="tooltip">The tooltip for this part.  Can be modified via <seealso cref="ItemPart.SetTooltip(Material, int, string)"/></param>
		public static void AddPart(Mod mod, int materialType, int rarity, int partID, ItemPartActionsBuilder actions, string tooltip)
			=> AddPart(mod, new Material(){ type = materialType, rarity = rarity }, partID, actions, tooltip);
	}
}