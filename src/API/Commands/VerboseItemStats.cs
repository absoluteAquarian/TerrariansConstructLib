using Microsoft.Xna.Framework;
using System.Linq;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.API.Commands {
	internal class VerboseItemStats : ModCommand {
		public override CommandType Type => CommandType.Chat;

		public override string Usage => "[c/ff6a00:Usage: /vis]";

		public override string Command => "vis";

		public override string Description => "Prints verbose information of the currently held item, if applicable.";

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 0) {
				caller.Reply("Expected no arguments", Color.Red);
				return;
			}

			if (caller.Player.HeldItem.ModItem is not BaseTCItem tc) {
				caller.Reply("Player's held item was not a constructed Terrarians' Construct item", Color.Red);
				return;
			}

			//Print item information
			Color section = new() { PackedValue = 0xff999999 };
			Color text = new() { PackedValue = 0xffcccccc };

			caller.Reply("=== ITEM STATS ===", section);
			var itemData = ItemRegistry.registeredIDs[tc.registeredItemID];

			// caller.Reply($"", text);
			caller.Reply($"Item Name: {itemData.name}", text);
			caller.Reply($"Registered ID: {tc.registeredItemID}", text);
			caller.Reply($"Registered Identifier: \"{itemData.mod.Name}:{itemData.internalName}\"", text);
			caller.Reply($"ModItem Type: {itemData.mod.Find<ModItem>(itemData.itemInternalName).GetType().FullName}", text);
			caller.Reply($"Local Textures Folder: \"{itemData.partVisualsFolder}\"", text);
			caller.Reply("Valid Item Parts:", text);
			foreach(var (id, idString) in itemData.validPartIDs.Select(i => (i, PartRegistry.IDToIdentifier(i))))
				caller.Reply($"  (ID: {id}) | {idString}", text);
			caller.Reply($"Durability: {tc.CurrentDurability} / {tc.GetMaxDurability()}", text);
			caller.Reply($"Base Damage: {tc.GetBaseDamage()}", text);
			caller.Reply($"Base Knockback: {tc.GetBaseKnockback()}", text);
			caller.Reply($"Base Crit: {tc.GetBaseCrit()}%", text);
			caller.Reply($"Base Use Speed: {tc.GetBaseUseSpeed()} ticks", text);
			caller.Reply($"Pickaxe Power: {tc.GetPickaxePower()}%", text);
			caller.Reply($"Axe Power: {tc.GetAxePower() * 5}%", text);
			caller.Reply($"Hammer Power: {tc.GetHammerPower()}%", text);
			caller.Reply($"Ammo Reserves: {tc.ammoReserve} / {tc.ammoReserveMax}", text);

			caller.Reply($"=== PART STATS ===", section);
			caller.Reply("Parts:", text);
			foreach (var name in tc.GetPartNamesForTooltip())
				caller.Reply($"  {name}", text);
			caller.Reply("Modifiers:");
			// TODO: modifier description line?
			foreach (var modifierName in tc.GetModifierTooltipLines())
				caller.Reply($"  {modifierName}", text);
		}
	}
}
