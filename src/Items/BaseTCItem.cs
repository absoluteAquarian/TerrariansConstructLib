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
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Definitions;
using TerrariansConstructLib.API.Numbers;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.API.Sources;
using TerrariansConstructLib.API.Stats;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Modifiers;

namespace TerrariansConstructLib.Items {
	/// <summary>
	/// The base item class for any items that can be created by the Terrarians' Construct Forge UI
	/// </summary>
	public abstract class BaseTCItem : ModItem {
		internal ItemPartSlotCollection parts;
		internal ModifierCollection modifiers;

		public T? GetModifier<T>() where T : BaseTrait
			=> modifiers.FirstOrDefault(t => t.GetType() == typeof(T)) as T;

		public int CountParts(Material material)
			=> parts.Count(p => p.material.Type == material.Type);

		/// <summary>
		/// The ID of the <see cref="TCItemDefinition"/> that this item retrieves its data from
		/// </summary>
		public abstract int ItemDefinition { get; }

		/// <summary>
		/// The ID of the <see cref="AmmoID"/> or item ID that this item is classified under.
		/// Defaults to 0, meaning this item is not ammo.
		/// </summary>
		public virtual int AmmoIDClassification => ItemID.None;

		/// <summary>
		/// The ID of the <see cref="AmmoID"/> or item ID that this item's ammo is classified under.
		/// Defaults to 0, meaning this item does not use ammo.
		/// </summary>
		public virtual int UseAmmoIDClassification => ItemID.None;

		/// <summary>
		/// The current durability for the item
		/// </summary>
		public int CurrentDurability { get; internal set; }

		private int? cachedPartsCount;
		public int PartsCount => cachedPartsCount ??= ItemDefinitionLoader.Get(ItemDefinition)?.GetForgeSlotConfiguration().Count() ?? 0;

		public ReadOnlySpan<ItemPart> GetParts() => parts.ToArray();

		protected IEnumerable<ItemPart> FilterParts(StatType type)
			=> parts?.Where(p => PartDefinitionLoader.Get(p.partID)?.StatType == type) ?? Array.Empty<ItemPart>();

		protected IEnumerable<ItemPart> FilterHeadParts() => FilterParts(StatType.Head);

		protected IEnumerable<ItemPart> FilterHandleParts() => FilterParts(StatType.Handle);

		protected IEnumerable<ItemPart> FilterExtraParts() => FilterParts(StatType.Extra);

		protected IEnumerable<S> GetPartStats<S>(StatType type) where S : class, IPartStats
			=> parts.Where(p => PartDefinitionLoader.Get(p.partID)?.StatType == type).Select(p => p.GetStat<S>(type)!).Where(s => s is not null);

		protected IEnumerable<HeadPartStats> GetHeadParts() => GetPartStats<HeadPartStats>(StatType.Head);

		protected IEnumerable<HandlePartStats> GetHandleParts() => GetPartStats<HandlePartStats>(StatType.Handle);

		protected IEnumerable<ExtraPartStats> GetExtraParts() => GetPartStats<ExtraPartStats>(StatType.Extra);

		protected IEnumerable<HeadPartStats> SelectToolPickaxeStats()
			=> parts.Where(p => (PartDefinitionLoader.Get(p.partID)?.ToolType & ToolType.Pickaxe) != 0).Select(p => p.GetStat<HeadPartStats>(StatType.Head)!).Where(s => s is not null);

		protected IEnumerable<HeadPartStats> SelectToolAxeStats()
			=> parts.Where(p => (PartDefinitionLoader.Get(p.partID)?.ToolType & ToolType.Axe) != 0).Select(p => p.GetStat<HeadPartStats>(StatType.Head)!).Where(s => s is not null);

		protected IEnumerable<HeadPartStats> SelectToolHammerStats()
			=> parts.Where(p => (PartDefinitionLoader.Get(p.partID)?.ToolType & ToolType.Hammer) != 0).Select(p => p.GetStat<HeadPartStats>(StatType.Head)!).Where(s => s is not null);

		public ItemPart this[int index] {
			get => parts[index];
			set => parts[index] = value;
		}

