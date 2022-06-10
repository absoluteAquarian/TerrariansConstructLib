using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace TerrariansConstructLib.Global {
	internal class NPCProjectileTracking : GlobalProjectile {
		public override bool InstancePerEntity => true;

		public int SourceWhoAmI { get; private set; }
		public int SourceUUID { get; private set; }
		public bool HasValidSource { get; private set; }

		public override void OnSpawn(Projectile projectile, IEntitySource source) {
			NPC? npcSource = null;

			if (source is EntitySource_Parent parent && parent.Entity is NPC parentNPC)
				npcSource = parentNPC;
			else if (source is EntitySource_BossSpawn bossSpawn && bossSpawn.Entity is NPC spawnNPC)
				npcSource = spawnNPC;
			else if (source is EntitySource_Death death && death.Entity is NPC deathNPC)
				npcSource = deathNPC;
			else if (source is EntitySource_HitEffect hit && hit.Entity is NPC hitNPC)
				npcSource = hitNPC;

			if (npcSource is not null && npcSource.TryGetGlobalNPC(out NPCGlobalUUID uuid)) {
				HasValidSource = true;
				SourceWhoAmI = npcSource.whoAmI;
				SourceUUID = uuid.UUID;
			}
		}

		public bool CheckSourceValidity() {
			if (!HasValidSource)
				return false;

			NPC source = Main.npc[SourceWhoAmI];

			if (!source.TryGetGlobalNPC(out NPCGlobalUUID uuid) || SourceUUID != uuid.UUID) {
				//NPC has changed; source is no longer valid
				return HasValidSource = false;
			}

			return true;
		}
	}
}
