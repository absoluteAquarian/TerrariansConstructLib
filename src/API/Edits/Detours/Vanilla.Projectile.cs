using Terraria;
using Terraria.DataStructures;
using TerrariansConstructLib.Items;
using TerrariansConstructLib.Projectiles;

namespace TerrariansConstructLib.API.Edits.Detours {
	internal static partial class Vanilla {
		internal static int Hook_Projectile_NewProjectile(On.Terraria.Projectile.orig_NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float orig, IEntitySource spawnSource, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1) {
			int spawned = orig(spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1);

			Projectile projectile = Main.projectile[spawned];

			if (projectile.ModProjectile is BaseTCProjectile tcProj) {
				//If the projectile wasn't spawned from an item, kill it immediately
				BaseTCItem? tcItem;

				if ((spawnSource is EntitySource_ItemUse sourceItem && (tcItem = sourceItem.Item.ModItem as BaseTCItem) is not null)
					|| (spawnSource is EntitySource_ItemUse_WithAmmo sourceItem_WithAmmo && (tcItem = sourceItem_WithAmmo.Item.ModItem as BaseTCItem) is not null)){
					tcProj.parts = new ItemPart[tcItem.parts.Length];
					tcProj.itemSource_registeredItemID = tcItem.registeredItemID;

					//Perform a deep copy of each part
					for (int i = 0; i < tcItem.parts.Length; i++) {
						ItemPart part = tcItem.parts[i];

						tcProj.parts[i] = new(){
							material = part.material.Clone(),
							partID = part.partID
						};

						tcProj.parts[i].OnProjectileSpawn?.Invoke(tcProj.parts[i].partID, projectile, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1);
					}

					tcProj.abilities = tcItem.abilities.Clone();
					
					tcProj.abilities.OnProjectileSpawn(tcProj, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1);
				} else {
					projectile.Kill();
					return Main.maxProjectiles;
				}
			}

			return spawned;
		}
	}
}
