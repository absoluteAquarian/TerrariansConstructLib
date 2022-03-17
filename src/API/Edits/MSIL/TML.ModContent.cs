using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Reflection;

namespace TerrariansConstructLib.API.Edits.MSIL {
	internal static partial class TML {
		internal static void Path_ModContent_Load(ILContext il) {
			FieldInfo Interface_loadMods = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.Interface").GetField("loadMods", BindingFlags.NonPublic | BindingFlags.Static);

			ILHelper.EnsureAreNotNull((Interface_loadMods, "Terraria.ModLoader.UI.Interface::loadMods"));

			ILCursor c = new(il);

			ILHelper.CompleteLog(CoreLibMod.directDetourInstance, c, beforeEdit: true);

			int patchNum = 1;

			if (!c.TryGotoNext(MoveType.Before, i => i.MatchLdsfld(Interface_loadMods),
				i => i.MatchLdstr("tModLoader.MSSettingUp"),
				i => i.MatchLdcI4(-1)))
				goto bad_il;

			patchNum++;

			c.EmitDelegate(() => {
				CoreLibMod instance = ModContent.GetInstance<CoreLibMod>();
				try {
					instance.LoadMolds();
				} catch (Exception ex) {
					string responsible = ex.Data.Contains("mod") ? ex.Data["mod"] as string : null;

					ex.Data["mod"] = responsible ?? instance.Name;
					throw;
				}
			});

			ILHelper.UpdateInstructionOffsets(c);

			ILHelper.CompleteLog(CoreLibMod.directDetourInstance, c, beforeEdit: false);

			return;
			bad_il:
			throw new Exception("Unable to fully patch " + il.Method.Name + "()\n" +
				"Reason: Could not find instruction sequence for patch #" + patchNum);
		}
	}
}
