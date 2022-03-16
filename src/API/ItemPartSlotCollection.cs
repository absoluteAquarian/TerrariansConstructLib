using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API {
	public class ItemPartSlotCollection : IEnumerable<ItemPart> {
		private ItemPartSlot[] slots = Array.Empty<ItemPartSlot>();

		public int Length {
			get => slots.Length;
			set {
				if (slots.Length != value) {
					int old = slots.Length;

					Array.Resize(ref slots, value);

					if (old < value) {
						for (int i = old; i < value; i++)
							slots[i] = new(i);
					}
				}
			}
		}

		public ItemPartSlotCollection(params ItemPartSlot[] slots) {
			this.slots = slots;
		}

		public ItemPartSlotCollection(params ItemPart[] parts) {
			slots = parts.Select((p, i) => new ItemPartSlot(i){ part = p }).ToArray();
		}

		public ItemPartSlotCollection(int count) {
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count), "Count must be a positive, non-zero integer");

			slots = Enumerable.Range(0, count).Select(i => new ItemPartSlot(i)).ToArray();
		}

		public ItemPart this[int slot] {
			get => slots[slot].part;
			set => TrySetItemPartSlot(slot, value);
		}

		public bool TrySetItemPartSlot(int slot, ItemPart part) {
			if (slots[slot]?.isPartIDValid?.Invoke(part.partID) ?? true) {
				(slots[slot] ??= new(slot)).part = part;
				return true;
			}

			return false;
		}

		public bool IsPartIDValidForAnySlot(int partID) {
			for (int i = 0; i < slots.Length; i++)
				if (slots[i]?.isPartIDValid?.Invoke(partID) ?? true)
					return true;

			return false;
		}

		public bool IsPartIDValidForSlot(int partID, int slot)
			=> slots[slot]?.isPartIDValid?.Invoke(partID) ?? true;

		public void SetValidFunc(int slot, Func<int, bool> func)
			=> (slots[slot] ??= new(slot)).isPartIDValid = func;

		public IEnumerator<ItemPart> GetEnumerator()
			=> slots.Select(s => s.part).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}
}
