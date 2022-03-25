using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Commands {
	internal class SetDurability : ModCommand {
		public override CommandType Type => CommandType.Chat;

		public override string Usage => "[c/ff6a00:Usage: /setdurability <number>]";

		public override string Command => "setdurability";

		public override string Description => "Sets the durability of the currently held item, if applicable.";

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length != 1) {
				caller.Reply("Expected only one integer argument", Color.Red);
				return;
			}

			if (!int.TryParse(args[0], out int durability) || durability < 0) {
				caller.Reply("Argument must be a non-negative integer", Color.Red);
				return;
			}

			if (caller.Player.HeldItem.ModItem is not BaseTCItem tc) {
				caller.Reply("Player's held item was not a constructed Terrarians' Construct item", Color.Red);
				return;
			}

			tc.CurrentDurability = durability;

			caller.Reply("Successfully set the player's held item's durability to " + durability, Color.Green);
		}
	}
}
