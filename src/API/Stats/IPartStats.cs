namespace TerrariansConstructLib.API.Stats {
	public interface IPartStats {
		StatType Type { get; }

		string GetTooltipLines(bool isAxeHeadPart);
	}
}
