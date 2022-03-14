﻿using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Items {
	public class ItemPart : TagSerializable {
		public delegate void PartItemFunc(int partID, Item item);
		public delegate void PartPlayerFunc(int partID, Player player, Item item);
		public delegate void PartProjectileFunc(int partID, Projectile projectile);
		public delegate void PartProjectileSpawnFunc(int partID, Projectile projectile, IEntitySource source, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1);
		public delegate void PartProjectileHitNPCFunc(int partID, Projectile projectile, NPC target, int damage, float knockback, bool crit);
		public delegate void PartProjectileHitPlayerFunc(int partID, Projectile projectile, Player targetpartID, int damage, bool crit);
		public delegate void PartModifyWeaponDamageFunc(int partID, Player player, ref StatModifier damage, ref float flat);
		public delegate void PartModifyWeaponKnockbackFunc(int partID, Player player, ref StatModifier knockback, ref float flat);
		public delegate void PartModifyWeaponCritFunc(int partID, Player player, ref int crit);

		internal static PartsDictionary<ItemPart> partData;

		public void SetGlobalTooltip(string tooltip)
			=> SetGlobalTooltip(material, partID, tooltip);

		public static void SetGlobalTooltip(Material material, int partID, string tooltip) {
			if (material is not UnloadedMaterial)
				partData.Get(material, partID).tooltip = tooltip;
		}

		public void SetGlobalModifierText(string modifierText)
			=> SetGlobalModifierText(material, partID, modifierText);

		public static void SetGlobalModifierText(Material material, int partID, string modifierText) {
			if (material is not UnloadedMaterial)
				partData.Get(material, partID).tooltip = modifierText;
		}

		/// <summary>
		/// The material used to create this item part
		/// </summary>
		public Material material;

		/// <summary>
		/// The part type associated with this item part
		/// </summary>
		public int partID;

		public string tooltip;

		public string modifierText;

		public virtual ItemPart Clone() => new(){
			material = material.Clone(),
			partID = partID,
			tooltip = tooltip,
			modifierText = modifierText
		};

		public PartItemFunc SetItemDefaults => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).setItemDefaults;

		public PartProjectileFunc SetProjectileDefaults => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).setProjectileDefaults;

		public PartPlayerFunc OnUse => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onUse;

		public PartPlayerFunc OnHold => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onHold;

		public PartPlayerFunc OnGenericHotkeyUsage => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onGenericHotkeyUsage;

		public PartProjectileSpawnFunc OnProjectileSpawn => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onProjectileSpawn;

		public PartProjectileHitNPCFunc OnHitNPC => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onHitNPC;

		public PartProjectileHitPlayerFunc OnHitPlayer => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).onHitPlayer;

		public PartModifyWeaponDamageFunc ModifyWeaponDamage => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).modifyWeaponDamage;

		public PartModifyWeaponKnockbackFunc ModifyWeaponKnockback => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).modifyWeaponKnockback;

		public PartModifyWeaponCritFunc ModifyWeaponCrit => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).modifyWeaponCrit;

		public PartProjectileFunc ProjectileAI => this is UnloadedItemPart ? null : PartActions.GetPartActions(material, partID).projectileAI;

		public TagCompound SerializeData() {
			TagCompound tag = new();
			
			tag["material"] = material;

			if (this is UnloadedItemPart u) {
				tag["part"] = new TagCompound() {
					["mod"] = u.mod,
					["name"] = u.internalName
				};

				return tag;
			}

			var data = PartRegistry.registeredIDs[partID];

			tag["part"] = new TagCompound() {
				["mod"] = data.mod.Name,
				["name"] = data.internalName
			};

			return tag;
		}

		public static Func<TagCompound, ItemPart> DESERIALIZER = tag => {
			Material material = tag.Get<Material>("material");

			if (material is null)
				material = tag.Get<UnloadedMaterial>("material");

			TagCompound part = tag.GetCompound("part");

			string modName = part.GetString("mod");
			string internalName = part.GetString("name");

			if (!ModLoader.TryGetMod(modName, out var mod) || !PartRegistry.TryFindData(mod, internalName, out int id)) {
				// Unloaded part.  Save the mod and name, but nothing else
				return new UnloadedItemPart() {
					mod = modName,
					internalName = internalName,
					partID = -1,
					material = material
				};
			}

			return (ItemPart)partData.Get(material, id).MemberwiseClone();
		};
	}
}
