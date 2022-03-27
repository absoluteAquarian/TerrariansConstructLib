using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.DataStructures;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.Abilities {
	/// <summary>
	/// The base class for any abilities (passive or not) that are activated via item parts
	/// </summary>
	public abstract class BaseAbility : ModType {
		public double Counter;

		/// <summary>
		/// Whether this ability is considered a singleton (only one instance exists on an item at any given moment)<br/>
		/// Defaults to <see langword="false"/>, which indicates that an instance is stored per item part
		/// </summary>
		public virtual bool IsSingleton => false;

		/// <summary>
		/// This hook is called when cloning instances of this ability
		/// </summary>
		public virtual BaseAbility Clone() => (BaseAbility)MemberwiseClone();

		/// <summary>
		/// Whether the <see cref="Counter"/> automatically increments (<see langword="true"/>) or decrements (<see langword="false"/>)<br/>
		/// This property is ignored if <see cref="ShouldUpdateCounter(Player)"/> returns <see langword="false"/><br/>
		/// This property defaults to <see langword="true"/>
		/// </summary>
		public bool CounterIncrements { get; protected set; } = true;

		protected sealed override void Register() {
			ModTypeLookup<BaseAbility>.Register(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();

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
		public virtual void UseSpeedMultiplier(Player player, BaseTCItem item, ref float multiplier) { }

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
		public virtual bool CanLoseDurability(Player player, BaseTCItem item) => true;

		/// <summary>
		/// Allows you to save custom data for this ability.<br/>
		/// <br/>
		/// <b>NOTE:</b> The provided tag is always empty by default, and is provided as an argument only for the sake of convenience and optimization.<br/>
		/// <b>NOTE:</b> Try to only save data that isn't default values.
		/// </summary>
		/// <param name="tag">The TagCompound to save data into. Note that this is always empty by default, and is provided as an argument only for the sake of convenience and optimization.</param>
		public virtual void SaveData(TagCompound tag) {
			tag["counter"] = Counter;
		}

		/// <summary>
		/// Allows you to load custom data that you have saved for this item.<br/>
		/// <b>Try to write defensive loading code that won't crash if something's missing.</b>
		/// </summary>
		/// <param name="tag">The TagCompound to load data from.</param>
		public virtual void LoadData(TagCompound tag) {
			Counter = tag.GetDouble("counter");
		}
	}
}
