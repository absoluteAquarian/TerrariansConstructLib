using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;
using TerrariansConstructLib.API.Sources;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Players;
using TerrariansConstructLib.Projectiles;

namespace TerrariansConstructLib.Modifiers {
	/// <summary>
	/// The base class for a trait on a Terrarians' Construct item
	/// </summary>
	public abstract class BaseTrait : ModType, INetHooks {
		public double Counter;

		public int Type { get; private set; }

		protected sealed override void Register() {
			ModTypeLookup<BaseTrait>.Register(this);
			Type = ModifierLoader.Add(this);
		}

		public string GetIdentifier() => Mod.Name + ":" + Name;

		public sealed override void SetupContent() => SetStaticDefaults();

		/// <summary>
		/// Gets how many "instances" of this trait's material are present on the item it's assigned to<br/>
		/// This property is affected by <see cref="IsEquivalentForTier(Type, out uint)"/>
		/// </summary>
		public int Tier { get; internal set; }

		/// <summary>
		/// Whether this ability is considered a singleton (only one instance exists on an item at any given moment)<br/>
		/// Defaults to <see langword="false"/>, which indicates that an instance is stored per item part
		/// </summary>
		public virtual bool IsSingleton => false;

		/// <summary>
		/// The colour for the modifier's name when in a tooltip
		/// </summary>
		public virtual Color TooltipColor => Color.White;

		/// <summary>
		/// The lang key used when displaying the tooltip for the modifier
		/// </summary>
		public abstract string LangKey { get; }

		/// <summary>
		/// This hook is called when cloning instances of this ability
		/// </summary>
		public virtual BaseTrait Clone() => (BaseTrait)MemberwiseClone();

		/// <summary>
		/// Override this hook to determine when another <see cref="BaseTrait"/> type can be considered equivalent when calculating tiers
		/// </summary>
		/// <param name="type">The type of the <see cref="BaseTrait"/></param>
		/// <param name="tierWorth">The worth of the tier.  Defaults to <c>1</c></param>
		/// <returns><c>GetType().IsAssignableFrom(type)</c> by default</returns>
		public virtual bool IsEquivalentForTier(Type type, out uint tierWorth) {
			tierWorth = 1;
			return GetType().IsAssignableFrom(type);
		}

		/// <summary>
		/// Whether the <see cref="Counter"/> automatically increments (<see langword="true"/>) or decrements (<see langword="false"/>)<br/>
		/// This property is ignored if <see cref="ShouldUpdateCounter(Player)"/> returns <see langword="false"/><br/>
		/// This property defaults to <see langword="true"/>
		/// </summary>
		public virtual bool CounterIncrements => true;

		/// <summary>
		/// Whether <see cref="Counter"/> should be automatically modified by 1 per tick.<br/>
		/// If your ability's counter isn't time-based (say, an ability whose counter is increased more the faster you move), return <see langword="false"/> in this hook<br/>
		/// Defaults to <see langword="false"/>
		/// </summary>
		/// <param name="player">The player</param>
		public virtual bool ShouldUpdateCounter(Player player) => false;

		/// <summary>
		/// Gets the expected target value that the counter will step toward
		/// </summary>
		/// <param name="player">The player</param>
		public virtual double GetExpectedCounterTarget(Player player) => 0;

		/// <summary>
		/// Gets the value used when resetting <see cref="Counter"/>
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public virtual double GetCounterValueOnTargetReached(Player player) => 0;

		/// <summary>
		/// This hook runs when <see cref="Counter"/> reaches the value returned by <see cref="GetExpectedCounterTarget(Player)"/><br/>
		/// If <see cref="CounterIncrements"/> is <see langword="true"/>, <see cref="Counter"/> must be greater than the aforementioned value, otherwise it must be less than the value.
		/// </summary>
		/// <param name="player">The player</param>
		public virtual void OnCounterTargetReached(Player player) { }

		/// <summary>
		/// Whether <see cref="Counter"/> can be reset once it reaches the value returned by <see cref="GetExpectedCounterTarget(Player)"/><br/>
		/// Return <see langword="false"/> to prevent <see cref="Counter"/> from resetting, which might be useful for abilities which are put in a "wait for activation" state.
		/// </summary>
		/// <param name="player">The player</param>
		public virtual bool CanResetCounter(Player player) => true;

