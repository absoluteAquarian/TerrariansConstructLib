using Microsoft.Xna.Framework;
using System;
using Terraria.ModLoader;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.Rarities {
	public sealed class BasePartMoldRarity : ModRarity {
		public readonly int moldTier;

		public override Color RarityColor => PartMoldTierRegistry.registeredIDs[moldTier].color;

		public override string Name => "PartMoldRarity_" + moldTier;

		public static BasePartMoldRarity GetInstance(int tier) {
			if (tier < 0 || tier >= PartMoldTierRegistry.Count)
				throw new ArgumentOutOfRangeException(nameof(tier), "Part Mold Tier ID was invalid");

			return PartMoldTierRegistry.registeredIDs[tier].rarity;
		}

		public BasePartMoldRarity() {
			moldTier = 0;
		}

		public BasePartMoldRarity(int tier) {
			if (tier < 0 || tier >= PartMoldTierRegistry.Count)
				throw new ArgumentOutOfRangeException(nameof(tier), "Part Mold Tier ID was invalid");

			moldTier = tier;
		}
	}
}
