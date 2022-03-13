using System;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API {
	public class ItemPartSlot {
		internal ItemPart part;

		public readonly int slot;

		public Func<int, bool> isPartIDValid;

		internal ItemPartSlot(int slot) {
			this.slot = slot;
		}

		internal ItemPartSlot(ItemPart part, int slot) {
			this.part = part;
			this.slot = slot;
		}
	}
}
