﻿using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Rarities {
	/// <summary>
	/// This type allows customization of where a rarity would be located compared to other rarities for <seealso cref="PartMold"/> tiers.<br/>
	/// Mold tiers set to a certain rarity can use any material at that rarity or below.
	/// </summary>
	public static class RarityClassification {
		private static Dictionary<int, float> rarities;

		internal static void Load() {
			rarities = new();

			for (int i = ItemRarityID.Gray; i < ItemRarityID.Count; i++)
				rarities[i] = i;
		}

		internal static void Unload() {
			rarities = null;
		}

		internal static IEnumerable<int> GetRaritiesBelowOrAt(int rarity)
			=> rarities.Where(kvp => kvp.Value <= rarities[rarity]).Select(kvp => kvp.Key);

		public static bool TryGetRarityLocation(int rarity, out float location)
			=> rarities.TryGetValue(rarity, out location);

		public static bool TryGetRarityLocation<T>(out float location) where T : ModRarity
			=> rarities.TryGetValue(ModContent.RarityType<T>(), out location);

		public static void SetRarityLocation(int rarity, float location)
			=> rarities[rarity] = location;

		public static void SetRarityLocation<T>(float location) where T : ModRarity
			=> rarities[ModContent.RarityType<T>()] = location;

		public static bool CanUseMaterial(int moldTier, Material material) {
			if (moldTier < 0 || moldTier >= PartMoldTierRegistry.Count)
				throw new ArgumentException("Mold Tier ID was invalid");

			var data = PartMoldTierRegistry.registeredIDs[moldTier];

			if (!rarities.TryGetValue(data.tierRarity, out float location) || !rarities.TryGetValue(material.rarity, out float materialLocation))
				return false;

			return materialLocation <= location;
		}
	}
}
