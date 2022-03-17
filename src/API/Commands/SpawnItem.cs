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

		public override string Usage => "[c/ff6a00:Usage: /si <registered item #> M<material 0> P<part #> M<material 1> P<part #> ...]";

		public override string Command => "si";

		public override string Description => "Spawns a constructed item with the specified item parts.  For \"Unloaded\" parts, you can use \"U\" for the #.  For \"Unknown\" parts, you can use \"K\" for the #.";

		public override void Action(CommandCaller caller, string input, string[] args) {
			if (args.Length < 5) {
				caller.Reply("Expected at least 5 parameters.", Color.Red);
				return;
			}

			if ((args.Length - 1) % 2 != 0) {
				caller.Reply("Final material argument did not have a matching part argument.");
				return;
			}

			if (!GetItemNum(caller, args, 0, out int registeredItemID))
				return;

			int numParts = (args.Length - 1) / 2;

			ItemPart[] parts = new ItemPart[numParts];

			for (int i = 0; i < numParts; i++) {
				if (!GetMaterialNum(caller, args, 1 + i * 2, out int materialType))
					return;

				if (!GetPartNum(caller, args, 1 + i * 2 + 1, out int partID))
					return;

				// TODO: material registry?
				parts[i] = new(){
					material = materialType == UnknownMaterial.StaticType ? new UnknownMaterial() : materialType == UnloadedMaterial.StaticType ? new UnloadedMaterial() : new Material(){ type = materialType },
					partID = partID
				};
			}

			var data = ItemRegistry.registeredIDs[registeredItemID];
			
			if (!data.mod.TryFind<ModItem>(data.itemInternalName, out var mItem)) {
				caller.Reply($"Registered item ID #{registeredItemID} ({data.mod.Name}:{data.internalName}) had an invalid item internal name: \"{data.itemInternalName}\".", Color.Red);
				return;
			}

			Item item = new(mItem.Type);
			BaseTCItem tc = item.ModItem as BaseTCItem;

			if (data.validPartIDs.Length != tc.PartsCount || !Array.TrueForAll(parts, part => tc.parts.IsPartIDValidForAnySlot(part.partID))) {
				caller.Reply($"Requested part IDs did not match the expected sequence of valid part IDs for the registered item ID #{registeredItemID} ({data.mod.Name}:{data.internalName}).", Color.Red);
				return;
			}

			//Assign the parts
			for (int i = 0; i < parts.Length; i++)
				tc.parts[i] = parts[i];

			//Drop the item in the world
			const int size = 2;
			Point tl = caller.Player.Center.ToPoint() - new Point(size / 2, size / 2);
			Rectangle area = new(tl.X, tl.Y, size, size);
			Utility.DropItem(new EntitySource_DebugCommand(), item, area);

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

		private static bool GetPartNum(CommandCaller caller, string[] args, int argNum, out int num) {
			num = -1;
			if (args[argNum][0] != 'P') {
				caller.Reply($"Argument #{argNum + 1} was an invalid part argument", Color.Red);
				return false;
			}

			args[argNum] = args[argNum][1..];

			if (!int.TryParse(args[argNum], out num)) {
				caller.Reply($"Argument #{argNum + 1} was not an integer.", Color.Red);
				return false;
			}

			if (num < 0 || num >= PartRegistry.Count) {
				caller.Reply("Part # exceeded the bounds of valid IDs.  Use \"/lp\" to list the registered part IDs.", Color.Red);
				return false;
			}

			return true;
		}
	}
}
