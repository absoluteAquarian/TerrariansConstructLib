using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using TerrariansConstructLib.API.UI;
using TerrariansConstructLib.Items;

namespace TerrariansConstructLib.API.Definitions {
	public abstract class TCItemDefinition : ModType {
		public int Type { get; private set; }

		/// <summary>
		/// The item ID of the <see cref="BaseTCItem"/> item that this item definition is tied to
		/// </summary>
		public abstract int ItemType { get; }

		/// <summary>
		/// The relative path in any mod for this item definition's part and modifier sprites.
		/// Defaults to <c>"Assets/Visuals/"+Name</c>
		/// </summary>
		public virtual string RelativeVisualsFolder => "Assets/Visuals/" + Name;

		/// <summary>
		/// The use speed multiplier applied to this item definition's item after calculating the stats from its parts' materials.
		/// Defaults to 1f
		/// </summary>
		public virtual float UseSpeedMultiplier => 1f;

		/// <summary>
		/// If this item definition's item uses ammo, this property indicates how much ammo it consumes per shot.
		/// Defaults to 1
		/// </summary>
		public virtual int AmmoConsumedPerShot => 1;

		/// <summary>
		/// If this item definition's item is a tool, this property indicates how many tile "hits" it performs per swing.
		/// Defaults to 2
		/// </summary>
		public virtual int HitsPerToolSwing => 2;

		protected sealed override void Register() {
			ModTypeLookup<TCItemDefinition>.Register(this);
			Type = ItemDefinitionLoader.Add(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();

		/// <summary>
		/// Return a collection of <see cref="ForgeUISlotConfiguration"/> values for this item definition here.<br/>
		/// A <see cref="ForgeUISlotConfiguration"/> value contains context for its item slot in the Forge UI
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<ForgeUISlotConfiguration> GetForgeSlotConfiguration();

		public IEnumerable<int> GetValidPartIDs() => GetForgeSlotConfiguration().Select(f => f.partID);
	}
}
