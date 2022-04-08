using Microsoft.Xna.Framework;
using System.IO;
using TerrariansConstructLib.API;

namespace TerrariansConstructLib.Modifiers {
	internal sealed class UnloadedTrait : BaseModifier {
		internal string mod;
		internal string name;
		
		public override int MaxTier => 1;

		public override string? VisualTexture => null;

		public override string LangKey => "Mods.TerrariansConstructLib.Traits.Unloaded";

		public override Color TooltipColor => Color.Pink;

		public override int GetUpgradeTarget() => -1;

		public override bool CanAcceptItemsForUpgrade(ItemData[] items, ref int upgradeCurrent, in int upgradeTarget) => false;

		public override void NetSend(BinaryWriter writer) {
			base.NetSend(writer);
			writer.Write(mod);
			writer.Write(name);
		}

		public override void NetReceive(BinaryReader reader) {
			base.NetReceive(reader);
			mod = reader.ReadString();
			name = reader.ReadString();
		}
	}
}
