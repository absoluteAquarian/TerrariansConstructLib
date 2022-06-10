using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Reflection;

namespace TerrariansConstructLib.API.Edits.MSIL {
	partial class TML {
		internal static void Patch_ModContent_Load(ILContext il) {
			FieldInfo UILoadMods_loadMods = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.Interface")!.GetCachedField("loadMods")!;
			
			ILCursor c = new(il);

			int patchNum = 1;

			ILHelper.CompleteLog(CoreLibMod.directDetourInstance, c, beforeEdit: true);

			if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdsfld(UILoadMods_loadMods),
				i => i.MatchLdstr("tModLoader.MSSettingUp")))
				goto bad_il;

			patchNum++;

			c.EmitDelegate(CoreLibMod.CheckItemDefinitions);
			c.EmitDelegate(CoreLibMod.AddMoldItems);
			c.EmitDelegate(CoreLibMod.AddPartItems);

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(CoreLibMod.directDetourInstance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}
	}
}