		/// <summary>
		/// Creates an instance of a <see cref="BaseTCItem"/>
		/// </summary>
		/// <exception cref="Exception"/>
		/// <exception cref="ArgumentException"/>
		protected BaseTCItem() {
			parts = new(PartsCount);
		}

		/// <summary>
		/// The name for the item, used in <see cref="SetStaticDefaults"/><br/>
		/// Defaults to:  <c>ItemDefinitionLoader.Get(registeredItemID)!.Name</c>
		/// </summary>
		public virtual string RegisteredItemTypeName => ItemDefinitionLoader.Get(ItemDefinition)!.Name;

		/// <summary>
		/// The tooltip for the item
		/// </summary>
		public virtual string? TooltipText => null;

		public sealed override string Texture => "TerrariansConstructLib/Assets/DummyItem";

		public sealed override void SetStaticDefaults() {
			SafeSetStaticDefaults();

			DisplayName.SetDefault("Constructed " + RegisteredItemTypeName);
			Tooltip.SetDefault((TooltipText is not null ? TooltipText + "\n" : "") +
				"<PART_TYPES>\n" +
				"<PART_TOOLTIPS>\n" +
				"<DURABILITY>\n" +
				"<ABILITY_COUNTERS>");
		}

		/// <inheritdoc cref="SetStaticDefaults"/>
		public virtual void SafeSetStaticDefaults() { }

		public sealed override void SetDefaults() {
			var data = ItemDefinitionLoader.Get(ItemDefinition)!;

			int[] validPartIDs = data.GetValidPartIDs().ToArray();

			if (data.Mod != Mod || ModContent.GetModItem(data.ItemType).Name != Name)
				throw new Exception($"Registered item ID {ItemDefinition} was assigned to an item of type \"{data.Mod.Name}:{ModContent.GetModItem(data.ItemType).Name}\" and cannot be assigned to an item of type \"{Mod.Name}:{Name}\"");

			if (validPartIDs.Length != PartsCount)
				throw new ArgumentException($"Part IDs length ({validPartIDs.Length}) for registered item ID \"{data.Name}\" ({ItemDefinition}) was not equal to the expected length of {PartsCount}");

			static ItemPartSlot CreateSlot(int partID, int slot) {
				ItemPart part = new(){
					material = CoreLibMod.RegisteredMaterials.Unknown,
					partID = partID
				};

				return new ItemPartSlot(slot){
					part = part
				};
			}

			parts = new(validPartIDs.Select(CreateSlot).ToArray());

			SafeSetDefaults();

			Item.maxStack = 1;
			Item.consumable = false;

			int ammo = AmmoIDClassification;

			if (ammo > ItemID.None) {
				Item.ammo = ammo;
				Item.useAmmo = ItemID.None;
				Item.shoot = ItemDefinitionLoader.Get(ItemDefinition)!.ProjectileSpawnedFromAmmo;
			}

			int useAmmo = UseAmmoIDClassification;

			if (useAmmo > 0)
				Item.useAmmo = useAmmo;
		}

		public void InitializeWithParts(params ItemPart[] parts) {
			var data = CoreLibMod.GetItemDefinition(ItemDefinition)!;
			
			int[] validIDs = data.GetValidPartIDs().ToArray();

			if (parts.Length != PartsCount)
				throw new ArgumentException($"Parts length ({parts.Length}) was not equal to the expected length of {PartsCount}");
			
			for (int i = 0; i < parts.Length; i++) {
				ItemPart part = this.parts[i] = parts[i];

				if (part is UnloadedItemPart unloaded) {
					if (CoreLibMod.MaterialType(part.material) > -1 && ModLoader.TryGetMod(unloaded.mod, out Mod source) && source.TryFind(unloaded.internalName, out PartDefinition definition)) {
						//Reassign the part since it's no longer unloaded
						this.parts[i] = part = new ItemPart() {
							material = part.material,
							partID = definition.Type
						};
					}
				}

				//Failsafe
				if (part.partID == -1)
					part.partID = validIDs[i];
			}

			Item.damage = GetBaseDamage();
			Item.knockBack = GetBaseKnockback();
			Item.crit = GetBaseCrit();
			InitializeUseTimeAndUseSpeed();

			Item.value = (int)parts.Select(p => (p.material.AsItem()?.value ?? 0f)
				* (PartDefinitionLoader.Get(p.partID)?.MaterialCost ?? 0)
				* (CoreLibMod.GetMaterialDefinition(p.material)?.MaterialWorth ?? 0) / 2f).Sum();

			CurrentDurability = GetMaxDurability();

			modifiers = new(this);

			OnInitializedWithParts();
		}

