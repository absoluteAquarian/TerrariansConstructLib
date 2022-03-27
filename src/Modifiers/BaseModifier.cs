using Microsoft.Xna.Framework;

namespace TerrariansConstructLib.API.Modifiers {
	/// <summary>
	/// The base class for a modifier that can be applied to a Terrarians' Construct item
	/// </summary>
	public abstract class BaseModifier {
		public int ID { get; internal set; } = -1;

		/// <summary>
		/// The highest tier that can be obtained from this modifier<br/>
		/// Making this property return <c>1</c> will prevent the roman numeral from appearing beside its name
		/// </summary>
		public abstract int MaxTier { get; }

		/// <summary>
		/// The colour for the modifier's name when in a tooltip
		/// </summary>
		public virtual Color TooltipColor => Color.White;

		public abstract string LangKey { get; }

		public abstract string VisualsFolder { get; }

		/// <summary>
		/// Return whether the item types defined in <paramref name="items"/> can be used to upgrade this modifier at the current tier, <paramref name="currentTier"/>
		/// </summary>
		/// <param name="currentTier">The current tier of the modifier</param>
		/// <param name="items">The item IDs that are in the Forge UI slots, starting with the topmost slot and moving clockwise</param>
		/// <param name="stacks">The item stacks for the items in <paramref name="items"/></param>
		/// <param name="upgradeCurrent">The current progress toward the next tier.  Once <paramref name="currentTier"/> surpasses <paramref name="upgradeTarget"/>, the modifier is upgraded to the next tier</param>
		/// <param name="upgradeTarget">The target progress needed to upgrade the modifier to the next tier</param>
		public abstract bool CanAcceptItemsForUpgrade(int currentTier, int[] items, int[] stacks, ref int upgradeCurrent, ref int upgradeTarget);
	}
}
