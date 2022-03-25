using System.Threading;

namespace TerrariansConstructLib.API.Edits.Detours {
	partial class TML {
		public delegate void orig_ModContent_Load(CancellationToken token);

		internal static void Hook_ModContent_Load(orig_ModContent_Load orig, CancellationToken token) {
			orig(token);

			CoreLibMod.writer.Dispose();
			CoreLibMod.writer = null!;
		}
	}
}