		internal void Update(Player player) {
			if (ShouldUpdateCounter(player))
				Counter += CounterIncrements ? 1 : -1;

			double target = GetExpectedCounterTarget(player);
			bool wasReset = false;
			if ((CounterIncrements && Counter > target) || (!CounterIncrements && Counter < target)) {
				OnCounterTargetReached(player);

				if (CanResetCounter(player)) {
					Counter = GetCounterValueOnTargetReached(player);

					wasReset = true;
				}
			}

			OnUpdate(player, wasReset);
		}

		/// <summary>
		/// This hook runs every game tick while the player is holding the item this ability is tied to
		/// </summary>
		/// <param name="player">The player</param>
		/// <param name="item">The item being held</param>
		public virtual void OnHoldItem(Player player, BaseTCItem item) { }

		/// <summary>
		/// This hook runs every game tick while the player has the item this ability is tied to in their inventory
		/// </summary>
		/// <param name="player">The player</param>
		/// <param name="item">The item instance</param>
		public virtual void OnUpdateInventory(Player player, BaseTCItem item) { }

		/// <summary>
		/// Perform update tasks in this hook.  This hook is called in <see cref="ModPlayer.PostUpdateMiscEffects"/>
		/// </summary>
		/// <param name="player">The player</param>
		/// <param name="counterWasReset">Whether <see cref="Counter"/> was reset in this game tick</param>
		public virtual void OnUpdate(Player player, bool counterWasReset) { }

		/// <summary>
		/// This hook runs when the ability hotkey is pressed
		/// </summary>
		/// <param name="player">The player</param>
		public virtual void OnAbilityHotkeyPressed(Player player) { }

		/// <summary>
		/// This hook runs in <see cref="BaseTCItem.UseSpeedMultiplier(Player)"/>
		/// </summary>
		/// <param name="player">The player</param>
		/// <param name="item">The item instance</param>
		/// <param name="multiplier">The multiplier</param>
		public virtual void UseSpeedMultiplier(Player player, BaseTCItem item, ref StatModifier multiplier) { }

		/// <summary>
		/// This hook runs before the mining tool, <paramref name="item"/>, hits a tile
		/// </summary>
		/// <param name="player">The player</param>
		/// <param name="item">The item doing the mining</param>
		/// <param name="context">The context.  Contains information about what tool type was used to hit the tile and the intended damage</param>
		/// <param name="power">The effective tool power</param>
		public virtual void ModifyToolPower(Player player, BaseTCItem item, TileDestructionContext context, ref int power) { }

		/// <summary>
		/// This hook runs after the player has destroyed a tile using a <see cref="BaseTCItem"/> item
		/// </summary>
		/// <param name="player">The player doing the mining</param>
		/// <param name="item">The item used to destroy the tile</param>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <param name="context">The context.  Contains information about what tool type was used to destroy the tile and how much damage was dealt to destroy the tile</param>
		public virtual void OnTileDestroyed(Player player, BaseTCItem item, int x, int y, TileDestructionContext context) { }

		/// <summary>
		/// Return false in this hook to prevent the <paramref name="item"/> from losing durability
		/// </summary>
		public virtual bool CanLoseDurability(Player player, BaseTCItem item, IDurabilityModificationSource source) => true;

		/// <summary>
		/// This hook runs in <see cref="BaseTCItem.ModifyHitNPC(Player, NPC, ref int, ref float, ref bool)"/>
		/// </summary>
		/// <param name="player">The player</param>
		/// <param name="target">The target</param>
		/// <param name="item">The item doing the hitting</param>
		/// <param name="damage">The damage</param>
		/// <param name="knockBack">The knockback</param>
		/// <param name="crit">If set to <see langword="true"/> [crit]</param>
		public virtual void ModifyHitNPC(Player player, NPC target, BaseTCItem item, ref int damage, ref float knockBack, ref bool crit) { }

		/// <summary>
		/// This hook runs in <see cref="BaseTCItem.ModifyHitNPC(Player, NPC, ref int, ref float, ref bool)"/>
		/// </summary>
		/// <param name="projectile">The projectile doing the hitting</param>
		/// <param name="target">The target</param>
		/// <param name="damage">The damage</param>
		/// <param name="knockBack">The knockback</param>
		/// <param name="crit">If set to <see langword="true"/> [crit]</param>
		/// <param name="hitDirection">The hit direction.  -1 is to the left, 1 is to the right and 0 is neither</param>
		public virtual void ModifyHitNPCWithProjectile(BaseTCProjectile projectile, NPC target, ref int damage, ref float knockBack, ref bool crit, ref int hitDirection) { }

