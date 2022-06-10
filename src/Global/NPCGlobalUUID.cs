using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace TerrariansConstructLib.Global {
	public sealed class NPCGlobalUUID : GlobalNPC {
		public override bool InstancePerEntity => true;

		public int UUID { get; internal set; }
		internal static int nextUUID;

		public override void OnSpawn(NPC npc, IEntitySource source) {
			UUID = nextUUID;
			nextUUID++;
		}
	}
}
