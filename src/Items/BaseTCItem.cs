using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.Abilities;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Numbers;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Items {
	/// <summary>
	/// The base item class for any items that can be created by the Terrarians' Construct Forge UI
	/// </summary>
	public class BaseTCItem : ModItem {
		internal ItemPartSlotCollection parts = new(2);
		internal AbilityCollection abilities;

		public IEnumerable<T> GetAbilities<T>() where T : BaseAbility
			=> abilities.Where(a => a is T).Select(a => (a as T)!);

		public int ammoReserve, ammoReserveMax;

		public readonly int registeredItemID = -1;

		/// <summary>
		/// The current durability for the item
		/// </summary>
		public int CurrentDurability { get; internal set; }

		public virtual int PartsCount => 0;

		protected ReadOnlySpan<ItemPart> GetParts() => parts.ToArray();

		protected IEnumerable<S> GetPartStats<S>(StatType type) where S : class, IPartStats
			=> parts.Where(p => PartRegistry.registeredIDs[p.partID].type == type).Select(p => p.material.GetStat<S>(type)!).Where(s => s is not null);

		protected IEnumerable<HeadPartStats> GetHeadParts() => GetPartStats<HeadPartStats>(StatType.Head);

		protected IEnumerable<HandlePartStats> GetHandleParts() => GetPartStats<HandlePartStats>(StatType.Handle);

		protected IEnumerable<ExtraPartStats> GetExtraParts() => GetPartStats<ExtraPartStats>(StatType.Extra);

		protected IEnumerable<HeadPartStats> SelectToolPickaxeStats()
			=> parts.Where(p => PartRegistry.isPickPart[p.partID]).Select(p => p.material.GetStat<HeadPartStats>(StatType.Head)!).Where(s => s is not null);

		protected IEnumerable<HeadPartStats> SelectToolAxeStats()
			=> parts.Where(p => PartRegistry.isAxePart[p.partID]).Select(p => p.material.GetStat<HeadPartStats>(StatType.Head)!).Where(s => s is not null);

		protected IEnumerable<HeadPartStats> SelectToolHammerStats()
			=> parts.Where(p => PartRegistry.isHammerPart[p.partID]).Select(p => p.material.GetStat<HeadPartStats>(StatType.Head)!).Where(s => s is not null);

		public ItemPart this[int index] {
			get => parts[index];
			set => parts[index] = value;
		}

		/// <summary>
		/// Creates an instance of a <see cref="BaseTCItem"/> using the data from a registered item ID
		/// </summary>
		/// <param name="registeredItemID">The registered item ID</param>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentException"/>
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

		/// <summary>
		/// The name for the item, used in <see cref="SetStaticDefaults"/><br/>
		/// Defaults to:  <c>CoreLibMod.GetItemName(registeredItemID)</c>
		/// </summary>
		public virtual string RegisteredItemTypeName => CoreLibMod.GetItemName(registeredItemID);

		/// <summary>
		/// The tooltip for the item
		/// </summary>
		public virtual string? TooltipText => null;

		public sealed override string Texture => "TerrariansConstructLib/Assets/DummyItem";

		public sealed override void SetStaticDefaults() {
			SafeSetStaticDefaults();

			DisplayName.SetDefault("Constructed " + RegisteredItemTypeName);
			Tooltip.SetDefault((TooltipText is not null ? TooltipText + "\n" : "") +
				"Materials:\n<PART_TYPES>\n" +
				"<PART_TOOLTIPS>\n" +
				"<MODIFIERS>\n" +
				"<AMMO_COUNT>\n" +
				"<DURABILITY>");
		}

		/// <inheritdoc cref="SetStaticDefaults"/>
		public virtual void SafeSetStaticDefaults() { }

		public sealed override void SetDefaults() {
			int[] validPartIDs = CoreLibMod.GetItemValidPartIDs(registeredItemID);

			var data = ItemRegistry.registeredIDs[registeredItemID];

			if (data.mod != Mod || data.itemInternalName != Name)
				throw new Exception($"Registered item ID {registeredItemID} was assigned to an item of type \"{data.mod.Name}:{data.internalName}\" and cannot be assigned to an item of type \"{Mod.Name}:{Name}\"");

			if (validPartIDs.Length != PartsCount)
				throw new ArgumentException($"Part IDs length ({validPartIDs.Length}) for registered item ID \"{CoreLibMod.GetItemInternalName(registeredItemID)}\" ({registeredItemID}) was not equal to the expected length of {PartsCount}");

			ItemPartSlot CreateSlot(int partID, int slot) {
				ItemPart part = new(){
					material = CoreLibMod.RegisteredMaterials.Unknown,
					partID = partID
				};

				return new ItemPartSlot(slot){
					part = part,
					isPartIDValid = id => id == partID
				};
			}

			parts = new(validPartIDs.Select(CreateSlot).ToArray());

			SafeSetDefaults();

			Item.maxStack = 1;
			Item.consumable = false;
		}

		public void InitializeWithParts(params ItemPart[] parts) {
			for (int i = 0; i < parts.Length; i++)
				this.parts[i] = parts[i];

			Item.damage = GetBaseDamage();
			Item.knockBack = GetBaseKnockback();
			Item.crit = GetBaseCrit();
			InitializeUseTimeAndUseSpeed();

			Item.value = (int)parts.Select(p => (p.material.AsItem()?.value ?? 0f) * PartRegistry.registeredIDs[p.partID].materialCost * Material.worthByMaterialID[p.material.Type] / 2f).Sum();

			CurrentDurability = GetMaxDurability();

			for (int i = 0; i < parts.Length; i++)
				parts[i].OnInitialized?.Invoke(parts[i].partID, Item);

			OnInitializedWithParts();

			abilities = new(this);
		}

		/// <summary>
		/// Called after setting the parts on the item.  Use this hook to initialize fields in the item such as <seealso cref="Item.pick"/>
		/// </summary>
		public virtual void OnInitializedWithParts() { }

		/// <inheritdoc cref="SetDefaults"/>
		public virtual void SafeSetDefaults() { }

		public sealed override void AutoStaticDefaults() {
			//Need to get an asset instance just so that we can replace the texture...
			Asset<Texture2D> asset = TextureAssets.Item[Item.type] = ReflectionHelper<Asset<Texture2D>>.InvokeCloneMethod(CoreLibMod.Instance.Assets.Request<Texture2D>("Assets/DummyItem", AssetRequestMode.ImmediateLoad));

			ReflectionHelper<Asset<Texture2D>>.InvokeSetterFunction("ownValue", asset, CoreLibMod.ItemTextures.Get(registeredItemID,
				CoreLibMod.GetItemValidPartIDs(registeredItemID)
					.Select(p => new ItemPart(){ material = CoreLibMod.RegisteredMaterials.Unknown, partID = p })
					.ToArray()));

			if (DisplayName.IsDefault())
				DisplayName.SetDefault(Regex.Replace(Name, "([A-Z])", " $1").Trim());
		}

		public sealed override void ModifyTooltips(List<TooltipLine> tooltips) {
			SafeModifyTooltips(tooltips);

			Utility.FindAndInsertLines(Mod, tooltips, "<PART_TYPES>", i => "PartType_" + i,
				string.Join('\n', GetPartNamesForTooltip()));

			Utility.FindAndInsertLines(Mod, tooltips, "<PART_TOOLTIPS>", i => "PartTooltip_" + i,
				string.Join('\n', GetModifierTooltipLines()));

			Utility.FindAndInsertLines(Mod, tooltips, "<MODIFIERS>", i => "Modifier_" + i,
				string.Join('\n', GetModifierLines()));

			if (ammoReserveMax > 0) {
				float pct = (float)ammoReserve / ammoReserveMax;

				Color color = pct > 0.5f ? Color.Green : pct > 3f / 16f ? Color.Yellow : Color.Red;

				Utility.FindAndModify(tooltips, "<AMMO_COUNT>", $"[c/{color.Hex3()}:{ammoReserve}/{ammoReserveMax}]");
			} else
				Utility.FindAndRemoveLine(tooltips, "<AMMO_COUNT>");

			if (TCConfig.Instance.UseDurability && ammoReserveMax <= 0) {
				int max = GetMaxDurability();

				float pct = (float)CurrentDurability / max;

				Color color = pct > 0.5f ? Color.Green : pct > 3f / 16f ? Color.Yellow : Color.Red;

				Utility.FindAndModify(tooltips, "<DURABILITY>", $"Durability: [c/{color.Hex3()}:{CurrentDurability} / {GetMaxDurability()}]");
			} else
				Utility.FindAndRemoveLine(tooltips, "<DURABILITY>");
		}

		public IEnumerable<string> GetPartNamesForTooltip()
			=> parts.Select(GetItemNameWithRarity).Distinct();

		public IEnumerable<string> GetModifierTooltipLines() {
			List<string> tooltips = parts.Select(p => CoreLibMod.GetPartTooltip(p.material, p.partID)!).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

			return tooltips
				.Distinct()
				.Select(tooltip => {
					if (tooltip.Contains("{R}"))
						tooltip = tooltip.Replace("{R}", Roman.Convert(tooltips.Count(s => s == tooltip)));

					return tooltip;
				});
		}
			//=> parts.Select(p => CoreLibMod.GetPartTooltip(p.material, p.partID)!).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct();

		public IEnumerable<string> GetModifierLines() {
			IReadOnlyList<ModifierText> dummy = new List<ModifierText>();
			return EvaluateModifiers(parts.SelectMany(p => CoreLibMod.GetPartModifierText(p.material, p.partID) ?? dummy)).Where(s => !string.IsNullOrWhiteSpace(s));
		}

		private static string GetItemNameWithRarity(ItemPart part) {
			Item? material = part.material.AsItem();

			string hex = material is null ? "ffffff" : Utility.GetRarityColor(material).Hex3();

			return $"  [c/{hex}:{part.material.GetItemName()} {CoreLibMod.GetPartName(part.partID)}]";
		}

		private static IEnumerable<string> EvaluateModifiers(IEnumerable<ModifierText> orig) {
			//Evaluate all of the lines from the enumeration
			List<ModifierText?> lines = new(orig);

			Dictionary<string, ModifierText> modifiers = new();

			foreach (ModifierText? modifier in lines) {
				string? langKey = modifier?.langKey;

				if (langKey is null)
					continue;

				if (modifiers.ContainsKey(langKey))
					modifiers[langKey].Stat = modifiers[langKey].Stat.CombineWith(modifier!.Stat);
				else
					modifiers[langKey] = modifier!.Clone();
			}

			return modifiers
				.Select(kvp => {
					if (kvp.Value.LangKeyIsLiteral)
						return kvp.Key;

					float value = kvp.Value.useMultiplicativeOnly
						? (kvp.Value.Stat.Multiplicative - 1f) * 100
						: kvp.Value.useAdditiveOnly
							? (kvp.Value.treatAdditiveAsMultiplier ? (kvp.Value.Stat.Additive - 1f) * 100 : kvp.Value.Stat.Additive)
							: ((float)kvp.Value.Stat - 1f) * 100;

					if (value == 0)
						return null!;

					string format = Language.GetTextValue(kvp.Key);

					float num4 = Main.mouseTextColor / 255f;
					byte a = Main.mouseTextColor;

					float colorvalue = kvp.Value.positiveValueIsGoodModifier ? value : -value;

					Color color = colorvalue < 0
						? new Color((byte)(190f * num4), (byte)(120f * num4), (byte)(120f * num4), a)
						: new Color((byte)(120f * num4), (byte)(190f * num4), (byte)(120f * num4), a);

					return "[c/" + color.Hex3() + ":" + string.Format(format, value) + "]";
				});
		}

		/// <inheritdoc cref="ModifyTooltips(List{TooltipLine})"/>
		public virtual void SafeModifyTooltips(List<TooltipLine> tooltips) { }

		public sealed override ModItem Clone(Item item) {
			BaseTCItem clone = (base.Clone(item) as BaseTCItem)!;

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

			if (TCConfig.Instance.UseDurability && CurrentDurability <= 0)
				damage -= 0.7f;
		}

		/// <inheritdoc cref="ModifyWeaponDamage(Player, ref StatModifier, ref float)"/>
		public virtual void SafeModifyWeaponDamage(Player player, ref StatModifier damage, ref float flat) { }

		public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback, ref float flat) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].ModifyWeaponKnockback?.Invoke(parts[i].partID, player, ref knockback, ref flat);

			SafeModifyWeaponKnockback(player, ref knockback, ref flat);

			if (TCConfig.Instance.UseDurability && CurrentDurability <= 0)
				knockback *= 0f;
		}

		/// <inheritdoc cref="ModifyWeaponKnockback(Player, ref StatModifier, ref float)"/>
		public virtual void SafeModifyWeaponKnockback(Player player, ref StatModifier knockback, ref float flat) { }

		public sealed override void ModifyWeaponCrit(Player player, ref int crit) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].ModifyWeaponCrit?.Invoke(parts[i].partID, player, ref crit);

			SafeModifyWeaponCrit(player, ref crit);

			if (TCConfig.Instance.UseDurability && CurrentDurability <= 0)
				crit = 0;
		}

		/// <inheritdoc cref="ModifyWeaponCrit(Player, ref int)"/>
		public virtual void SafeModifyWeaponCrit(Player player, ref int crit) { }

		public sealed override float UseSpeedMultiplier(Player player) {
			float speed = 1f;

			for (int i = 0; i < parts.Length; i++)
				parts[i].UseSpeedMultiplier?.Invoke(parts[i].partID, Item, player, ref speed);

			abilities.UseSpeedMultiplier(player, this, ref speed);

			return (TCConfig.Instance.UseDurability && CurrentDurability <= 0 ? 1f / 1.5f : speed) * SafeUseSpeedMultiplier(player);
		}

		/// <inheritdoc cref="UseSpeedMultiplier(Player)"/>
		public virtual float SafeUseSpeedMultiplier(Player player) => 1f;

		public bool HasPartOfType(int type, out int partIndex) {
			for (int i = 0; i < parts.Length; i++) {
				if (parts[i].material.Type == type) {
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

			abilities.HoldItem(player, this);

			SafeHoldItem(player);
		}

		/// <inheritdoc cref="HoldItem(Player)"/>
		public virtual void SafeHoldItem(Player player) { }

		public sealed override void UpdateInventory(Player player) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].OnUpdateInventory?.Invoke(parts[i].partID, player, Item);

			abilities.UpdateInventory(player, this);

			SafeUpdateInventory(player);
		}

		/// <inheritdoc cref="UpdateInventory(Player)"/>
		public virtual void SafeUpdateInventory(Player player) { }

		public sealed override bool? UseItem(Player player) {
			for (int i = 0; i < parts.Length; i++)
				parts[i].OnUse?.Invoke(parts[i].partID, player, Item);

			SafeUseItem(player);

			return true;
		}

		/// <inheritdoc cref="UseItem(Player)"/>
		public virtual void SafeUseItem(Player player) { }

		public sealed override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
			TryReduceDurability(player, HasAnyToolPower() ? 2 : 1);

			for (int i = 0; i < parts.Length; i++)
				parts[i].OnItemHitNPC?.Invoke(parts[i].partID, Item, player, target, damage, knockBack, crit);

			SafeOnHitNPC(player, target, damage, knockBack, crit);
		}

		/// <inheritdoc cref="OnHitNPC(Player, NPC, int, float, bool)"/>
		public virtual void SafeOnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) { }

		public sealed override void OnHitPvp(Player player, Player target, int damage, bool crit) {
			TryReduceDurability(player, HasAnyToolPower() ? 2 : 1);

			for (int i = 0; i < parts.Length; i++)
				parts[i].OnItemHitPlayer?.Invoke(parts[i].partID, Item, player, target, damage, crit);

			SafeOnHitPvp(player, target, damage, crit);
		}

		/// <inheritdoc cref="OnHitPvp(Player, Player, int, bool)"/>
		public virtual void SafeOnHitPvp(Player player, Player target, int damage, bool crit) { }

		internal void OnTileDestroyed(Player player, int x, int y, TileDestructionContext context) {
			TryReduceDurability(player, 1);

			for (int i = 0; i < parts.Length; i++)
				parts[i].OnTileDestroyed?.Invoke(parts[i].partID, player, Item, x, y, context);

			abilities.OnTileDestroyed(player, this, x, y, context);

			SafeOnTileDestroyed(player, x, y, context);
		}

		/// <summary>
		/// Allows you to customize what happens when this tool destroys a tile.  This method is called clientside.
		/// </summary>
		/// <param name="player">The player doing the mining</param>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <param name="context">The context.  Contains information about what tool type was used to destroy the tile and how much damage was dealt to destroy the tile</param>
		public virtual void SafeOnTileDestroyed(Player player, int x, int y, TileDestructionContext context) { }

		public int GetMaxDurability() {
			double averageHead = GetHeadParts().Average(p => p.durability);
			double averageHandle = GetHandleParts().Average(p => p.durability.Multiplicative);
			double handleAdd = GetHandleParts().Sum(p => p.durability.Additive);
			double averageExtra = GetExtraParts().Average(p => p.Get(CoreLibMod.KnownStatModifiers.ExtraDurability).Multiplicative);
			double extraAdd = GetExtraParts().Average(p => p.Get(CoreLibMod.KnownStatModifiers.ExtraDurability, new StatModifier()).Additive);

			return (int)Math.Max(1, averageHead * (averageHandle + averageExtra - 1) + handleAdd + extraAdd);
		}

		public int GetBaseDamage() {
			double averageHead = GetHeadParts().Average(p => p.damage);
			double averageHandle = GetHandleParts().Average(p => p.attackDamage.Multiplicative);
			double handleAdd = GetHandleParts().Sum(p => p.attackDamage.Additive);

			return (int)Math.Max(1, averageHead * averageHandle + handleAdd);
		}

		public int GetBaseKnockback() {
			double averageHead = GetHeadParts().Average(p => p.knockback);
			double averageHandle = GetHandleParts().Average(p => p.attackKnockback.Multiplicative);
			double handleAdd = GetHandleParts().Sum(p => p.attackKnockback.Additive);

			return (int)Math.Max(1, averageHead * averageHandle + handleAdd);
		}

		protected void InitializeUseTimeAndUseSpeed() {
			float mult = ItemRegistry.registeredIDs[registeredItemID].useSpeedMultiplier;

			if (!HasAnyToolPower())
				Item.useTime = Item.useAnimation = (int)Math.Max(1, GetBaseUseSpeed() * mult);
			else {
				float time = Math.Max(1, GetBaseMiningSpeed() * mult);

				Item.useTime = (int)Math.Max(1, time / 2);
				Item.useAnimation = (int)time;
			}
		}

		public int GetBaseUseSpeed() {
			double averageHead = GetHeadParts().Average(p => p.useSpeed);
			StatModifier handle = GetHandleParts().Sum(p => p.attackSpeed);

			return (int)Math.Max(1, averageHead * handle);
		}

		/// <summary>
		/// Gets the base mining speed for the item
		/// </summary>
		/// <returns>Zero if the item isn't a mining tool, the base mining speed otherwise</returns>
		public int GetBaseMiningSpeed() {
			if (!HasAnyToolPower())
				return 0;

			double averageHead = GetHeadParts().Average(p => p.useSpeed);
			double handle = 1f;

			foreach (var stat in GetHandleParts().Select(p => p.miningSpeed))
				handle *= stat;

			return (int)Math.Max(1, averageHead * handle);
		}

		public int GetBaseCrit() {
			double head = GetHeadParts().Sum(p => p.crit);

			return (int)head;
		}

		public int GetPickaxePower() => AverageToolStats(SelectToolPickaxeStats(), p => p.pickPower);

		public int GetAxePower() => AverageToolStats(SelectToolAxeStats(), p => p.axePower) / 5;

		public int GetHammerPower() => AverageToolStats(SelectToolHammerStats(), p => p.hammerPower);

		private static int AverageToolStats(IEnumerable<HeadPartStats> stats, Func<HeadPartStats, double> func) {
			double average = stats.Average(func);

			return average > 0 ? (int)Math.Ceiling(average) : 0;
		}

		public bool HasAnyToolPower()
			=> SelectToolAxeStats().Any(p => p.axePower > 0) || SelectToolPickaxeStats().Any(p => p.pickPower > 0) || SelectToolHammerStats().Any(p => p.hammerPower > 0);

		public void TryIncreaseDurability(int amount) {
			if (amount <= 0)
				return;

			int max = GetMaxDurability();
			if (CurrentDurability < max && TCConfig.Instance.UseDurability) {
				CurrentDurability += amount;

				if (CurrentDurability > max)
					CurrentDurability = max;
			}
		}

		public void TryReduceDurability(Player player, int amount) {
			if (amount <= 0)
				return;

			bool lose = TCConfig.Instance.UseDurability;

			if (!lose)
				return;

			for (int i = 0; i < parts.Length; i++)
				lose &= parts[i].CanLoseDurability?.Invoke(parts[i].partID, player, Item) ?? true;

			lose &= abilities.CanLoseDurability(player, this);

			if (lose && CurrentDurability > 0) {
				CurrentDurability -= amount;

				if (CurrentDurability < 0)
					CurrentDurability = 0;

				if (CurrentDurability == 0) {
					SoundEngine.PlaySound(SoundID.Tink, player.Center, style: 0);
					SoundEngine.PlaySound(SoundID.Item50, player.Center);

					const int sizeX = 6 * 16;
					const int sizeY = 10 * 16;
					Point tl = (player.Center + new Vector2(-sizeX / 2f, -sizeY / 2f)).ToPoint();
					Rectangle area = new(tl.X, tl.Y, sizeX, sizeY);
					CombatText.NewText(area, CombatText.DamagedFriendlyCrit, Language.GetTextValue("Prefix.Broken").ToUpper(), dramatic: true);
				}
			}
		}

		public override void SaveData(TagCompound tag) {
			tag["parts"] = parts.ToList();
			tag["durability"] = CurrentDurability;
			tag["ammo"] = ammoReserve;
			tag["ammoMax"] = ammoReserveMax;
			
			abilities.SaveData(tag);
		}

		public override void LoadData(TagCompound tag) {
			InitializeWithParts(tag.GetList<ItemPart>("parts").ToArray());

			CurrentDurability = tag.GetInt("durability");
			ammoReserve = tag.GetInt("ammo");
			ammoReserveMax = tag.GetInt("ammoMax");

			abilities = new(this);
			
			abilities.LoadData(tag);

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

			CoreLibMod.writer.WriteLine($"Created recipe for BaseTCItem \"{GetType().GetSimplifiedGenericTypeName()}\"");
			CoreLibMod.writer.Flush();
		}

		public sealed override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			Texture2D texture = CoreLibMod.ItemTextures.Get(registeredItemID, parts);

			spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0);

			int max = GetMaxDurability();
			if (TCConfig.Instance.UseDurability && CurrentDurability < max) {
				Texture2D durabilityBar = CoreLibMod.Instance.Assets.Request<Texture2D>("Assets/DurabilityBar").Value;

				int frameY = (int)(15 * (1 - (float)CurrentDurability / max));
				Rectangle barFrame = durabilityBar.Frame(1, 16, 0, frameY);

				spriteBatch.Draw(durabilityBar, position, barFrame, Color.White, 0f, Vector2.Zero, Main.inventoryScale, SpriteEffects.None, 0);
			}

			return false;
		}

		public sealed override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
			Texture2D texture = CoreLibMod.ItemTextures.Get(registeredItemID, parts);

			Rectangle frame = texture.Frame();

			Vector2 vector = frame.Size() / 2f;
			Vector2 value = new(Item.width / 2 - vector.X, Item.height - frame.Height);
			Vector2 vector2 = Item.position - Main.screenPosition + vector + value;

			spriteBatch.Draw(texture, vector2, frame, lightColor, rotation, vector, scale, SpriteEffects.None, 0);

			return false;
		}
	}
}
