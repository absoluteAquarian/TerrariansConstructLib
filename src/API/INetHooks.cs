using System.IO;

namespace TerrariansConstructLib.API {
	public interface INetHooks {
		void NetSend(BinaryWriter writer);

		void NetReceive(BinaryReader reader);
	}
}
