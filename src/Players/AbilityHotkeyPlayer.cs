using Terraria.GameInput;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.Players {
	internal class AbilityHotkeyPlayer : ModPlayer {
		public override void ProcessTriggers(TriggersSet triggersSet) {
			if (CoreLibMod.ActivateAbility.JustPressed && Player.HeldItem.ModItem is BaseTCItem tc) {
				for (int i = 0; i < tc.parts.Length; i++)
					tc.parts[i].OnGenericHotkeyUsage(tc.parts[i].partID, Player, Player.HeldItem);
			}
		}
	}
}
