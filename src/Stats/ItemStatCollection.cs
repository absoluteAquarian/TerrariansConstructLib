using System.Collections.Generic;
using System.Text;
using Terraria.ModLoader;

namespace TerrariansConstructLib.Stats {
	/// <summary>
	/// A collection of stat modifiers used for formatting tooltips
	/// </summary>
	public static class ItemStatCollection {
		internal static Dictionary<string, ItemStat> stats;

		internal static void Load() {
			stats = new();

			AddStat(CoreLibMod.KnownStatModifiers.HeadDamage, "Mods.TerrariansConstructLib.StatFormats.Head.Damage");
			AddStat(CoreLibMod.KnownStatModifiers.HeadKnockback, "Mods.TerrariansConstructLib.StatFormats.Head.Knockback");
			AddStat(CoreLibMod.KnownStatModifiers.HeadCrit, "Mods.TerrariansConstructLib.StatFormats.Head.Crit");
			AddStat(CoreLibMod.KnownStatModifiers.HeadUseSpeed, "Mods.TerrariansConstructLib.StatFormats.Head.UseSpeed");
			AddStat(CoreLibMod.KnownStatModifiers.HeadPickPower, "Mods.TerrariansConstructLib.StatFormats.Head.PickPower");
			AddStat(CoreLibMod.KnownStatModifiers.HeadAxePower, "Mods.TerrariansConstructLib.StatFormats.Head.AxePower");
			AddStat(CoreLibMod.KnownStatModifiers.HeadHammerPower, "Mods.TerrariansConstructLib.StatFormats.Head.HammerPower");
			AddStat(CoreLibMod.KnownStatModifiers.HeadDurability, "Mods.TerrariansConstructLib.StatFormats.Head.Durability");

			AddStat(CoreLibMod.KnownStatModifiers.HandleMiningSpeed, "Mods.TerrariansConstructLib.StatFormats.Handle.MiningSpeed");
			AddStat(CoreLibMod.KnownStatModifiers.HandleAttackSpeed, "Mods.TerrariansConstructLib.StatFormats.Handle.AttackSpeed");
			AddStats(CoreLibMod.KnownStatModifiers.HandleAttackDamage, "Mods.TerrariansConstructLib.StatFormats.Handle.AttackDamage");
			AddStats(CoreLibMod.KnownStatModifiers.HandleAttackKnockback, "Mods.TerrariansConstructLib.StatFormats.Handle.AttackKnockback");
			AddStats(CoreLibMod.KnownStatModifiers.HandleDurability, "Mods.TerrariansConstructLib.StatFormats.Handle.Durability");

			AddStats(CoreLibMod.KnownStatModifiers.ExtraDurability, "Mods.TerrariansConstructLib.StatFormats.Extra.Durability");
			AddStats(CoreLibMod.KnownStatModifiers.BowDrawSpeed, "Mods.TerrariansConstructLib.StatFormats.Extra.BowDrawSpeed");
			AddStats(CoreLibMod.KnownStatModifiers.BowArrowSpeed, "Mods.TerrariansConstructLib.StatFormats.Extra.BowArrowSpeed");
		}

		/// <summary>
		/// Adds the stat information to the internal dictionary
		/// </summary>
		/// <param name="baseName">The base name for the stat modifier keys</param>
		/// <param name="baseLangKey">The base lang key used for the stat modifier keys</param>
		/// <remarks>The two entries added are intended to be used with <see cref="Format(string, StatModifier)"/></remarks>
		public static void AddStats(string baseName, string baseLangKey) {
			stats.Add(baseName + ".add", new(baseName + ".add", baseLangKey + "Add"));
			stats.Add(baseName + ".mult", new(baseName + ".mult", baseLangKey + "Mult"));
		}

		/// <summary>
		/// Adds the stat information to the internal dictionary
		/// </summary>
		/// <param name="name">The name for the stat modifier key</param>
		/// <param name="langKey">The lang key used for the stat modifier key</param>
		/// <remarks>The entry added is intended to be used with <see cref="Format(string, float)"/></remarks>
		public static void AddStat(string name, string langKey) {
			stats.Add(name, new(name, langKey));
		}

		internal static void Unload() {
			stats?.Clear();
			stats = null!;
		}

		public static string? Format(string name, StatModifier stat) {
			StringBuilder sb = new();

			bool newline = false;
			if (stat.Additive != 0 && stats.TryGetValue(name + ".add", out var add)) {
				sb.Append(add.Format(stat.Additive));
				newline = true;
			}
			if (stat.Multiplicative != 1 && stats.TryGetValue(name + ".mult", out var mult)) {
				string fmt = mult.Format(stat.Multiplicative);

				if (newline)
					sb.Append('\n');

				sb.Append(fmt);
			}

			return sb.Length == 0 ? null : sb.ToString();
		}

		public static string? Format(string name, float stat)
			=> stats.TryGetValue(name, out var itemStat) ? itemStat.Format(stat) : null;
	}
}
