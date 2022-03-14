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
	}
}
