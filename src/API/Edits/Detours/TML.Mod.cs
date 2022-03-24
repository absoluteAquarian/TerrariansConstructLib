using System.Linq;
using Terraria.ModLoader;

namespace TerrariansConstructLib.API.Edits.Detours {
	partial class TML {
		public delegate void orig_Mod_SetupContent(Mod self);

		internal static void Hook_Mod_SetupContent(orig_Mod_SetupContent orig, Mod self) {
			orig(self);

			if (CoreLibMod.Dependents.Contains(self))
				CoreLibMod.SetLoadingSubProgressText("");
		}
	}
}
