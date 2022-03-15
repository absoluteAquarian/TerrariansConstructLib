﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Items {
	/// <summary>
	/// The base item class for any items that can be created by the Terrarians' Construct Forge UI
	/// </summary>
	[Autoload(false)]
	public class BaseTCItem : ModItem {
		internal ItemPartSlotCollection parts;

		public int ammoReserve, ammoReserveMax;

		public readonly int registeredItemID;

		public virtual int PartsCount => 0;

		/// <summary>
		/// The location of the folder for this item's visuals (item part pieces used for constructing the texture) <br/>
		/// Example: <c>"TerrariansConstruct/Assets/Visuals/Sword"</c>
		/// </summary>
		public virtual string VisualsFolderPath => null;

		protected ReadOnlySpan<ItemPart> GetParts() => parts.ToArray();

		protected ItemPart this[int index] {
			get => parts[index];
			set => parts[index] = value;
		}

		public BaseTCItem() {
			parts = new(PartsCount);
			registeredItemID = -1;
		}

		/// <summary>
		/// Creates an instance of a <see cref="BaseTCItem"/> using the data from a registered item ID
		/// </summary>
		/// <param name="registeredItemID">The registered item ID</param>
		/// <exception cref="Exception"></exception>
		/// <exception cref="ArgumentException"></exception>
		public BaseTCItem(int registeredItemID) {
			int[] validPartIDs = CoreLibMod.GetItemValidPartIDs(registeredItemID);

			var data = ItemRegistry.registeredIDs[registeredItemID];

			if (data.mod != Mod || data.itemInternalName != Name)
				throw new Exception($"Registered item ID {registeredItemID} was assigned to an item of type \"{data.mod.Name}:{data.internalName}\" and cannot be assigned to an item of type \"{Mod.Name}:{Name}\"");

			this.registeredItemID = registeredItemID;

			if (validPartIDs.Length != PartsCount)
				throw new ArgumentException($"Part IDs length ({validPartIDs.Length}) for registered item ID \"{CoreLibMod.GetItemInternalName(registeredItemID)}\" ({registeredItemID}) was not equal to the expected length of {PartsCount}");

			parts = new(validPartIDs.Select((p, i) => new ItemPartSlot(i){ isPartIDValid = id => id == p }).ToArray());
		}

		public void SetUseNoAmmo() {
			Item.shoot = ProjectileID.None;
			Item.ammo = 0;
		}

		public void SetUseAmmo(int constructedAmmoID) {
			Item.shoot = CoreLibMod.GetAmmoProjectileType(constructedAmmoID);
			Item.useAmmo = CoreLibMod.GetAmmoID(constructedAmmoID);
		}

		// TODO: move these to an Ability object maybe?
		public float copperPartCharge;
		public const float CopperPartChargeMax = 6f * 2.5f * 60 * 60;  //6 velocity for at least 2.5 minutes
		public bool copperChargeActivated;

		public bool CopperChargeReady => !copperChargeActivated && copperPartCharge >= CopperPartChargeMax;

		/// <summary>
		/// The name for the item, used in <see cref="SetStaticDefaults"/><br/>
		/// Defaults to:  <c>CoreLibMod.GetItemInternalName(registeredItemID)</c>
		/// </summary>
		public virtual string RegisteredItemTypeName => CoreLibMod.GetItemInternalName(registeredItemID);

		/// <summary>
		/// The tooltip for the item
		/// </summary>
		public virtual string TooltipText => null;

		public sealed override void SetStaticDefaults() {
			SafeSetStaticDefaults();

			DisplayName.SetDefault("Constructed " + RegisteredItemTypeName);
			Tooltip.SetDefault((TooltipText is not null ? TooltipText + "\n" : "") +
				"<PART_TYPES>\n" +
				"<PART_TOOLTIPS>\n" +
				"<MODIFIERS>\n" +
				"<AMMO_COUNT>");
		}

		/// <inheritdoc cref="SetStaticDefaults"/>
		public virtual void SafeSetStaticDefaults() { }

		public sealed override void SetDefaults() {
			SafeSetDefaults();

			for (int i = 0; i < parts.Length; i++)
				parts[i].SetItemDefaults?.Invoke(parts[i].partID, Item);

			Item.maxStack = 1;
			Item.consumable = false;
		}

		/// <inheritdoc cref="SetDefaults"/>
		public virtual void SafeSetDefaults() { }

		public sealed override void ModifyTooltips(List<TooltipLine> tooltips) {
			Utility.FindAndInsertLines(Mod, tooltips, "<PART_TYPES>", i => "PartType_" + i,
				string.Join('\n', parts.Select(p => p.material.GetItemName())));

			Utility.FindAndInsertLines(Mod, tooltips, "<PART_TYPES>", i => "PartTooltip_" + i,
				string.Join('\n', parts.Select(p => p.tooltip).Where(s => !string.IsNullOrWhiteSpace(s))));

			Utility.FindAndInsertLines(Mod, tooltips, "<MODIFIERS>", i => "Modifier_" + i,
				string.Join('\n', parts.Select(p => p.modifierText).Where(s => !string.IsNullOrWhiteSpace(s))));

			if (ammoReserveMax > 0)
				Utility.FindAndModify(tooltips, "<AMMO_COUNT>", $"{ammoReserve}/{ammoReserveMax}");
			else
				Utility.FindAndRemoveLine(tooltips, "<AMMO_COUNT>");
		}

		/// <inheritdoc cref="ModifyTooltips(List{TooltipLine})"/>
		public virtual void SafeModifyTooltips(List<TooltipLine> tooltips) { }

		public sealed override ModItem Clone(Item item) {
			BaseTCItem source = item.ModItem as BaseTCItem;
			BaseTCItem clone = new();

			Clone(source, clone);

			clone.parts = new(source.parts.ToArray());

			return clone;
		}

		/// <inheritdoc cref="Clone(Item)"/>
		public virtual void Clone(BaseTCItem source, BaseTCItem clone) { }

		public sealed override bool CanBeConsumedAsAmmo(Player player) => false;

		public sealed override void ModifyWeaponDamage(Player player, ref StatModifier damage, ref float flat) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].ModifyWeaponDamage?.Invoke(parts[i].partID, player, ref damage, ref flat);

			SafeModifyWeaponDamage(player, ref damage, ref flat);
		}

		/// <inheritdoc cref="ModifyWeaponDamage(Player, ref StatModifier, ref float)"/>
		public virtual void SafeModifyWeaponDamage(Player player, ref StatModifier damage, ref float flat) { }

		public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback, ref float flat) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].ModifyWeaponKnockback?.Invoke(parts[i].partID, player, ref knockback, ref flat);

			SafeModifyWeaponKnockback(player, ref knockback, ref flat);
		}

		/// <inheritdoc cref="ModifyWeaponKnockback(Player, ref StatModifier, ref float)"/>
		public virtual void SafeModifyWeaponKnockback(Player player, ref StatModifier knockback, ref float flat) { }

		public sealed override void ModifyWeaponCrit(Player player, ref int crit) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].ModifyWeaponCrit?.Invoke(parts[i].partID, player, ref crit);

			SafeModifyWeaponCrit(player, ref crit);
		}

		/// <inheritdoc cref="ModifyWeaponCrit(Player, ref int)"/>
		public virtual void SafeModifyWeaponCrit(Player player, ref int crit) { }

		public bool HasPartOfType(int type, out int partIndex) {
			for (int i = 0; i < parts.Length; i++) {
				if (parts[i].material.type == type) {
					partIndex = i;
					return true;
				}
			}

			partIndex = -1;
			return false;
		}

		public sealed override void HoldItem(Player player) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].OnHold?.Invoke(parts[i].partID, player, Item);

			//Hardcoded here to make the ability only apply once, regardless of how many parts are Copper
			// TODO: have a flag or something dictate if a part's ability should only activate once
			if (HasPartOfType(ItemID.CopperBar, out _)) {
				if (copperChargeActivated) {
					copperPartCharge -= CopperPartChargeMax / (7 * 60);  //7 seconds of usage

					const int area = 6;

					if (Main.rand.NextFloat() < 0.3f) {
						Vector2 velocity = Vector2.UnitX.RotatedByRandom(MathHelper.Pi) * 3f;

						Dust.NewDust(player.Center - new Vector2(area / 2f), area, area, DustID.MartianSaucerSpark, velocity.X, velocity.Y);
					}

					if (copperPartCharge < 0) {
						copperPartCharge = 0;
						copperChargeActivated = false;
					}
				} else if (player.velocity.Y == 0 && (player.controlLeft || player.controlRight)) {
					copperPartCharge += Math.Abs(player.velocity.X);

					if (copperPartCharge > CopperPartChargeMax)
						copperPartCharge = CopperPartChargeMax;
				}
			}

			SafeHoldItem(player);
		}

		/// <inheritdoc cref="HoldItem(Player)"/>
		public virtual void SafeHoldItem(Player player) { }

		public sealed override bool? UseItem(Player player) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].OnUse?.Invoke(parts[i].partID, player, Item);

			SafeUseItem(player);

			return true;
		}

		/// <inheritdoc cref="UseItem(Player)"/>
		public virtual void SafeUseItem(Player player) { }

		public override void SaveData(TagCompound tag) {
			tag["parts"] = parts.ToList();
		}

		public override void LoadData(TagCompound tag) {
			parts = new(tag.GetList<ItemPart>("parts").ToArray());

			if (parts.Length != PartsCount)
				throw new IOException($"Saved parts list length ({parts.Length}) was not equal to the expected length of {PartsCount}");
		}
	}
}
