namespace TerrariansConstructLib.DataStructures {
	public struct TileDestructionContext {
		/// <summary>
		/// <see langword="true"/> if a tile was destroyed by a pickaxe
		/// </summary>
		public readonly bool pickaxe;
		/// <summary>
		/// <see langword="true"/> if a tile was destroyed by an axe
		/// </summary>
		public readonly bool axe;
		/// <summary>
		/// <see langword="true"/> if a tile was destroyed by a hammer
		/// </summary>
		public readonly bool hammer;
		/// <summary>
		/// <see langword="true"/> if a wall was destroyed by a hammer
		/// </summary>
		public readonly bool hammerWall;

		/// <summary>
		/// The calculated damage used to destroy the tile
		/// </summary>
		public readonly int damage;

		internal TileDestructionContext(int damage, bool pickaxe = false, bool axe = false, bool hammer = false, bool hammerWall = false) {
			this.pickaxe = pickaxe;
			this.axe = axe;
			this.hammer = hammer;
			this.hammerWall = hammerWall;
			this.damage = damage;
		}
	}
}
