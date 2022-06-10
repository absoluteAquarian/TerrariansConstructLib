using System.IO;
using Terraria.ModLoader;

namespace TerrariansConstructLib {
	internal enum MessageType {
		// TODO
	}
	
	internal static class NetHelper {
		/// <inheritdoc cref="Mod.HandlePacket(BinaryReader, int)"/>
		internal static void HandlePacket(BinaryReader reader, int whoAmI) {
			MessageType msg = (MessageType)reader.ReadByte();

			// TODO
		}

		public static ModPacket GetPacket(MessageType type) {
			ModPacket packet = CoreLibMod.Instance.GetPacket();
			packet.Write((byte)type);
			return packet;
		}
	}
}