		/// <summary>
		/// Called after setting the parts on the item.  Use this hook to initialize fields in the item such as <seealso cref="Item.pick"/>
		/// </summary>
		public virtual void OnInitializedWithParts() { }

		/// <inheritdoc cref="SetDefaults"/>
		public virtual void SafeSetDefaults() { }

		public sealed override void AutoStaticDefaults() {
			//Need to get an asset instance just so that we can replace the texture...
			TextureAssets.Item[Type] = Utility.CloneAndOverwriteValue(
				CoreLibMod.Instance.Assets.Request<Texture2D>("Assets/DummyItem", AssetRequestMode.ImmediateLoad),
				CoreLibMod.ItemTextures.Get(
					ItemDefinition,
					ItemDefinitionLoader.Get(ItemDefinition)!.GetValidPartIDs().Select(p => new ItemPart(){ material = CoreLibMod.RegisteredMaterials.Unknown, partID = p }).ToArray(),
					Array.Empty<BaseTrait>()));

			if (DisplayName.IsDefault())
				DisplayName.SetDefault(Regex.Replace(Name, "([A-Z])", " $1").Trim());
		}

		public sealed override void ModifyTooltips(List<TooltipLine> tooltips) {
			SafeModifyTooltips(tooltips);
			
			if (TCClientConfig.Instance.DisplayItemParts)
				Utility.FindAndInsertLines(Mod, tooltips, "<PART_TYPES>", i => "PartType_" + i, "Parts:\n" + string.Join('\n', GetPartNamesForTooltip()));
			else
				Utility.FindAndRemoveLine(tooltips, "<PART_TOOLTIPS>");
			
			Utility.FindAndInsertLines(Mod, tooltips, "<PART_TOOLTIPS>", i => "PartTooltip_" + i, string.Join('\n', GetModifierTooltipLines()));

			if (TCConfig.Instance.UseDurability) {
				int max = GetMaxDurability();

				float pct = (float)CurrentDurability / max;

				Color color = pct > 0.5f ? Color.Green : pct > 3f / 16f ? Color.Yellow : Color.Red;

				Utility.FindAndModify(tooltips, "<DURABILITY>", $"Durability: [c/{color.Hex3()}:{CurrentDurability} / {GetMaxDurability()}]");
			} else
				Utility.FindAndRemoveLine(tooltips, "<DURABILITY>");

			if (TCClientConfig.Instance.DisplayAbilityCounters)
				Utility.FindAndInsertLines(Mod, tooltips, "<ABILITY_COUNTERS>", i => "CounterTooltip_" + i, "Counters:\n" + string.Join('\n', GetCounterLines()));
			else
				Utility.FindAndRemoveLine(tooltips, "<ABILITY_COUNTERS>");
		}

		public IEnumerable<string> GetPartNamesForTooltip()
			=> parts.Select(GetItemNameWithRarity).Distinct();

		public IEnumerable<string> GetModifierTooltipLines() {
			if (modifiers is null)
				return Array.Empty<string>();

			return modifiers.Select(t => (t, $"[c/{t.TooltipColor.Hex3()}:{Language.GetTextValue(t.LangKey)} {(t is BaseModifier ? " ({T})" : "")}]"))
				.Distinct()
				.Select(tuple => {
					string tooltip = tuple.Item2;
					
					if (tooltip.Contains("{R}"))
						tooltip = tooltip.Replace("{R}", Roman.Convert(tuple.t.Tier));
					
					if (tooltip.Contains("{T}") && tuple.t is BaseModifier m)
						tooltip = tooltip.Replace("{T}", $"{m.CurrentUpgradeProgress} / {m.UpgradeTarget}");

					return tooltip;
				})
				.DefaultIfEmpty("none");
		}

