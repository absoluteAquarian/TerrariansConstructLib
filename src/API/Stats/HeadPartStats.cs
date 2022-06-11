using System.Text;
using TerrariansConstructLib.Stats;

namespace TerrariansConstructLib.API.Stats {
	/// <summary>
	/// An object representing the head stats for an item part (e.g. Sword Blade or Pickaxe Head)
	/// </summary>
	public sealed class HeadPartStats : IPartStats {
		public StatType Type => StatType.Head;

		/// <summary>
		/// The damage stat for the part
		/// </summary>
		public readonly int damage = 1;
		/// <summary>
		/// The knockback stat for the part
		/// </summary>
		public readonly float knockback;
		/// <summary>
		/// The crit stat for the part
		/// </summary>
		public readonly int crit;
		/// <summary>
		/// The use speed stat for the part, measured in ticks per use
		/// </summary>
		public readonly int useSpeed = 20;
		/// <summary>
		/// The pickaxe power for the part
		/// </summary>
		public readonly int pickPower;
		/// <summary>
		/// The axe power for the part, multiplied by 5
		/// </summary>
		public readonly int axePower;
		/// <summary>
		/// The hammer power for the part
		/// </summary>
		public readonly int hammerPower;
		/// <summary>
		/// The durability for the part
		/// </summary>
		public readonly int durability = 1;
		/// <summary>
		/// The tool range modifier
		/// </summary>
		public readonly int toolRange;

		public HeadPartStats(int damage = 0, float knockback = 0, int crit = 0, int useSpeed = 20, int pickPower = 0, int axePower = 0, int hammerPower = 0, int durability = 1, int toolRange = 0) {
			this.damage = damage;
			this.knockback = knockback;
			this.crit = crit;
			this.useSpeed = useSpeed;
			this.pickPower = pickPower;
			this.axePower = axePower;
			this.hammerPower = hammerPower;
			this.durability = durability;
			this.toolRange = toolRange;
		}

		public string GetTooltipLines(int partID) {
			StringBuilder sb = new();
			
			var definition = PartDefinitionLoader.Get(partID)!;

			bool axeHeadPart = (definition.ToolType & ToolType.Axe) != 0,
				pickHeadPart = (definition.ToolType & ToolType.Pickaxe) != 0,
				hammerHeadPart = (definition.ToolType & ToolType.Hammer) != 0;

			sb.Append(ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadDamage, damage));

			if (knockback > 0)
				sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadKnockback, knockback));

			if (crit != 0)
				sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadCrit, crit));

			if (useSpeed > 0)
				sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadUseSpeed, useSpeed));

			if (pickHeadPart && pickPower > 0)
				sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadPickPower, pickPower));

			if (axeHeadPart && axePower > 0)
				sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadAxePower, axePower));

			if (hammerHeadPart && hammerPower > 0)
				sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadHammerPower, hammerPower));

			sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.ExtraDurability + ".add", durability));

			return sb.ToString();
		}
	}
}
