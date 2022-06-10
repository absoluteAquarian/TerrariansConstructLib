using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using TerrariansConstructLib.Global;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.Players {
	internal class ItemModifierPlayer : ModPlayer {
		public override void ProcessTriggers(TriggersSet triggersSet) {
			if (CoreLibMod.ActivateAbility.JustPressed && Player.HeldItem.ModItem is BaseTCItem tc)
				tc.modifiers.OnHotkeyPressed(Player);
		}

		public override void PostUpdateMiscEffects() {
			if (Player.HeldItem.ModItem is BaseTCItem tc)
				tc.modifiers.Update(Player);
		}

		public override void OnHitByNPC(NPC npc, int damage, bool crit) {
			if (Player.HeldItem.ModItem is BaseTCItem tc)
				tc.modifiers.OnHitByNPC(npc, Player, damage, crit);
		}

		public override void OnHitByProjectile(Projectile proj, int damage, bool crit) {
			if (!proj.TryGetGlobalProjectile(out NPCProjectileTracking tracking) || !tracking.CheckSourceValidity())
				return;

			if (Player.HeldItem.ModItem is BaseTCItem tc)
				tc.modifiers.OnHitByNPCProjectile(proj, Main.npc[tracking.SourceWhoAmI], Player, damage, crit);
		}
	}
}
