using Microsoft.Xna.Framework.Input;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Edits;
using TerrariansConstructLib.ID;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Projectiles;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib {
	public class CoreLibMod : Mod {
		public static ModKeybind ActivateAbility;

		private static bool hasReachedPostSetupContent = false;

		public static bool LogAddedParts { get; set; }

		public static CoreLibMod Instance => ModContent.GetInstance<CoreLibMod>();

		public override void Load() {
			hasReachedPostSetupContent = false;

			ConstructedAmmoID.Load();
			MaterialPartID.Load();
			PartActions.builders = new();
			ItemPart.partData = new();
			ItemPartItem.registeredPartsByItemID = new();

			if (!Main.dedServ) {
				ActivateAbility = KeybindLoader.RegisterKeybind(this, "Activate Tool Ability", Keys.G);
			}

			EditsLoader.Load();

			DirectDetourManager.Load();
		}

		public override void PostSetupContent() {
			hasReachedPostSetupContent = true;
		}

		public override void Unload() {
			DirectDetourManager.Unload();
			
			ConstructedAmmoID.Unload();
			MaterialPartID.Unload();

			PartActions.builders = null;
			ItemPart.partData = null;
			ItemPartItem.registeredPartsByItemID = null;
		}

		//No Mod.Call() implementation.  If people want to add content/add support for content to this mod, they better use a strong/weak reference

		/// <summary>
		/// Registers a part definition
		/// </summary>
		/// <param name="internalName">The internal name of the part</param>
		/// <param name="name">The name of the part</param>
		/// <param name="assetFolderPath">The path to the folder containing the part's textures</param>
		/// <returns>The ID of the registered part</returns>
		public static int RegisterPart(string internalName, string name, string assetFolderPath) {
			if (hasReachedPostSetupContent)
				throw new Exception("Method called too late.  This method should be called in Mod.Load()");

			return MaterialPartID.Register(internalName, name, assetFolderPath);
		}

		/// <summary>
		/// Registers a name and <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> for the next constructed ammo type to be assigned an ID
		/// </summary>
		/// <param name="name">The name of the constructed ammo type</param>
		/// <param name="ammoID"></param>
		/// <typeparam name="T">The type of the projectile to spawn when using the ammo</typeparam>
		/// <returns>The ID of the registered constructed ammo type</returns>
		/// <remarks>Note: The returned ID does not correlate with <seealso cref="AmmoID"/> nor <seealso cref="ItemID"/></remarks>
		public static int RegisterAmmo<T>(string name, int ammoID) where T : BaseTCProjectile{
			if (hasReachedPostSetupContent)
				throw new Exception("Method called too late.  This method should be called in Mod.Load()");

			return ConstructedAmmoID.Register<T>(name, ammoID);
		}

		/// <summary>
		/// Gets the name of a registered part
		/// </summary>
		/// <param name="id">The ID of the part to get</param>
		/// <returns>The name of the registered part, or throws an exception if a part of type <paramref name="id"/> does not exist</returns>
		/// <exception cref="Exception"/>
		public static string GetPartName(int id)
			=> id >= 0 && id < MaterialPartID.TotalCount ? MaterialPartID.registeredIDsToNames[id] : throw new Exception($"A part with ID {id} does not exist");

		/// <summary>
		/// Gets the name of a registered constructed ammo type
		/// </summary>
		/// <param name="id">The ID of the constructed ammo type to get</param>
		/// <returns>The name of the registered constructed ammo type, or throws an exception if a constructed ammo type of type <paramref name="id"/> does not exist</returns>
		/// <exception cref="Exception"/>
		/// <remarks>Note: The input ID does not correlate with <seealso cref="AmmoID"/> nor <seealso cref="ItemID"/></remarks>
		public static string GetAmmoName(int id)
			=> id >= 0 && id < ConstructedAmmoID.TotalCount ? ConstructedAmmoID.registeredIDsToNames[id] : null;

		/// <summary>
		/// Gets the <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> of a registered constructed ammo type
		/// </summary>
		/// <param name="constructedAmmoID">The ID of the constructed ammo type to get</param>
		/// <returns>The <seealso cref="ItemID"/>/<seealso cref="AmmoID"/> of the registered constructed ammo type, or throws an exception if a constructed ammo type of type <paramref name="constructedAmmoID"/> does not exist</returns>
		/// <exception cref="Exception"/>
		/// <remarks>Note: The input ID does not correlate with <seealso cref="AmmoID"/> nor <seealso cref="ItemID"/></remarks>
		public static int GetAmmoID(int constructedAmmoID)
			=> constructedAmmoID >= 0 && constructedAmmoID < ConstructedAmmoID.TotalCount
				? ConstructedAmmoID.registeredIDsToAmmoIDs[constructedAmmoID]
				: throw new Exception($"A constructed ammo type with ID {constructedAmmoID} does not exist");

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

			for (int partID = 0; partID < MaterialPartID.TotalCount; partID++) {
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
			ItemPartItem.registeredPartsByItemID[item.Type] = item.part;

			mod.AddContent(item);

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