﻿using Terraria.GameInput;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.Players {
	internal class AbilityPlayer : ModPlayer {
		public override void ProcessTriggers(TriggersSet triggersSet) {
			if (CoreLibMod.ActivateAbility.JustPressed && Player.HeldItem.ModItem is BaseTCItem tc) {
				for (int i = 0; i < tc.parts.Length; i++)
					tc.parts[i].OnGenericHotkeyUsage?.Invoke(tc.parts[i].partID, Player, Player.HeldItem);

				tc.abilities.OnHotkeyPressed(Player);
			}
		}

		public override void PostUpdateMiscEffects() {
			if (Player.HeldItem.ModItem is BaseTCItem tc)
				tc.abilities.Update(Player);
		}
	}
}