		/// <summary>
		/// This hook runs in <see cref="BaseTCItem.ModifyHitPvp(Player, Player, ref int, ref bool)"/>
		/// </summary>
		/// <param name="owner">The player</param>
		/// <param name="target">The target</param>
		/// <param name="item">The item that hit the target</param>
		/// <param name="damage">The damage</param>
		/// <param name="crit">If set to <see langword="true"/> [crit]</param>
		public virtual void ModifyHitPlayer(Player owner, Player target, BaseTCItem item, ref int damage, ref bool crit) { }

		/// <summary>
		/// This hook runs in <see cref="BaseTCProjectile.ModifyHitPlayer(Player, ref int, ref bool)"/>
		/// </summary>
		/// <param name="projectile">The projectile</param>
		/// <param name="target">The target</param>
		/// <param name="damage">The damage</param>
		/// <param name="crit">If set to <see langword="true"/> [crit]</param>
		public virtual void ModifyHitPlayerWithProjectile(BaseTCProjectile projectile, Player target, ref int damage, ref bool crit) { }

		/// <summary>
		/// This hook runs in <see cref="BaseTCItem.OnHitNPC(Player, NPC, int, float, bool)"/>
		/// </summary>
		/// <param name="player">The player</param>
		/// <param name="target">The target</param>
		/// <param name="item">The item that hit the target</param>
		/// <param name="damage">The damage</param>
		/// <param name="knockBack">The knockback</param>
		/// <param name="crit">If set to <see langword="true"/> [crit]</param>
		public virtual void OnHitNPC(Player player, NPC target, BaseTCItem item, int damage, float knockBack, bool crit) { }

		/// <summary>
		/// This hook runs in <see cref="BaseTCItem.OnHitPvp(Player, Player, int, bool)"/>
		/// </summary>
		/// <param name="owner">The player</param>
		/// <param name="target">The target</param>
		/// <param name="item">The item that hit the target</param>
		/// <param name="damage">The damage</param>
		/// <param name="crit">If set to <see langword="true"/> [crit]</param>
		public virtual void OnHitPlayer(Player owner, Player target, BaseTCItem item, int damage, bool crit) { }

		/// <summary>
		/// This hook runs in <see cref="BaseTCProjectile.OnHitPlayer(Player, int, bool)"/>
		/// </summary>
		/// <param name="projectile">The projectile</param>
		/// <param name="target">The target</param>
		/// <param name="damage">The damage</param>
		/// <param name="crit">If set to <see langword="true"/> [crit]</param>
		public virtual void OnHitPlayerWithProjectile(BaseTCProjectile projectile, Player target, int damage, bool crit) { }

		/// <summary>
		/// This hook rusn in <see cref="BaseTCProjectile.OnHitNPC(NPC, int, float, bool)"/>
		/// </summary>
		/// <param name="projectile">The projectile</param>
		/// <param name="target">The target</param>
		/// <param name="damage">The damage</param>
		/// <param name="knockBack">The knockback</param>
		/// <param name="crit">If set to <see langword="true"/> [crit]</param>
		public virtual void OnHitNPCWithProjectile(BaseTCProjectile projectile, NPC target, int damage, float knockBack, bool crit) { }

		/// <summary>
		/// This hook runs in <see cref="ItemModifierPlayer.OnHitByNPC(NPC, int, bool)"/>
		/// </summary>
		/// <param name="npc">The NPC doing the hitting</param>
		/// <param name="target">The target</param>
		/// <param name="damage">The damage</param>
		/// <param name="crit">If set to <see langword="true"/> [crit]</param>
		public virtual void OnHitByNPC(NPC npc, Player target, int damage, bool crit) { }

		/// <summary>
		/// This hook runs in <see cref="ItemModifierPlayer.OnHitByProjectile(Projectile, int, bool)"/>
		/// </summary>
		/// <param name="projectile">The projectile doing the hitting</param>
		/// <param name="target">The target</param>
		/// <param name="damage">The damage</param>
		/// <param name="crit">If set to <see langword="true"/> [crit]</param>
		public virtual void OnHitByNPCProjectile(Projectile projectile, Player target, int damage, bool crit) { }

