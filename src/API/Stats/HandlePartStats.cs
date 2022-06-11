using System.Text;
using Terraria.ModLoader;
using TerrariansConstructLib.Stats;

namespace TerrariansConstructLib.API.Stats {
	/// <summary>
	/// An object representing the handle stats for an item part (e.g. Tool Rod)
	/// </summary>
	public sealed class HandlePartStats : IPartStats {
		public StatType Type => StatType.Handle;

		/// <summary>
		/// The modifier for the mining use speed
		/// </summary>
		public readonly float miningSpeed = 1;
		/// <summary>
		/// The modifier for the attack use speed
		/// </summary>
		public readonly StatModifier attackSpeed = StatModifier.Default;
		/// <summary>
		/// The modifier for the attack damage.  The <seealso cref="StatModifier.Additive"/> property is considered a flat increase, whereas the <seealso cref="StatModifier.Multiplicative"/> is treated as normal.
		/// </summary>
		public readonly StatModifier attackDamage = new(0, 1);
		/// <summary>
		/// The modifier for the attack knocckback.  The <seealso cref="StatModifier.Additive"/> property is considered a flat increase, whereas the <seealso cref="StatModifier.Multiplicative"/> is treated as normal.
		/// </summary>
		public readonly StatModifier attackKnockback = new(0, 1);
		/// <summary>
		/// The modifier for the item's durability.  The <seealso cref="StatModifier.Additive"/> property is considered a flat increase, whereas the <seealso cref="StatModifier.Multiplicative"/> is treated as normal.
		/// </summary>
		public readonly StatModifier durability = new(0, 1);

		public HandlePartStats(float? miningSpeed = null, StatModifier? attackSpeed = null, StatModifier? attackDamage = null, StatModifier? attackKnockback = null, StatModifier? durability = null) {
			this.miningSpeed = miningSpeed ?? 1f;
			this.attackSpeed = attackSpeed ?? StatModifier.Default;
			this.attackDamage = attackDamage ?? new StatModifier(0, 1);
			this.attackKnockback = attackKnockback ?? new StatModifier(0, 1);
			this.durability = durability ?? new StatModifier(0, 1);
		}

		public string GetTooltipLines(int partID) {
			StringBuilder sb = new();

			bool needsNewline = false;
			
			void AppendFormatSingle(string identifier, float stat) {
				string? fmt = ItemStatCollection.Format(identifier, stat);

				if (fmt is not null) {
					if (needsNewline)
						sb.Append('\n');

					sb.Append(fmt);

					needsNewline = true;
				}
			}

			void AppendFormat(string identifier, StatModifier modifier) {
				string? fmt = ItemStatCollection.Format(identifier, modifier);
				if (fmt is not null) {
					if (needsNewline)
						sb.Append('\n');

					sb.Append(fmt);

					needsNewline = true;
				}
			}

			if (miningSpeed != 1)
				AppendFormatSingle(CoreLibMod.KnownStatModifiers.HandleMiningSpeed, miningSpeed);
			
			float attack = attackSpeed.ApplyTo(1f);
			if (attack != 1)
				AppendFormatSingle(CoreLibMod.KnownStatModifiers.HandleAttackSpeed, attack);
			
			AppendFormat(CoreLibMod.KnownStatModifiers.HandleAttackDamage, attackDamage);
			AppendFormat(CoreLibMod.KnownStatModifiers.HandleAttackKnockback, attackKnockback);
			AppendFormat(CoreLibMod.KnownStatModifiers.HandleDurability, durability);

			return sb.Length == 0 ? "" : sb.ToString();
		}
	}
}
