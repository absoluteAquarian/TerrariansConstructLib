using Terraria.UI;
using TerrariansConstructLib.API.UI;

namespace TerrariansConstructLib.API.Edits.Detours {
	partial class Vanilla {
		internal static void Hook_ItemSlot_LeftClick_ItemArray_int_int(On.Terraria.UI.ItemSlot.orig_LeftClick_ItemArray_int_int orig, Terraria.Item[] inv, int context, int slot) {
			if (TCUIItemSlot.SlotContexts.IsValidContext(context))
				context = ItemSlot.Context.BankItem;

			orig(inv, context, slot);
		}

		internal static void Hook_ItemSlot_RightClick_ItemArray_int_int(On.Terraria.UI.ItemSlot.orig_RightClick_ItemArray_int_int orig, Terraria.Item[] inv, int context, int slot) {
			if (TCUIItemSlot.SlotContexts.IsValidContext(context))
				context = ItemSlot.Context.BankItem;

			orig(inv, context, slot);
		}
	}
}