		/// <summary>
		/// This hook runs before durability is added to or subtracted from the <paramref name="item"/>
		/// </summary>
		/// <param name="player">The player</param>
		/// <param name="item">The item</param>
		/// <param name="source">The modification source</param>
		/// <param name="amount">The amount to modify the durability by.  If the value is &lt; 0, then the modification was a durability removal, otherwise it's a durability addition</param>
		public virtual void PreModifyDurability(Player player, BaseTCItem item, IDurabilityModificationSource source, ref int amount) { }

		/// <summary>
		/// This hook runs when a <see cref="BaseTCProjectile"/> projectile is spawned which has a part with this ability
		/// </summary>
		/// <param name="projectile">The spawned projectile</param>
		/// <param name="source">The spawn source</param>
		public virtual void OnProjectileSpawn(BaseTCProjectile projectile, IEntitySource source) { }

		/// <inheritdoc cref="BaseTCItem.ModifyWeaponDamage(Player, ref StatModifier)"/>
		public virtual void ModifyWeaponDamage(Player player, BaseTCItem item, ref StatModifier damage) { }

		/// <inheritdoc cref="BaseTCItem.ModifyWeaponKnockback(Player, ref StatModifier)"/>
		public virtual void ModifyWeaponKnockback(Player player, BaseTCItem item, ref StatModifier knockback) { }

		/// <inheritdoc cref="BaseTCItem.ModifyWeaponCrit(Player, ref float)"/>
		public virtual void ModifyWeaponCrit(Player player, BaseTCItem item, ref float crit) { }

		/// <summary>
		/// This hook runs in <see cref="BaseTCItem.UseItem(Player)"/>
		/// </summary>
		/// <param name="player">The player</param>
		/// <param name="item">The item</param>
		public virtual void UseItem(Player player, BaseTCItem item) { }

		/// <summary>
		/// This hook runs in <see cref="BaseTCItem.CanConsumeAmmo(Item, Player)"/>
		/// </summary>
		/// <param name="weapon">The weapon</param>
		/// <param name="ammo">The ammo potentially being consumed</param>
		/// <param name="player">The player</param>
		/// <returns>Whether the ammo can be consumed</returns>
		public virtual bool CanConsumeAmmo(BaseTCItem weapon, BaseTCItem ammo, Player player) => true;

		/// <inheritdoc cref="GlobalItem.OnPickup(Item, Player)"/>
		public virtual bool OnPickup(Item item, Player player) => true;

		/// <summary>
		/// Allows you to save custom data for this ability.<br/>
		/// <br/>
		/// <b>NOTE:</b> The provided tag is always empty by default, and is provided as an argument only for the sake of convenience and optimization.<br/>
		/// <b>NOTE:</b> Try to only save data that isn't default values.
		/// </summary>
		/// <param name="tag">The TagCompound to save data into. Note that this is always empty by default, and is provided as an argument only for the sake of convenience and optimization.</param>
		public virtual void SaveData(TagCompound tag) {
			tag["counter"] = Counter;
			tag["tier"] = Tier;
		}

		/// <summary>
		/// Allows you to load custom data that you have saved for this item.<br/>
		/// <b>Try to write defensive loading code that won't crash if something's missing.</b>
		/// </summary>
		/// <param name="tag">The TagCompound to load data from.</param>
		public virtual void LoadData(TagCompound tag) {
			Counter = tag.GetDouble("counter");
			Tier = tag.GetInt("tier");
		}

		public static void DisplayMessageAbovePlayer(Player player, Color color, string message) {
			const int sizeX = 6 * 16;
			const int sizeY = 10 * 16;
			Point tl = (player.Center + new Vector2(-sizeX / 2f, -sizeY / 2f)).ToPoint();
			Rectangle area = new(tl.X, tl.Y, sizeX, sizeY);
			CombatText.NewText(area, color, message);
		}

		public virtual void NetSend(BinaryWriter writer) {
			writer.Write(Counter);
			writer.Write(Tier);
		}

		public virtual void NetReceive(BinaryReader reader) {
			Counter = reader.ReadDouble();
			Tier = reader.ReadInt32();
		}
	}
}
