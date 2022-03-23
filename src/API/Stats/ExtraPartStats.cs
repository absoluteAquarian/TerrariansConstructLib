using System;
using System.Collections.Generic;
using System.Text;
using Terraria.ModLoader;
using TerrariansConstructLib.Stats;

namespace TerrariansConstructLib.API.Stats {
	/// <summary>
	/// An object representing any additional stats for an item part (e.g. Tool Binding)<br/>
	/// </summary>
	public sealed class ExtraPartStats : IPartStats {
		public StatType Type => StatType.Extra;

		private readonly Dictionary<string, StatModifier> modifiers = new();

		/// <summary>
		/// Sets the stat with the given identifier.  Calling this method more than once for the same identifier will throw an exception
		/// </summary>
		/// <param name="identifier">The name of the stat</param>
		/// <param name="stat">The stat value</param>
		/// <exception cref="ArgumentException"></exception>
		public ExtraPartStats With(string identifier, StatModifier stat) {
			if (!modifiers.ContainsKey(identifier))
				modifiers[identifier] = stat;
			else
				throw new ArgumentException($"A stat with the identifier \"{identifier}\" was already set");

			return this;
		}

		/// <summary>
		/// Combines this object's stats with another object's stats<br/>
		/// If <paramref name="other"/> has a modifier with the same identifier, the two stats are combined
		/// </summary>
		/// <param name="other">The other <see cref="ExtraPartStats"/> object</param>
		/// <returns>A new <see cref="ExtraPartStats"/> instance containing the stats of both</returns>
		public ExtraPartStats With(ExtraPartStats other) {
			ExtraPartStats instance = new();

			foreach (var (id, m) in modifiers)
				instance.modifiers[id] = m;

			foreach (var (id, m) in other.modifiers) {
				if (!instance.modifiers.ContainsKey(id))
					instance.modifiers[id] = m;
				else
					instance.modifiers[id] = instance.modifiers[id].CombineWith(m);
			}

			return instance;
		}

		/// <summary>
		/// Gets the stat with the given identifier
		/// </summary>
		/// <param name="identifier">The name of the stat</param>
		/// <returns>The value of the stat if it's defined, <seealso cref="StatModifier.One"/> otherwise</returns>
		public StatModifier Get(string identifier)
			=> modifiers.TryGetValue(identifier, out var stat) ? stat : StatModifier.One;

		public StatModifier this[string identifier] {
			get => Get(identifier);
			set => With(identifier, value);
		}

		public string GetTooltipLines(bool isAxeHeadPart) {
			StringBuilder sb = new();

			foreach (var (name, stat) in modifiers)
				sb.Append((sb.Length > 0 ? "\n" : "") + ItemStatCollection.Format(name, stat));

			return sb.ToString();
		}
	}
}
