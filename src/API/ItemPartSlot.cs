using System;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API {
	public class ItemPartSlot {
		internal ItemPart part;

		public readonly int slot;

		internal ItemPartSlot(int slot) {
			this.slot = slot;
		}
	}
}
