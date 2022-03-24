using Terraria.Localization;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Reflection;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Edits.Detours {
	internal static partial class TML {
		public delegate void orig_ModItem_SetupContent(ModItem self);

		internal static void Hook_ModItem_SetupContent(orig_ModItem_SetupContent orig, ModItem self) {
			if (self is BaseTCItem && (bool)typeof(ModLoader).GetCachedField("isLoading")!.GetValue(null)!)
				CoreLibMod.SetLoadingSubProgressText(Language.GetTextValue("Mods.TerrariansConstructLib.Loading.TCItemDefaults", self.GetType().FullName));

			orig(self);
		}
	}
}
