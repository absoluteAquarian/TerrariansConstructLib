namespace TerrariansConstructLib.Items {
	internal class UnloadedItemPart : ItemPart {
		public string mod, internalName;

		public override ItemPart Clone() => new UnloadedItemPart(){
			mod = mod,
			internalName = internalName,
			material = material.Clone(),
			partID = partID,
			tooltip = tooltip,
			modifierText = modifierText
		};
	}
}
