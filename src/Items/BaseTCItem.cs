using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.Exceptions;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Items {
	/// <summary>
	/// The base item class for any items that can be created by the Terrarians' Construct Forge UI
	/// </summary>
	public class BaseTCItem : ModItem {
		internal ItemPartSlotCollection parts = new(2);

		public int ammoReserve, ammoReserveMax;

		public readonly int registeredItemID = -1;

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

		/// <summary>
		/// Creates an instance of a <see cref="BaseTCItem"/> using the data from a registered item ID
		/// </summary>
		/// <param name="registeredItemID">The registered item ID</param>
		/// <exception cref="Exception"></exception>
		/// <exception cref="ArgumentException"></exception>
		public BaseTCItem(int registeredItemID) {
			this.registeredItemID = registeredItemID;
		}

		//Can't use [Autoload(false)] lest deriving types not get added
		public sealed override bool IsLoadingEnabled(Mod mod) => SafeIsLoadingEnabled(mod) ?? false;

		/// <summary>
		/// Allows you to safely request whether this item should be autoloaded
		/// </summary>
		/// <param name="mod">The mod adding this item</param>
		/// <returns><see langword="null"/> for the default behaviour (don't autoload item), <see langword="true"/> to let the item autoload or <see langword="false"/> to prevent the item from autoloading</returns>
		public virtual bool? SafeIsLoadingEnabled(Mod mod) => null;

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
		/// Defaults to:  <c>CoreLibMod.GetItemName(registeredItemID)</c>
		/// </summary>
		public virtual string RegisteredItemTypeName => CoreLibMod.GetItemName(registeredItemID);

		/// <summary>
		/// The tooltip for the item
		/// </summary>
		public virtual string TooltipText => null;

		public sealed override string Texture => "TerrariansConstructLib/Assets/DummyItem";

		public sealed override void SetStaticDefaults() {
			SafeSetStaticDefaults();

			DisplayName.SetDefault("Constructed " + RegisteredItemTypeName);
			Tooltip.SetDefault((TooltipText is not null ? TooltipText + "\n" : "") +
				"Materials:\n<PART_TYPES>\n" +
				"<PART_TOOLTIPS>\n" +
				"<MODIFIERS>\n" +
				"<AMMO_COUNT>");
		}

		/// <inheritdoc cref="SetStaticDefaults"/>
		public virtual void SafeSetStaticDefaults() { }

		public sealed override void SetDefaults() {
			int[] validPartIDs = CoreLibMod.GetItemValidPartIDs(registeredItemID);

			var data = ItemRegistry.registeredIDs[registeredItemID];

			if (Mod is null)
				throw new Exception("Mod was null... wait what?");

			if (data.mod != Mod || data.itemInternalName != Name)
				throw new Exception($"Registered item ID {registeredItemID} was assigned to an item of type \"{data.mod.Name}:{data.internalName}\" and cannot be assigned to an item of type \"{Mod.Name}:{Name}\"");

			if (validPartIDs.Length != PartsCount)
				throw new ArgumentException($"Part IDs length ({validPartIDs.Length}) for registered item ID \"{CoreLibMod.GetItemInternalName(registeredItemID)}\" ({registeredItemID}) was not equal to the expected length of {PartsCount}");

			ItemPartSlot CreateSlot(int partID, int slot) {
				ItemPart part = new(){
					material = new UnknownMaterial(),
					partID = partID
				};

				return new ItemPartSlot(slot){
					part = part,
					isPartIDValid = id => id == partID
				};
			}

			parts = new(validPartIDs.Select(CreateSlot).ToArray());

			SafeSetDefaults();

			for (int i = 0; i < parts.Length; i++)
				parts[i].SetItemDefaults?.Invoke(parts[i].partID, Item);

			Item.maxStack = 1;
			Item.consumable = false;
		}

		/// <inheritdoc cref="SetDefaults"/>
		public virtual void SafeSetDefaults() { }

		public sealed override void AutoStaticDefaults() {
			//Need to get an asset instance just so that we can replace the texture...
			Asset<Texture2D> asset = TextureAssets.Item[Item.type] = CoreLibMod.Instance.Assets.Request<Texture2D>("Assets/DummyItem", AssetRequestMode.ImmediateLoad);

			ReflectionHelper<Asset<Texture2D>>.InvokeSetterFunction("ownValue", asset, CoreLibMod.itemTextures.Get(registeredItemID,
				new(CoreLibMod.GetItemValidPartIDs(registeredItemID)
					.Select((p, i) => new ItemPartSlot(i){ part = new(){ material = new UnknownMaterial(), partID = p }, isPartIDValid = id => id == p })
					.ToArray())));

			if (DisplayName.IsDefault())
				DisplayName.SetDefault(Regex.Replace(Name, "([A-Z])", " $1").Trim());
		}

		public sealed override void ModifyTooltips(List<TooltipLine> tooltips) {
			Utility.FindAndInsertLines(Mod, tooltips, "<PART_TYPES>", i => "PartType_" + i,
				string.Join('\n', parts.Select(GetItemNameWithRarity).Distinct()));

			Utility.FindAndInsertLines(Mod, tooltips, "<PART_TOOLTIPS>", i => "PartTooltip_" + i,
				string.Join('\n', parts.Select(p => CoreLibMod.GetPartTooltip(p.material, p.partID)).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()));

			Utility.FindAndInsertLines(Mod, tooltips, "<MODIFIERS>", i => "Modifier_" + i,
				string.Join('\n', EvaluateModifiers(parts.Select(p => CoreLibMod.GetPartModifierText(p.material, p.partID))).Where(s => !string.IsNullOrWhiteSpace(s))));

			if (ammoReserveMax > 0)
				Utility.FindAndModify(tooltips, "<AMMO_COUNT>", $"{ammoReserve}/{ammoReserveMax}");
			else
				Utility.FindAndRemoveLine(tooltips, "<AMMO_COUNT>");
		}

		private string GetItemNameWithRarity(ItemPart part) {
			Item material = part.material.AsItem();

			return $"  [c/{Utility.GetRarityColor(material).Hex3()}:{part.material.GetItemName()} {CoreLibMod.GetPartName(part.partID)}]";
		}

		private IEnumerable<string> EvaluateModifiers(IEnumerable<ModifierText> orig) {
			//Evaluate all of the lines from the enumeration
			List<ModifierText> lines = new(orig);

			Dictionary<ItemPart, ModifierText> modifiers = new();

			foreach (ModifierText modifier in lines) {
				var part = modifier.GetPart();

				if (part.GetModifierText() is null)
					continue;

				if (!modifiers.ContainsKey(part))
					modifiers[part] = modifier.Clone();
				else {
					var mod = modifiers[part];

					mod.Stat = mod.Stat.CombineWith(modifier.Stat);
				}
			}

			return modifiers
				.Select(kvp => {
					string format = Language.GetTextValue(kvp.Value.langText);

					if (kvp.Key.GetBaseStatForModifierText is not ItemPart.PartItemModifierFunc func)
						return format;

					float stat = func.Invoke(kvp.Key.partID, Item) * 100;  //0.01 --> 1%

					return string.Format(format, stat * (float)kvp.Value.Stat - stat);
				});
		}

		/// <inheritdoc cref="ModifyTooltips(List{TooltipLine})"/>
		public virtual void SafeModifyTooltips(List<TooltipLine> tooltips) { }

		public sealed override ModItem Clone(Item item) {
			BaseTCItem clone = base.Clone(item) as BaseTCItem;

			Clone(item, clone);

			return clone;
		}

		/// <inheritdoc cref="Clone(Item)"/>
		public virtual void Clone(Item item, BaseTCItem clone) { }

		public sealed override bool CanBeConsumedAsAmmo(Player player) => false;

		public sealed override void PickAmmo(Item weapon, Player player, ref int type, ref float speed, ref int damage, ref float knockback) { }

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

		public sealed override void AddRecipes() {
			var data = ItemRegistry.registeredIDs[registeredItemID];

			Recipe recipe = CreateRecipe();

			foreach (int part in data.validPartIDs)
				recipe.AddRecipeGroup(CoreLibMod.GetRecipeGroupName(part));

			// TODO: forge tile?

			recipe.AddCondition(NetworkText.FromLiteral("Must be crafted from the Forge UI"), r => false);

			recipe.Register();

			CoreLibMod.Instance.Logger.Debug($"Created recipe for BaseTCItem \"{GetType().GetSimplifiedGenericTypeName()}\"");
		}

		public sealed override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			Texture2D texture = CoreLibMod.itemTextures.Get(registeredItemID, parts);

			spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0);

			return false;
		}

		public sealed override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
			Texture2D texture = CoreLibMod.itemTextures.Get(registeredItemID, parts);

			Rectangle frame = texture.Frame();

			Vector2 vector = frame.Size() / 2f;
			Vector2 value = new(Item.width / 2 - vector.X, Item.height - frame.Height);
			Vector2 vector2 = Item.position - Main.screenPosition + vector + value;

			spriteBatch.Draw(texture, vector2, frame, lightColor, rotation, vector, scale, SpriteEffects.None, 0);

			return false;
		}
	}
}