		private static string GetItemNameWithRarity(ItemPart part) {
			Item? material = part.material.AsItem();

			string hex = material is null ? "ffffff" : Utility.GetRarityColor(material).Hex3();

			return $"  [c/{hex}:{part.material.GetItemName()} {PartDefinitionLoader.Get(part.partID)!.DisplayName}]";
		}

		private IEnumerable<string> GetCounterLines() {
			if (modifiers is null)
				return Array.Empty<string>();
			
			return modifiers.Select(m => (m, $"  [c/{m.TooltipColor.Hex3()}:{Language.GetTextValue(m.LangKey)}]"))
				.Select(tuple => {
					string tooltip = tuple.Item2;
					
					if (tooltip.Contains("{R}"))
						tooltip = tooltip.Replace("{R}", Roman.Convert(tuple.m.Tier));

					return (tuple.m, tooltip);
				})
				.Select(tuple => tuple.tooltip + "\n" +
					$"    {tuple.m.Counter:0.######}")
				.DefaultIfEmpty("none");
		}

		/// <inheritdoc cref="ModifyTooltips(List{TooltipLine})"/>
		public virtual void SafeModifyTooltips(List<TooltipLine> tooltips) { }

		protected override bool CloneNewInstances => true;

		public sealed override ModItem Clone(Item item) {
			BaseTCItem clone = (base.Clone(item) as BaseTCItem)!;

			Clone(item, clone);

			return clone;
		}

		/// <inheritdoc cref="Clone(Item)"/>
		public virtual void Clone(Item item, BaseTCItem clone) { }

		public sealed override bool CanBeConsumedAsAmmo(Item weapon, Player player) => false;

		public sealed override void PickAmmo(Item weapon, Player player, ref int type, ref float speed, ref StatModifier damage, ref float knockback) { }

		public sealed override bool CanConsumeAmmo(Item ammo, Player player) {
			bool consume = ammo.ModItem is not BaseTCItem tc || modifiers.CanConsumeAmmo(this, tc, player);

			consume &= SafeCanConsumeAmmo(ammo, player);

			return consume;
		}

		/// <inheritdoc cref="CanConsumeAmmo(Item, Player)"/>
		public virtual bool SafeCanConsumeAmmo(Item ammo, Player player) => true;

		public sealed override bool? CanChooseAmmo(Item ammo, Player player) {
			bool valid = ammo.ammo == Item.useAmmo && (ammo.ModItem is not BaseTCItem tc || tc.CurrentDurability > 0);

			valid &= SafeCanChooseAmmo(ammo, player);

			return valid;
		}

		/// <inheritdoc cref="CanChooseAmmo(Item, Player)"/>
		public virtual bool SafeCanChooseAmmo(Item ammo, Player player) => true;

		public sealed override bool? CanBeChosenAsAmmo(Item weapon, Player player) {
			bool valid = Item.ammo == weapon.useAmmo && CurrentDurability > 0;

			valid &= SafeCanBeChosenAsAmmo(weapon, player);

			return valid;
		}

		/// <inheritdoc cref="CanBeChosenAsAmmo(Item, Player)"/>
		/// <remarks>NOTE: Terrarians' Construct disables <see langword="null"/> returns since Item.ammo and Item.useAmmo are already checked</remarks>
		public virtual bool SafeCanBeChosenAsAmmo(Item weapon, Player player) => true;

		public sealed override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			modifiers.ModifyWeaponDamage(player, this, ref damage);

			SafeModifyWeaponDamage(player, ref damage);

			if (TCConfig.Instance.UseDurability && CurrentDurability <= 0)
				damage -= 0.7f;
		}

		/// <inheritdoc cref="ModifyWeaponDamage(Player, ref StatModifier)"/>
		public virtual void SafeModifyWeaponDamage(Player player, ref StatModifier damage) { }

