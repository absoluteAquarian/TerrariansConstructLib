using System.IO;
using Terraria.ID;
using Terraria.ModLoader.IO;
using TerrariansConstructLib.API;

namespace TerrariansConstructLib.Modifiers {
	/// <summary>
	/// The base class for a modifier that can be applied to a Terrarians' Construct item
	/// </summary>
	public abstract class BaseModifier : BaseTrait {
		public sealed override bool IsSingleton => true;

		public int CurrentUpgradeProgress { get; internal set; }
		public int UpgradeTarget { get; internal set; }

		/// <summary>
		/// The highest tier that can be obtained from this modifier<br/>
		/// Making this property return <c>1</c> will prevent the roman numeral from appearing beside its name
		/// </summary>
		public abstract int MaxTier { get; }

		/// <summary>
		/// The relative path for the visual texture of this modifier<br/>
		/// Return <see langword="null"/> if there is no visual
		/// </summary>
		public abstract string? VisualTexture { get; }

		/// <summary>
		/// Whether the visual texture of this modifier is drawn above the item's texture<br/>
		/// Defaults to <see langword="true"/>
		/// </summary>
		public virtual bool VisualIsDisplayedAboveItem => true;

		/// <summary>
		/// Return the next upgrade target for the current tier here, or -1 if the tier is at the maximum
		/// </summary>
		public abstract int GetUpgradeTarget();

		/// <summary>
		/// Return whether the item types defined in <paramref name="items"/> were used to increase <paramref name="upgradeCurrent"/>
		/// </summary>
		/// <param name="items">The items that are in the Forge UI slots, starting with the topmost slot and moving clockwise</param>
		/// <param name="upgradeCurrent">The current progress toward the next tier.  Once <paramref name="upgradeCurrent"/> surpasses <paramref name="upgradeTarget"/>, the modifier is upgraded to the next tier</param>
		/// <param name="upgradeTarget">The target progress needed to upgrade the modifier to the next tier</param>
		public abstract bool CanAcceptItemsForUpgrade(ItemData[] items, ref int upgradeCurrent, in int upgradeTarget);

		protected static int CountItems(ItemData[] items, int type) {
			int count = 0;

			for (int i = 0; i < items.Length; i++) {
				if (items[i].type == ItemID.None || items[i].stack <= 0)
					continue;

				if (items[i].type == type)
					count += items[i].stack;
			}

			return count;
		}

		/// <summary>
		/// Removes the item of type, <paramref name="typeToRemove"/>, from <paramref name="items"/>
		/// </summary>
		/// <param name="items">The items</param>
		/// <param name="typeToRemove">The type to remove from the stacks</param>
		/// <param name="stackToRemove">How much of the item to remove</param>
		/// <returns>Whether <paramref name="stackToRemove"/> was removed completely</returns>
		protected static bool RemoveItems(ItemData[] items, int typeToRemove, int stackToRemove) {
			for (int i = items.Length - 1; i >= 0; i--) {
				if (items[i].type == ItemID.None || items[i].stack <= 0)
					continue;

				if (items[i].type == typeToRemove) {
					ref int stack = ref items[i].stack;
					
					if (stack >= stackToRemove) {
						stack -= stackToRemove;
						return true;
					} else if (stack <= stackToRemove) {
						stackToRemove -= stack;
						stack = 0;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns whether <paramref name="items"/> only contains item types found in <paramref name="validTypes"/><br/>
		/// Empty slots are considered "valid"
		/// </summary>
		/// <param name="items">The items</param>
		/// <param name="validTypes">The valid types</param>
		protected static bool ItemsOnlyContain(ItemData[] items, params int[] validTypes) {
			for (int i = 0; i < items.Length; i++) {
				if (items[i].type == ItemID.None || items[i].stack <= 0)
					continue;
				
				bool valid = false;

				for (int j = 0; j < validTypes.Length; j++)
					if (validTypes[j] == items[i].type)
						valid = true;

				if (!valid)
					return false;
			}

			return true;
		}

		public override void NetSend(BinaryWriter writer) {
			base.NetSend(writer);
			writer.Write(CurrentUpgradeProgress);
		}

		public override void NetReceive(BinaryReader reader) {
			base.NetReceive(reader);
			CurrentUpgradeProgress = reader.ReadInt32();
		}

		public override void SaveData(TagCompound tag) {
			base.SaveData(tag);
			tag["progress"] = CurrentUpgradeProgress;
		}

		public override void LoadData(TagCompound tag) {
			base.LoadData(tag);
			CurrentUpgradeProgress = tag.GetInt("progress");
		}
	}
}
