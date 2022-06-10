using System;

namespace TerrariansConstructLib.API.Stats {
	/// <summary>
	/// A classification for <see cref="StatType.Head"/> parts
	/// </summary>
	[Flags]
	public enum ToolType {
		None = 0x0,
		Pickaxe = 0x1,
		Axe = 0x2,
		Hammer = 0x4
	}
}