		public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback) {
			modifiers.ModifyWeaponKnockback(player, this, ref knockback);

			SafeModifyWeaponKnockback(player, ref knockback);

			if (TCConfig.Instance.UseDurability && CurrentDurability <= 0)
				knockback *= 0f;
		}

		/// <inheritdoc cref="ModifyWeaponKnockback(Player, ref StatModifier)"/>
		public virtual void SafeModifyWeaponKnockback(Player player, ref StatModifier knockback) { }

		public sealed override void ModifyWeaponCrit(Player player, ref float crit) {
			modifiers.ModifyWeaponCrit(player, this, ref crit);

			SafeModifyWeaponCrit(player, ref crit);

			if (TCConfig.Instance.UseDurability && CurrentDurability <= 0)
				crit = 0;
		}

		/// <inheritdoc cref="ModifyWeaponCrit(Player, ref float)"/>
		public virtual void SafeModifyWeaponCrit(Player player, ref float crit) { }

		public sealed override float UseSpeedMultiplier(Player player) {
			StatModifier speed = StatModifier.Default;

			modifiers.UseSpeedMultiplier(player, this, ref speed);

			return (TCConfig.Instance.UseDurability && CurrentDurability <= 0 ? 1f / 1.5f : speed.ApplyTo(1f)) * SafeUseSpeedMultiplier(player);
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
			modifiers.HoldItem(player, this);

			SafeHoldItem(player);
		}

		/// <inheritdoc cref="HoldItem(Player)"/>
		public virtual void SafeHoldItem(Player player) { }

		public sealed override void UpdateInventory(Player player) {
			modifiers.UpdateInventory(player, this);

			SafeUpdateInventory(player);
		}

		/// <inheritdoc cref="UpdateInventory(Player)"/>
		public virtual void SafeUpdateInventory(Player player) { }

		public sealed override bool? UseItem(Player player) {
			modifiers.UseItem(player, this);

			SafeUseItem(player);

			return true;
		}

		/// <inheritdoc cref="UseItem(Player)"/>
		public virtual void SafeUseItem(Player player) { }

		public sealed override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
			TryReduceDurability(player, 1, new DurabilityModificationSource_HitEntity(target, HasAnyToolPower()));

			modifiers.OnHitNPC(player, target, this, damage, knockBack, crit);

			SafeOnHitNPC(player, target, damage, knockBack, crit);
		}

		/// <inheritdoc cref="OnHitNPC(Player, NPC, int, float, bool)"/>
		public virtual void SafeOnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) { }

		public sealed override void OnHitPvp(Player player, Player target, int damage, bool crit) {
			TryReduceDurability(player, 1, new DurabilityModificationSource_HitEntity(target, HasAnyToolPower()));

			modifiers.OnHitPlayer(player, target, this, damage, crit);

			SafeOnHitPvp(player, target, damage, crit);
		}

		/// <inheritdoc cref="OnHitPvp(Player, Player, int, bool)"/>
		public virtual void SafeOnHitPvp(Player player, Player target, int damage, bool crit) { }

		public sealed override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
			modifiers.ModifyHitNPC(player, target, this, ref damage, ref knockBack, ref crit);

			SafeModifyHitNPC(player, target, ref damage, ref knockBack, ref crit);
		}

		/// <inheritdoc cref="ModifyHitNPC(Player, NPC, ref int, ref float, ref bool)"/>
		public virtual void SafeModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) { }

		public sealed override void ModifyHitPvp(Player player, Player target, ref int damage, ref bool crit) {
			modifiers.ModifyHitPlayer(player, target, this, ref damage, ref crit);

			SafeModifyHitPlayer(player, target, ref damage, ref crit);
		}

		/// <inheritdoc cref="ModifyHitPvp(Player, Player, ref int, ref bool)"/>
		public virtual void SafeModifyHitPlayer(Player player, Player target, ref int damage, ref bool crit) { }

		internal void OnTileDestroyed(Player player, int x, int y, TileDestructionContext context) {
			TryReduceDurability(player, 1, new DurabilityModificationSource_Mining(context, x, y));

			modifiers.OnTileDestroyed(player, this, x, y, context);

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
			double averageHead = AverageWeightedHeadStats(p => p.durability, 1);
			double averageHandle = GetHandleParts().Average(p => p.durability.Multiplicative);
			double handleAdd = GetHandleParts().Sum(p => p.durability.Additive);
			double averageExtra = GetExtraParts().Average(p => p.Get(CoreLibMod.KnownStatModifiers.ExtraDurability).Multiplicative);
			double extraAdd = GetExtraParts().Average(p => p.Get(CoreLibMod.KnownStatModifiers.ExtraDurability, new StatModifier()).Additive);

			return (int)Math.Max(1, averageHead * (averageHandle + averageExtra - 1) + handleAdd + extraAdd);
		}

		public int GetBaseDamage() {
			double averageHead = AverageWeightedHeadStats(p => p.damage, 1);
			double averageHandle = GetHandleParts().Average(p => p.attackDamage.Multiplicative);
			double handleAdd = GetHandleParts().Sum(p => p.attackDamage.Additive);

			return (int)Math.Max(1, averageHead * averageHandle + handleAdd);
		}

		public int GetBaseKnockback() {
			double averageHead = AverageWeightedHeadStats(p => p.knockback, 0);
			double averageHandle = GetHandleParts().Average(p => p.attackKnockback.Multiplicative);
			double handleAdd = GetHandleParts().Sum(p => p.attackKnockback.Additive);

			return (int)Math.Max(1, averageHead * averageHandle + handleAdd);
		}

		protected void InitializeUseTimeAndUseSpeed() {
			var data = ItemDefinitionLoader.Get(ItemDefinition)!;
			float mult = data.UseSpeedMultiplier;

			if (!HasAnyToolPower())
				Item.useTime = Item.useAnimation = (int)Math.Max(1, GetBaseUseSpeed() * mult);
			else {
				float time = Math.Max(1, GetBaseMiningSpeed() * mult);

				Item.useTime = (int)Math.Max(1, time / data.HitsPerToolSwing * 1.3f);
				Item.useAnimation = (int)time;
			}
		}

		public int GetBaseUseSpeed() {
			double averageHead = AverageWeightedHeadStats(p => p.useSpeed, 20);
			StatModifier handle = GetHandleParts().Sum(p => p.attackSpeed);

			return (int)Math.Max(1, handle.ApplyTo(averageHead));
		}

		/// <summary>
		/// Gets the base mining speed for the item
		/// </summary>
		/// <returns>Zero if the item isn't a mining tool, the base mining speed otherwise</returns>
		public int GetBaseMiningSpeed() {
			if (!HasAnyToolPower())
				return 0;

			double averageHead = AverageWeightedHeadStats(p => p.useSpeed, 20);
			double handle = 1f;

			foreach (var stat in GetHandleParts().Select(p => p.miningSpeed))
				handle *= stat;

			return (int)Math.Max(1, averageHead * handle);
		}

		public int GetBaseCrit() {
			double head = AverageWeightedHeadStats(p => p.crit, 0);

			return (int)head;
		}

		private double AverageWeightedHeadStats(Func<HeadPartStats, double> func, double defaultValueWhenEmpty) {
			var heads = FilterHeadParts().ToList();

			if (heads.Count == 0)
				return defaultValueWhenEmpty;
			
			double materialCostSum = heads.Sum(p => PartDefinitionLoader.Get(p.partID)!.MaterialCost);

			return heads.Select(p => (PartDefinitionLoader.Get(p.partID)!.MaterialCost, p.GetStat<HeadPartStats>(StatType.Head)!))
				.Where(h => h.Item2 is not null)
				.Average(t => func(t.Item2) * (t.MaterialCost / materialCostSum));
		}

		public int GetPickaxePower() => MaximumToolStat(SelectToolPickaxeStats(), p => p.pickPower);

		public int GetAxePower() => MaximumToolStat(SelectToolAxeStats(), p => p.axePower) / 5;

		public int GetHammerPower() => MaximumToolStat(SelectToolHammerStats(), p => p.hammerPower);

		private static int MaximumToolStat(IEnumerable<HeadPartStats> stats, Func<HeadPartStats, double> func) {
			var eval = stats.ToList();
			
			double max = eval.Count > 0 ? stats.Max(func) : 0;

			return max > 0 ? (int)Math.Ceiling(max) : 0;
		}

		public bool HasAnyToolPower()
			=> SelectToolAxeStats().Any(p => p.axePower / 5 > 0) || SelectToolPickaxeStats().Any(p => p.pickPower > 0) || SelectToolHammerStats().Any(p => p.hammerPower > 0);

		public bool TryIncreaseDurability(Player player, int amount, IDurabilityModificationSource source) {
			if (amount <= 0)
				return false;

			int max = GetMaxDurability();

			bool canChange = TCConfig.Instance.UseDurability;
			if (CurrentDurability < max && canChange) {
				modifiers.PreModifyDurability(player, this, source, ref amount);

				if (amount <= 0)
					return false;

				CurrentDurability += amount;

				if (CurrentDurability > max)
					CurrentDurability = max;
			}

			return canChange;
		}

		public bool TryReduceDurability(Player player, int amount, IDurabilityModificationSource source) {
			if (amount <= 0)
				return false;

			bool lose = TCConfig.Instance.UseDurability;
			
			if (!lose)
				return false;

			lose = modifiers.CanLoseDurability(player, this, source);

			if (lose && CurrentDurability > 0) {
				//Indicate to the API that the modification is a removal
				amount = -amount;

				modifiers.PreModifyDurability(player, this, source, ref amount);

				if (amount >= 0)
					return false;

				amount = -amount;

				if (source is DurabilityModificationSource_HitEntity hitEntity && hitEntity.doubledLossFromUsingMiningTool)
					amount *= 2;

				CurrentDurability -= amount;

				if (CurrentDurability < 0)
					CurrentDurability = 0;

				if (CurrentDurability == 0) {
					SoundEngine.PlaySound(SoundID.Tink, player.Center);
					SoundEngine.PlaySound(SoundID.Item50, player.Center);

					const int sizeX = 6 * 16;
					const int sizeY = 10 * 16;
					Point tl = (player.Center + new Vector2(-sizeX / 2f, -sizeY / 2f)).ToPoint();
					Rectangle area = new(tl.X, tl.Y, sizeX, sizeY);
					CombatText.NewText(area, CombatText.DamagedFriendlyCrit, Language.GetTextValue("Prefix.Broken").ToUpper(), dramatic: true);
				}
			}

			return true;
		}

		public override void SaveData(TagCompound tag) {
			tag["parts"] = parts.ToList();
			tag["durability"] = CurrentDurability;
			
			modifiers.SaveData(tag);
		}

		public override void LoadData(TagCompound tag) {
			InitializeWithParts(tag.GetList<ItemPart>("parts").ToArray());

			CurrentDurability = tag.GetInt("durability");

			modifiers = new(this);
			
			modifiers.LoadData(tag);

			if (parts.Length != PartsCount)
				throw new IOException($"Saved parts list length ({parts.Length}) was not equal to the expected length of {PartsCount}");
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(CurrentDurability);

			modifiers.NetSend(writer);
		}
		
		public override void NetReceive(BinaryReader reader) {
			CurrentDurability = reader.ReadInt32();

			modifiers.NetReceive(reader);
		}

		public sealed override void AddRecipes() {
			var data = ItemDefinitionLoader.Get(ItemDefinition)!;

			Recipe recipe = CreateRecipe();

			foreach (int part in data.GetValidPartIDs())
				recipe.AddRecipeGroup(CoreLibMod.GetRecipeGroupName(part));

			// TODO: forge tile?

			recipe.AddCondition(NetworkText.FromLiteral("Must be crafted from the Forge UI"), r => false);

			recipe.Register();

			CoreLibMod.writer.WriteLine($"Created recipe for BaseTCItem \"{GetType().GetSimplifiedGenericTypeName()}\"");
			CoreLibMod.writer.Flush();
		}

		public sealed override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			Texture2D texture = CoreLibMod.ItemTextures.Get(ItemDefinition, parts, modifiers);

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
			Texture2D texture = CoreLibMod.ItemTextures.Get(ItemDefinition, parts, modifiers);

			Rectangle frame = texture.Frame();

			Vector2 vector = frame.Size() / 2f;
			Vector2 value = new(Item.width / 2 - vector.X, Item.height - frame.Height);
			Vector2 vector2 = Item.position - Main.screenPosition + vector + value;

			spriteBatch.Draw(texture, vector2, frame, lightColor, rotation, vector, scale, SpriteEffects.None, 0);

			return false;
		}
	}
}
