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
		public readonly float miningSpeed;
		/// <summary>
		/// The modifier for the attack use speed
		/// </summary>
		public readonly StatModifier attackSpeed;
		/// <summary>
		/// The modifier for the attack damage.  The <seealso cref="StatModifier.Additive"/> property is considered a flat increase, whereas the <seealso cref="StatModifier.Multiplicative"/> is treated as normal.
		/// </summary>
		public readonly StatModifier attackDamage;
		/// <summary>
		/// The modifier for the attack knocckback.  The <seealso cref="StatModifier.Additive"/> property is considered a flat increase, whereas the <seealso cref="StatModifier.Multiplicative"/> is treated as normal.
		/// </summary>
		public readonly StatModifier attackKnockback;
		/// <summary>
		/// The modifier for the item's durability.  The <seealso cref="StatModifier.Additive"/> property is considered a flat increase, whereas the <seealso cref="StatModifier.Multiplicative"/> is treated as normal.
		/// </summary>
		public readonly StatModifier durability;

		public HandlePartStats(float? miningSpeed = null, StatModifier? attackSpeed = null, StatModifier? attackDamage = null, StatModifier? attackKnockback = null, StatModifier? durability = null) {
			this.miningSpeed = miningSpeed ?? 1f;
			this.attackSpeed = attackSpeed ?? StatModifier.One;
			this.attackDamage = attackDamage ?? StatModifier.One;
			this.attackKnockback = attackKnockback ?? StatModifier.One;
			this.durability = durability ?? StatModifier.One;
		}

		public string GetTooltipLines() {
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

			AppendFormatSingle(CoreLibMod.KnownStatModifiers.HandleMiningSpeed, miningSpeed);
			AppendFormatSingle(CoreLibMod.KnownStatModifiers.HandleAttackSpeed, attackSpeed);
			AppendFormat(CoreLibMod.KnownStatModifiers.HandleAttackDamage, attackDamage);
			AppendFormat(CoreLibMod.KnownStatModifiers.HandleAttackKnockback, attackKnockback);
			AppendFormat(CoreLibMod.KnownStatModifiers.HandleDurability, durability);

			return sb.Length == 0 ? "" : sb.ToString();
		}
	}
}
