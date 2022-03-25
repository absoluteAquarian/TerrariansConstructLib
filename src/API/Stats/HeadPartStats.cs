using System.Text;
using Terraria;
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
		public readonly int damage;
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
		public readonly int useSpeed;
		/// <summary>
		/// The tool power for the part
		/// </summary>
		public readonly int toolPower;
		/// <summary>
		/// The durability for the part
		/// </summary>
		public readonly int durability;

		public HeadPartStats(int damage, float knockback, int crit, int useSpeed, int toolPower, int durability) {
			this.damage = damage;
			this.knockback = knockback;
			this.crit = crit;
			this.useSpeed = useSpeed;
			this.toolPower = toolPower;
			this.durability = durability;
		}

		public string GetTooltipLines() {
			StringBuilder sb = new();

			sb.Append(ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadDamage, damage));

			if (knockback > 0)
				sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadKnockback, knockback));

			if (crit > 0)
				sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadCrit, crit));

			if (useSpeed > 0)
				sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadUseSpeed, useSpeed));

			if (toolPower > 0)
				sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.HeadToolPower, toolPower));

			sb.Append("\n" + ItemStatCollection.Format(CoreLibMod.KnownStatModifiers.ExtraDurability + ".add", durability));

			return sb.ToString();
		}
	}
}
