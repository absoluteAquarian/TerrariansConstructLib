using System.Text.RegularExpressions;
using Terraria.ModLoader;
using TerrariansConstructLib.API.Stats;

namespace TerrariansConstructLib.API.Definitions {
	public abstract class PartDefinition : ModType {
		public int Type { get; private set; }

		/// <summary>
		/// What stat classification this part uses
		/// </summary>
		public abstract StatType StatType { get; }

		/// <summary>
		/// What tool classification this part falls under
		/// </summary>
		public virtual ToolType ToolType => ToolType.None;

		/// <summary>
		/// The required number of material items needed to make this part, times two.
		/// </summary>
		public abstract int MaterialCost { get; }

		/// <summary>
		/// Whether this part can be created via a wooden part mold.
		/// Defaults to true.
		/// </summary>
		public virtual bool HasWoodMold => true;

		/// <summary>
		/// The relative path in any mod for this part definition's sprites.
		/// Defaults to <c>"Assets/Parts/"+Name</c>
		/// </summary>
		public virtual string RelativeAssetFolder => "Assets/Parts/" + Name;

		/// <summary>
		/// The publicly-visible name for this part definition.
		/// Defaults to <c>Name</c>, but with the Proper Words separated by spaces (e.g. "ToolRod" becomes "Tool Rod")
		/// </summary>
		public virtual string DisplayName => Regex.Replace(Name, "([A-Z])", " $1").Trim();

		internal string GetIdentifier() => Mod.Name + ":" + Name;

		protected sealed override void Register() {
			ModTypeLookup<PartDefinition>.Register(this);
			Type = PartDefinitionLoader.Add(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();
	}
}
