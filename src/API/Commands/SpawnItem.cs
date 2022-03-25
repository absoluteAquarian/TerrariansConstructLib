using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Materials;
using TerrariansConstructLib.Registry;

namespace TerrariansConstructLib.API.Commands {
	internal class SpawnItem : ModCommand {
		public override CommandType Type => CommandType.Chat;

		public override string Usage => "[c/ff6a00:Usage: /si <registered item #> M<material 0 #> M<material 1 #> ...]";

		public override string Command => "si";

		public override string Description => "Spawns a constructed item with the specified item parts.  For \"Unloaded\" parts, you can use \"U\" for the #.  For \"Unknown\" parts, you can use \"K\" for the #.";

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length < 5) {
				caller.Reply("Expected at least 5 parameters.", Color.Red);
				return;
			}

			if ((args.Length - 1) % 2 != 0) {
				caller.Reply("Final material argument did not have a matching part argument.", Color.Red);
				return;
			}

			if (!GetItemNum(caller, args, 0, out int registeredItemID))
				return;

			int numParts = args.Length - 1;

			ItemPart[] parts = new ItemPart[numParts];

			int[] partIDs = ItemRegistry.registeredIDs[registeredItemID].validPartIDs;

			if (partIDs.Length != numParts) {
				caller.Reply($"Registered Item {registeredItemID} expects {partIDs.Length} parts", Color.Red);
				return;
			}

			for (int i = 0; i < numParts; i++) {
				if (!GetMaterialNum(caller, args, 1 + i * 2, out int materialType))
					return;

				parts[i] = new(){
					material = Material.FromItem(materialType),
					partID = partIDs[i]
				};
			}

			var data = ItemRegistry.registeredIDs[registeredItemID];
			
			if (!data.mod.TryFind<ModItem>(data.itemInternalName, out var mItem)) {
				caller.Reply($"Registered item ID #{registeredItemID} ({data.mod.Name}:{data.internalName}) had an invalid item internal name: \"{data.itemInternalName}\".", Color.Red);
				return;
			}

			Item item = new(mItem.Type);
			BaseTCItem tc = (item.ModItem as BaseTCItem)!;

			//Assign the parts
			tc.InitializeWithParts(parts);

			//Drop the item in the world
			caller.Player.QuickSpawnClonedItem(new EntitySource_DebugCommand(), tc.Item, tc.Item.stack);

			caller.Reply("Successfully spawned the item.", Color.Green);
		}

		private static bool GetItemNum(CommandCaller caller, string[] args, int argNum, out int num) {
			if (!int.TryParse(args[argNum], out num)) {
				caller.Reply($"Argument #{argNum + 1} was not an integer", Color.Red);
				return false;
			}

			if (num < 0 || num >= ItemRegistry.Count) {
				caller.Reply("Registered item # exceeded the bounds of valid IDs.  Use \"/li\" to list the registered item IDs.", Color.Red);
				return false;
			}

			return true;
		}

		private static bool GetMaterialNum(CommandCaller caller, string[] args, int argNum, out int num) {
			num = -1;
			if (args[argNum][0] != 'M') {
				caller.Reply($"Argument #{argNum + 1} was an invalid material argument", Color.Red);
				return false;
			}

			args[argNum] = args[argNum][1..];

			if (args[argNum] == "K") {
				num = UnknownMaterial.StaticType;
				return true;
			}

			if (args[argNum] == "U") {
				num = UnloadedMaterial.StaticType;
				return true;
			}

			if (!int.TryParse(args[argNum], out num)) {
				caller.Reply($"Argument #{argNum + 1} was not an integer.", Color.Red);
				return false;
			}

			return true;
		}
	}
}
