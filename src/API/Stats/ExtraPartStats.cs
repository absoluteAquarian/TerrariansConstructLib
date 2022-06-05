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

		private readonly Dictionary<string, int[]?> validPartIDsForTooltipKey = new();

		/// <summary>
		/// Sets the stat with the given identifier.  Calling this method more than once for the same identifier will throw an exception
		/// </summary>
		/// <param name="identifier">The name of the stat</param>
		/// <param name="stat">The stat value</param>
		/// <exception cref="ArgumentException"/>
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

		public ExtraPartStats SetValidPartIDs(string identifier, params int[]? validIDs) {
			validPartIDsForTooltipKey[identifier] = validIDs;
			return this;
		}

		/// <summary>
		/// Gets the stat with the given identifier
		/// </summary>
		/// <param name="identifier">The name of the stat</param>
		/// <param name="defaultValueIfMissing">The default value that should be returned if the stat isn't present</param>
		/// <returns>The value of the stat if it's defined or <paramref name="defaultValueIfMissing"/> if it's not <see langword="null"/>, <see cref="StatModifier.Default"/> otherwise</returns>
		public StatModifier Get(string identifier, StatModifier? defaultValueIfMissing = null)
			=> modifiers.TryGetValue(identifier, out var stat) ? stat : defaultValueIfMissing ?? StatModifier.Default;

		public string GetTooltipLines(int partID) {
			StringBuilder sb = new();

			foreach (var (name, stat) in modifiers)
				if (!validPartIDsForTooltipKey.TryGetValue(name, out var ids) || ids is null || Array.IndexOf(ids, partID) >= 0)
					sb.Append((sb.Length > 0 ? "\n" : "") + ItemStatCollection.Format(name, stat));

			return sb.ToString();
		}
	}
}
