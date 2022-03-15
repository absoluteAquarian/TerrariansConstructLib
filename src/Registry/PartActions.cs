using TerrariansConstructLib.API;
using TerrariansConstructLib.Materials;

namespace TerrariansConstructLib.Registry {
	/// <summary>
	/// A collection of action builders
	/// </summary>
	public static class PartActions {
		internal static PartsDictionary<ItemPartActionsBuilder> builders;

		public static ItemPartActionsBuilder GetPartActions(Material material, int partID)
			=> builders.Get(material, partID);

		public static readonly ItemPartActionsBuilder NoActions = new(isReadonly: true);
	}
}
